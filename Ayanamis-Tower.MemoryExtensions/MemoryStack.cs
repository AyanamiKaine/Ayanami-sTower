using System;

namespace AyanamisTower.MemoryExtensions;

/// <summary>
/// A stack implementation using Memory&lt;byte&gt; with built-in pointer management.
/// Provides type-safe push/pop operations for various value types.
/// Supports dynamic growth when capacity is exceeded.
/// </summary>
/// <remarks>
/// Initializes a new instance of the MemoryStack with the specified size.
/// </remarks>
/// <param name="size">The size of the stack in bytes.</param>
public struct MemoryStack(int size)
{
    private int _pointer = 0;
    private byte[] _buffer = new byte[size];

    /// <summary>
    /// Gets the underlying memory buffer.
    /// </summary>
    public readonly Memory<byte> Memory => _buffer;

    /// <summary>
    /// Gets the current capacity of the stack in bytes.
    /// </summary>
    public readonly int Capacity => _buffer.Length;

    /// <summary>
    /// Gets the current stack pointer (top of stack index).
    /// </summary>
    public readonly int Pointer => _pointer;

    /// <summary>
    /// Gets whether the stack is empty.
    /// </summary>
    public readonly bool IsEmpty => _pointer == 0;

    /// <summary>
    /// Gets or sets the byte at the specified index in the underlying buffer.
    /// This provides array-like access: memory[index].
    /// </summary>
    /// <param name="index">The zero-based index of the byte to get or set.</param>
    /// <returns>The byte at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when index is negative or >= Capacity.</exception>
    public readonly byte this[int index]
    {
        get
        {
            if (index < 0 || index >= _buffer.Length)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_buffer.Length}).");
            return _buffer[index];
        }
        set
        {
            if (index < 0 || index >= _buffer.Length)
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {_buffer.Length}).");
            _buffer[index] = value;
        }
    }

    /// <summary>
    /// Pushes a byte value onto the stack.
    /// </summary>
    /// <param name="value">The byte value to push.</param>
    public void Push(byte value)
    {
        Memory.Push(ref _pointer, value);
    }

    /// <summary>
    /// Pops a byte value from the stack.
    /// </summary>
    /// <returns>The popped byte value.</returns>
    public byte Pop()
    {
        return Memory.Pop(ref _pointer);
    }

    /// <summary>
    /// Peeks at the top byte value on the stack without removing it.
    /// </summary>
    /// <returns>The byte value at the top of the stack.</returns>
    public byte Peek()
    {
        int tempPointer = _pointer;
        return Memory.Peek(ref tempPointer);
    }

    /// <summary>
    /// Pushes a 32-bit integer value onto the stack.
    /// </summary>
    /// <param name="value">The integer value to push.</param>
    public void PushInt(int value)
    {
        Memory.PushInt(ref _pointer, value);
    }

    /// <summary>
    /// Pops a 32-bit integer value from the stack.
    /// </summary>
    /// <returns>The popped integer value.</returns>
    public int PopInt()
    {
        return Memory.PopInt(ref _pointer);
    }

    /// <summary>
    /// Peeks at the top 32-bit integer value on the stack without removing it.
    /// </summary>
    /// <returns>The integer value at the top of the stack.</returns>
    public int PeekInt()
    {
        int tempPointer = _pointer;
        return Memory.PeekInt(ref tempPointer);
    }

    /// <summary>
    /// Pushes a 16-bit integer value onto the stack.
    /// </summary>
    /// <param name="value">The short value to push.</param>
    public void PushShort(short value)
    {
        Memory.PushShort(ref _pointer, value);
    }

    /// <summary>
    /// Pops a 16-bit integer value from the stack.
    /// </summary>
    /// <returns>The popped short value.</returns>
    public short PopShort()
    {
        return Memory.PopShort(ref _pointer);
    }

    /// <summary>
    /// Peeks at the top 16-bit integer value on the stack without removing it.
    /// </summary>
    /// <returns>The short value at the top of the stack.</returns>
    public short PeekShort()
    {
        int tempPointer = _pointer;
        return Memory.PeekShort(ref tempPointer);
    }

    /// <summary>
    /// Pushes an unsigned 32-bit integer value onto the stack.
    /// </summary>
    /// <param name="value">The uint value to push.</param>
    public void PushUInt(uint value)
    {
        Memory.PushUInt(ref _pointer, value);
    }

    /// <summary>
    /// Pops an unsigned 32-bit integer value from the stack.
    /// </summary>
    /// <returns>The popped uint value.</returns>
    public uint PopUInt()
    {
        return Memory.PopUInt(ref _pointer);
    }

    /// <summary>
    /// Peeks at the top unsigned 32-bit integer value on the stack without removing it.
    /// </summary>
    /// <returns>The uint value at the top of the stack.</returns>
    public uint PeekUInt()
    {
        int tempPointer = _pointer;
        return Memory.PeekUInt(ref tempPointer);
    }

    /// <summary>
    /// Pushes a 64-bit integer value onto the stack.
    /// </summary>
    /// <param name="value">The long value to push.</param>
    public void PushLong(long value)
    {
        Memory.PushLong(ref _pointer, value);
    }

    /// <summary>
    /// Pops a 64-bit integer value from the stack.
    /// </summary>
    /// <returns>The popped long value.</returns>
    public long PopLong()
    {
        return Memory.PopLong(ref _pointer);
    }

    /// <summary>
    /// Peeks at the top 64-bit integer value on the stack without removing it.
    /// </summary>
    /// <returns>The long value at the top of the stack.</returns>
    public long PeekLong()
    {
        int tempPointer = _pointer;
        return Memory.PeekLong(ref tempPointer);
    }

    /// <summary>
    /// Peeks at the top 64-bit integer value on the stack without removing it.
    /// </summary>
    /// <returns>The long value at the top of the stack.</returns>
    public long PeekCell()
    {
        return PeekLong();
    }

    /// <summary>
    /// Pushes an unsigned 64-bit integer value onto the stack.
    /// </summary>
    /// <param name="value">The ulong value to push.</param>
    public void PushULong(ulong value)
    {
        Memory.PushULong(ref _pointer, value);
    }

    /// <summary>
    /// Pops an unsigned 64-bit integer value from the stack.
    /// </summary>
    /// <returns>The popped ulong value.</returns>
    public ulong PopULong()
    {
        return Memory.PopULong(ref _pointer);
    }

    /// <summary>
    /// Peeks at the top unsigned 64-bit integer value on the stack without removing it.
    /// </summary>
    /// <returns>The ulong value at the top of the stack.</returns>
    public ulong PeekULong()
    {
        int tempPointer = _pointer;
        return Memory.PeekULong(ref tempPointer);
    }

    /// <summary>
    /// Pushes a 32-bit floating-point value onto the stack.
    /// </summary>
    /// <param name="value">The float value to push.</param>
    public void PushFloat(float value)
    {
        Memory.PushFloat(ref _pointer, value);
    }

    /// <summary>
    /// Pops a 32-bit floating-point value from the stack.
    /// </summary>
    /// <returns>The popped float value.</returns>
    public float PopFloat()
    {
        return Memory.PopFloat(ref _pointer);
    }

    /// <summary>
    /// Peeks at the top 32-bit floating-point value on the stack without removing it.
    /// </summary>
    /// <returns>The float value at the top of the stack.</returns>
    public float PeekFloat()
    {
        int tempPointer = _pointer;
        return Memory.PeekFloat(ref tempPointer);
    }

    /// <summary>
    /// Pushes a 64-bit floating-point value onto the stack.
    /// </summary>
    /// <param name="value">The double value to push.</param>
    public void PushDouble(double value)
    {
        Memory.PushDouble(ref _pointer, value);
    }

    /// <summary>
    /// Pops a 64-bit floating-point value from the stack.
    /// </summary>
    /// <returns>The popped double value.</returns>
    public double PopDouble()
    {
        return Memory.PopDouble(ref _pointer);
    }

    /// <summary>
    /// Peeks at the top 64-bit floating-point value on the stack without removing it.
    /// </summary>
    /// <returns>The double value at the top of the stack.</returns>
    public double PeekDouble()
    {
        int tempPointer = _pointer;
        return Memory.PeekDouble(ref tempPointer);
    }

    /// <summary>
    /// Clears the stack by resetting the pointer to 0.
    /// </summary>
    public void Clear()
    {
        _pointer = 0;
    }

    /// <summary>
    /// Resets the stack pointer to 0, allowing reuse of the allocated memory.
    /// This is an alias for Clear() and allows you to overwrite existing data
    /// without reallocating the underlying buffer.
    /// </summary>
    public void Reset()
    {
        _pointer = 0;
    }

    #region Indexed Access Helpers

    /// <summary>
    /// Reads a 64-bit integer (cell) from the specified byte index in memory.
    /// Does not affect the stack pointer.
    /// </summary>
    /// <param name="byteIndex">The byte index to read from.</param>
    /// <returns>The 64-bit integer value at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index or read would exceed buffer bounds.</exception>
    public readonly long ReadCellAt(int byteIndex)
    {
        if (byteIndex < 0 || byteIndex + 8 > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(byteIndex),
                $"Cannot read cell at index {byteIndex}: out of bounds [0, {_buffer.Length - 8}].");

        return System.Runtime.InteropServices.MemoryMarshal.Read<long>(_buffer.AsSpan(byteIndex, 8));
    }

    /// <summary>
    /// Writes a 64-bit integer (cell) to the specified byte index in memory.
    /// Does not affect the stack pointer.
    /// </summary>
    /// <param name="byteIndex">The byte index to write to.</param>
    /// <param name="value">The 64-bit integer value to write.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index or write would exceed buffer bounds.</exception>
    public void WriteCellAt(int byteIndex, long value)
    {
        if (byteIndex < 0 || byteIndex + 8 > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(byteIndex),
                $"Cannot write cell at index {byteIndex}: out of bounds [0, {_buffer.Length - 8}].");

        System.Runtime.InteropServices.MemoryMarshal.Write(_buffer.AsSpan(byteIndex, 8), in value);
    }

    /// <summary>
    /// Reads a 32-bit integer from the specified byte index in memory.
    /// Does not affect the stack pointer.
    /// </summary>
    /// <param name="byteIndex">The byte index to read from.</param>
    /// <returns>The 32-bit integer value at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index or read would exceed buffer bounds.</exception>
    public readonly int ReadIntAt(int byteIndex)
    {
        if (byteIndex < 0 || byteIndex + 4 > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(byteIndex),
                $"Cannot read int at index {byteIndex}: out of bounds [0, {_buffer.Length - 4}].");

        return System.Runtime.InteropServices.MemoryMarshal.Read<int>(_buffer.AsSpan(byteIndex, 4));
    }

    /// <summary>
    /// Writes a 32-bit integer to the specified byte index in memory.
    /// Does not affect the stack pointer.
    /// </summary>
    /// <param name="byteIndex">The byte index to write to.</param>
    /// <param name="value">The 32-bit integer value to write.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index or write would exceed buffer bounds.</exception>
    public void WriteIntAt(int byteIndex, int value)
    {
        if (byteIndex < 0 || byteIndex + 4 > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(byteIndex),
                $"Cannot write int at index {byteIndex}: out of bounds [0, {_buffer.Length - 4}].");

        System.Runtime.InteropServices.MemoryMarshal.Write(_buffer.AsSpan(byteIndex, 4), in value);
    }

    /// <summary>
    /// Reads a 16-bit integer from the specified byte index in memory.
    /// Does not affect the stack pointer.
    /// </summary>
    /// <param name="byteIndex">The byte index to read from.</param>
    /// <returns>The 16-bit integer value at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index or read would exceed buffer bounds.</exception>
    public readonly short ReadShortAt(int byteIndex)
    {
        if (byteIndex < 0 || byteIndex + 2 > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(byteIndex),
                $"Cannot read short at index {byteIndex}: out of bounds [0, {_buffer.Length - 2}].");

        return System.Runtime.InteropServices.MemoryMarshal.Read<short>(_buffer.AsSpan(byteIndex, 2));
    }

    /// <summary>
    /// Writes a 16-bit integer to the specified byte index in memory.
    /// Does not affect the stack pointer.
    /// </summary>
    /// <param name="byteIndex">The byte index to write to.</param>
    /// <param name="value">The 16-bit integer value to write.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index or write would exceed buffer bounds.</exception>
    public void WriteShortAt(int byteIndex, short value)
    {
        if (byteIndex < 0 || byteIndex + 2 > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(byteIndex),
                $"Cannot write short at index {byteIndex}: out of bounds [0, {_buffer.Length - 2}].");

        System.Runtime.InteropServices.MemoryMarshal.Write(_buffer.AsSpan(byteIndex, 2), in value);
    }

    /// <summary>
    /// Reads a 64-bit double from the specified byte index in memory.
    /// Does not affect the stack pointer.
    /// </summary>
    /// <param name="byteIndex">The byte index to read from.</param>
    /// <returns>The 64-bit double value at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index or read would exceed buffer bounds.</exception>
    public readonly double ReadDoubleAt(int byteIndex)
    {
        if (byteIndex < 0 || byteIndex + 8 > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(byteIndex),
                $"Cannot read double at index {byteIndex}: out of bounds [0, {_buffer.Length - 8}].");

        return System.Runtime.InteropServices.MemoryMarshal.Read<double>(_buffer.AsSpan(byteIndex, 8));
    }

    /// <summary>
    /// Writes a 64-bit double to the specified byte index in memory.
    /// Does not affect the stack pointer.
    /// </summary>
    /// <param name="byteIndex">The byte index to write to.</param>
    /// <param name="value">The 64-bit double value to write.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index or write would exceed buffer bounds.</exception>
    public void WriteDoubleAt(int byteIndex, double value)
    {
        if (byteIndex < 0 || byteIndex + 8 > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(byteIndex),
                $"Cannot write double at index {byteIndex}: out of bounds [0, {_buffer.Length - 8}].");

        System.Runtime.InteropServices.MemoryMarshal.Write(_buffer.AsSpan(byteIndex, 8), in value);
    }

    #endregion

    /// <summary>
    /// Grows the stack capacity to the specified size in bytes.
    /// Preserves existing data. If the new size is smaller than current capacity, no action is taken.
    /// </summary>
    /// <param name="newSizeBytes">The new capacity in bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when newSizeBytes is negative or exceeds int.MaxValue.</exception>
    public void GrowTo(int newSizeBytes)
    {
        if (newSizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newSizeBytes), "Size must be non-negative.");
        }

        if (newSizeBytes <= _buffer.Length)
        {
            return; // Already large enough
        }

        var newBuffer = new byte[newSizeBytes];
        Array.Copy(_buffer, newBuffer, _buffer.Length);
        _buffer = newBuffer;
    }

    /// <summary>
    /// Grows the stack capacity by the specified number of bytes.
    /// Preserves existing data.
    /// </summary>
    /// <param name="additionalBytes">The number of bytes to add to current capacity.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when additionalBytes is negative or result exceeds int.MaxValue.</exception>
    public void GrowBy(int additionalBytes)
    {
        if (additionalBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(additionalBytes), "Additional bytes must be non-negative.");
        }

        long newSize = (long)_buffer.Length + additionalBytes;
        if (newSize > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(additionalBytes),
                $"Resulting size {newSize} exceeds maximum allowed size {int.MaxValue}.");
        }

        GrowTo((int)newSize);
    }

    /// <summary>
    /// Grows the stack capacity by a factor (e.g., 2.0 for doubling).
    /// Preserves existing data.
    /// </summary>
    /// <param name="factor">The growth factor (must be >= 1.0).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when factor &lt; 1.0 or result exceeds int.MaxValue.</exception>
    public void GrowByFactor(double factor)
    {
        if (factor < 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(factor), "Growth factor must be >= 1.0.");
        }

        long newSize = (long)(_buffer.Length * factor);
        if (newSize > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(factor),
                $"Resulting size {newSize} exceeds maximum allowed size {int.MaxValue}.");
        }

        GrowTo((int)newSize);
    }

    /// <summary>
    /// Doubles the stack capacity. Preserves existing data.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when doubling would exceed int.MaxValue.</exception>
    public void Double()
    {
        GrowByFactor(2.0);
    }

    /// <summary>
    /// Grows the stack to accommodate at least the specified number of additional bytes from current pointer.
    /// Useful for ensuring space before a series of push operations.
    /// </summary>
    /// <param name="requiredBytes">The number of bytes needed.</param>
    public void EnsureCapacity(int requiredBytes)
    {
        int neededCapacity = _pointer + requiredBytes;
        if (neededCapacity > _buffer.Length)
        {
            // Grow to at least double the current size or enough for required bytes, whichever is larger
            int newSize = Math.Max(neededCapacity, _buffer.Length * 2);
            if (newSize < 0) // Overflow check
            {
                newSize = int.MaxValue;
            }
            GrowTo(newSize);
        }
    }

    /// <summary>
    /// Creates a new MemoryStack with the specified size in bytes.
    /// </summary>
    /// <param name="bytes">The size of the stack in bytes.</param>
    /// <returns>A new MemoryStack instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when bytes is negative.</exception>
    public static MemoryStack FromBytes(int bytes)
    {
        if (bytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "Bytes must be non-negative.");
        }

        return new MemoryStack(bytes);
    }

    /// <summary>
    /// Creates a new MemoryStack with the specified size in words (16-bit/2 bytes).
    /// </summary>
    /// <param name="words">The size of the stack in words (1 word = 2 bytes).</param>
    /// <returns>A new MemoryStack instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when words is negative or the result exceeds int.MaxValue.</exception>
    public static MemoryStack FromWords(int words)
    {
        if (words < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(words), "Words must be non-negative.");
        }

        long bytes = (long)words * 2;
        if (bytes > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(words), "Resulting size exceeds maximum allowed size.");
        }

        return new MemoryStack((int)bytes);
    }

    /// <summary>
    /// Creates a new MemoryStack with the specified size in double words (32-bit/4 bytes).
    /// </summary>
    /// <param name="dwords">The size of the stack in double words (1 dword = 4 bytes).</param>
    /// <returns>A new MemoryStack instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when dwords is negative or the result exceeds int.MaxValue.</exception>
    public static MemoryStack FromDWords(int dwords)
    {
        if (dwords < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dwords), "DWords must be non-negative.");
        }

        long bytes = (long)dwords * 4;
        if (bytes > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(dwords), "Resulting size exceeds maximum allowed size.");
        }

        return new MemoryStack((int)bytes);
    }

    /// <summary>
    /// Creates a new MemoryStack with the specified size in quad words (64-bit/8 bytes).
    /// </summary>
    /// <param name="qwords">The size of the stack in quad words (1 qword = 8 bytes).</param>
    /// <returns>A new MemoryStack instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when qwords is negative or the result exceeds int.MaxValue.</exception>
    public static MemoryStack FromQWords(int qwords)
    {
        if (qwords < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(qwords), "QWords must be non-negative.");
        }

        long bytes = (long)qwords * 8;
        if (bytes > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(qwords), "Resulting size exceeds maximum allowed size.");
        }

        return new MemoryStack((int)bytes);
    }

    /// <summary>
    /// Creates a new MemoryStack with the specified size in cells (64-bit/8 bytes).
    /// Alias for FromQWords - represents the natural word size for 64-bit systems.
    /// </summary>
    /// <param name="cells">The size of the stack in cells (1 cell = 8 bytes).</param>
    /// <returns>A new MemoryStack instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when cells is negative or the result exceeds int.MaxValue.</exception>
    public static MemoryStack FromCells(int cells)
    {
        return FromQWords(cells);
    }

    /// <summary>
    /// Creates a new MemoryStack with the specified size in kilobytes.
    /// </summary>
    /// <param name="kilobytes">The size of the stack in kilobytes.</param>
    /// <returns>A new MemoryStack instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when kilobytes is negative or the result exceeds int.MaxValue.</exception>
    public static MemoryStack FromKilobytes(int kilobytes)
    {
        if (kilobytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(kilobytes), "Kilobytes must be non-negative.");
        }

        long bytes = (long)kilobytes * 1024;
        if (bytes > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(kilobytes), "Resulting size exceeds maximum allowed size.");
        }

        return new MemoryStack((int)bytes);
    }

    /// <summary>
    /// Creates a new MemoryStack with the specified size in megabytes.
    /// </summary>
    /// <param name="megabytes">The size of the stack in megabytes.</param>
    /// <returns>A new MemoryStack instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when megabytes is negative or the result exceeds int.MaxValue.</exception>
    public static MemoryStack FromMegabytes(int megabytes)
    {
        if (megabytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(megabytes), "Megabytes must be non-negative.");
        }

        long bytes = (long)megabytes * 1024 * 1024;
        if (bytes > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(megabytes), "Resulting size exceeds maximum allowed size.");
        }

        return new MemoryStack((int)bytes);
    }

    /// <summary>
    /// Creates a new MemoryStack with the specified size in gigabytes.
    /// </summary>
    /// <param name="gigabytes">The size of the stack in gigabytes.</param>
    /// <returns>A new MemoryStack instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when gigabytes is negative or the result exceeds int.MaxValue (approximately 2GB).</exception>
    public static MemoryStack FromGigabytes(int gigabytes)
    {
        if (gigabytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gigabytes), "Gigabytes must be non-negative.");
        }

        // Since max size is int.MaxValue (~2GB), only gigabytes <= 2 are valid
        if (gigabytes > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(gigabytes), "Gigabytes cannot exceed 2 (maximum stack size is ~2GB).");
        }

        long bytes = (long)gigabytes * 1024 * 1024 * 1024;
        if (bytes > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(gigabytes), "Resulting size exceeds maximum allowed size.");
        }

        return new MemoryStack((int)bytes);
    }
}
