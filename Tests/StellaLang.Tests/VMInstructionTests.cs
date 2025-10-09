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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        const long expectedTopValue = 100;

        Assert.Equal(expectedTopValue, currentTopStackValue);

        // After duplication, the second value should also be 100
        vm.Execute(new CodeBuilder()
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

        Assert.Throws<InvalidOperationException>(() => vm.Execute(code));
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

        vm.Execute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        long expectedTopValue = 100;

        Assert.Equal(expectedTopValue, currentTopStackValue);

        // After duplication, the second value should also be 100
        vm.Execute(new CodeBuilder()
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

        vm.Execute(code);

        var currentTopStackValue = vm.DataStack.PeekLong();
        long expectedTopValue = 1;

        Assert.Equal(expectedTopValue, currentTopStackValue);

        vm.Execute(new CodeBuilder()
            .Drop()
            .Build());

        currentTopStackValue = vm.DataStack.PeekLong();
        expectedTopValue = 3;
        Assert.Equal(expectedTopValue, currentTopStackValue);
        
        vm.Execute(new CodeBuilder()
            .Drop()
            .Build());

        currentTopStackValue = vm.DataStack.PeekLong();
        expectedTopValue = 2;
        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void TO_RVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void R_FROMVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void R_FETCHVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void ANDVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void ORVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void XORVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void NOTVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void SHLVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void SHRVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

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

        vm.Execute(code);

        var currentTopStackValue = vm.FloatStack.PeekDouble();
        const double expectedTopValue = 1234.5;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }

    [Fact]
    public void CELL_TO_FLOATVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void FLOAT_TO_CELLVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void JMPVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void JZVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void JNZVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void CALLVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void RETCALLVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }

    [Fact]
    public void HALTVMInstructionTest()
    {
        var vm = new VM();

        var code = new CodeBuilder()
            .Halt()
            .Build();

        vm.Execute(code);

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

        vm.Execute(code);

        Assert.Throws<InvalidOperationException>(() => vm.DataStack.PeekLong());
    }

    [Fact]
    public void SYSCALLVMInstructionTest()
    {
        Assert.Fail("TEST NOT YET IMPLEMENTED");
    }
}
