namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class StructForthTests
{
    [Fact]
    public void DefineStruct_UsingExistingWords_WithFIELDGetSet()
    {
        var vm = new VMActor();

        // Allocate space for one Circle2D (X,Y,R) 3 * 8 bytes
        new ProgramBuilder()
            .Push(24)
            .Word("ALLOT")
            .Word("HERE") // HERE now points after allocation; base address is HERE-24
            .Push(24)
            .Op(OpCode.SUB)
            .RunOn(vm);

        var baseAddr = vm.DataStack.First().AsPointer();

        // Set fields using generic FIELD!: (value addr offset -- )
        new ProgramBuilder().Push(10).Push(baseAddr).Push(0).Word("FIELD!").RunOn(vm);  // X=10
        new ProgramBuilder().Push(20).Push(baseAddr).Push(8).Word("FIELD!").RunOn(vm);  // Y=20
        new ProgramBuilder().Push(3).Push(baseAddr).Push(16).Word("FIELD!").RunOn(vm);  // R=3

        // Get fields using FIELD@: (addr offset -- value)
        new ProgramBuilder().Push(baseAddr).Push(0).Word("FIELD@").RunOn(vm);
        Assert.Equal(10, vm.DataStack.First().AsInteger());

        new ProgramBuilder().Push(baseAddr).Push(8).Word("FIELD@").RunOn(vm);
        Assert.Equal(20, vm.DataStack.First().AsInteger());

        new ProgramBuilder().Push(baseAddr).Push(16).Word("FIELD@").RunOn(vm);
        Assert.Equal(3, vm.DataStack.First().AsInteger());
    }
}
