using System;
using System.Collections.Generic;

namespace StellaLang;

/// <summary>
/// Fluent builder for constructing StellaLang bytecode programs.
/// Provides a simple API to build VM programs without needing a scanner/parser.
/// </summary>
/// <example>
/// <code>
/// var code = new CodeBuilder()
///     .PushCell(10)
///     .PushCell(20)
///     .Add()
///     .Halt()
///     .Build();
/// </code>
/// </example>
public class CodeBuilder
{
    private readonly List<byte> _bytecode = [];
    private readonly Dictionary<string, int> _labels = [];
    private readonly List<(int position, string label)> _unresolvedJumps = [];

    /// <summary>
    /// Pushes a 64-bit integer (cell) onto the stack.
    /// </summary>
    public CodeBuilder PushCell(long value)
    {
        EmitOpCode(OPCode.PUSH_CELL);
        EmitLong(value);
        return this;
    }

    /// <summary>
    /// Pushes a 64-bit double onto the float stack.
    /// </summary>
    public CodeBuilder FPushDouble(double value)
    {
        EmitOpCode(OPCode.FPUSH_DOUBLE);
        EmitDouble(value);
        return this;
    }

    /// <summary>
    /// Pushes a 64-bit float onto the float stack.
    /// </summary>
    public CodeBuilder FPushFloat(float value)
    {
        EmitOpCode(OPCode.FPUSH_DOUBLE);
        EmitDouble(value);
        return this;
    }

    // ===== Stack Manipulation =====

    /// <summary>Duplicates the top of the stack.</summary>
    public CodeBuilder Dup() { EmitOpCode(OPCode.DUP); return this; }

    /// <summary>Removes the top of the stack.</summary>
    public CodeBuilder Drop() { EmitOpCode(OPCode.DROP); return this; }

    /// <summary>Swaps the top two stack elements.</summary>
    public CodeBuilder Swap() { EmitOpCode(OPCode.SWAP); return this; }

    /// <summary>Copies the second element to the top.</summary>
    public CodeBuilder Over() { EmitOpCode(OPCode.OVER); return this; }

    /// <summary>Rotates the top three elements.</summary>
    public CodeBuilder Rot() { EmitOpCode(OPCode.ROT); return this; }

    // ===== Return Stack Manipulation =====

    /// <summary>Move value from data stack to return stack.</summary>
    public CodeBuilder ToR() { EmitOpCode(OPCode.TO_R); return this; }

    /// <summary>Move value from return stack to data stack.</summary>
    public CodeBuilder RFrom() { EmitOpCode(OPCode.R_FROM); return this; }

    /// <summary>Copy top of return stack to data stack.</summary>
    public CodeBuilder RFetch() { EmitOpCode(OPCode.R_FETCH); return this; }

    // ===== Arithmetic =====

    /// <summary>Add two values.</summary>
    public CodeBuilder Add() { EmitOpCode(OPCode.ADD); return this; }

    /// <summary>Subtract two values.</summary>
    public CodeBuilder Sub() { EmitOpCode(OPCode.SUB); return this; }

    /// <summary>Multiply two values.</summary>
    public CodeBuilder Mul() { EmitOpCode(OPCode.MUL); return this; }

    /// <summary>Divide two values.</summary>
    public CodeBuilder Div() { EmitOpCode(OPCode.DIV); return this; }

    /// <summary>Modulo operation.</summary>
    public CodeBuilder Mod() { EmitOpCode(OPCode.MOD); return this; }

    /// <summary>Combined division and modulo.</summary>
    public CodeBuilder DivMod() { EmitOpCode(OPCode.DIVMOD); return this; }

    /// <summary>Negate a value.</summary>
    public CodeBuilder Neg() { EmitOpCode(OPCode.NEG); return this; }

    // ===== Bitwise Operations =====

    /// <summary>Bitwise AND.</summary>
    public CodeBuilder And() { EmitOpCode(OPCode.AND); return this; }

    /// <summary>Bitwise OR.</summary>
    public CodeBuilder Or() { EmitOpCode(OPCode.OR); return this; }

    /// <summary>Bitwise XOR.</summary>
    public CodeBuilder Xor() { EmitOpCode(OPCode.XOR); return this; }

    /// <summary>Bitwise NOT.</summary>
    public CodeBuilder Not() { EmitOpCode(OPCode.NOT); return this; }

    /// <summary>Left bit shift.</summary>
    public CodeBuilder Shl() { EmitOpCode(OPCode.SHL); return this; }

    /// <summary>Right bit shift.</summary>
    public CodeBuilder Shr() { EmitOpCode(OPCode.SHR); return this; }

    // ===== Comparison =====

    /// <summary>Equal comparison.</summary>
    public CodeBuilder Eq() { EmitOpCode(OPCode.EQ); return this; }

