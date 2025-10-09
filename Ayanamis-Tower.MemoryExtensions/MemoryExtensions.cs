namespace AyanamisTower.MemoryExtensions;

/// <summary>
/// Extension methods for Memory&lt;byte&gt; to provide stack-like operations.
/// </summary>
public static class MemoryExtensions
{
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
    /// Pushes a 32-bit integer value onto the stack as 4 bytes (little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The integer value to push.</param>
    public static void PushInt(this Memory<byte> stack, ref int pointer, int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        for (int i = 0; i < 4; i++)
        {
            stack.Push(ref pointer, bytes[i]);
        }
    }

    /// <summary>
    /// Pops a 32-bit integer value from the stack (4 bytes, little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped integer value.</returns>
    public static int PopInt(this Memory<byte> stack, ref int pointer)
    {
        byte[] bytes = new byte[4];
        for (int i = 3; i >= 0; i--)
        {
            bytes[i] = stack.Pop(ref pointer);
        }
        return BitConverter.ToInt32(bytes, 0);
    }

    /// <summary>
    /// Pushes a 16-bit integer value onto the stack as 2 bytes (little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The short value to push.</param>
    public static void PushShort(this Memory<byte> stack, ref int pointer, short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        for (int i = 0; i < 2; i++)
        {
            stack.Push(ref pointer, bytes[i]);
        }
    }

    /// <summary>
    /// Pops a 16-bit integer value from the stack (2 bytes, little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped short value.</returns>
    public static short PopShort(this Memory<byte> stack, ref int pointer)
    {
        byte[] bytes = new byte[2];
        for (int i = 1; i >= 0; i--)
        {
            bytes[i] = stack.Pop(ref pointer);
        }
        return BitConverter.ToInt16(bytes, 0);
    }

    /// <summary>
    /// Pushes an unsigned 32-bit integer value onto the stack as 4 bytes (little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The uint value to push.</param>
    public static void PushUInt(this Memory<byte> stack, ref int pointer, uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        for (int i = 0; i < 4; i++)
        {
            stack.Push(ref pointer, bytes[i]);
        }
    }

    /// <summary>
    /// Pops an unsigned 32-bit integer value from the stack (4 bytes, little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped uint value.</returns>
    public static uint PopUInt(this Memory<byte> stack, ref int pointer)
    {
        byte[] bytes = new byte[4];
        for (int i = 3; i >= 0; i--)
        {
            bytes[i] = stack.Pop(ref pointer);
        }
        return BitConverter.ToUInt32(bytes, 0);
    }

    /// <summary>
    /// Pushes a 64-bit integer value onto the stack as 8 bytes (little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The long value to push.</param>
    public static void PushLong(this Memory<byte> stack, ref int pointer, long value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        for (int i = 0; i < 8; i++)
        {
            stack.Push(ref pointer, bytes[i]);
        }
    }

    /// <summary>
    /// Pops a 64-bit integer value from the stack (8 bytes, little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped long value.</returns>
    public static long PopLong(this Memory<byte> stack, ref int pointer)
    {
        byte[] bytes = new byte[8];
        for (int i = 7; i >= 0; i--)
        {
            bytes[i] = stack.Pop(ref pointer);
        }
        return BitConverter.ToInt64(bytes, 0);
    }

    /// <summary>
    /// Pushes an unsigned 64-bit integer value onto the stack as 8 bytes (little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The ulong value to push.</param>
    public static void PushULong(this Memory<byte> stack, ref int pointer, ulong value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        for (int i = 0; i < 8; i++)
        {
            stack.Push(ref pointer, bytes[i]);
        }
    }

    /// <summary>
    /// Pops an unsigned 64-bit integer value from the stack (8 bytes, little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped ulong value.</returns>
    public static ulong PopULong(this Memory<byte> stack, ref int pointer)
    {
        byte[] bytes = new byte[8];
        for (int i = 7; i >= 0; i--)
        {
            bytes[i] = stack.Pop(ref pointer);
        }
        return BitConverter.ToUInt64(bytes, 0);
    }

    /// <summary>
    /// Pushes a 32-bit floating-point value onto the stack as 4 bytes (little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The float value to push.</param>
    public static void PushFloat(this Memory<byte> stack, ref int pointer, float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        for (int i = 0; i < 4; i++)
        {
            stack.Push(ref pointer, bytes[i]);
        }
    }

    /// <summary>
    /// Pops a 32-bit floating-point value from the stack (4 bytes, little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped float value.</returns>
    public static float PopFloat(this Memory<byte> stack, ref int pointer)
    {
        byte[] bytes = new byte[4];
        for (int i = 3; i >= 0; i--)
        {
            bytes[i] = stack.Pop(ref pointer);
        }
        return BitConverter.ToSingle(bytes, 0);
    }

    /// <summary>
    /// Pushes a 64-bit floating-point value onto the stack as 8 bytes (little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <param name="value">The double value to push.</param>
    public static void PushDouble(this Memory<byte> stack, ref int pointer, double value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        for (int i = 0; i < 8; i++)
        {
            stack.Push(ref pointer, bytes[i]);
        }
    }

    /// <summary>
    /// Pops a 64-bit floating-point value from the stack (8 bytes, little-endian).
    /// </summary>
    /// <param name="stack">The memory representing the stack.</param>
    /// <param name="pointer">Reference to the stack pointer.</param>
    /// <returns>The popped double value.</returns>
    public static double PopDouble(this Memory<byte> stack, ref int pointer)
    {
        byte[] bytes = new byte[8];
        for (int i = 7; i >= 0; i--)
        {
            bytes[i] = stack.Pop(ref pointer);
        }
        return BitConverter.ToDouble(bytes, 0);
    }
}
