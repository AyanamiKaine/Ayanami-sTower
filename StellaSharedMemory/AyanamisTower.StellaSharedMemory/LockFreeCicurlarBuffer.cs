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
    public const int WritePosOffset = 0;
    public const int CapacityOffset = 8;
    public const int NextTicketOffset = 16;
    public const int CurrentTicketOffset = 24;

    public static unsafe long GetWritePositionVolatile(MemoryMappedViewAccessor accessor)
    {
        byte* ptr = (byte*)0;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try { return Volatile.Read(ref ((long*)(ptr + WritePosOffset))[0]); }
        finally { accessor.SafeMemoryMappedViewHandle.ReleasePointer(); }
    }

    public static unsafe void SetWritePositionVolatile(MemoryMappedViewAccessor accessor, long value)
    {
        byte* ptr = (byte*)0;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try { Volatile.Write(ref ((long*)(ptr + WritePosOffset))[0], value); }
        finally { accessor.SafeMemoryMappedViewHandle.ReleasePointer(); }
    }

    // --- Ticket Lock Helpers ---
    // The ticket lock system provides a fair, FIFO (First-In, First-Out) queue for multiple writers.
    // It works like a deli counter:
    // 1. A writer arrives and takes a number (`NextTicket`).
    // 2. The writer waits until its number is called (`CurrentTicket`).
    // 3. When finished, the writer increments the "now serving" number (`CurrentTicket`), allowing the next writer to proceed.
    // This prevents writer starvation and ensures order.

    /// <summary>
    /// Atomically increments the `NextTicket` counter and returns the *new* value.
    /// The caller's ticket is the value *before* the increment, so it should use `result - 1`.
    /// This is the "take a number" step.
    /// </summary>
    public static unsafe long GetNextTicket(MemoryMappedViewAccessor accessor)
    {
        byte* ptr = (byte*)0;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try
        {
            // Interlocked.Increment is an atomic operation, crucial for preventing race conditions
            // where multiple writers might try to get a ticket at the same time.
            return Interlocked.Increment(ref ((long*)(ptr + NextTicketOffset))[0]);
        }
        finally
        {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    /// <summary>
    /// Reads the `CurrentTicket`, which is the ticket currently being "served".
    /// A writer compares its own ticket to this value to know when it's their turn.
    /// This is the "check the 'now serving' display" step.
    /// </summary>
    public static unsafe long GetCurrentTicket(MemoryMappedViewAccessor accessor)
    {
        byte* ptr = (byte*)0;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try
        {
            // Volatile.Read ensures we get the latest value written by any process.
            return Volatile.Read(ref ((long*)(ptr + CurrentTicketOffset))[0]);
        }
        finally
        {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    /// <summary>
    /// Atomically increments the `CurrentTicket`, signaling that the current writer is done
    /// and the next writer in the queue can proceed.
    /// This is the "call the next number" step.
    /// </summary>
    public static unsafe void AdvanceCurrentTicket(MemoryMappedViewAccessor accessor)
    {
        byte* ptr = (byte*)0;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try
        {
            // Using Interlocked.Increment ensures this operation is atomic.
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
    private const int HeaderSize = 32;

    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly long _dataBufferCapacity;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiWriterCircularBuffer"/> class.
    /// </summary>
    /// <param name="name">The name of the shared memory segment.</param>
    /// <param name="mode">The mode to open the shared memory (Create or Open).</param>
    /// <param name="capacity">The capacity of the buffer in bytes. Required when mode is Create.</param>
    public MultiWriterCircularBuffer(string name, SharedMemoryMode mode, long capacity = 0)
    {
        if (mode == SharedMemoryMode.Create && capacity <= HeaderSize)
        {
            throw new ArgumentException("Capacity must be larger than the header size.", nameof(capacity));
        }

        string mmfPath = Path.Combine(GetSharedDirectory(), name);

        FileMode fileMode = mode == SharedMemoryMode.Create ? FileMode.Create : FileMode.Open;

        using (var fs = new FileStream(mmfPath, fileMode, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            long mmfCapacity = mode == SharedMemoryMode.Create ? capacity : fs.Length;
            _mmf = MemoryMappedFile.CreateFromFile(fs, null, mmfCapacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
        }

        _accessor = _mmf.CreateViewAccessor();

        if (mode == SharedMemoryMode.Create)
        {
            _dataBufferCapacity = capacity - HeaderSize;
            _accessor.Write(SharedHeader.CapacityOffset, _dataBufferCapacity);
            SharedHeader.SetWritePositionVolatile(_accessor, 0);
            // Initialize ticket system
            _accessor.Write(SharedHeader.NextTicketOffset, 0L);
            _accessor.Write(SharedHeader.CurrentTicketOffset, 0L);
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
        AcquireLock(); // This now uses the ticket lock logic
        try
        {
            long requiredSpace = message.Length + 4;
            if (requiredSpace > _dataBufferCapacity)
            {
                throw new ArgumentException("Message is larger than the buffer capacity.", nameof(message));
            }

            long currentWritePos = _accessor.ReadInt64(0); // Remember non-volatile read is fine inside lock

            if (currentWritePos + requiredSpace > _dataBufferCapacity)
            {
                if (currentWritePos + 4 <= _dataBufferCapacity)
                {
                    _accessor.Write(HeaderSize + (int)currentWritePos, 0);
                }
                currentWritePos = 0;
            }

            _accessor.Write(HeaderSize + (int)currentWritePos, message.Length);
            _accessor.WriteArray(HeaderSize + (int)currentWritePos + 4, message, 0, message.Length);

            long nextWritePos = currentWritePos + requiredSpace;

            SharedHeader.SetWritePositionVolatile(_accessor, nextWritePos);
        }
        finally
        {
            ReleaseLock(); // This now advances the ticket
        }
    }

    private void AcquireLock()
    {
        // 1. Atomically get our ticket number.
        long myTicket = SharedHeader.GetNextTicket(_accessor) - 1;

        // 2. Wait until the current ticket matches ours.
        // SpinWait is an efficient way to wait for short periods without yielding the thread immediately.
        var spinner = new SpinWait();
        while (SharedHeader.GetCurrentTicket(_accessor) != myTicket)
        {
            spinner.SpinOnce(-1); // A parameter of -1 is optimized for yielding on multi-core systems.
        }
    }

    private void ReleaseLock()
    {
        // 3. Our work is done, advance the ticket to let the next process in.
        SharedHeader.AdvanceCurrentTicket(_accessor);
    }

    private static string GetSharedDirectory() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/dev/shm" : Path.GetTempPath();

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
    private const int HeaderSize = 32;
    private readonly string _readerStatePath;
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private long _currentReadPos;

    /// <summary>
    /// Initializes a new instance of the <see cref="LockFreeBufferReader"/> class.
    /// </summary>
    /// <param name="logName">The name of the shared memory segment to read from.</param>
    /// <param name="readerName">The unique name for this reader instance.</param>
    public LockFreeBufferReader(string logName, string readerName)
    {
        _readerStatePath = Path.Combine(GetSharedDirectory(), $"{logName}-reader-{readerName}.state");

        string mmfPath = Path.Combine(GetSharedDirectory(), logName);
        var fs = new FileStream(mmfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        _mmf = MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
        _accessor = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

        _currentReadPos = LoadReaderState() ?? SharedHeader.GetWritePositionVolatile(_accessor);
    }

    /// <summary>
    /// Reads the next message from the circular buffer.
    /// </summary>
    /// <returns>The message as a byte array, or null if no new message is available.</returns>
    public byte[]? Read()
    {
        long writePos = SharedHeader.GetWritePositionVolatile(_accessor);

        if (_currentReadPos == writePos) return null;

        if (_currentReadPos > writePos)
        {
            _currentReadPos = writePos;
            // We should give the user to option to manually safe, or to activate the automatic safe here
            // safing the reader state is a high latency operation because it results in a context switch.
            //SaveReaderState(_currentReadPos);
            return null;
        }

        int messageLength = _accessor.ReadInt32(HeaderSize + _currentReadPos);

        if (messageLength == 0)
        {
            _currentReadPos = 0;
            //SaveReaderState(_currentReadPos);
            return Read();
        }

        byte[] message = new byte[messageLength];
        _accessor.ReadArray(HeaderSize + _currentReadPos + 4, message, 0, messageLength);

        _currentReadPos += messageLength + 4;
        //SaveReaderState(_currentReadPos);

        return message;
    }

    private long? LoadReaderState()
    {
        if (!File.Exists(_readerStatePath)) return null;
        try { return BitConverter.ToInt64(File.ReadAllBytes(_readerStatePath)); }
        catch { return null; }
    }

    // Maybe turning this to a public method?
    private void SaveReaderState(long position)
    {
        try { File.WriteAllBytes(_readerStatePath, BitConverter.GetBytes(position)); }
        catch { /* Handle errors */ }
    }

    private static string GetSharedDirectory() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/dev/shm" : Path.GetTempPath();

    /// <inheritdoc/>
    public void Dispose()
    {
        _accessor?.Dispose();
        _mmf?.Dispose();
        GC.SuppressFinalize(this);
    }
}