    /// <summary>Not equal comparison.</summary>
    public CodeBuilder Neq() { EmitOpCode(OPCode.NEQ); return this; }

    /// <summary>Less than comparison.</summary>
    public CodeBuilder Lt() { EmitOpCode(OPCode.LT); return this; }

    /// <summary>Less than or equal comparison.</summary>
    public CodeBuilder Lte() { EmitOpCode(OPCode.LTE); return this; }

    /// <summary>Greater than comparison.</summary>
    public CodeBuilder Gt() { EmitOpCode(OPCode.GT); return this; }

    /// <summary>Greater than or equal comparison.</summary>
    public CodeBuilder Gte() { EmitOpCode(OPCode.GTE); return this; }

    // ===== Memory Access =====

    /// <summary>Fetch cell from memory.</summary>
    public CodeBuilder Fetch() { EmitOpCode(OPCode.FETCH); return this; }

    /// <summary>Store cell to memory.</summary>
    public CodeBuilder Store() { EmitOpCode(OPCode.STORE); return this; }

    /// <summary>Fetch byte from memory.</summary>
    public CodeBuilder FetchByte() { EmitOpCode(OPCode.FETCH_BYTE); return this; }

    /// <summary>Store byte to memory.</summary>
    public CodeBuilder StoreByte() { EmitOpCode(OPCode.STORE_BYTE); return this; }

    /// <summary>Fetch 16-bit short from memory.</summary>
    public CodeBuilder FetchShort() { EmitOpCode(OPCode.FETCH_SHORT); return this; }

    /// <summary>Store 16-bit short to memory.</summary>
    public CodeBuilder StoreShort() { EmitOpCode(OPCode.STORE_SHORT); return this; }

    /// <summary>Fetch 32-bit int from memory.</summary>
    public CodeBuilder FetchInt() { EmitOpCode(OPCode.FETCH_INT); return this; }

    /// <summary>Store 32-bit int to memory.</summary>
    public CodeBuilder StoreInt() { EmitOpCode(OPCode.STORE_INT); return this; }

    // ===== Float Operations =====

    /// <summary>Duplicate float stack top.</summary>
    public CodeBuilder FDup() { EmitOpCode(OPCode.FDUP); return this; }

    /// <summary>Drop float stack top.</summary>
    public CodeBuilder FDrop() { EmitOpCode(OPCode.FDROP); return this; }

    /// <summary>Swap float stack top two.</summary>
    public CodeBuilder FSwap() { EmitOpCode(OPCode.FSWAP); return this; }

    /// <summary>Copy second float to top.</summary>
    public CodeBuilder FOver() { EmitOpCode(OPCode.FOVER); return this; }

    /// <summary>Float addition.</summary>
    public CodeBuilder FAdd() { EmitOpCode(OPCode.FADD); return this; }

    /// <summary>Float subtraction.</summary>
    public CodeBuilder FSub() { EmitOpCode(OPCode.FSUB); return this; }

    /// <summary>Float multiplication.</summary>
    public CodeBuilder FMul() { EmitOpCode(OPCode.FMUL); return this; }

    /// <summary>Float division.</summary>
    public CodeBuilder FDiv() { EmitOpCode(OPCode.FDIV); return this; }

    /// <summary>Float negation.</summary>
    public CodeBuilder FNeg() { EmitOpCode(OPCode.FNEG); return this; }

    /// <summary>Float equal comparison.</summary>
    public CodeBuilder FEq() { EmitOpCode(OPCode.FEQ); return this; }

    /// <summary>Float not equal comparison.</summary>
    public CodeBuilder FNeq() { EmitOpCode(OPCode.FNEQ); return this; }

    /// <summary>Float less than comparison.</summary>
    public CodeBuilder FLt() { EmitOpCode(OPCode.FLT); return this; }

    /// <summary>Float less than or equal comparison.</summary>
    public CodeBuilder FLte() { EmitOpCode(OPCode.FLTE); return this; }

    /// <summary>Float greater than comparison.</summary>
    public CodeBuilder FGt() { EmitOpCode(OPCode.FGT); return this; }

    /// <summary>Float greater than or equal comparison.</summary>
    public CodeBuilder FGte() { EmitOpCode(OPCode.FGTE); return this; }

    /// <summary>Fetch float from memory.</summary>
    public CodeBuilder FFetch() { EmitOpCode(OPCode.FFETCH); return this; }

    /// <summary>Store float to memory.</summary>
    public CodeBuilder FStore() { EmitOpCode(OPCode.FSTORE); return this; }

    /// <summary>Convert cell to float.</summary>
    public CodeBuilder CellToFloat() { EmitOpCode(OPCode.CELL_TO_FLOAT); return this; }

