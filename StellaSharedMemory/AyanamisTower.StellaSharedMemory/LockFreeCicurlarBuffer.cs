// LockFreeCircularBuffer.cs
// A lock-free, single-writer, multiple-reader circular buffer for high-performance IPC.
// This class handles the writer-side logic.
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using AyanamisTower.StellaSharedMemory;

namespace AyanamisTower.StellaSharedMemory;

/// <summary>
/// Defines the behavior of the Write method when the buffer is full.
/// </summary>
public enum WriteBehavior
{
    /// <summary>
    /// The writer will overwrite the oldest messages if the buffer is full. (Default)
    /// </summary>
    Overwrite,

    /// <summary>
    /// The writer will block and wait for a reader to consume messages and free up space.
    /// </summary>
    Block,
}

/// <summary>
/// A lightweight static helper to manage atomic operations on the shared buffer header.
/// The header contains the write position and ticket lock values for multi-writer synchronization.
/// </summary>
internal static class SharedHeader
{
    // Header Layout (all values are 64-bit longs):
    // 0-7:   WritePosition - The position where the next message will be written.
    // 8-15:  Capacity - The total capacity of the data buffer (excluding the header).
    // 16-23: NextTicket - The next available ticket number for a writer.
    // 24-31: CurrentTicket - The ticket number of the writer currently holding the lock.
    // 32-..: ReaderPositions - A registry for active readers and their current positions.
    public const int WritePosOffset = 0;
    public const int CapacityOffset = 8;
    public const int NextTicketOffset = 16;
    public const int CurrentTicketOffset = 24;
    public const int ReaderRegistryOffset = 32;

    // We'll reserve space for up to 64 readers. Each reader entry is a long (8 bytes).
    public const int MaxReaders = 64;
    public const int ReaderRegistrySize = MaxReaders * sizeof(long);
    public const int HeaderSize = ReaderRegistryOffset + ReaderRegistrySize;

