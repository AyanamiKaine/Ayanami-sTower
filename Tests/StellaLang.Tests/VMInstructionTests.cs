namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Here we are testing the instructions of the VM
/// </summary>
public class VMInstructionTests
{
    [Fact]
    public void PushCellVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(100)
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 100;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }


    [Fact]
    public void PushDoubleVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .FPushDouble(100.5)
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.FloatStack.PeekDouble();
        const double expectedTopValue = 100.5;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void DUPVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(100)
            .Dup()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 100;

        Assert.Equal(expectedTopValue, currentTopStackValue);

        // After duplication, the second value should also be 100
        vm.LoadAndExecute(new CodeBuilder()
            .Drop()
            .Build());

        currentTopStackValue = vm.DataStack.PeekLong();

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void DUPStackUnderFlowVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .Dup()
            .Build();

        Assert.Throws<StackUnderflowException>(() => vm.LoadAndExecute(code));
    }

    [Fact]
    public void SWAPVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(100)
            .PushCell(141)
            .Swap()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        long expectedTopValue = 100;

        Assert.Equal(expectedTopValue, currentTopStackValue);

        // After duplication, the second value should also be 100
        vm.LoadAndExecute(new CodeBuilder()
            .Drop()
            .Build());

        expectedTopValue = 141;
        currentTopStackValue = vm.DataStack.PeekLong();

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void ROTVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(1)
            .PushCell(2)
            .PushCell(3)
            .Rot()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        long expectedTopValue = 1;

        Assert.Equal(expectedTopValue, currentTopStackValue);

        vm.LoadAndExecute(new CodeBuilder()
            .Drop()
            .Build());

        currentTopStackValue = vm.DataStack.PeekLong();
        expectedTopValue = 3;
        Assert.Equal(expectedTopValue, currentTopStackValue);

        vm.LoadAndExecute(new CodeBuilder()
            .Drop()
            .Build());

        currentTopStackValue = vm.DataStack.PeekLong();
        expectedTopValue = 2;
        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void TO_RVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(10)
            .ToR()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.ReturnStack.PeekLong();
        const long expectedTopValue = 10;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void R_FROMVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(10)
            .ToR()
            .RFrom()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 10;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void R_FETCHVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(10)
            .ToR()
            .RFetch()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValueDataStack = vm.DataStack.PeekLong();
        const long expectedTopValueDataStack = 10;

        var currentTopStackValueReturnStack = vm.ReturnStack.PeekLong();
        const long expectedTopValueReturnStack = 10;

        Assert.Equal(expectedTopValueDataStack, currentTopStackValueDataStack);
        Assert.Equal(expectedTopValueReturnStack, currentTopStackValueReturnStack);

    }

    [Fact]
    public void ANDVMInstructionTest1()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(1)
            .PushCell(1)
            .And()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValueDataStack = vm.DataStack.PeekLong();
        const long expectedTopValueDataStack = 1;

        Assert.Equal(expectedTopValueDataStack, currentTopStackValueDataStack);
    }

    [Fact]
    public void ANDVMInstructionTest2()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(1)
            .PushCell(0)
            .And()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValueDataStack = vm.DataStack.PeekLong();
        const long expectedTopValueDataStack = 0;

        Assert.Equal(expectedTopValueDataStack, currentTopStackValueDataStack);
    }

    [Fact]
    public void ORVMInstructionTest1()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(1)
            .PushCell(0)
            .Or()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValueDataStack = vm.DataStack.PeekLong();
        const long expectedTopValueDataStack = 1;

        Assert.Equal(expectedTopValueDataStack, currentTopStackValueDataStack);
    }

    [Fact]
    public void ORVMInstructionTest2()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(0)
            .PushCell(0)
            .Or()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValueDataStack = vm.DataStack.PeekLong();
        const long expectedTopValueDataStack = 0;

        Assert.Equal(expectedTopValueDataStack, currentTopStackValueDataStack);
    }

    [Fact]
    public void XORVMInstructionTest1()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(0)
            .PushCell(0)
            .Xor()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValueDataStack = vm.DataStack.PeekLong();
        const long expectedTopValueDataStack = 0;

        Assert.Equal(expectedTopValueDataStack, currentTopStackValueDataStack);
    }

    [Fact]
    public void XORVMInstructionTest2()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(0)
            .PushCell(1)
            .Xor()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValueDataStack = vm.DataStack.PeekLong();
        const long expectedTopValueDataStack = 1;

        Assert.Equal(expectedTopValueDataStack, currentTopStackValueDataStack);
    }

    [Fact]
    public void XORVMInstructionTest3()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(1)
            .PushCell(0)
            .Xor()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValueDataStack = vm.DataStack.PeekLong();
        const long expectedTopValueDataStack = 1;

        Assert.Equal(expectedTopValueDataStack, currentTopStackValueDataStack);
    }

    [Fact]
    public void NOTVMInstructionTest1()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(1)
            .Not()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValueDataStack = vm.DataStack.PeekLong();
        const long expectedTopValueDataStack = -2;

        Assert.Equal(expectedTopValueDataStack, currentTopStackValueDataStack);
    }

    [Fact]
    public void NOTVMInstructionTest2()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(0)
            .Not()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValueDataStack = vm.DataStack.PeekLong();
        const long expectedTopValueDataStack = -1;

        Assert.Equal(expectedTopValueDataStack, currentTopStackValueDataStack);
    }

    [Fact]
    public void SHLVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(5)
            .PushCell(9)
            .Shl()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValueDataStack = vm.DataStack.PeekLong();
        const long expectedTopValueDataStack = 2560;

        Assert.Equal(expectedTopValueDataStack, currentTopStackValueDataStack);
    }

    [Fact]
    public void SHRVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(5)
            .PushCell(9)
            .Shr()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValueDataStack = vm.DataStack.PeekLong();
        const long expectedTopValueDataStack = 0;

        Assert.Equal(expectedTopValueDataStack, currentTopStackValueDataStack);
    }

    [Fact]
    public void AddVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(42)
            .PushCell(100)
            .Add()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 142;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void SUBVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(42)
            .PushCell(100)
            .Sub()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = -58;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void MULVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(42)
            .PushCell(100)
            .Mul()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 4200;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void DIVMODVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(42) // Dividend
            .PushCell(10) // Divisor
            .DivMod()
            .Build();

        vm.LoadAndExecute(code);

        /*
        [
            4 <- Quotient 
            2 <- Remainder <- Top of Stack
        ]
        */

        const long expectedQuotient = 4;
        const long expectedRemainder = 2;

        long currentRemainder = vm.DataStack.PeekLong();

        vm.DataStack.PopLong();
        long currentQuotient = vm.DataStack.PeekLong();

        Assert.Equal(expectedRemainder, currentQuotient);
        Assert.Equal(expectedQuotient, currentRemainder);
    }

    [Fact]
    public void FAddVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .FPushDouble(42.5)
            .FPushDouble(100.0)
            .FAdd()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.FloatStack.PeekDouble();
        const double expectedTopValue = 142.5;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void FMulVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .FPushDouble(42.5)
            .FPushDouble(100.0)
            .FMul()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.FloatStack.PeekDouble();
        const double expectedTopValue = 4250.0;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void FDIVMODVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .FPushDouble(42.5) // Dividend
            .FPushDouble(10.0) // Divisor
            .FDiv()
            .Build();

        vm.LoadAndExecute(code);

        /*
        [
            4.25 <- Quotient <- Top of Stack
        ]
        */
        const double expectedTopStackValue = 4.25;
        double currentTopStackValue = vm.FloatStack.PeekDouble();

        Assert.Equal(expectedTopStackValue, currentTopStackValue);
    }

    [Fact]
    public void NEGVMInstructionTest1()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(42)
            .Neg()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = -42;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void NEGVMInstructionTest2()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(-42)
            .Neg()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 42;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }


    [Fact]
    public void EQVMInstructionTest1()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(42)
            .PushCell(42)
            .Eq()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 1;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void EQVMInstructionTest2()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(42)
            .PushCell(50)
            .Eq()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 0;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void NEQVMInstructionTest1()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(42)
            .PushCell(50)
            .Neq()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 1;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void NEQVMInstructionTest2()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(42)
            .PushCell(42)
            .Neq()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 0;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void LTVMInstructionTest1()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(42)
            .PushCell(50)
            .Lt()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 1;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void LTVMInstructionTest2()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(50)
            .PushCell(42)
            .Lt()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 0;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void LTEVMInstructionTest1()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(50)
            .PushCell(50)
            .Lte()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 1;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void LTEVMInstructionTest2()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(50)
            .PushCell(42)
            .Lte()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 0;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void GTVMInstructionTest1()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(42)
            .PushCell(50)
            .Gt()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 0;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void GTVMInstructionTest2()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(50)
            .PushCell(42)
            .Gt()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 1;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void GTEVMInstructionTest1()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(50)
            .PushCell(50)
            .Gte()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 1;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void GTEVMInstructionTest2()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(50)
            .PushCell(42)
            .Gte()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 1;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void OVERVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(50)
            .PushCell(42)
            .Over()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 50;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void STOREFETCHVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(1234)      // Value to store
            .PushCell(50)         // Address to store at
            .Store()             // STORE instruction
            .PushCell(50)         // Address to fetch from
            .Fetch()             // FETCH instruction
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 1234;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void FSTOREFFETCHVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .FPushDouble(1234.5)      // Value to store
            .PushCell(50)         // Address to store at
            .FStore()             // STORE instruction
            .PushCell(50)         // Address to fetch from
            .FFetch()             // FETCH instruction
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.FloatStack.PeekDouble();
        const double expectedTopValue = 1234.5;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void CELL_TO_FLOATVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(50)
            .CellToFloat()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.FloatStack.PeekDouble();
        const double expectedTopValue = 50.0;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void FLOAT_TO_CELLVMInstructionTest1()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .FPushDouble(55.2)
            .FloatToCell()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 55;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }


    [Fact]
    public void FLOAT_TO_CELLVMInstructionTest2()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .FPushDouble(55.9)
            .FloatToCell()
            .Build();

        vm.LoadAndExecute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 56;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void JMPVMInstructionTest()
    {
        var vm = new VM();

        // Test that JMP unconditionally jumps over code
        var code = new CodeBuilder()
            .PushCell(1)           // Push 1 onto stack
            .Jmp("skip")           // Jump over the next instruction
            .PushCell(999)         // This should be SKIPPED
            .Label("skip")         // Jump target
            .PushCell(2)           // This should execute
            .Build();

        vm.LoadAndExecute(code);

        // Stack should be: [1, 2] (top)
        // If JMP failed, it would be: [1, 999, 2]

        long topValue = vm.DataStack.PopLong();
        Assert.Equal(2, topValue);  // Should be 2 (the value after the label)

        long secondValue = vm.DataStack.PopLong();
        Assert.Equal(1, secondValue);  // Should be 1 (before the jump)

        // Stack should now be empty
        Assert.True(vm.DataStack.IsEmpty);
    }

    [Fact]
    public void JZVMInstructionTest()
    {
        // Test JZ (Jump if Zero) - should jump when top of stack is 0
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(0)           // Push 0 (condition is true)
            .Jz("zero_case")       // Should jump because top is 0
            .PushCell(999)         // This should be SKIPPED
            .Label("zero_case")
            .PushCell(42)          // This should execute
            .Build();

        vm.LoadAndExecute(code);

        long topValue = vm.DataStack.PopLong();
        Assert.Equal(42, topValue);  // Should be 42 (jumped successfully)

        // Stack should now be empty (the 0 was consumed by JZ)
        Assert.True(vm.DataStack.IsEmpty);
    }

    [Fact]
    public void JZVMInstructionTestNoJump()
    {
        // Test JZ when condition is false (non-zero) - should NOT jump
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(1)           // Push 1 (condition is false)
            .Jz("zero_case")       // Should NOT jump because top is not 0
            .PushCell(100)         // This should execute
            .Halt()                // End program
            .Label("zero_case")
            .PushCell(999)         // This should be SKIPPED
            .Halt()
            .Build();

        vm.LoadAndExecute(code);

        long topValue = vm.DataStack.PopLong();
        Assert.Equal(100, topValue);  // Should be 100 (did not jump)

        Assert.True(vm.DataStack.IsEmpty);
    }

    [Fact]
    public void JNZVMInstructionTest()
    {
        // Test JNZ (Jump if Not Zero) - should jump when top of stack is non-zero
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(1)           // Push 1 (condition is true)
            .Jnz("nonzero_case")   // Should jump because top is non-zero
            .PushCell(999)         // This should be SKIPPED
            .Label("nonzero_case")
            .PushCell(42)          // This should execute
            .Build();

        vm.LoadAndExecute(code);

        long topValue = vm.DataStack.PopLong();
        Assert.Equal(42, topValue);  // Should be 42 (jumped successfully)

        // Stack should now be empty (the 1 was consumed by JNZ)
        Assert.True(vm.DataStack.IsEmpty);
    }

    [Fact]
    public void JNZVMInstructionTestNoJump()
    {
        // Test JNZ when condition is false (zero) - should NOT jump
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(0)           // Push 0 (condition is false)
            .Jnz("nonzero_case")   // Should NOT jump because top is 0
            .PushCell(100)         // This should execute
            .Halt()                // End program
            .Label("nonzero_case")
            .PushCell(999)         // This should be SKIPPED
            .Halt()
            .Build();

        vm.LoadAndExecute(code);

        long topValue = vm.DataStack.PopLong();
        Assert.Equal(100, topValue);  // Should be 100 (did not jump)

        Assert.True(vm.DataStack.IsEmpty);
    }

    [Fact]
    public void CALLVMInstructionTest()
    {
        // Test CALL instruction - should save return address and jump to function
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(10)          // Initial value
            .Call("function")      // Call the function
            .PushCell(20)          // This executes after return
            .Halt()                // End program
            .Label("function")     // Function starts here
            .PushCell(5)           // Function body: push 5
            .Ret()                 // Return to caller
            .Build();

        vm.LoadAndExecute(code);

        // Stack should contain (from bottom): [10, 5, 20]
        long topValue = vm.DataStack.PopLong();
        Assert.Equal(20, topValue);  // Value after function call

        long secondValue = vm.DataStack.PopLong();
        Assert.Equal(5, secondValue);  // Value pushed by function

        long thirdValue = vm.DataStack.PopLong();
        Assert.Equal(10, thirdValue);  // Initial value

        Assert.True(vm.DataStack.IsEmpty);
        Assert.True(vm.ReturnStack.IsEmpty);  // Return stack should be empty after RET
    }

    [Fact]
    public void RETCALLVMInstructionTest()
    {
        // Test nested CALL/RET - function calling another function
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(1)           // Initial value
            .Call("func_a")        // Call function A
            .PushCell(4)           // After both functions return
            .Halt()                // End program
            .Label("func_a")       // Function A
            .PushCell(2)
            .Call("func_b")        // Function A calls function B
            .Ret()                 // Return from A
            .Label("func_b")       // Function B
            .PushCell(3)
            .Ret()                 // Return from B
            .Build();

        vm.LoadAndExecute(code);

        // Stack should contain: [1, 2, 3, 4]
        long val4 = vm.DataStack.PopLong();
        Assert.Equal(4, val4);

        long val3 = vm.DataStack.PopLong();
        Assert.Equal(3, val3);

        long val2 = vm.DataStack.PopLong();
        Assert.Equal(2, val2);

        long val1 = vm.DataStack.PopLong();
        Assert.Equal(1, val1);

        Assert.True(vm.DataStack.IsEmpty);
        Assert.True(vm.ReturnStack.IsEmpty);  // All returns completed
    }

    [Fact]
    public void HALTVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .Halt()
            .Build();

        vm.LoadAndExecute(code);

        const bool expectedHaltValue = true;

        var isHalted = vm.Halted;

        Assert.Equal(expectedHaltValue, isHalted);
    }

    [Fact]
    public void NOPVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .Nop()
            .Build();

        vm.LoadAndExecute(code);

        Assert.Throws<InvalidOperationException>(() => vm.DataStack.PeekLong());
    }

    [Fact]
    public void SYSCALLVMInstructionTest()
    {
        var vm = new VM();

        // Register a syscall handler
        bool syscallWasCalled = false;
        long capturedValue = 0;

        vm.SyscallHandlers[42] = (vm) =>
        {
            syscallWasCalled = true;
            // Syscall can read from the data stack
            if (!vm.DataStack.IsEmpty)
            {
                capturedValue = vm.DataStack.PopLong();
            }
            // And push results back
            vm.DataStack.PushLong(100);
        };

        var code = new CodeBuilder()
            .PushCell(999)         // Value for syscall to consume
            .PushCell(42)          // Syscall ID
            .Syscall()             // Invoke syscall
            .Build();

        vm.LoadAndExecute(code);

        Assert.True(syscallWasCalled);
        Assert.Equal(999, capturedValue);

        // Syscall should have pushed 100
        long result = vm.DataStack.PopLong();
        Assert.Equal(100, result);

        Assert.True(vm.DataStack.IsEmpty);
    }

    [Fact]
    public void SYSCALLVMInstructionTestUnknown()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .PushCell(999)         // Unknown syscall ID
            .Syscall()
            .Build();

        // Should throw for unknown syscall
        Assert.Throws<UnknownSyscallException>(() => vm.LoadAndExecute(code));
    }
}
