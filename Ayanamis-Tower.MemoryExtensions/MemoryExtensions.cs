using System.Runtime.InteropServices;

namespace AyanamisTower.MemoryExtensions;

/// <summary>
/// Extension methods for Memory&lt;byte&gt; to provide stack-like operations.
/// This version is refactored to use MemoryMarshal for zero-allocation push, pop, and peek operations,
/// significantly improving performance and reducing GC pressure.
/// </summary>
public static class MemoryExtensions
{
    // ===== Core Byte Operations =====

    /// <summary>
    /// Pushes a byte value onto the stack.
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The byte value to push.</param>
    public static void Push(this Memory<byte> stack, ref int pointer, byte value)
    {
        if (pointer >= stack.Length)
            throw new InvalidOperationException("Stack overflow");
        stack.Span[pointer++] = value;
    }

    /// <summary>
    /// Pops a byte value from the stack.
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped byte value.</returns>
    public static byte Pop(this Memory<byte> stack, ref int pointer)
    {
        if (pointer <= 0)
            throw new InvalidOperationException("Stack underflow");
        return stack.Span[--pointer];
    }

    /// <summary>
    /// Peeks at the top byte value on the stack without removing it.
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The byte value at the top of the stack.</returns>
    public static byte Peek(this Memory<byte> stack, ref int pointer)
    {
        if (pointer <= 0)
            throw new InvalidOperationException("Stack is empty");
        return stack.Span[pointer - 1];
    }

    // ===== Zero-Allocation Multi-Byte Operations =====

    // ===== Zero-Allocation Multi-Byte Operations =====

    /// <summary>
    /// Pushes a 32-bit integer value onto the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The integer value to push.</param>
    public static void PushInt(this Memory<byte> stack, ref int pointer, int value)
    {
        const int size = sizeof(int);
        if (pointer + size > stack.Length)
            throw new InvalidOperationException("Stack overflow");

        MemoryMarshal.Write(stack.Span[pointer..], in value);
        pointer += size;
    }

    /// <summary>
    /// Pops a 32-bit integer value from the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped integer value.</returns>
    public static int PopInt(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(int);
        if (pointer < size)
            throw new InvalidOperationException("Stack underflow");

        pointer -= size;
        return MemoryMarshal.Read<int>(stack.Span[pointer..]);
    }

    /// <summary>
    /// Peeks at the top 32-bit integer value on the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The integer value at the top of the stack.</returns>
    public static int PeekInt(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(int);
        if (pointer < size)
            throw new InvalidOperationException("Not enough data on stack");

        return MemoryMarshal.Read<int>(stack.Span[(pointer - size)..]);
    }

    /// <summary>
    /// Pushes a 16-bit integer value onto the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The short value to push.</param>
    public static void PushShort(this Memory<byte> stack, ref int pointer, short value)
    {
        const int size = sizeof(short);
        if (pointer + size > stack.Length)
            throw new InvalidOperationException("Stack overflow");

        MemoryMarshal.Write(stack.Span[pointer..], in value);
        pointer += size;
    }

    /// <summary>
    /// Pops a 16-bit integer value from the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped short value.</returns>
    public static short PopShort(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(short);
        if (pointer < size)
            throw new InvalidOperationException("Stack underflow");

        pointer -= size;
        return MemoryMarshal.Read<short>(stack.Span[pointer..]);
    }

    /// <summary>
    /// Peeks at the top 16-bit integer value on the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The short value at the top of the stack.</returns>
    public static short PeekShort(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(short);
        if (pointer < size)
            throw new InvalidOperationException("Not enough data on stack");

        return MemoryMarshal.Read<short>(stack.Span[(pointer - size)..]);
    }

    /// <summary>
    /// Pushes an unsigned 32-bit integer value onto the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The uint value to push.</param>
    public static void PushUInt(this Memory<byte> stack, ref int pointer, uint value)
    {
        const int size = sizeof(uint);
        if (pointer + size > stack.Length)
            throw new InvalidOperationException("Stack overflow");

        MemoryMarshal.Write(stack.Span[pointer..], in value);
        pointer += size;
    }

    /// <summary>
    /// Pops an unsigned 32-bit integer value from the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped uint value.</returns>
    public static uint PopUInt(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(uint);
        if (pointer < size)
            throw new InvalidOperationException("Stack underflow");

        pointer -= size;
        return MemoryMarshal.Read<uint>(stack.Span[pointer..]);
    }

