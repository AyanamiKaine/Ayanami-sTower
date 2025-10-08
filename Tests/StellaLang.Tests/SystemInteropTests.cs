using System;

namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class SystemInteropTests
{
    [Fact]
    public void Bind_SystemMath_Max_As_Word()
    {
        var vm = new VMActor();
        vm.DefineNativeStatic("MAX", typeof(Math), nameof(Math.Max), typeof(int), typeof(int));

        new ProgramBuilder().Push(10).Push(42).RunOn(vm);
        vm.ExecuteWord("MAX");

        Assert.Single(vm.DataStack);
        Assert.Equal(42, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void Bind_SystemConvert_ToInt32_As_Word()
    {
        var vm = new VMActor();
        vm.DefineNativeStatic("TOINT", typeof(Convert), nameof(Convert.ToInt32), typeof(double));

        new ProgramBuilder().Push(41).RunOn(vm);
        vm.ExecuteWord("TOINT");

        Assert.Single(vm.DataStack);
        Assert.Equal(41, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void Bind_Random_Next_Instance_Method()
    {
        var vm = new VMActor();
        var rng = new Random(123);
        vm.DefineNativeInstance("RANGE", rng, nameof(Random.Next), typeof(int), typeof(int));

        // Generate a value in [0,100)
        new ProgramBuilder().Push(0).Push(100).RunOn(vm);
        vm.ExecuteWord("RANGE");

        // Value shape check (not exact to avoid flakiness); ensure within bounds
        var v = vm.DataStack.First().AsInteger();
        Assert.True(v >= 0 && v < 100);
    }

    [Fact]
    public void Bind_Console_WriteLine_Int_Void()
    {
        var vm = new VMActor();
        int captured = 0;
        // wrap Console.WriteLine with a local capture to assert; also prove void return works
        vm.DefineNative("WRITE-INT", (Action<int>)(i => captured = i));

        new ProgramBuilder().Push(77).RunOn(vm);
        vm.ExecuteWord("WRITE-INT");

        Assert.Empty(vm.DataStack);
        Assert.Equal(77, captured);
    }
}
