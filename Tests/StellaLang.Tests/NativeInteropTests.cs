namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class NativeInteropTests
{
    [Fact]
    public void NativeAdd_Ints_PopsTwoPushesOne()
    {
        var vm = new VMActor();
        vm.DefineNative("NADD", (Func<int, int, int>)((a, b) => a + b));

        // Push 40 and 2 then call NADD -> 42 on stack
        var prog = new BytecodeBuilder()
            .Push(40)
            .Push(2)
            .Build();
        vm.LoadBytecode(prog);
        vm.Run();

        vm.ExecuteWord("NADD");

        Assert.Single(vm.DataStack);
        Assert.Equal(42, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void NativeMul_Doubles_UsingDoubleSignature()
    {
        var vm = new VMActor();
        vm.DefineNative("DMUL", (Func<double, double, double>)((a, b) => a * b));

        // Push 6 and 7 -> 42.0 via double multiply
        var prog = new BytecodeBuilder()
            .Push(6)
            .Push(7)
            .Build();
        vm.LoadBytecode(prog);
        vm.Run();

        vm.ExecuteWord("DMUL");
        Assert.Single(vm.DataStack);
        Assert.Equal(42.0, vm.DataStack.First().AsFloat());
    }

    [Fact]
    public void NativeConsoleWriteLine_VoidSignature_PopsArgumentOnly()
    {
        var vm = new VMActor();
        int capture = 0;
        // Simulate Console.WriteLine(int) with a capture
        vm.DefineNative("PRINT-INT", (Action<int>)(i => capture = i));

        var prog = new BytecodeBuilder()
            .Push(123)
            .Build();
        vm.LoadBytecode(prog);
        vm.Run();

        vm.ExecuteWord("PRINT-INT");

        Assert.Empty(vm.DataStack);
        Assert.Equal(123, capture);
    }

    [Fact]
    public void NativeBoolAnd_WorksWithBooleans()
    {
        var vm = new VMActor();
        vm.DefineNative("BAND", (Func<bool, bool, bool>)((a, b) => a && b));

        // true && false -> false
        var prog = new ProgramBuilder()
            .Push(1) // true
            .Push(0) // false
            .RunOn(vm);
        vm.ExecuteWord("BAND");

        Assert.Single(vm.DataStack);
        Assert.False(vm.DataStack.First().AsBoolean());
    }

    [Fact]
    public void NativeValueEcho_CanAcceptAndReturnValue()
    {
        var vm = new VMActor();
        vm.DefineNative("ECHO", (Func<Value, Value>)(v => v));

        new ProgramBuilder().Push(999).RunOn(vm);
        vm.ExecuteWord("ECHO");
        Assert.Equal(999, vm.DataStack.First().AsInteger());
    }
}
