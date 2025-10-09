using System;

namespace AyanamisTower.MemoryExtensions;

/// <summary>
/// A stack implementation using Memory&lt;byte&gt; with built-in pointer management.
/// Provides type-safe push/pop operations for various value types.
/// </summary>
/// <remarks>
/// Initializes a new instance of the MemoryStack with the specified size.
/// </remarks>
/// <param name="size">The size of the stack in bytes.</param>
public struct MemoryStack(int size)
{
    private int _pointer = 0;

    /// <summary>
    /// Gets the underlying memory buffer.
    /// </summary>
    public Memory<byte> Memory { get; } = new byte[size];

    /// <summary>
    /// Gets the current stack pointer (top of stack index).
    /// </summary>
    public readonly int Pointer => _pointer;

    /// <summary>
    /// Gets whether the stack is empty.
    /// </summary>
    public readonly bool IsEmpty => _pointer == 0;

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
    public readonly byte Peek()
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
    public readonly int PeekInt()
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
    public readonly short PeekShort()
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
    public readonly uint PeekUInt()
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
    public readonly long PeekLong()
    {
        int tempPointer = _pointer;
        return Memory.PeekLong(ref tempPointer);
    }

    /// <summary>
    /// Peeks at the top 64-bit integer value on the stack without removing it.
    /// </summary>
    /// <returns>The long value at the top of the stack.</returns>
    public readonly long PeekCell()
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
    public readonly ulong PeekULong()
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
    public readonly float PeekFloat()
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
    public readonly double PeekDouble()
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
}
