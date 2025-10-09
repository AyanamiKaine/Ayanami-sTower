namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Here we are testing the instructions of the VM
/// </summary>
public class VMInstructionTests
{
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

}
