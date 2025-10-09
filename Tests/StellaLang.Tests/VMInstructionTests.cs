namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Here we are testing the instructions of the VM
/// </summary>
public class VMInstructionTests
{
    [Fact]
    public void PushVMInstructionTest()
    {
        var vm = new VM();

        // The VM should intepret the byte code so it sees push and interprets the next string as an integer.
        // and pushes the value onto the data stack. It should push four bytes (32bit integer = 4 bytes)
        const string code =
        """
        PUSH 839
        """;
        vm.Execute(code);

        var currentTopStackValue = vm.DataStack.PeekInt();
        const int expectedTopValue = 839;

        Assert.Equal(expectedTopValue, currentTopStackValue);
    }
}
