using System;

namespace StellaLang;

/// <summary>
/// A builder class for constructing bytecode sequences.
/// </summary>
public class BytecodeBuilder
{
    private readonly List<byte> _bytes = [];

    /// <summary>
    /// Append an opcode to the bytecode sequence.
    /// </summary>
    /// <param name="opcode"></param>
    /// <returns></returns>
    public BytecodeBuilder Op(OpCode opcode)
    {
        _bytes.Add((byte)opcode);
        return this;
    }
    /// <summary>
    /// Append a PUSH operation followed by the given long value to the bytecode sequence.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public BytecodeBuilder Push(long value)
    {
        _bytes.Add((byte)OpCode.PUSH);
        _bytes.AddRange(BitConverter.GetBytes(value));
        return this;
    }

    /// <summary>
    /// Append a JUMP operation with the given offset.
    /// </summary>
    /// <param name="offset">Relative offset from the end of this instruction.</param>
    /// <returns></returns>
    public BytecodeBuilder Jump(short offset)
    {
        _bytes.Add((byte)OpCode.JUMP);
        _bytes.AddRange(BitConverter.GetBytes(offset));
        return this;
    }

    /// <summary>
    /// Append a JUMPZ (jump if zero/false) operation with the given offset.
    /// Pops a value from the stack and jumps if it's zero.
    /// </summary>
    /// <param name="offset">Relative offset from the end of this instruction.</param>
    /// <returns></returns>
    public BytecodeBuilder JumpZ(short offset)
    {
        _bytes.Add((byte)OpCode.JUMPZ);
        _bytes.AddRange(BitConverter.GetBytes(offset));
        return this;
    }

    /// <summary>
    /// Append a JUMPNZ (jump if non-zero/true) operation with the given offset.
    /// Pops a value from the stack and jumps if it's non-zero.
    /// </summary>
    /// <param name="offset">Relative offset from the end of this instruction.</param>
    /// <returns></returns>
    public BytecodeBuilder JumpNZ(short offset)
    {
        _bytes.Add((byte)OpCode.JUMPNZ);
        _bytes.AddRange(BitConverter.GetBytes(offset));
        return this;
    }

    /// <summary>
    /// Build and return the final bytecode array.
    /// </summary>
    /// <returns></returns>
    public byte[] Build() => [.. _bytes];
}
