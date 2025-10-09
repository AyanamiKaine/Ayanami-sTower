namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class StructTests
{
    [Fact]
    public void DefineAndUse_Circle2D_Struct()
    {
        var vm = new VMActor();
        vm.Allot(0); // ensure dictionary initialized

        // Define a Circle2D struct layout: X (i64) at 0, Y (i64) at 8, R (i64) at 16
        StructDefiner.DefineI64Struct(vm, "CIRCLE2D", new[]
        {
            new StructDefiner.Field("X", 0),
            new StructDefiner.Field("Y", 8),
            new StructDefiner.Field("R", 16),
        }, sizeBytes: 24);

        // Create a circle: X=10, Y=20, R=3 => returns addr
        new ProgramBuilder()
            .Push(10)
            .Push(20)
            .Push(3)
            .Word("NEW-CIRCLE2D")
            .RunOn(vm);

        var addr = vm.DataStack.First().AsPointer();

        // Read fields via getters
        new ProgramBuilder().Push(addr).Word("GET-CIRCLE2D-X").RunOn(vm);
        Assert.Equal(10, vm.DataStack.First().AsInteger());

        new ProgramBuilder().Push(addr).Word("GET-CIRCLE2D-Y").RunOn(vm);
        Assert.Equal(20, vm.DataStack.First().AsInteger());

        new ProgramBuilder().Push(addr).Word("GET-CIRCLE2D-R").RunOn(vm);
        Assert.Equal(3, vm.DataStack.First().AsInteger());

        // Update Y to 25
        new ProgramBuilder().Push(25).Push(addr).Word("SET-CIRCLE2D-Y").RunOn(vm);
        new ProgramBuilder().Push(addr).Word("GET-CIRCLE2D-Y").RunOn(vm);
        Assert.Equal(25, vm.DataStack.First().AsInteger());
    }
}
