namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class ProgramBuilderTests
{
    [Fact]
    public void ProgramBuilder_MixesPushOpsAndWords()
    {
        var vm = new VMActor();

        // Define DOUBLE: DUP ADD
        var doubleWord = new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("DOUBLE", doubleWord);

        // Program: Push 7, DOUBLE, Push 3, ADD => (7*2)+3 = 17
        new ProgramBuilder()
            .Push(7)
            .Word("DOUBLE")
            .Push(3)
            .Op(OpCode.ADD)
            .RunOn(vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(17, vm.DataStack.First().AsInteger());
    }
}