    /// <summary>Convert float to cell.</summary>
    public CodeBuilder FloatToCell() { EmitOpCode(OPCode.FLOAT_TO_CELL); return this; }

    // ===== Control Flow =====

    /// <summary>Unconditional jump to address.</summary>
    public CodeBuilder Jmp(long address)
    {
        EmitOpCode(OPCode.JMP);
        EmitLong(address);
        return this;
    }

    /// <summary>Jump to label (resolved at build time).</summary>
    public CodeBuilder Jmp(string label)
    {
        EmitOpCode(OPCode.JMP);
        _unresolvedJumps.Add((_bytecode.Count, label));
        EmitLong(0); // Placeholder
        return this;
    }

    /// <summary>Jump if zero to address.</summary>
    public CodeBuilder Jz(long address)
    {
        EmitOpCode(OPCode.JZ);
        EmitLong(address);
        return this;
    }

    /// <summary>Jump if zero to label.</summary>
    public CodeBuilder Jz(string label)
    {
        EmitOpCode(OPCode.JZ);
        _unresolvedJumps.Add((_bytecode.Count, label));
        EmitLong(0); // Placeholder
        return this;
    }

    /// <summary>Jump if not zero to address.</summary>
    public CodeBuilder Jnz(long address)
    {
        EmitOpCode(OPCode.JNZ);
        EmitLong(address);
        return this;
    }

    /// <summary>Jump if not zero to label.</summary>
    public CodeBuilder Jnz(string label)
    {
        EmitOpCode(OPCode.JNZ);
        _unresolvedJumps.Add((_bytecode.Count, label));
        EmitLong(0); // Placeholder
        return this;
    }

    /// <summary>Call function at address.</summary>
    public CodeBuilder Call(long address)
    {
        EmitOpCode(OPCode.CALL);
        EmitLong(address);
        return this;
    }

    /// <summary>Call function at label.</summary>
    public CodeBuilder Call(string label)
    {
        EmitOpCode(OPCode.CALL);
        _unresolvedJumps.Add((_bytecode.Count, label));
        EmitLong(0); // Placeholder
        return this;
    }

    /// <summary>Return from function.</summary>
    public CodeBuilder Ret() { EmitOpCode(OPCode.RET); return this; }

    /// <summary>Halt execution.</summary>
    public CodeBuilder Halt() { EmitOpCode(OPCode.HALT); return this; }

    /// <summary>No operation.</summary>
    public CodeBuilder Nop() { EmitOpCode(OPCode.NOP); return this; }

    /// <summary>System call with identifier.</summary>
    public CodeBuilder Syscall() { EmitOpCode(OPCode.SYSCALL); return this; }

    // ===== Label Management =====

    /// <summary>
    /// Defines a label at the current position in the bytecode.
    /// </summary>
    public CodeBuilder Label(string name)
    {
        if (_labels.ContainsKey(name))
            throw new InvalidOperationException($"Label '{name}' already defined.");
        _labels[name] = _bytecode.Count;
        return this;
    }

    // ===== Building =====

    /// <summary>
    /// Builds the final bytecode array, resolving all labels.
    /// </summary>
    public byte[] Build()
    {
        // Resolve all label references
        foreach (var (position, label) in _unresolvedJumps)
        {
            if (!_labels.TryGetValue(label, out int targetAddress))
                throw new InvalidOperationException($"Undefined label: '{label}'");

            // Write the resolved address (8 bytes for long)
            byte[] addressBytes = BitConverter.GetBytes((long)targetAddress);
            for (int i = 0; i < 8; i++)
            {
                _bytecode[position + i] = addressBytes[i];
            }
        }

        return [.. _bytecode];
    }

    /// <summary>
    /// Gets the current bytecode size.
    /// </summary>
    public int Size => _bytecode.Count;

    // ===== Helper Methods =====

    private void EmitOpCode(OPCode opcode)
    {
        _bytecode.Add((byte)opcode);
    }

    private void EmitByte(byte value)
    {
        _bytecode.Add(value);
    }

    private void EmitLong(long value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        // Enforce little-endian
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        _bytecode.AddRange(bytes);
    }

    private void EmitDouble(double value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        // Enforce little-endian
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        _bytecode.AddRange(bytes);
    }

    /// <summary>
    /// Clears all bytecode and labels, allowing the builder to be reused.
    /// </summary>
    public CodeBuilder Clear()
    {
        _bytecode.Clear();
        _labels.Clear();
        _unresolvedJumps.Clear();
        return this;
    }

    /// <summary>
    /// Returns a human-readable representation of the current bytecode.
    /// </summary>
    public override string ToString()
    {
        return $"CodeBuilder: {_bytecode.Count} bytes, {_labels.Count} labels";
    }
}
