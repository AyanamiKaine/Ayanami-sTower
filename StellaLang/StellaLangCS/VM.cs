using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
    public MemoryStack DataStack;

    /// <summary>
    /// Float stack for floating-point operations.
    /// </summary>
    public MemoryStack FloatStack;

    /// <summary>
    /// Return stack for storing return addresses and temporary values.
    /// </summary>
    public MemoryStack ReturnStack;

    /// <summary>
    /// VM memory for FETCH/STORE operations.
    /// </summary>
    public MemoryStack Memory;

    /// <summary>
    /// The bytecode being executed.
    /// </summary>
    private byte[] _bytecode = [];

    /// <summary>
    /// The program counter - points to the next instruction to execute.
    /// </summary>
    public int PC;

    /// <summary>
    /// Instruction dispatch table - maps opcodes to their implementation.
    /// </summary>
    private readonly Dictionary<OPCode, Action> _dispatchTable;

    /// <summary>
    /// System call handlers - maps syscall IDs to native functions.
    /// </summary>
    public Dictionary<long, Action<VM>> SyscallHandlers = [];

    /// <summary>
    /// Flag indicating if the VM should halt.
    /// </summary>
    private bool _halted;

    /// <summary>
    /// Initializes a new instance of the VM with default stack sizes.
    /// </summary>
    public VM() : this(40, 8, 12, 124)
    {
    }

    /// <summary>
    /// Initializes a new instance of the VM with custom stack sizes.
    /// </summary>
    /// <param name="dataStackMB">Data stack size in megabytes.</param>
    /// <param name="floatStackMB">Float stack size in megabytes.</param>
    /// <param name="returnStackMB">Return stack size in megabytes.</param>
    /// <param name="memoryMB">Memory size in megabytes.</param>
    public VM(int dataStackMB, int floatStackMB, int returnStackMB, int memoryMB)
    {
        DataStack = MemoryStack.FromMegabytes(dataStackMB);
        FloatStack = MemoryStack.FromMegabytes(floatStackMB);
        ReturnStack = MemoryStack.FromMegabytes(returnStackMB);
        Memory = MemoryStack.FromMegabytes(memoryMB);
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
            [OPCode.DUP] = () =>
            {
                CheckDataStackDepth(1, "DUP");
                DataStack.PushLong(DataStack.PeekLong());
            },
            [OPCode.DROP] = () =>
            {
                CheckDataStackDepth(1, "DROP");
                DataStack.PopLong();
            },
            [OPCode.SWAP] = () =>
            {
                CheckDataStackDepth(2, "SWAP");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(b);
                DataStack.PushLong(a);
            },
            [OPCode.OVER] = () =>
            {
                CheckDataStackDepth(2, "OVER");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a);
                DataStack.PushLong(b);
                DataStack.PushLong(a);
            },
            [OPCode.ROT] = () =>
            {
                CheckDataStackDepth(3, "ROT");
                long c = DataStack.PopLong();
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(b);
                DataStack.PushLong(c);
                DataStack.PushLong(a);
            },

            // Return stack manipulation
            [OPCode.TO_R] = () =>
            {
                CheckDataStackDepth(1, "TO_R");
                ReturnStack.PushLong(DataStack.PopLong());
            },
            [OPCode.R_FROM] = () =>
            {
                CheckReturnStackDepth(1, "R_FROM");
                DataStack.PushLong(ReturnStack.PopLong());
            },
            [OPCode.R_FETCH] = () =>
            {
                CheckReturnStackDepth(1, "R_FETCH");
                DataStack.PushLong(ReturnStack.PeekLong());
            },

            // Arithmetic
            [OPCode.ADD] = () =>
            {
                CheckDataStackDepth(2, "ADD");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a + b);
            },
            [OPCode.SUB] = () =>
            {
                CheckDataStackDepth(2, "SUB");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a - b);
            },
            [OPCode.MUL] = () =>
            {
                CheckDataStackDepth(2, "MUL");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a * b);
            },
            [OPCode.DIV] = () =>
            {
                CheckDataStackDepth(2, "DIV");
                long b = DataStack.PopLong();
                if (b == 0)
                    throw new DivideByZeroException("Division by zero in DIV operation");
                long a = DataStack.PopLong();
                DataStack.PushLong(a / b);
            },
            [OPCode.MOD] = () =>
            {
                CheckDataStackDepth(2, "MOD");
                long b = DataStack.PopLong();
                if (b == 0)
                    throw new DivideByZeroException("Division by zero in MOD operation");
                long a = DataStack.PopLong();
                DataStack.PushLong(a % b);
            },
            [OPCode.DIVMOD] = () =>
            {
                CheckDataStackDepth(2, "DIVMOD");
                long b = DataStack.PopLong();
                if (b == 0)
                    throw new DivideByZeroException("Division by zero in DIVMOD operation");
                long a = DataStack.PopLong();
                DataStack.PushLong(a / b);  // quotient first (Forth convention)
                DataStack.PushLong(a % b);  // remainder second
            },
            [OPCode.NEG] = () =>
            {
                CheckDataStackDepth(1, "NEG");
                DataStack.PushLong(-DataStack.PopLong());
            },

            // Bitwise operations
            [OPCode.AND] = () =>
            {
                CheckDataStackDepth(2, "AND");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a & b);
            },
            [OPCode.OR] = () =>
            {
                CheckDataStackDepth(2, "OR");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a | b);
            },
            [OPCode.XOR] = () =>
            {
                CheckDataStackDepth(2, "XOR");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a ^ b);
            },
            [OPCode.NOT] = () =>
            {
                CheckDataStackDepth(1, "NOT");
                DataStack.PushLong(~DataStack.PopLong());
            },
            [OPCode.SHL] = () =>
            {
                CheckDataStackDepth(2, "SHL");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a << (int)b);
            },
            [OPCode.SHR] = () =>
            {
                CheckDataStackDepth(2, "SHR");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a >> (int)b);
            },

            // Comparison
            [OPCode.EQ] = () =>
            {
                CheckDataStackDepth(2, "EQ");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a == b ? 1 : 0);
            },
            [OPCode.NEQ] = () =>
            {
                CheckDataStackDepth(2, "NEQ");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a != b ? 1 : 0);
            },
            [OPCode.LT] = () =>
            {
                CheckDataStackDepth(2, "LT");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a < b ? 1 : 0);
            },
            [OPCode.LTE] = () =>
            {
                CheckDataStackDepth(2, "LTE");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a <= b ? 1 : 0);
            },
            [OPCode.GT] = () =>
            {
                CheckDataStackDepth(2, "GT");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a > b ? 1 : 0);
            },
            [OPCode.GTE] = () =>
            {
                CheckDataStackDepth(2, "GTE");
                long b = DataStack.PopLong();
                long a = DataStack.PopLong();
                DataStack.PushLong(a >= b ? 1 : 0);
            },

            // Memory access - cell (64-bit)
            [OPCode.FETCH] = () =>
            {
                CheckDataStackDepth(1, "FETCH");
                long addr = DataStack.PopLong();
                ValidateMemoryAccess(addr, 8, "FETCH");
                long value = MemoryMarshal.Read<long>(Memory.Memory.Span.Slice((int)addr, 8));
                DataStack.PushLong(value);
            },
            [OPCode.STORE] = () =>
            {
                CheckDataStackDepth(2, "STORE");
                long addr = DataStack.PopLong();
                long value = DataStack.PopLong();
                ValidateMemoryAccess(addr, 8, "STORE");
                MemoryMarshal.Write(Memory.Memory.Span.Slice((int)addr, 8), in value);
            },

            // Memory access - byte
            [OPCode.FETCH_BYTE] = () =>
            {
                CheckDataStackDepth(1, "FETCH_BYTE");
                long addr = DataStack.PopLong();
                ValidateMemoryAccess(addr, 1, "FETCH_BYTE");
                DataStack.PushLong(Memory.Memory.Span[(int)addr]);
            },
            [OPCode.STORE_BYTE] = () =>
            {
                CheckDataStackDepth(2, "STORE_BYTE");
                long addr = DataStack.PopLong();
                long value = DataStack.PopLong();
                ValidateMemoryAccess(addr, 1, "STORE_BYTE");
                Memory.Memory.Span[(int)addr] = (byte)value;
            },

            // Memory access - word (16-bit)
            [OPCode.FETCH_WORD] = () =>
            {
                CheckDataStackDepth(1, "FETCH_WORD");
                long addr = DataStack.PopLong();
                ValidateMemoryAccess(addr, 2, "FETCH_WORD");
                short value = MemoryMarshal.Read<short>(Memory.Memory.Span.Slice((int)addr, 2));
                DataStack.PushLong(value);
            },
            [OPCode.STORE_WORD] = () =>
            {
                CheckDataStackDepth(2, "STORE_WORD");
                long addr = DataStack.PopLong();
                long value = DataStack.PopLong();
                ValidateMemoryAccess(addr, 2, "STORE_WORD");
                short shortValue = (short)value;
                MemoryMarshal.Write(Memory.Memory.Span.Slice((int)addr, 2), in shortValue);
            },

            // Memory access - long (32-bit)
            [OPCode.FETCH_LONG] = () =>
            {
                CheckDataStackDepth(1, "FETCH_LONG");
                long addr = DataStack.PopLong();
                ValidateMemoryAccess(addr, 4, "FETCH_LONG");
                int value = MemoryMarshal.Read<int>(Memory.Memory.Span.Slice((int)addr, 4));
                DataStack.PushLong(value);
            },
            [OPCode.STORE_LONG] = () =>
            {
                CheckDataStackDepth(2, "STORE_LONG");
                long addr = DataStack.PopLong();
                long value = DataStack.PopLong();
                ValidateMemoryAccess(addr, 4, "STORE_LONG");
                int intValue = (int)value;
                MemoryMarshal.Write(Memory.Memory.Span.Slice((int)addr, 4), in intValue);
            },

            // Float operations
            [OPCode.FDUP] = () =>
            {
                CheckFloatStackDepth(1, "FDUP");
                FloatStack.PushDouble(FloatStack.PeekDouble());
            },
            [OPCode.FDROP] = () =>
            {
                CheckFloatStackDepth(1, "FDROP");
                FloatStack.PopDouble();
            },
            [OPCode.FSWAP] = () =>
            {
                CheckFloatStackDepth(2, "FSWAP");
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                FloatStack.PushDouble(b);
                FloatStack.PushDouble(a);
            },
            [OPCode.FOVER] = () =>
            {
                CheckFloatStackDepth(2, "FOVER");
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                FloatStack.PushDouble(a);
                FloatStack.PushDouble(b);
                FloatStack.PushDouble(a);
            },

            // Float arithmetic
            [OPCode.FADD] = () =>
            {
                CheckFloatStackDepth(2, "FADD");
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                FloatStack.PushDouble(a + b);
            },
            [OPCode.FSUB] = () =>
            {
                CheckFloatStackDepth(2, "FSUB");
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                FloatStack.PushDouble(a - b);
            },
            [OPCode.FMUL] = () =>
            {
                CheckFloatStackDepth(2, "FMUL");
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                FloatStack.PushDouble(a * b);
            },
            [OPCode.FDIV] = () =>
            {
                CheckFloatStackDepth(2, "FDIV");
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                FloatStack.PushDouble(a / b);
            },
            [OPCode.FNEG] = () =>
            {
                CheckFloatStackDepth(1, "FNEG");
                FloatStack.PushDouble(-FloatStack.PopDouble());
            },

            // Float comparison
            [OPCode.FEQ] = () =>
            {
                CheckFloatStackDepth(2, "FEQ");
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                DataStack.PushLong(a == b ? 1 : 0);
            },
            [OPCode.FNEQ] = () =>
            {
                CheckFloatStackDepth(2, "FNEQ");
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                DataStack.PushLong(a != b ? 1 : 0);
            },
            [OPCode.FLT] = () =>
            {
                CheckFloatStackDepth(2, "FLT");
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                DataStack.PushLong(a < b ? 1 : 0);
            },
            [OPCode.FLTE] = () =>
            {
                CheckFloatStackDepth(2, "FLTE");
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                DataStack.PushLong(a <= b ? 1 : 0);
            },
            [OPCode.FGT] = () =>
            {
                CheckFloatStackDepth(2, "FGT");
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                DataStack.PushLong(a > b ? 1 : 0);
            },
            [OPCode.FGTE] = () =>
            {
                CheckFloatStackDepth(2, "FGTE");
                double b = FloatStack.PopDouble();
                double a = FloatStack.PopDouble();
                DataStack.PushLong(a >= b ? 1 : 0);
            },

            // Float memory access
            [OPCode.FFETCH] = () =>
            {
                CheckDataStackDepth(1, "FFETCH");
                long addr = DataStack.PopLong();
                ValidateMemoryAccess(addr, 8, "FFETCH");
                double value = MemoryMarshal.Read<double>(Memory.Memory.Span.Slice((int)addr, 8));
                FloatStack.PushDouble(value);
            },
            [OPCode.FSTORE] = () =>
            {
                CheckDataStackDepth(1, "FSTORE");
                CheckFloatStackDepth(1, "FSTORE");
                long addr = DataStack.PopLong();
                double value = FloatStack.PopDouble();
                ValidateMemoryAccess(addr, 8, "FSTORE");
                MemoryMarshal.Write(Memory.Memory.Span.Slice((int)addr, 8), in value);
            },

            // Type conversion
            [OPCode.CELL_TO_FLOAT] = () =>
            {
                CheckDataStackDepth(1, "CELL_TO_FLOAT");
                FloatStack.PushDouble(DataStack.PopLong());
            },
            [OPCode.FLOAT_TO_CELL] = () =>
            {
                CheckFloatStackDepth(1, "FLOAT_TO_CELL");
                DataStack.PushLong((long)FloatStack.PopDouble());
            },

            // Control flow
            [OPCode.JMP] = () =>
            {
                long addr = ReadLong();
                ValidateJumpTarget(addr, "JMP");
                PC = (int)addr;
            },
            [OPCode.JZ] = () =>
            {
                CheckDataStackDepth(1, "JZ");
                long addr = ReadLong();
                if (DataStack.PopLong() == 0)
                {
                    ValidateJumpTarget(addr, "JZ");
                    PC = (int)addr;
                }
            },
            [OPCode.JNZ] = () =>
            {
                CheckDataStackDepth(1, "JNZ");
                long addr = ReadLong();
                if (DataStack.PopLong() != 0)
                {
                    ValidateJumpTarget(addr, "JNZ");
                    PC = (int)addr;
                }
            },
            [OPCode.CALL] = () =>
            {
                long addr = ReadLong();
                ValidateJumpTarget(addr, "CALL");
                ReturnStack.PushLong(PC);
                PC = (int)addr;
            },
            [OPCode.RET] = () =>
            {
                CheckReturnStackDepth(1, "RET");
                long addr = ReturnStack.PopLong();
                ValidateJumpTarget(addr, "RET");
                PC = (int)addr;
            },
            [OPCode.HALT] = () => _halted = true,
            [OPCode.NOP] = () => { /* Do nothing */ },

            // System call
            [OPCode.SYSCALL] = () =>
            {
                CheckDataStackDepth(1, "SYSCALL");
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

    // Helper methods for validation

    /// <summary>
    /// Validates that the data stack has at least the required depth.
    /// </summary>
    /// <param name="requiredDepth">The minimum number of elements required on the stack.</param>
    /// <param name="operation">The operation name for error reporting.</param>
    /// <exception cref="InvalidOperationException">Thrown when stack underflow would occur.</exception>
    private void CheckDataStackDepth(int requiredDepth, string operation)
    {
        int currentDepth = DataStack.Pointer / sizeof(long);
        if (currentDepth < requiredDepth)
        {
            throw new InvalidOperationException(
                $"Data stack underflow in {operation}: required {requiredDepth} elements, have {currentDepth}");
        }
    }

    /// <summary>
    /// Validates that the float stack has at least the required depth.
    /// </summary>
    /// <param name="requiredDepth">The minimum number of elements required on the stack.</param>
    /// <param name="operation">The operation name for error reporting.</param>
    /// <exception cref="InvalidOperationException">Thrown when stack underflow would occur.</exception>
    private void CheckFloatStackDepth(int requiredDepth, string operation)
    {
        int currentDepth = FloatStack.Pointer / sizeof(double);
        if (currentDepth < requiredDepth)
        {
            throw new InvalidOperationException(
                $"Float stack underflow in {operation}: required {requiredDepth} elements, have {currentDepth}");
        }
    }

    /// <summary>
    /// Validates that the return stack has at least the required depth.
    /// </summary>
    /// <param name="requiredDepth">The minimum number of elements required on the stack.</param>
    /// <param name="operation">The operation name for error reporting.</param>
    /// <exception cref="InvalidOperationException">Thrown when stack underflow would occur.</exception>
    private void CheckReturnStackDepth(int requiredDepth, string operation)
    {
        int currentDepth = ReturnStack.Pointer / sizeof(long);
        if (currentDepth < requiredDepth)
        {
            throw new InvalidOperationException(
                $"Return stack underflow in {operation}: required {requiredDepth} elements, have {currentDepth}");
        }
    }

    /// <summary>
    /// Validates that a memory access is within bounds.
    /// </summary>
    /// <param name="address">The memory address to access.</param>
    /// <param name="size">The number of bytes to access.</param>
    /// <param name="operation">The operation name for error reporting.</param>
    /// <exception cref="InvalidOperationException">Thrown when memory access is out of bounds.</exception>
    private void ValidateMemoryAccess(long address, int size, string operation)
    {
        if (address < 0 || address > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"Memory access violation in {operation}: address {address} out of valid range");
        }

        int addr = (int)address;
        if (addr + size > Memory.Memory.Length)
        {
            throw new InvalidOperationException(
                $"Memory access violation in {operation}: address {address} + size {size} exceeds memory bounds ({Memory.Memory.Length})");
        }
    }

    /// <summary>
    /// Validates that a jump target is within bytecode bounds.
    /// </summary>
    /// <param name="address">The jump target address.</param>
    /// <param name="operation">The operation name for error reporting.</param>
    /// <exception cref="InvalidOperationException">Thrown when jump target is invalid.</exception>
    private void ValidateJumpTarget(long address, string operation)
    {
        if (address < 0 || address > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"Invalid jump target in {operation}: address {address} out of valid range");
        }

        int addr = (int)address;
        if (addr >= _bytecode.Length)
        {
            throw new InvalidOperationException(
                $"Invalid jump target in {operation}: address {address} exceeds bytecode length ({_bytecode.Length})");
        }
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
