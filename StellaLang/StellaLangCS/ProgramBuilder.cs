using System;
using System.Collections.Generic;

namespace StellaLang;

/// <summary>
/// A fluent builder to compose programs that mix raw bytecode (Push/Op) and named word invocations.
/// It executes the program step-by-step on a provided VM, automatically segmenting bytecode
/// between word calls and preserving the stack across segments.
/// </summary>
public class ProgramBuilder
{
    private readonly List<IStep> _steps = new();
    private BytecodeBuilder? _current;

    private interface IStep { }
    private sealed record BytecodeChunk(byte[] Code) : IStep;
    private sealed record WordCall(string Name) : IStep;

    private void EnsureBytecode() => _current ??= new BytecodeBuilder();

    private void FlushBytecodeIfAny()
    {
        if (_current is null) return;
        var bytes = _current.Build();
        if (bytes.Length > 0)
            _steps.Add(new BytecodeChunk(bytes));
        _current = null;
    }

    /// <summary>
    /// Append a PUSH immediate value.
    /// </summary>
    public ProgramBuilder Push(long value)
    {
        EnsureBytecode();
        _current!.Push(value);
        return this;
    }

    /// <summary>
    /// Append a raw opcode (e.g., ADD, DUP, STORE, ...).
    /// </summary>
    public ProgramBuilder Op(OpCode opcode)
    {
        EnsureBytecode();
        _current!.Op(opcode);
        return this;
    }

    /// <summary>
    /// Append a named word call (built-in or user-defined).
    /// </summary>
    public ProgramBuilder Word(string name)
    {
        // close any pending bytecode segment before a word call
        FlushBytecodeIfAny();
        _steps.Add(new WordCall(name));
        return this;
    }

    /// <summary>
    /// Execute the composed program on the provided VM.
    /// Segments of bytecode are executed via LoadBytecode/Run, and word calls are invoked via ExecuteWord.
    /// </summary>
    public void Execute(VMActor vm)
    {
        // finalize trailing bytecode segment (if any)
        FlushBytecodeIfAny();

        for (int i = 0; i < _steps.Count; i++)
        {
            switch (_steps[i])
            {
                case BytecodeChunk chunk:
                    {
                        // Always HALT-terminate each chunk for safe execution
                        var bytes = new byte[chunk.Code.Length + 1];
                        Buffer.BlockCopy(chunk.Code, 0, bytes, 0, chunk.Code.Length);
                        bytes[^1] = (byte)OpCode.HALT;

                        vm.LoadBytecode(bytes);
                        vm.Run();
                        break;
                    }
                case WordCall call:
                    {
                        vm.ExecuteWord(call.Name);
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// Helper to both build and run on a VM in a single call.
    /// </summary>
    public ProgramBuilder RunOn(VMActor vm)
    {
        Execute(vm);
        return this;
    }
}
