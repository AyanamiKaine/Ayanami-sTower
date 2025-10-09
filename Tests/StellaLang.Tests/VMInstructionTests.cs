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
    public void NEGVMInstructionTest()
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
    public void EQVMInstructionTest()
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
}
