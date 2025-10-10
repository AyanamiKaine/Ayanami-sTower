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
        forth.Interpret("10 20 !");
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

    /// <summary>
    /// * Multiply
    /// </summary>
    [Fact]
    public void MultiplyAsteriskSymbolTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("10 20 *");
        // pushes two values and multiplies them. 
        // The result is pushed to stack.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(200, currentTopStackValue);
    }

    /// <summary>
    /// - Subtract
    /// </summary>
    [Fact]
    public void SubtractMinusSymbolTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("50 20 -");
        // pushes two values and subtracts them (50 - 20). 
        // The result is pushed to stack.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(30, currentTopStackValue);
    }

    /// <summary>
    /// / Divide
    /// </summary>
    [Fact]
    public void DivideSlashSymbolTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("100 20 /");
        // pushes two values and divides them (100 / 20). 
        // The result is pushed to stack.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(5, currentTopStackValue);
    }

    /// <summary>
    /// MOD Modulo
    /// </summary>
    [Fact]
    public void ModuloTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("17 5 MOD");
        // pushes two values and computes modulo (17 % 5). 
        // The result is pushed to stack.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(2, currentTopStackValue);
    }

    /// <summary>
    /// /MOD Divide and Modulo (returns both quotient and remainder)
    /// </summary>
    [Fact]
    public void DivModTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("17 5 /MOD");
        // Divides 17 by 5, pushes remainder (2) then quotient (3).
        // Stack after: remainder=2, quotient=3 (top)

        long quotient = vm.DataStack.PopLong();
        long remainder = vm.DataStack.PopLong();
        Assert.Equal(3, quotient);
        Assert.Equal(2, remainder);
    }

    /// <summary>
    /// NEGATE Negate a number
    /// </summary>
    [Fact]
    public void NegateTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("42 NEGATE");
        // Negates 42 to -42.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(-42, currentTopStackValue);
    }

    /// <summary>
    /// ABS Absolute value
    /// </summary>
    [Fact]
    public void AbsoluteValueTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("-42 ABS");
        // Returns absolute value of -42.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(42, currentTopStackValue);
    }

    /// <summary>
    /// 1+ Increment by 1
    /// </summary>
    [Fact]
    public void IncrementOnePlusTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("41 1+");
        // Increments 41 by 1.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(42, currentTopStackValue);
    }

    /// <summary>
    /// 1- Decrement by 1
    /// </summary>
    [Fact]
    public void DecrementOneMinusTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("43 1-");
        // Decrements 43 by 1.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(42, currentTopStackValue);
    }

    /// <summary>
    /// 2* Multiply by 2 (left shift)
    /// </summary>
    [Fact]
    public void MultiplyByTwoTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("21 2*");
        // Multiplies 21 by 2.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(42, currentTopStackValue);
    }

    /// <summary>
    /// 2/ Divide by 2 (right shift)
    /// </summary>
    [Fact]
    public void DivideByTwoTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("84 2/");
        // Divides 84 by 2.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(42, currentTopStackValue);
    }

    /// <summary>
    /// MAX Return maximum of two values
    /// </summary>
    [Fact]
    public void MaximumTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("10 42 MAX");
        // Returns the maximum of 10 and 42.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(42, currentTopStackValue);
    }

    /// <summary>
    /// MIN Return minimum of two values
    /// </summary>
    [Fact]
    public void MinimumTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("10 42 MIN");
        // Returns the minimum of 10 and 42.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(10, currentTopStackValue);
    }

    [Fact]
    public void DefiningNewWord()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret(": DOUBLE 2 * ;");
        // Defines a new word DOUBLE that multiplies by 2.

        forth.Interpret("15 DOUBLE");
        // Executes DOUBLE.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(30, currentTopStackValue);
    }
}

