namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class VariableForthTests
{
    private static int DefineVariable(VMActor vm, string name)
    {
        // Support both legacy helper and the new VARIABLE word
        vm.ExecuteWord("VARIABLE");
        vm.ExecuteWord(name);

        // Fetch the address by invoking the word and reading the stack
        new ProgramBuilder().Word(name).RunOn(vm);
        return vm.DataStack.First().AsPointer();
    }

    [Fact]
    public void Variable_BasicStoreFetch_And_ManualIncrement()
    {
        var vm = new VMActor();

        // VARIABLE APPLES
        int applesAddr = DefineVariable(vm, "APPLES");

        // 20 APPLES !  (store 20)
        new ProgramBuilder()
            .Push(20)
            .Word("APPLES")
            .Word("!")
            .RunOn(vm);

        // APPLES @  -> 20
        new ProgramBuilder().Word("APPLES").Word("@").RunOn(vm);
        Assert.Equal(20, vm.DataStack.First().AsInteger());

        // 1 APPLES +!   implemented as: APPLES DUP @ 1 + SWAP !
        new ProgramBuilder()
            .Word("APPLES")      // addr
            .Op(OpCode.DUP)       // addr addr
            .Word("@")            // addr value
            .Push(1)              // addr value 1
            .Op(OpCode.ADD)       // addr (value+1)
            .Word("SWAP")         // (value+1) addr
            .Word("!")           // store
            .RunOn(vm);

        // APPLES @  -> 21
        new ProgramBuilder().Word("APPLES").Word("@").RunOn(vm);
        Assert.Equal(21, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void IndirectVariable_APPLES_PointsToRedsOrGreens()
    {
        var vm = new VMActor();

        // COLOR, REDS, GREENS variables
        int colorAddr = DefineVariable(vm, "COLOR");
        int redsAddr = DefineVariable(vm, "REDS");
        int greensAddr = DefineVariable(vm, "GREENS");

        // RED: ( -- )  REDS COLOR !
        vm.DefineWord("RED", new BytecodeBuilder()
            .Push(redsAddr)
            .Push(colorAddr)
            .Op(OpCode.STORE)
            .Op(OpCode.RETURN)
            .Build());

        // GREEN: ( -- )  GREENS COLOR !
        vm.DefineWord("GREEN", new BytecodeBuilder()
            .Push(greensAddr)
            .Push(colorAddr)
            .Op(OpCode.STORE)
            .Op(OpCode.RETURN)
            .Build());

        // Initial APPLES: simple variable, then redefine below
        vm.DefineWord("APPLES", new BytecodeBuilder().Push(redsAddr).Op(OpCode.RETURN).Build());

        // Redefine APPLES as an indirect variable: ( -- addr ) COLOR @
        var applesIndirect = new BytecodeBuilder()
            .Push(colorAddr)
            .Op(OpCode.FETCH)
            .Op(OpCode.RETURN)
            .Build();
        vm.RedefineWord("APPLES", applesIndirect);

        // RED APPLES ! 20  -> set reds tally to 20
        new ProgramBuilder()
            .Word("RED")
            .Push(20)
            .Word("APPLES")
            .Word("!")
            .RunOn(vm);

        // GREEN APPLES ! 1 -> set greens tally to 1
        new ProgramBuilder()
            .Word("GREEN")
            .Push(1)
            .Word("APPLES")
            .Word("!")
            .RunOn(vm);

        // RED APPLES @ -> 20
        new ProgramBuilder().Word("RED").Word("APPLES").Word("@").RunOn(vm);
        Assert.Equal(20, vm.DataStack.First().AsInteger());

        // GREEN APPLES @ -> 1
        new ProgramBuilder().Word("GREEN").Word("APPLES").Word("@").RunOn(vm);
        Assert.Equal(1, vm.DataStack.First().AsInteger());

        // Increment GREEN by 1: APPLES DUP @ 1 + SWAP !  -> 2
        new ProgramBuilder()
            .Word("GREEN")
            .Word("APPLES")
            .Op(OpCode.DUP)
            .Word("@")
            .Push(1)
            .Op(OpCode.ADD)
            .Word("SWAP")
            .Word("!")
            .RunOn(vm);

        new ProgramBuilder().Word("GREEN").Word("APPLES").Word("@").RunOn(vm);
        Assert.Equal(2, vm.DataStack.First().AsInteger());

        // Confirm RED unchanged: 20
        new ProgramBuilder().Word("RED").Word("APPLES").Word("@").RunOn(vm);
        Assert.Equal(20, vm.DataStack.First().AsInteger());
    }
}
