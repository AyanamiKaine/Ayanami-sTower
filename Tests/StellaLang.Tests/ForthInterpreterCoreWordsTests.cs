using System;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace StellaLang.Tests;

/// <summary>
/// Here we are testing the core Forth words implemented in the ForthInterpreter.
/// </summary>
public class ForthInterpreterCoreWordsTests
{

    /// <summary>
    /// ! store
    /// </summary>
    [Fact]
    public void StoreExclamationMarkTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("10 20 !");
        // Store 20 at address 10

        long result = vm.Memory.ReadCellAt(10);
        Assert.Equal(20, result);
    }

    /// <summary>
    /// @ Fetch
    /// </summary>
    [Fact]
    public void FetchAtSymbolTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("10 @");
        // fetch the value at address 10 push it to stack

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(20, currentTopStackValue);
    }

    /// <summary>
    /// + Add
    /// </summary>
    [Fact]
    public void AddPlusSymbolTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("10 20 +");
        // pushes two values and adds them. 
        // The result is pushed to stack.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(30, currentTopStackValue);
    }
}
