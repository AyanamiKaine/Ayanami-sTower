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
    /// The word definition currently being executed.
    /// Set by the interpreter when executing colon definitions to enable
    /// runtime introspection for features like DOES>.
    /// </summary>
    public object? CurrentlyExecutingWord;

    /// <summary>
    /// Flag indicating if the VM should halt.
    /// </summary>
    public bool Halted => _halted;

    private bool _halted;

    /// <summary>
    /// Initializes a new instance of the VM with default stack sizes (4MB each stack, 12MB memory).
    /// </summary>
    public VM() : this(
        MemoryStack.FromMegabytes(4),
        MemoryStack.FromMegabytes(4),
        MemoryStack.FromMegabytes(4),
        MemoryStack.FromMegabytes(12))
    {
    }

    /// <summary>
    /// Initializes a new instance of the VM with custom stack sizes in megabytes.
    /// </summary>
    /// <param name="dataStackMB">Data stack size in megabytes.</param>
    /// <param name="floatStackMB">Float stack size in megabytes.</param>
    /// <param name="returnStackMB">Return stack size in megabytes.</param>
    /// <param name="memoryMB">Memory size in megabytes.</param>
    public VM(int dataStackMB, int floatStackMB, int returnStackMB, int memoryMB)
        : this(
            MemoryStack.FromMegabytes(dataStackMB),
            MemoryStack.FromMegabytes(floatStackMB),
            MemoryStack.FromMegabytes(returnStackMB),
            MemoryStack.FromMegabytes(memoryMB))
    {
    }

    /// <summary>
    /// Initializes a new instance of the VM with explicitly provided MemoryStack instances.
    /// This constructor provides maximum flexibility for custom stack configurations.
    /// </summary>
    /// <param name="dataStack">The data stack for integer/cell operations.</param>
    /// <param name="floatStack">The float stack for floating-point operations.</param>
    /// <param name="returnStack">The return stack for return addresses and temporary values.</param>
    /// <param name="memory">The VM memory for FETCH/STORE operations.</param>
    public VM(MemoryStack dataStack, MemoryStack floatStack, MemoryStack returnStack, MemoryStack memory)
    {
        DataStack = dataStack;
        FloatStack = floatStack;
        ReturnStack = returnStack;
        Memory = memory;
    }

    #region Factory Methods

    /// <summary>
    /// Creates a VM with tiny stack sizes (256KB each stack, 512KB memory).
    /// Suitable for embedded scenarios or very small programs.
    /// </summary>
    public static VM CreateTiny() => new(
        MemoryStack.FromKilobytes(256),
        MemoryStack.FromKilobytes(256),
        MemoryStack.FromKilobytes(256),
        MemoryStack.FromKilobytes(512));

    /// <summary>
    /// Creates a VM with small stack sizes (1MB each stack, 2MB memory).
    /// Suitable for small programs and testing.
    /// </summary>
    public static VM CreateSmall() => new(
        MemoryStack.FromMegabytes(1),
        MemoryStack.FromMegabytes(1),
        MemoryStack.FromMegabytes(1),
        MemoryStack.FromMegabytes(2));

    /// <summary>
    /// Creates a VM with default/medium stack sizes (4MB each stack, 12MB memory).
    /// This is the same as calling new VM().
    /// </summary>
    public static VM CreateDefault() => new VM();

    /// <summary>
    /// Creates a VM with large stack sizes (16MB each stack, 64MB memory).
    /// Suitable for larger programs with significant memory needs.
    /// </summary>
    public static VM CreateLarge() => new(
        MemoryStack.FromMegabytes(16),
        MemoryStack.FromMegabytes(16),
        MemoryStack.FromMegabytes(16),
        MemoryStack.FromMegabytes(64));

    /// <summary>
    /// Creates a VM with very large stack sizes (64MB each stack, 256MB memory).
    /// Suitable for complex programs with heavy computation or large data sets.
    /// </summary>
    public static VM CreateHuge() => new(
        MemoryStack.FromMegabytes(64),
        MemoryStack.FromMegabytes(64),
        MemoryStack.FromMegabytes(64),
        MemoryStack.FromMegabytes(256));

    /// <summary>
    /// Creates a VM with all stacks sized in kilobytes.
    /// </summary>
    /// <param name="dataStackKB">Data stack size in kilobytes.</param>
    /// <param name="floatStackKB">Float stack size in kilobytes.</param>
    /// <param name="returnStackKB">Return stack size in kilobytes.</param>
    /// <param name="memoryKB">Memory size in kilobytes.</param>
    public static VM FromKilobytes(int dataStackKB, int floatStackKB, int returnStackKB, int memoryKB) => new(
        MemoryStack.FromKilobytes(dataStackKB),
        MemoryStack.FromKilobytes(floatStackKB),
        MemoryStack.FromKilobytes(returnStackKB),
        MemoryStack.FromKilobytes(memoryKB));

    /// <summary>
    /// Creates a VM with all stacks sized in cells (64-bit/8 bytes each).
    /// Useful for precise control over stack depth.
    /// </summary>
    /// <param name="dataStackCells">Data stack depth in cells.</param>
    /// <param name="floatStackCells">Float stack depth in cells (doubles).</param>
    /// <param name="returnStackCells">Return stack depth in cells.</param>
    /// <param name="memoryCells">Memory size in cells.</param>
    public static VM FromCells(int dataStackCells, int floatStackCells, int returnStackCells, int memoryCells) => new(
        MemoryStack.FromCells(dataStackCells),
        MemoryStack.FromCells(floatStackCells),
        MemoryStack.FromCells(returnStackCells),
        MemoryStack.FromCells(memoryCells));

    /// <summary>
    /// Creates a VM with uniform stack sizes in megabytes.
    /// All stacks and memory will be the same size.
    /// </summary>
    /// <param name="sizeMB">Size in megabytes for all stacks and memory.</param>
    public static VM FromUniformSize(int sizeMB) => new(sizeMB, sizeMB, sizeMB, sizeMB);

    /// <summary>
    /// Creates a VM with uniform stack sizes in kilobytes.
    /// All stacks and memory will be the same size.
    /// </summary>
    /// <param name="sizeKB">Size in kilobytes for all stacks and memory.</param>
    public static VM FromUniformKilobytes(int sizeKB) => FromKilobytes(sizeKB, sizeKB, sizeKB, sizeKB);

    /// <summary>
    /// Creates a VM with uniform stack sizes in cells.
    /// All stacks and memory will be the same size.
    /// </summary>
    /// <param name="sizeCells">Size in cells for all stacks and memory.</param>
    public static VM FromUniformCells(int sizeCells) => FromCells(sizeCells, sizeCells, sizeCells, sizeCells);

    #endregion

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

        // Memory access - short (16-bit)
        table[(byte)OPCode.FETCH_SHORT] = static vm =>
        {
            vm.CheckDataStackDepth(1, "FETCH_SHORT");
            long addr = vm.DataStack.PopLong();
            vm.ValidateMemoryAccess(addr, 2, "FETCH_SHORT");
            short value = MemoryMarshal.Read<short>(vm.Memory.Memory.Span.Slice((int)addr, 2));
            vm.DataStack.PushLong(value);
        };
        table[(byte)OPCode.STORE_SHORT] = static vm =>
        {
            vm.CheckDataStackDepth(2, "STORE_SHORT");
            long addr = vm.DataStack.PopLong();
            long value = vm.DataStack.PopLong();
            vm.ValidateMemoryAccess(addr, 2, "STORE_SHORT");
            short shortValue = (short)value;
            MemoryMarshal.Write(vm.Memory.Memory.Span.Slice((int)addr, 2), in shortValue);
        };

        // Memory access - int (32-bit)
        table[(byte)OPCode.FETCH_INT] = static vm =>
        {
            vm.CheckDataStackDepth(1, "FETCH_INT");
            long addr = vm.DataStack.PopLong();
            vm.ValidateMemoryAccess(addr, 4, "FETCH_INT");
            int value = MemoryMarshal.Read<int>(vm.Memory.Memory.Span.Slice((int)addr, 4));
            vm.DataStack.PushLong(value);
        };
        table[(byte)OPCode.STORE_INT] = static vm =>
        {
            vm.CheckDataStackDepth(2, "STORE_INT");
            long addr = vm.DataStack.PopLong();
            long value = vm.DataStack.PopLong();
            vm.ValidateMemoryAccess(addr, 4, "STORE_INT");
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
        // round to nearest 5.3 -> 5, 5.5 -> 6 (away from zero)
        table[(byte)OPCode.FLOAT_TO_CELL] = static vm =>
        {
            vm.CheckFloatStackDepth(1, "FLOAT_TO_CELL");
            vm.DataStack.PushLong((long)Math.Round(vm.FloatStack.PopDouble(), MidpointRounding.AwayFromZero));
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
        table[(byte)OPCode.NOP] = static _ => { /* Do nothing */ };

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
    /// Resets the VM to initial state. This does not free the memory allocated for stacks and memory.
    /// It just resets the pointer of the memory to the beginning. Overwrites existing data.
    /// </summary>
    public void Reset()
    {
        PC = 0;
        _halted = false;
        DataStack.Clear();
        FloatStack.Clear();
        ReturnStack.Clear();
        Memory.Clear();
    }

    #region Memory Growth Methods

    /// <summary>
    /// Grows the data stack capacity to the specified size in megabytes.
    /// Preserves existing stack contents.
    /// </summary>
    /// <param name="megabytes">The new data stack size in megabytes.</param>
    public void GrowDataStack(int megabytes)
    {
        DataStack.GrowTo(megabytes * 1024 * 1024);
    }

    /// <summary>
    /// Grows the float stack capacity to the specified size in megabytes.
    /// Preserves existing stack contents.
    /// </summary>
    /// <param name="megabytes">The new float stack size in megabytes.</param>
    public void GrowFloatStack(int megabytes)
    {
        FloatStack.GrowTo(megabytes * 1024 * 1024);
    }

    /// <summary>
    /// Grows the return stack capacity to the specified size in megabytes.
    /// Preserves existing stack contents.
    /// </summary>
    /// <param name="megabytes">The new return stack size in megabytes.</param>
    public void GrowReturnStack(int megabytes)
    {
        ReturnStack.GrowTo(megabytes * 1024 * 1024);
    }

    /// <summary>
    /// Grows the VM memory capacity to the specified size in megabytes.
    /// Preserves existing memory contents.
    /// </summary>
    /// <param name="megabytes">The new memory size in megabytes.</param>
    public void GrowMemory(int megabytes)
    {
        Memory.GrowTo(megabytes * 1024 * 1024);
    }

    /// <summary>
    /// Doubles the capacity of all stacks and memory.
    /// Preserves existing contents.
    /// </summary>
    public void DoubleAllCapacity()
    {
        DataStack.Double();
        FloatStack.Double();
        ReturnStack.Double();
        Memory.Double();
    }

    /// <summary>
    /// Doubles the data stack capacity.
    /// Preserves existing stack contents.
    /// </summary>
    public void DoubleDataStack()
    {
        DataStack.Double();
    }

    /// <summary>
    /// Doubles the float stack capacity.
    /// Preserves existing stack contents.
    /// </summary>
    public void DoubleFloatStack()
    {
        FloatStack.Double();
    }

    /// <summary>
    /// Doubles the return stack capacity.
    /// Preserves existing stack contents.
    /// </summary>
    public void DoubleReturnStack()
    {
        ReturnStack.Double();
    }

    /// <summary>
    /// Doubles the memory capacity.
    /// Preserves existing memory contents.
    /// </summary>
    public void DoubleMemory()
    {
        Memory.Double();
    }

    #endregion

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
                int offset = DataStack.Pointer - ((i + 1) * sizeof(long));
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
                int offset = FloatStack.Pointer - ((i + 1) * sizeof(double));
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
                int offset = ReturnStack.Pointer - ((i + 1) * sizeof(long));
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