    public static unsafe long GetWritePositionVolatile(MemoryMappedViewAccessor accessor)
    {
        byte* ptr = (byte*)0;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try
        {
            return Volatile.Read(ref ((long*)(ptr + WritePosOffset))[0]);
        }
        finally
        {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    public static unsafe void SetWritePositionVolatile(
        MemoryMappedViewAccessor accessor,
        long value
    )
    {
        byte* ptr = (byte*)0;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try
        {
            Volatile.Write(ref ((long*)(ptr + WritePosOffset))[0], value);
        }
        finally
        {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    // --- Reader Registry Helpers ---

    /// <summary>
    /// Gets the slowest read position from all registered readers.
    /// </summary>
    public static unsafe long GetMinimumReaderPosition(MemoryMappedViewAccessor accessor)
    {
        long minPos = long.MaxValue;
        bool anyActive = false;
        byte* ptr = (byte*)0;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try
        {
            long* readerPtr = (long*)(ptr + ReaderRegistryOffset);
            for (int i = 0; i < MaxReaders; i++)
            {
                long pos = Volatile.Read(ref readerPtr[i]);
                // A position of -1 indicates an inactive/unregistered reader slot.
                if (pos != -1)
                {
                    anyActive = true;
                    if (pos < minPos)
                    {
                        minPos = pos;
                    }
                }
            }
        }
        finally
        {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }

        // If no readers are active, return the current write position to prevent blocking.
        return anyActive ? minPos : GetWritePositionVolatile(accessor);
    }

    // --- Ticket Lock Helpers ---
    public static unsafe long GetNextTicket(MemoryMappedViewAccessor accessor)
    {
        byte* ptr = (byte*)0;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try
        {
            return Interlocked.Increment(ref ((long*)(ptr + NextTicketOffset))[0]);
        }
        finally
        {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    public static unsafe long GetCurrentTicket(MemoryMappedViewAccessor accessor)
    {
        byte* ptr = (byte*)0;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try
        {
            return Volatile.Read(ref ((long*)(ptr + CurrentTicketOffset))[0]);
        }
        finally
        {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    public static unsafe void AdvanceCurrentTicket(MemoryMappedViewAccessor accessor)
    {
        byte* ptr = (byte*)0;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try
        {
            Interlocked.Increment(ref ((long*)(ptr + CurrentTicketOffset))[0]);
        }
        finally
        {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }
}

/// <summary>
/// A circular buffer optimized for multiple producers (writers) and multiple consumers (readers).
/// Writers use a fair ticket lock to ensure FIFO access.
/// </summary>
public class MultiWriterCircularBuffer : IDisposable
{
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly long _dataBufferCapacity;
    private readonly WriteBehavior _writeBehavior;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiWriterCircularBuffer"/> class.
    /// </summary>
    /// <param name="name">The name of the shared memory segment.</param>
    /// <param name="mode">The mode to open the shared memory (Create or Open).</param>
    /// <param name="capacity">The capacity of the buffer in bytes. Required when mode is Create.</param>
    /// <param name="behavior">The behavior to use when the buffer is full. Defaults to Overwrite.</param>
    public MultiWriterCircularBuffer(
        string name,
        SharedMemoryMode mode,
        long capacity = 0,
        WriteBehavior behavior = WriteBehavior.Overwrite
    )
    {
        if (mode == SharedMemoryMode.Create && capacity <= SharedHeader.HeaderSize)
        {
            throw new ArgumentException(
                "Capacity must be larger than the header size.",
                nameof(capacity)
            );
        }

        _writeBehavior = behavior;
        string mmfPath = Path.Combine(GetSharedDirectory(), name);
        FileMode fileMode = mode == SharedMemoryMode.Create ? FileMode.Create : FileMode.Open;

        using (
            var fs = new FileStream(mmfPath, fileMode, FileAccess.ReadWrite, FileShare.ReadWrite)
        )
        {
            long mmfCapacity = mode == SharedMemoryMode.Create ? capacity : fs.Length;
            _mmf = MemoryMappedFile.CreateFromFile(
                fs,
                null,
                mmfCapacity,
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                false
            );
        }

        _accessor = _mmf.CreateViewAccessor();

        if (mode == SharedMemoryMode.Create)
        {
            _dataBufferCapacity = capacity - SharedHeader.HeaderSize;
            _accessor.Write(SharedHeader.CapacityOffset, _dataBufferCapacity);
            SharedHeader.SetWritePositionVolatile(_accessor, 0);
            _accessor.Write(SharedHeader.NextTicketOffset, 0L);
            _accessor.Write(SharedHeader.CurrentTicketOffset, 0L);

            // Initialize reader registry with -1 (inactive)
            for (int i = 0; i < SharedHeader.MaxReaders; i++)
            {
                _accessor.Write(SharedHeader.ReaderRegistryOffset + (i * sizeof(long)), -1L);
            }
        }
        else
        {
            _dataBufferCapacity = _accessor.ReadInt64(SharedHeader.CapacityOffset);
        }
    }

    /// <summary>
    /// Writes a message to the circular buffer using a fair ticket-based locking system.
    /// </summary>
    /// <param name="message">The byte array containing the message to write to the buffer.</param>
    /// <exception cref="ArgumentException">Thrown when the message is larger than the buffer capacity.</exception>
    public void Write(byte[] message)
    {
        long requiredSpace = message.Length + 4;
        if (requiredSpace > _dataBufferCapacity)
        {
            throw new ArgumentException(
                "Message is larger than the buffer capacity.",
                nameof(message)
            );
        }

        AcquireLock();
        try
        {
            if (_writeBehavior == WriteBehavior.Block)
            {
                WaitForSpace(requiredSpace);
            }

            long currentWritePos = _accessor.ReadInt64(SharedHeader.WritePosOffset);

            // Check for wrap-around
            if (currentWritePos + requiredSpace > _dataBufferCapacity)
            {
                // Before wrapping, check if the wrapped write would overwrite the slowest reader.
                if (_writeBehavior == WriteBehavior.Block)
                {
                    WaitForWrapSpace(requiredSpace);
                }

                // Write a zero-length message as a wrap-around marker
                if (currentWritePos + 4 <= _dataBufferCapacity)
                {
                    _accessor.Write(SharedHeader.HeaderSize + (int)currentWritePos, 0);
                }
                currentWritePos = 0; // Wrap to the beginning
            }

            _accessor.Write(SharedHeader.HeaderSize + (int)currentWritePos, message.Length);
            _accessor.WriteArray(
                SharedHeader.HeaderSize + (int)currentWritePos + 4,
                message,
                0,
                message.Length
            );

            long nextWritePos = currentWritePos + requiredSpace;

            SharedHeader.SetWritePositionVolatile(_accessor, nextWritePos);
        }
        finally
        {
            ReleaseLock();
        }
    }

    private void WaitForSpace(long requiredSpace)
    {
        var spinner = new SpinWait();
        while (true)
        {
            long writePos = SharedHeader.GetWritePositionVolatile(_accessor);
            long minReadPos = SharedHeader.GetMinimumReaderPosition(_accessor);

            // If minReadPos is "behind" writePos, it's a simple calculation.
            // If minReadPos is "ahead" of writePos, it means the readers have wrapped around while the writer has not.
            bool hasWrapped = writePos < minReadPos;
            long freeSpace = hasWrapped
                ? minReadPos - writePos
                : (_dataBufferCapacity - writePos) + minReadPos;

            if (freeSpace > requiredSpace)
            {
                return; // Enough space
            }
            spinner.SpinOnce(-1);
        }
    }

    private void WaitForWrapSpace(long requiredSpace)
    {
        var spinner = new SpinWait();
        while (true)
        {
            long minReadPos = SharedHeader.GetMinimumReaderPosition(_accessor);
            // If the slowest reader is at position 0, we can't wrap.
            // If the required space is larger than the slowest reader's position, we can't wrap.
            if (minReadPos > 0 && requiredSpace < minReadPos)
            {
                return; // Enough space at the start of the buffer
            }
            spinner.SpinOnce(-1);
        }
    }

    private void AcquireLock()
    {
        long myTicket = SharedHeader.GetNextTicket(_accessor) - 1;
        var spinner = new SpinWait();
        while (SharedHeader.GetCurrentTicket(_accessor) != myTicket)
        {
            spinner.SpinOnce(-1);
        }
    }

    private void ReleaseLock()
    {
        SharedHeader.AdvanceCurrentTicket(_accessor);
    }

    private static string GetSharedDirectory() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/dev/shm" : Path.GetTempPath();

    /// <inheritdoc/>
    public void Dispose()
    {
        _accessor?.Dispose();
        _mmf?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// An independent, lock-free reader for a circular buffer.
/// </summary>
public class LockFreeBufferReader : IDisposable
{
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private long _currentReadPos;
    private readonly int _readerSlot;

    /// <summary>
    /// Initializes a new instance of the <see cref="LockFreeBufferReader"/> class.
    /// </summary>
    /// <param name="logName">The name of the shared memory segment to read from.</param>
    public LockFreeBufferReader(string logName)
    {
        string mmfPath = Path.Combine(GetSharedDirectory(), logName);
        var fs = new FileStream(mmfPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

        _mmf = MemoryMappedFile.CreateFromFile(
            fs,
            null,
            0,
            MemoryMappedFileAccess.ReadWrite,
            HandleInheritability.None,
            false
        );
        // Open with ReadWrite access to update our position in the header
        _accessor = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);

        _readerSlot = RegisterReader();
        _currentReadPos = SharedHeader.GetWritePositionVolatile(_accessor);
        UpdateReaderPosition(_currentReadPos);
    }

    /// <summary>
    /// Reads the next message from the circular buffer.
    /// </summary>
    /// <returns>The message as a byte array, or null if no new message is available.</returns>
    public byte[]? Read()
    {
        long writePos = SharedHeader.GetWritePositionVolatile(_accessor);

        if (_currentReadPos == writePos)
            return null;

        // This handles the case where the writer has wrapped around but we haven't yet.
        if (_currentReadPos > writePos)
        {
            _currentReadPos = 0; // Follow the wrap
        }

        int messageLength = _accessor.ReadInt32(SharedHeader.HeaderSize + _currentReadPos);

        // A zero-length message is a signal that the writer has wrapped around.
        if (messageLength == 0)
        {
            _currentReadPos = 0;
            UpdateReaderPosition(_currentReadPos);
            // After wrapping, immediately try to read from the new position.
            return Read();
        }

        if (_currentReadPos + 4 + messageLength > writePos && writePos > _currentReadPos)
        {
            // The writer has not yet finished writing this message fully.
            return null;
        }

        byte[] message = new byte[messageLength];
        _accessor.ReadArray(
            SharedHeader.HeaderSize + _currentReadPos + 4,
            message,
            0,
            messageLength
        );

        _currentReadPos += messageLength + 4;
        UpdateReaderPosition(_currentReadPos);

        return message;
    }

    private int RegisterReader()
    {
        for (int i = 0; i < SharedHeader.MaxReaders; i++)
        {
            long offset = SharedHeader.ReaderRegistryOffset + (i * sizeof(long));
            // Attempt to claim an empty slot (-1) by setting it to 0.
            long currentValue = Interlocked.CompareExchange(ref GetRef(offset), 0, -1);
            if (currentValue == -1)
            {
                return i; // Successfully claimed slot i
            }
        }
        throw new InvalidOperationException("Maximum number of readers reached.");
    }

    private void UnregisterReader()
    {
        if (_readerSlot != -1)
        {
            // Set our slot back to -1 to mark it as inactive.
            UpdateReaderPosition(-1);
        }
    }

    private void UpdateReaderPosition(long position)
    {
        Volatile.Write(
            ref GetRef(SharedHeader.ReaderRegistryOffset + (_readerSlot * sizeof(long))),
            position
        );
    }

    private unsafe ref long GetRef(long offset)
    {
        byte* ptr = (byte*)0;
        _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        return ref *(long*)(ptr + offset);
    }

    private static string GetSharedDirectory() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/dev/shm" : Path.GetTempPath();

    /// <inheritdoc/>
    public void Dispose()
    {
        UnregisterReader();
        _accessor?.Dispose();
        _mmf?.Dispose();
        GC.SuppressFinalize(this);
    }
}
