namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class StructForthWordsTests
{
    private static void DefineCircleAccessWords(VMActor vm)
    {
        // CIRCLE-X@: (addr -- x)
        vm.DefineWord("CIRCLE-X@", new BytecodeBuilder()
            .Push(0)
            .Op(OpCode.ADD)
            .Op(OpCode.FETCH)
            .Op(OpCode.RETURN)
            .Build());

        // CIRCLE-Y@: (addr -- y)
        vm.DefineWord("CIRCLE-Y@", new BytecodeBuilder()
            .Push(8)
            .Op(OpCode.ADD)
            .Op(OpCode.FETCH)
            .Op(OpCode.RETURN)
            .Build());

        // CIRCLE-R@: (addr -- r)
        vm.DefineWord("CIRCLE-R@", new BytecodeBuilder()
            .Push(16)
            .Op(OpCode.ADD)
            .Op(OpCode.FETCH)
            .Op(OpCode.RETURN)
            .Build());

        // CIRCLE-X!: (value addr -- )
        vm.DefineWord("CIRCLE-X!", new BytecodeBuilder()
            .Push(0)
            .Op(OpCode.ADD)
            .Op(OpCode.STORE)
            .Op(OpCode.RETURN)
            .Build());

        // CIRCLE-Y!: (value addr -- )
        vm.DefineWord("CIRCLE-Y!", new BytecodeBuilder()
            .Push(8)
            .Op(OpCode.ADD)
            .Op(OpCode.STORE)
            .Op(OpCode.RETURN)
            .Build());

        // CIRCLE-R!: (value addr -- )
        vm.DefineWord("CIRCLE-R!", new BytecodeBuilder()
            .Push(16)
            .Op(OpCode.ADD)
            .Op(OpCode.STORE)
            .Op(OpCode.RETURN)
            .Build());

        // CIRCLE-DIAMETER: (addr -- d) => 2 * R
        vm.DefineWord("CIRCLE-DIAMETER", new BytecodeBuilder()
            .Push(16)
            .Op(OpCode.ADD)
            .Op(OpCode.FETCH)
            .Push(2)
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build());

        // CIRCLE-AREA-PI3: (addr -- area) ~ 3 * r^2 (integer pi approximation)
        vm.DefineWord("CIRCLE-AREA-PI3", new BytecodeBuilder()
            .Push(16)
            .Op(OpCode.ADD)
            .Op(OpCode.FETCH) // r
            .Op(OpCode.DUP)
            .Op(OpCode.MUL)   // r*r
            .Push(3)
            .Op(OpCode.MUL)   // 3*r*r
            .Op(OpCode.RETURN)
            .Build());
    }

    private static int AllocateCircle(VMActor vm, long x, long y, long r)
    {
        // Allocate 24 bytes and return base address: HERE (after allot) - 24
        new ProgramBuilder()
            .Push(24)
            .Word("ALLOT")
            .Word("HERE")
            .Push(24)
            .Op(OpCode.SUB)
            .RunOn(vm);

        int baseAddr = vm.DataStack.First().AsPointer();

        // Initialize fields
        new ProgramBuilder().Push(x).Push(baseAddr).Word("CIRCLE-X!").RunOn(vm);
        new ProgramBuilder().Push(y).Push(baseAddr).Word("CIRCLE-Y!").RunOn(vm);
        new ProgramBuilder().Push(r).Push(baseAddr).Word("CIRCLE-R!").RunOn(vm);

        return baseAddr;
    }

    [Fact]
    public void AccessorWords_GetAndSet_Work()
    {
        var vm = new VMActor();
        DefineCircleAccessWords(vm);

        int addr = AllocateCircle(vm, 10, 20, 3);

        // Validate getters
        new ProgramBuilder().Push(addr).Word("CIRCLE-X@").RunOn(vm);
        Assert.Equal(10, vm.DataStack.First().AsInteger());
        new ProgramBuilder().Push(addr).Word("CIRCLE-Y@").RunOn(vm);
        Assert.Equal(20, vm.DataStack.First().AsInteger());
        new ProgramBuilder().Push(addr).Word("CIRCLE-R@").RunOn(vm);
        Assert.Equal(3, vm.DataStack.First().AsInteger());

        // Update and read back via setters/getters
        new ProgramBuilder().Push(42).Push(addr).Word("CIRCLE-X!").RunOn(vm);
        new ProgramBuilder().Push(addr).Word("CIRCLE-X@").RunOn(vm);
        Assert.Equal(42, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void UtilityWords_Diameter_And_AreaPi3_Work()
    {
        var vm = new VMActor();
        DefineCircleAccessWords(vm);

        int addr = AllocateCircle(vm, 1, 1, 5);

        // Diameter = 10
        new ProgramBuilder().Push(addr).Word("CIRCLE-DIAMETER").RunOn(vm);
        Assert.Equal(10, vm.DataStack.First().AsInteger());

        // Area ~ 3 * r^2 = 75
        new ProgramBuilder().Push(addr).Word("CIRCLE-AREA-PI3").RunOn(vm);
        Assert.Equal(75, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void MoveAndScale_UsingAccessors_ComposesNaturally()
    {
        var vm = new VMActor();
        DefineCircleAccessWords(vm);

        int addr = AllocateCircle(vm, 10, 20, 3);

        // Move by (+7, -5) using accessors
        new ProgramBuilder()
            .Push(addr).Word("CIRCLE-X@")
            .Push(7).Op(OpCode.ADD)
            .Push(addr).Word("CIRCLE-X!")
            .RunOn(vm);

        new ProgramBuilder()
            .Push(addr).Word("CIRCLE-Y@")
            .Push(-5).Op(OpCode.ADD)
            .Push(addr).Word("CIRCLE-Y!")
            .RunOn(vm);

        new ProgramBuilder().Push(addr).Word("CIRCLE-X@").RunOn(vm);
        Assert.Equal(17, vm.DataStack.First().AsInteger());
        new ProgramBuilder().Push(addr).Word("CIRCLE-Y@").RunOn(vm);
        Assert.Equal(15, vm.DataStack.First().AsInteger());

        // Scale radius by 2
        new ProgramBuilder()
            .Push(addr).Word("CIRCLE-R@")
            .Push(2).Op(OpCode.MUL)
            .Push(addr).Word("CIRCLE-R!")
            .RunOn(vm);

        new ProgramBuilder().Push(addr).Word("CIRCLE-R@").RunOn(vm);
        Assert.Equal(6, vm.DataStack.First().AsInteger());
    }
}