    /// <summary>
    /// Peeks at the top unsigned 32-bit integer value on the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The uint value at the top of the stack.</returns>
    public static uint PeekUInt(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(uint);
        if (pointer < size)
            throw new InvalidOperationException("Not enough data on stack");

        return MemoryMarshal.Read<uint>(stack.Span[(pointer - size)..]);
    }

    /// <summary>
    /// Pushes a 64-bit integer value onto the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The long value to push.</param>
    public static void PushLong(this Memory<byte> stack, ref int pointer, long value)
    {
        const int size = sizeof(long);
        if (pointer + size > stack.Length)
            throw new InvalidOperationException("Stack overflow");

        MemoryMarshal.Write(stack.Span[pointer..], in value);
        pointer += size;
    }

    /// <summary>
    /// Pops a 64-bit integer value from the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped long value.</returns>
    public static long PopLong(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(long);
        if (pointer < size)
            throw new InvalidOperationException("Stack underflow");

        pointer -= size;
        return MemoryMarshal.Read<long>(stack.Span[pointer..]);
    }

    /// <summary>
    /// Peeks at the top 64-bit integer value on the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The long value at the top of the stack.</returns>
    public static long PeekLong(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(long);
        if (pointer < size)
            throw new InvalidOperationException("Not enough data on stack");

        return MemoryMarshal.Read<long>(stack.Span[(pointer - size)..]);
    }

    /// <summary>
    /// Pushes an unsigned 64-bit integer value onto the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The ulong value to push.</param>
    public static void PushULong(this Memory<byte> stack, ref int pointer, ulong value)
    {
        const int size = sizeof(ulong);
        if (pointer + size > stack.Length)
            throw new InvalidOperationException("Stack overflow");

        MemoryMarshal.Write(stack.Span[pointer..], in value);
        pointer += size;
    }

    /// <summary>
    /// Pops an unsigned 64-bit integer value from the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped ulong value.</returns>
    public static ulong PopULong(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(ulong);
        if (pointer < size)
            throw new InvalidOperationException("Stack underflow");

        pointer -= size;
        return MemoryMarshal.Read<ulong>(stack.Span[pointer..]);
    }

    /// <summary>
    /// Peeks at the top unsigned 64-bit integer value on the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The ulong value at the top of the stack.</returns>
    public static ulong PeekULong(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(ulong);
        if (pointer < size)
            throw new InvalidOperationException("Not enough data on stack");

        return MemoryMarshal.Read<ulong>(stack.Span[(pointer - size)..]);
    }

    /// <summary>
    /// Pushes a 32-bit floating-point value onto the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The float value to push.</param>
    public static void PushFloat(this Memory<byte> stack, ref int pointer, float value)
    {
        const int size = sizeof(float);
        if (pointer + size > stack.Length)
            throw new InvalidOperationException("Stack overflow");

        MemoryMarshal.Write(stack.Span[pointer..], in value);
        pointer += size;
    }

    /// <summary>
    /// Pops a 32-bit floating-point value from the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped float value.</returns>
    public static float PopFloat(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(float);
        if (pointer < size)
            throw new InvalidOperationException("Stack underflow");

        pointer -= size;
        return MemoryMarshal.Read<float>(stack.Span[pointer..]);
    }

    /// <summary>
    /// Peeks at the top 32-bit floating-point value on the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The float value at the top of the stack.</returns>
    public static float PeekFloat(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(float);
        if (pointer < size)
            throw new InvalidOperationException("Not enough data on stack");

        return MemoryMarshal.Read<float>(stack.Span[(pointer - size)..]);
    }

    /// <summary>
    /// Pushes a 64-bit floating-point value onto the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The double value to push.</param>
    public static void PushDouble(this Memory<byte> stack, ref int pointer, double value)
    {
        const int size = sizeof(double);
        if (pointer + size > stack.Length)
            throw new InvalidOperationException("Stack overflow");

        MemoryMarshal.Write(stack.Span[pointer..], in value);
        pointer += size;
    }

    /// <summary>
    /// Pops a 64-bit floating-point value from the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped double value.</returns>
    public static double PopDouble(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(double);
        if (pointer < size)
            throw new InvalidOperationException("Stack underflow");

        pointer -= size;
        return MemoryMarshal.Read<double>(stack.Span[pointer..]);
    }

    /// <summary>
    /// Peeks at the top 64-bit floating-point value on the stack (zero-allocation).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The double value at the top of the stack.</returns>
    public static double PeekDouble(this Memory<byte> stack, ref int pointer)
    {
        const int size = sizeof(double);
        if (pointer < size)
            throw new InvalidOperationException("Not enough data on stack");

        return MemoryMarshal.Read<double>(stack.Span[(pointer - size)..]);
    }
}
