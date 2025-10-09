using System;
using System.Collections.Generic;
using AyanamisTower.MemoryExtensions;

namespace StellaLang;

/// <summary>
/// A simple stack-based virtual machine for executing StellaLang bytecode.
/// Uses an instruction dispatch table for efficient opcode execution.
/// </summary>
public class VM
{
    /// <summary>
    /// Data stack for integer/cell operations.
    /// </summary>
    public MemoryStack DataStack = new(1024 * 8);

    /// <summary>
    /// Float stack for floating-point operations.
    /// </summary>
    public MemoryStack FloatStack = new(1024 * 8);

    /// <summary>
    /// Return stack for storing return addresses and temporary values.
    /// </summary>
    public MemoryStack ReturnStack = new(1024 * 8);

    /// <summary>
    /// VM memory for FETCH/STORE operations.
    /// </summary>
    public MemoryStack Memory = new(1024 * 64);

    /// <summary>
    /// The bytecode being executed.
    /// </summary>
    private byte[] _bytecode = [];

    /// <summary>
    /// The program counter - points to the next instruction to execute.
    /// </summary>
    public int PC = 0;

    /// <summary>
    /// Instruction dispatch table - maps opcodes to their implementation.
    /// </summary>
    private readonly Dictionary<OPCode, Action> _dispatchTable;

    /// <summary>
    /// System call handlers - maps syscall IDs to native functions.
    /// </summary>
    public Dictionary<long, Action<VM>> SyscallHandlers = new();

    /// <summary>
    /// Flag indicating if the VM should halt.
    /// </summary>
    private bool _halted;

    /// <summary>
    /// Initializes a new instance of the VM.
    /// </summary>
    public VM()
    {
        _dispatchTable = InitializeDispatchTable();
    }

