using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AyanamisTower.MemoryExtensions;

namespace StellaLang;

/// <summary>
/// Delegate for opcode handlers that take a VM instance as parameter.
/// This allows static lambdas without closure captures for better JIT optimization.
/// </summary>
public delegate void OpHandler(VM vm);

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
    /// Instruction dispatch table - array-based for fast opcode execution.
    /// Index by opcode byte value for O(1) lookup without hash computation.
    /// Uses static delegates to avoid closure captures and enable JIT inlining.
    /// </summary>
    private static readonly OpHandler[] _dispatchTable = InitializeDispatchTable();

    /// <summary>
    /// System call handlers - maps syscall IDs to native functions.
    /// </summary>
    public Dictionary<long, Action<VM>> SyscallHandlers = [];

    /// <summary>
    /// Optional trace hook called before each instruction is executed.
    /// Useful for debugging, logging, or single-stepping through programs.
    /// </summary>
    public Action<VM>? TraceHook;

    /// <summary>
    /// Flag indicating if the VM should halt.
    /// </summary>
    public bool Halted => _halted;

    private bool _halted;

    /// <summary>
    /// Initializes a new instance of the VM with default stack sizes.
    /// </summary>
    public VM() : this(4, 4, 4, 12)
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
    }

    /// <summary>
    /// Initializes the instruction dispatch table with all opcode implementations.
    /// Uses array-based dispatch for O(1) lookup without hash computation.
    /// Static lambdas with VM parameter avoid closure captures for better JIT optimization.
    /// </summary>
    private static OpHandler[] InitializeDispatchTable()
    {
        var table = new OpHandler[256];

        // Push operations
        table[(byte)OPCode.PUSH_CELL] = static vm => vm.DataStack.PushLong(vm.ReadLong());
        table[(byte)OPCode.FPUSH_DOUBLE] = static vm => vm.FloatStack.PushDouble(vm.ReadDouble());

        // Stack manipulation
        table[(byte)OPCode.DUP] = static vm =>
        {
            vm.CheckDataStackDepth(1, "DUP");
            vm.DataStack.PushLong(vm.DataStack.PeekLong());
        };
        table[(byte)OPCode.DROP] = static vm =>
        {
            vm.CheckDataStackDepth(1, "DROP");
            vm.DataStack.PopLong();
        };
        table[(byte)OPCode.SWAP] = static vm =>
        {
            vm.CheckDataStackDepth(2, "SWAP");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(b);
            vm.DataStack.PushLong(a);
        };
        table[(byte)OPCode.OVER] = static vm =>
        {
            vm.CheckDataStackDepth(2, "OVER");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a);
            vm.DataStack.PushLong(b);
            vm.DataStack.PushLong(a);
        };
        table[(byte)OPCode.ROT] = static vm =>
        {
            vm.CheckDataStackDepth(3, "ROT");
            long c = vm.DataStack.PopLong();
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(b);
            vm.DataStack.PushLong(c);
            vm.DataStack.PushLong(a);
        };

        // Return stack manipulation
        table[(byte)OPCode.TO_R] = static vm =>
        {
            vm.CheckDataStackDepth(1, "TO_R");
            vm.ReturnStack.PushLong(vm.DataStack.PopLong());
        };
        table[(byte)OPCode.R_FROM] = static vm =>
        {
            vm.CheckReturnStackDepth(1, "R_FROM");
            vm.DataStack.PushLong(vm.ReturnStack.PopLong());
        };
        table[(byte)OPCode.R_FETCH] = static vm =>
        {
            vm.CheckReturnStackDepth(1, "R_FETCH");
            vm.DataStack.PushLong(vm.ReturnStack.PeekLong());
        };

        // Arithmetic
        table[(byte)OPCode.ADD] = static vm =>
        {
            vm.CheckDataStackDepth(2, "ADD");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a + b);
        };
        table[(byte)OPCode.SUB] = static vm =>
        {
            vm.CheckDataStackDepth(2, "SUB");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a - b);
        };
        table[(byte)OPCode.MUL] = static vm =>
        {
            vm.CheckDataStackDepth(2, "MUL");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a * b);
        };
        table[(byte)OPCode.DIV] = static vm =>
        {
            vm.CheckDataStackDepth(2, "DIV");
            long b = vm.DataStack.PopLong();
            if (b == 0)
                throw new DivideByZeroException("Division by zero in DIV operation");
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a / b);
        };
        table[(byte)OPCode.MOD] = static vm =>
        {
            vm.CheckDataStackDepth(2, "MOD");
            long b = vm.DataStack.PopLong();
            if (b == 0)
                throw new DivideByZeroException("Division by zero in MOD operation");
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a % b);
        };
        table[(byte)OPCode.DIVMOD] = static vm =>
        {
            vm.CheckDataStackDepth(2, "DIVMOD");
            long b = vm.DataStack.PopLong();
            if (b == 0)
                throw new DivideByZeroException("Division by zero in DIVMOD operation");
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a % b);  // remainder first (Forth convention)
            vm.DataStack.PushLong(a / b);  // quotient second 
        };
        table[(byte)OPCode.NEG] = static vm =>
        {
            vm.CheckDataStackDepth(1, "NEG");
            vm.DataStack.PushLong(-vm.DataStack.PopLong());
        };

        // Bitwise operations
        table[(byte)OPCode.AND] = static vm =>
        {
            vm.CheckDataStackDepth(2, "AND");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a & b);
        };
        table[(byte)OPCode.OR] = static vm =>
        {
            vm.CheckDataStackDepth(2, "OR");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a | b);
        };
        table[(byte)OPCode.XOR] = static vm =>
        {
            vm.CheckDataStackDepth(2, "XOR");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a ^ b);
        };
        table[(byte)OPCode.NOT] = static vm =>
        {
            vm.CheckDataStackDepth(1, "NOT");
            vm.DataStack.PushLong(~vm.DataStack.PopLong());
        };
        table[(byte)OPCode.SHL] = static vm =>
        {
            vm.CheckDataStackDepth(2, "SHL");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a << (int)b);
        };
        table[(byte)OPCode.SHR] = static vm =>
        {
            vm.CheckDataStackDepth(2, "SHR");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a >> (int)b);
        };

        // Comparison
        table[(byte)OPCode.EQ] = static vm =>
        {
            vm.CheckDataStackDepth(2, "EQ");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a == b ? 1 : 0);
        };
        table[(byte)OPCode.NEQ] = static vm =>
        {
            vm.CheckDataStackDepth(2, "NEQ");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a != b ? 1 : 0);
        };
        table[(byte)OPCode.LT] = static vm =>
        {
            vm.CheckDataStackDepth(2, "LT");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a < b ? 1 : 0);
        };
        table[(byte)OPCode.LTE] = static vm =>
        {
            vm.CheckDataStackDepth(2, "LTE");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a <= b ? 1 : 0);
        };
        table[(byte)OPCode.GT] = static vm =>
        {
            vm.CheckDataStackDepth(2, "GT");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a > b ? 1 : 0);
        };
        table[(byte)OPCode.GTE] = static vm =>
        {
            vm.CheckDataStackDepth(2, "GTE");
            long b = vm.DataStack.PopLong();
            long a = vm.DataStack.PopLong();
            vm.DataStack.PushLong(a >= b ? 1 : 0);
        };

        // Memory access - cell (64-bit)
        table[(byte)OPCode.FETCH] = static vm =>
        {
            vm.CheckDataStackDepth(1, "FETCH");
            long addr = vm.DataStack.PopLong();
            vm.ValidateMemoryAccess(addr, 8, "FETCH");
            long value = MemoryMarshal.Read<long>(vm.Memory.Memory.Span.Slice((int)addr, 8));
            vm.DataStack.PushLong(value);
        };
        table[(byte)OPCode.STORE] = static vm =>
        {
            vm.CheckDataStackDepth(2, "STORE");
            long addr = vm.DataStack.PopLong();
            long value = vm.DataStack.PopLong();
            vm.ValidateMemoryAccess(addr, 8, "STORE");
            MemoryMarshal.Write(vm.Memory.Memory.Span.Slice((int)addr, 8), in value);
        };

        // Memory access - byte
        table[(byte)OPCode.FETCH_BYTE] = static vm =>
        {
            vm.CheckDataStackDepth(1, "FETCH_BYTE");
            long addr = vm.DataStack.PopLong();
            vm.ValidateMemoryAccess(addr, 1, "FETCH_BYTE");
            vm.DataStack.PushLong(vm.Memory.Memory.Span[(int)addr]);
        };
        table[(byte)OPCode.STORE_BYTE] = static vm =>
        {
            vm.CheckDataStackDepth(2, "STORE_BYTE");
            long addr = vm.DataStack.PopLong();
            long value = vm.DataStack.PopLong();
            vm.ValidateMemoryAccess(addr, 1, "STORE_BYTE");
            vm.Memory.Memory.Span[(int)addr] = (byte)value;
        };

        // Memory access - word (16-bit)
        table[(byte)OPCode.FETCH_WORD] = static vm =>
        {
            vm.CheckDataStackDepth(1, "FETCH_WORD");
            long addr = vm.DataStack.PopLong();
            vm.ValidateMemoryAccess(addr, 2, "FETCH_WORD");
            short value = MemoryMarshal.Read<short>(vm.Memory.Memory.Span.Slice((int)addr, 2));
            vm.DataStack.PushLong(value);
        };
        table[(byte)OPCode.STORE_WORD] = static vm =>
        {
            vm.CheckDataStackDepth(2, "STORE_WORD");
            long addr = vm.DataStack.PopLong();
            long value = vm.DataStack.PopLong();
            vm.ValidateMemoryAccess(addr, 2, "STORE_WORD");
            short shortValue = (short)value;
            MemoryMarshal.Write(vm.Memory.Memory.Span.Slice((int)addr, 2), in shortValue);
        };

        // Memory access - long (32-bit)
        table[(byte)OPCode.FETCH_LONG] = static vm =>
        {
            vm.CheckDataStackDepth(1, "FETCH_LONG");
            long addr = vm.DataStack.PopLong();
            vm.ValidateMemoryAccess(addr, 4, "FETCH_LONG");
            int value = MemoryMarshal.Read<int>(vm.Memory.Memory.Span.Slice((int)addr, 4));
            vm.DataStack.PushLong(value);
        };
        table[(byte)OPCode.STORE_LONG] = static vm =>
        {
            vm.CheckDataStackDepth(2, "STORE_LONG");
            long addr = vm.DataStack.PopLong();
            long value = vm.DataStack.PopLong();
            vm.ValidateMemoryAccess(addr, 4, "STORE_LONG");
            int intValue = (int)value;
            MemoryMarshal.Write(vm.Memory.Memory.Span.Slice((int)addr, 4), in intValue);
        };

        // Float operations
        table[(byte)OPCode.FDUP] = static vm =>
        {
            vm.CheckFloatStackDepth(1, "FDUP");
            vm.FloatStack.PushDouble(vm.FloatStack.PeekDouble());
        };
        table[(byte)OPCode.FDROP] = static vm =>
        {
            vm.CheckFloatStackDepth(1, "FDROP");
            vm.FloatStack.PopDouble();
        };
        table[(byte)OPCode.FSWAP] = static vm =>
        {
            vm.CheckFloatStackDepth(2, "FSWAP");
            double b = vm.FloatStack.PopDouble();
            double a = vm.FloatStack.PopDouble();
            vm.FloatStack.PushDouble(b);
            vm.FloatStack.PushDouble(a);
        };
        table[(byte)OPCode.FOVER] = static vm =>
        {
            vm.CheckFloatStackDepth(2, "FOVER");
            double b = vm.FloatStack.PopDouble();
            double a = vm.FloatStack.PopDouble();
            vm.FloatStack.PushDouble(a);
            vm.FloatStack.PushDouble(b);
            vm.FloatStack.PushDouble(a);
        };

        // Float arithmetic
        table[(byte)OPCode.FADD] = static vm =>
        {
            vm.CheckFloatStackDepth(2, "FADD");
            double b = vm.FloatStack.PopDouble();
            double a = vm.FloatStack.PopDouble();
            vm.FloatStack.PushDouble(a + b);
        };
        table[(byte)OPCode.FSUB] = static vm =>
        {
            vm.CheckFloatStackDepth(2, "FSUB");
            double b = vm.FloatStack.PopDouble();
            double a = vm.FloatStack.PopDouble();
            vm.FloatStack.PushDouble(a - b);
        };
        table[(byte)OPCode.FMUL] = static vm =>
        {
            vm.CheckFloatStackDepth(2, "FMUL");
            double b = vm.FloatStack.PopDouble();
            double a = vm.FloatStack.PopDouble();
            vm.FloatStack.PushDouble(a * b);
        };
        table[(byte)OPCode.FDIV] = static vm =>
        {
            vm.CheckFloatStackDepth(2, "FDIV");
            double b = vm.FloatStack.PopDouble();
            double a = vm.FloatStack.PopDouble();
            vm.FloatStack.PushDouble(a / b);
        };
        table[(byte)OPCode.FNEG] = static vm =>
        {
            vm.CheckFloatStackDepth(1, "FNEG");
            vm.FloatStack.PushDouble(-vm.FloatStack.PopDouble());
        };

        // Float comparison
        table[(byte)OPCode.FEQ] = static vm =>
        {
            vm.CheckFloatStackDepth(2, "FEQ");
            double b = vm.FloatStack.PopDouble();
            double a = vm.FloatStack.PopDouble();
            vm.DataStack.PushLong(a == b ? 1 : 0);
        };
        table[(byte)OPCode.FNEQ] = static vm =>
        {
            vm.CheckFloatStackDepth(2, "FNEQ");
            double b = vm.FloatStack.PopDouble();
            double a = vm.FloatStack.PopDouble();
            vm.DataStack.PushLong(a != b ? 1 : 0);
        };
        table[(byte)OPCode.FLT] = static vm =>
        {
            vm.CheckFloatStackDepth(2, "FLT");
            double b = vm.FloatStack.PopDouble();
            double a = vm.FloatStack.PopDouble();
            vm.DataStack.PushLong(a < b ? 1 : 0);
        };
        table[(byte)OPCode.FLTE] = static vm =>
        {
            vm.CheckFloatStackDepth(2, "FLTE");
            double b = vm.FloatStack.PopDouble();
            double a = vm.FloatStack.PopDouble();
            vm.DataStack.PushLong(a <= b ? 1 : 0);
        };
        table[(byte)OPCode.FGT] = static vm =>
        {
            vm.CheckFloatStackDepth(2, "FGT");
            double b = vm.FloatStack.PopDouble();
            double a = vm.FloatStack.PopDouble();
            vm.DataStack.PushLong(a > b ? 1 : 0);
        };
        table[(byte)OPCode.FGTE] = static vm =>
        {
            vm.CheckFloatStackDepth(2, "FGTE");
            double b = vm.FloatStack.PopDouble();
            double a = vm.FloatStack.PopDouble();
            vm.DataStack.PushLong(a >= b ? 1 : 0);
        };

        // Float memory access
        table[(byte)OPCode.FFETCH] = static vm =>
        {
            vm.CheckDataStackDepth(1, "FFETCH");
            long addr = vm.DataStack.PopLong();
            vm.ValidateMemoryAccess(addr, 8, "FFETCH");
            double value = MemoryMarshal.Read<double>(vm.Memory.Memory.Span.Slice((int)addr, 8));
            vm.FloatStack.PushDouble(value);
        };
        table[(byte)OPCode.FSTORE] = static vm =>
        {
            vm.CheckDataStackDepth(1, "FSTORE");
            vm.CheckFloatStackDepth(1, "FSTORE");
            long addr = vm.DataStack.PopLong();
            double value = vm.FloatStack.PopDouble();
            vm.ValidateMemoryAccess(addr, 8, "FSTORE");
            MemoryMarshal.Write(vm.Memory.Memory.Span.Slice((int)addr, 8), in value);
        };

        // Type conversion
        table[(byte)OPCode.CELL_TO_FLOAT] = static vm =>
        {
            vm.CheckDataStackDepth(1, "CELL_TO_FLOAT");
            vm.FloatStack.PushDouble(vm.DataStack.PopLong());
        };
        table[(byte)OPCode.FLOAT_TO_CELL] = static vm =>
        {
            vm.CheckFloatStackDepth(1, "FLOAT_TO_CELL");
            vm.DataStack.PushLong((long)vm.FloatStack.PopDouble());
        };

        // Control flow
        table[(byte)OPCode.JMP] = static vm =>
        {
            long addr = vm.ReadLong();
            vm.ValidateJumpTarget(addr, "JMP");
            vm.PC = (int)addr;
        };
        table[(byte)OPCode.JZ] = static vm =>
        {
            vm.CheckDataStackDepth(1, "JZ");
            long addr = vm.ReadLong();
            if (vm.DataStack.PopLong() == 0)
            {
                vm.ValidateJumpTarget(addr, "JZ");
                vm.PC = (int)addr;
            }
        };
        table[(byte)OPCode.JNZ] = static vm =>
        {
            vm.CheckDataStackDepth(1, "JNZ");
            long addr = vm.ReadLong();
            if (vm.DataStack.PopLong() != 0)
            {
                vm.ValidateJumpTarget(addr, "JNZ");
                vm.PC = (int)addr;
            }
        };
        table[(byte)OPCode.CALL] = static vm =>
        {
            long addr = vm.ReadLong();
            vm.ValidateJumpTarget(addr, "CALL");
            vm.ReturnStack.PushLong(vm.PC);
            vm.PC = (int)addr;
        };
        table[(byte)OPCode.RET] = static vm =>
        {
            vm.CheckReturnStackDepth(1, "RET");
            long addr = vm.ReturnStack.PopLong();
            vm.ValidateJumpTarget(addr, "RET");
            vm.PC = (int)addr;
        };
        table[(byte)OPCode.HALT] = static vm => vm._halted = true;
        table[(byte)OPCode.NOP] = static vm => { /* Do nothing */ };

        // System call
        table[(byte)OPCode.SYSCALL] = static vm =>
        {
            vm.CheckDataStackDepth(1, "SYSCALL");
            long syscallId = vm.DataStack.PopLong();
            if (vm.SyscallHandlers.TryGetValue(syscallId, out var handler))
                handler(vm);
            else
                throw new InvalidOperationException($"Unknown syscall: {syscallId}");
        };

        return table;
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

        // Call trace hook before executing instruction (if registered)
        TraceHook?.Invoke(this);

        byte opcodeByte = _bytecode[PC++];
        OpHandler? handler = _dispatchTable[opcodeByte];

        if (handler != null)
        {
            handler(this);
        }
        else
        {
            throw new InvalidOperationException($"Unknown opcode: {opcodeByte} at PC={PC - 1}");
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
        /*
        Since the underlying Memory<byte> and arrays in .NET are limited to int.MaxValue elements, the VM is effectively limited to a 2GB address space.
        */
        
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
        /*
        Since the underlying Memory<byte> and arrays in .NET are limited to int.MaxValue elements, the VM is effectively limited to a 2GB address space.
        */

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
    /// Provides a comprehensive summary of PC, halt status, and stack depths.
    /// </summary>
    public override string ToString()
    {
        int dataDepth = DataStack.Pointer / sizeof(long);
        int floatDepth = FloatStack.Pointer / sizeof(double);
        int returnDepth = ReturnStack.Pointer / sizeof(long);

        string opcodeInfo = "";
        if (PC < _bytecode.Length)
        {
            byte opcode = _bytecode[PC];
            opcodeInfo = $", NextOp=0x{opcode:X2}";
        }

        return $"VM [PC={PC}/{_bytecode.Length}, Halted={_halted}{opcodeInfo}, " +
               $"DataStack={dataDepth}, FloatStack={floatDepth}, ReturnStack={returnDepth}]";
    }

    /// <summary>
    /// Gets detailed VM state including stack contents for deep debugging.
    /// </summary>
    /// <param name="maxStackItems">Maximum number of stack items to display per stack (default: 5).</param>
    /// <returns>Detailed string representation of VM state.</returns>
    public string ToDetailedString(int maxStackItems = 5)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== VM State ===");
        sb.AppendLine($"PC: {PC}/{_bytecode.Length}");
        sb.AppendLine($"Halted: {_halted}");

        if (PC < _bytecode.Length)
        {
            byte opcode = _bytecode[PC];
            sb.AppendLine($"Next Opcode: 0x{opcode:X2} ({(OPCode)opcode})");
        }

        // Data stack
        int dataDepth = DataStack.Pointer / sizeof(long);
        sb.AppendLine($"\nData Stack (depth={dataDepth}):");
        if (dataDepth > 0)
        {
            int itemsToShow = Math.Min(dataDepth, maxStackItems);
            for (int i = 0; i < itemsToShow; i++)
            {
                int offset = DataStack.Pointer - (i + 1) * sizeof(long);
                long value = BitConverter.ToInt64(DataStack.Memory.Span.Slice(offset, sizeof(long)));
                sb.AppendLine($"  [{dataDepth - i - 1}]: {value} (0x{value:X})");
            }
            if (dataDepth > maxStackItems)
            {
                sb.AppendLine($"  ... ({dataDepth - maxStackItems} more items)");
            }
        }
        else
        {
            sb.AppendLine("  (empty)");
        }

        // Float stack
        int floatDepth = FloatStack.Pointer / sizeof(double);
        sb.AppendLine($"\nFloat Stack (depth={floatDepth}):");
        if (floatDepth > 0)
        {
            int itemsToShow = Math.Min(floatDepth, maxStackItems);
            for (int i = 0; i < itemsToShow; i++)
            {
                int offset = FloatStack.Pointer - (i + 1) * sizeof(double);
                double value = BitConverter.ToDouble(FloatStack.Memory.Span.Slice(offset, sizeof(double)));
                sb.AppendLine($"  [{floatDepth - i - 1}]: {value}");
            }
            if (floatDepth > maxStackItems)
            {
                sb.AppendLine($"  ... ({floatDepth - maxStackItems} more items)");
            }
        }
        else
        {
            sb.AppendLine("  (empty)");
        }

        // Return stack
        int returnDepth = ReturnStack.Pointer / sizeof(long);
        sb.AppendLine($"\nReturn Stack (depth={returnDepth}):");
        if (returnDepth > 0)
        {
            int itemsToShow = Math.Min(returnDepth, maxStackItems);
            for (int i = 0; i < itemsToShow; i++)
            {
                int offset = ReturnStack.Pointer - (i + 1) * sizeof(long);
                long value = BitConverter.ToInt64(ReturnStack.Memory.Span.Slice(offset, sizeof(long)));
                sb.AppendLine($"  [{returnDepth - i - 1}]: {value} (PC)");
            }
            if (returnDepth > maxStackItems)
            {
                sb.AppendLine($"  ... ({returnDepth - maxStackItems} more items)");
            }
        }
        else
        {
            sb.AppendLine("  (empty)");
        }

        sb.AppendLine("================");
        return sb.ToString();
    }
}
