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
    /// Clears the stack by resetting the pointer to 0.
    /// </summary>
    public void Clear()
    {
        _pointer = 0;
    }
}