    /// <summary>
    /// Initializes the instruction dispatch table with all opcode implementations.
    /// </summary>
    private Dictionary<OPCode, Action> InitializeDispatchTable()
    {
        return new Dictionary<OPCode, Action>
        {
            // Push operations
            [OPCode.PUSH_CELL] = () => DataStack.PushLong(ReadLong()),
            [OPCode.FPUSH_DOUBLE] = () => FloatStack.PushDouble(ReadDouble()),

            // Stack manipulation
            [OPCode.DUP] = () => DataStack.PushLong(DataStack.PeekLong()),
            [OPCode.DROP] = () => DataStack.PopLong(),
            [OPCode.SWAP] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(b);
                DataStack.PushLong(a);
            },
            [OPCode.OVER] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a);
                DataStack.PushLong(b);
                DataStack.PushLong(a);
            },
            [OPCode.ROT] = () =>
            {
                long c = DataStack.PopLong();
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(b);
                DataStack.PushLong(c);
                DataStack.PushLong(a);
            },

            // Return stack manipulation
            [OPCode.TO_R] = () => ReturnStack.PushLong(DataStack.PopLong()),
            [OPCode.R_FROM] = () => DataStack.PushLong(ReturnStack.PopLong()),
            [OPCode.R_FETCH] = () => DataStack.PushLong(ReturnStack.PeekLong()),

            // Arithmetic
            [OPCode.ADD] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a + b);
            },
            [OPCode.SUB] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a - b);
            },
            [OPCode.MUL] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a * b);
            },
            [OPCode.DIV] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a / b);
            },
            [OPCode.MOD] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a % b);
            },
            [OPCode.DIVMOD] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a % b);  // remainder
                DataStack.PushLong(a / b);  // quotient
            },
            [OPCode.NEG] = () => DataStack.PushLong(-DataStack.PopLong()),

            // Bitwise operations
            [OPCode.AND] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a & b);
            },
            [OPCode.OR] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a | b);
            },
            [OPCode.XOR] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a ^ b);
            },
            [OPCode.NOT] = () => DataStack.PushLong(~DataStack.PopLong()),
            [OPCode.SHL] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a << (int)b);
            },
            [OPCode.SHR] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a >> (int)b);
            },

            // Comparison
            [OPCode.EQ] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a == b ? 1 : 0);
            },
            [OPCode.NEQ] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a != b ? 1 : 0);
            },
            [OPCode.LT] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a < b ? 1 : 0);
            },
            [OPCode.LTE] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a <= b ? 1 : 0);
            },
            [OPCode.GT] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a > b ? 1 : 0);
            },
            [OPCode.GTE] = () =>
            {
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a >= b ? 1 : 0);
            },

            // Memory access - cell (64-bit)
            [OPCode.FETCH] = () =>
            {
                long addr = DataStack.PopLong();
                // Create a temporary pointer for reading
                int tempPtr = (int)addr;
                DataStack.PushLong(Memory.Memory.PopLong(ref tempPtr));
            },
            [OPCode.STORE] = () =>
            {
                long addr = DataStack.PopLong();
                long value = DataStack.PopLong();
                int tempPtr = (int)addr;
                Memory.Memory.PushLong(ref tempPtr, value);
            },

            // Memory access - byte
            [OPCode.FETCH_BYTE] = () =>
            {
                long addr = DataStack.PopLong();
                int tempPtr = (int)addr;
                DataStack.PushLong(Memory.Memory.Pop(ref tempPtr));
            },
            [OPCode.STORE_BYTE] = () =>
            {
                long addr = DataStack.PopLong();
                long value = DataStack.PopLong();
                int tempPtr = (int)addr;
                Memory.Memory.Push(ref tempPtr, (byte)value);
            },

            // Memory access - word (16-bit)
            [OPCode.FETCH_WORD] = () =>
            {
                long addr = DataStack.PopLong();
                int tempPtr = (int)addr;
                DataStack.PushLong(Memory.Memory.PopShort(ref tempPtr));
            },
            [OPCode.STORE_WORD] = () =>
            {
                long addr = DataStack.PopLong();
                long value = DataStack.PopLong();
                int tempPtr = (int)addr;
                Memory.Memory.PushShort(ref tempPtr, (short)value);
            },

            // Memory access - long (32-bit)
            [OPCode.FETCH_LONG] = () =>
            {
                long addr = DataStack.PopLong();
                int tempPtr = (int)addr;
                DataStack.PushLong(Memory.Memory.PopInt(ref tempPtr));
            },
            [OPCode.STORE_LONG] = () =>
            {
                long addr = DataStack.PopLong();
                long value = DataStack.PopLong();
                int tempPtr = (int)addr;
                Memory.Memory.PushInt(ref tempPtr, (int)value);
            },

            // Float operations
            [OPCode.FDUP] = () => FloatStack.PushDouble(FloatStack.PeekDouble()),
            [OPCode.FDROP] = () => FloatStack.PopDouble(),
            [OPCode.FSWAP] = () =>
            {
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                FloatStack.PushDouble(b);
                FloatStack.PushDouble(a);
            },
            [OPCode.FOVER] = () =>
            {
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                FloatStack.PushDouble(a);
                FloatStack.PushDouble(b);
                FloatStack.PushDouble(a);
            },

            // Float arithmetic
            [OPCode.FADD] = () =>
            {
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                FloatStack.PushDouble(a + b);
            },
            [OPCode.FSUB] = () =>
            {
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                FloatStack.PushDouble(a - b);
            },
            [OPCode.FMUL] = () =>
            {
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                FloatStack.PushDouble(a * b);
            },
            [OPCode.FDIV] = () =>
            {
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                FloatStack.PushDouble(a / b);
            },
            [OPCode.FNEG] = () => FloatStack.PushDouble(-FloatStack.PopDouble()),

            // Float comparison
            [OPCode.FEQ] = () =>
            {
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                DataStack.PushLong(a == b ? 1 : 0);
            },
            [OPCode.FNEQ] = () =>
            {
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                DataStack.PushLong(a != b ? 1 : 0);
            },
            [OPCode.FLT] = () =>
            {
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                DataStack.PushLong(a < b ? 1 : 0);
            },
            [OPCode.FLTE] = () =>
            {
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                DataStack.PushLong(a <= b ? 1 : 0);
            },
            [OPCode.FGT] = () =>
            {
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                DataStack.PushLong(a > b ? 1 : 0);
            },
            [OPCode.FGTE] = () =>
            {
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                DataStack.PushLong(a >= b ? 1 : 0);
            },

            // Float memory access
            [OPCode.FFETCH] = () =>
            {
                long addr = DataStack.PopLong();
                int tempPtr = (int)addr;
                FloatStack.PushDouble(Memory.Memory.PopDouble(ref tempPtr));
            },
            [OPCode.FSTORE] = () =>
            {
                long addr = DataStack.PopLong();
                double value = FloatStack.PopDouble();
                int tempPtr = (int)addr;
                Memory.Memory.PushDouble(ref tempPtr, value);
            },

            // Type conversion
            [OPCode.CELL_TO_FLOAT] = () => FloatStack.PushDouble(DataStack.PopLong()),
            [OPCode.FLOAT_TO_CELL] = () => DataStack.PushLong((long)FloatStack.PopDouble()),

            // Control flow
            [OPCode.JMP] = () => PC = (int)ReadLong(),
            [OPCode.JZ] = () =>
            {
                long addr = ReadLong();
                if (DataStack.PopLong() == 0)
                    PC = (int)addr;
            },
            [OPCode.JNZ] = () =>
            {
                long addr = ReadLong();
                if (DataStack.PopLong() != 0)
                    PC = (int)addr;
            },
            [OPCode.CALL] = () =>
            {
                long addr = ReadLong();
                ReturnStack.PushLong(PC);
                PC = (int)addr;
            },
            [OPCode.RET] = () => PC = (int)ReturnStack.PopLong(),
            [OPCode.HALT] = () => _halted = true,
            [OPCode.NOP] = () => { /* Do nothing */ },

            // System call
            [OPCode.SYSCALL] = () =>
            {
                long syscallId = DataStack.PopLong();
                if (SyscallHandlers.TryGetValue(syscallId, out var handler))
                    handler(this);
                else
                    throw new InvalidOperationException($"Unknown syscall: {syscallId}");
            }
        };
    }

    /// <summary>
    /// Loads bytecode into the VM for execution.
    /// </summary>
    public void Load(byte[] bytecode)
    {
        _bytecode = bytecode;
        PC = 0;
        _halted = false;
    }

    /// <summary>
    /// Executes a single instruction.
    /// </summary>
    public void Step()
    {
        if (_halted || PC >= _bytecode.Length)
            return;

        OPCode opcode = (OPCode)_bytecode[PC++];

        if (_dispatchTable.TryGetValue(opcode, out var handler))
        {
            handler();
        }
        else
        {
            throw new InvalidOperationException($"Unknown opcode: {opcode} at PC={PC - 1}");
        }
    }

    /// <summary>
    /// Executes bytecode until HALT is encountered or end of bytecode is reached.
    /// </summary>
    public void Execute(byte[] bytecode)
    {
        Load(bytecode);
        Execute();
    }

    /// <summary>
    /// Continues execution from current PC until HALT or end of bytecode.
    /// </summary>
    public void Execute()
    {
        while (!_halted && PC < _bytecode.Length)
        {
            Step();
        }
    }

    /// <summary>
    /// Resets the VM to initial state.
    /// </summary>
    public void Reset()
    {
        PC = 0;
        _halted = false;
        DataStack = new(1024 * 8);
        FloatStack = new(1024 * 8);
        ReturnStack = new(1024 * 8);
        Memory = new(1024 * 64);
    }

    // Helper methods for reading operands from bytecode

    private byte ReadByte()
    {
        return _bytecode[PC++];
    }

    private long ReadLong()
    {
        if (PC + 8 > _bytecode.Length)
            throw new InvalidOperationException("Unexpected end of bytecode while reading long");

        long value = BitConverter.ToInt64(_bytecode, PC);
        PC += 8;
        return value;
    }

    private double ReadDouble()
    {
        if (PC + 8 > _bytecode.Length)
            throw new InvalidOperationException("Unexpected end of bytecode while reading double");

        double value = BitConverter.ToDouble(_bytecode, PC);
        PC += 8;
        return value;
    }

    /// <summary>
    /// Gets the current state of the VM for debugging.
    /// </summary>
    public override string ToString()
    {
        return $"VM [PC={PC}, Halted={_halted}, DataStack depth={DataStack.Pointer}, FloatStack depth={FloatStack.Pointer}]";
    }
}
