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
    /// F+ Floating-point add (this is a legacy test that should use F+)
    /// </summary>
    [Fact]
    public void FloatAddPlusSymbolTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("10.5 20.5 F+");
        // pushes two float values and adds them with F+. 
        // The result is pushed to float stack.

        double currentTopStackValue = vm.FloatStack.PeekDouble();
        Assert.Equal(31, currentTopStackValue);
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
    public void DefiningNewWord1Test()
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

    [Fact]
    public void DefiningNewWord2Test()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret(": DOUBLE 2 * ;");
        forth.Interpret(": QUADRUPLE DOUBLE DOUBLE ;");

        // Defines a new word DOUBLE that multiplies by 2.

        forth.Interpret("15 QUADRUPLE");
        // Executes QUADRUPLE.

        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(60, currentTopStackValue);
    }

    [Fact]
    public void DefiningNewWord3Test()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret(": TEST 2 5 MAX ;");

        // Defines a new word TEST that finds the maximum of 2 and 5, then multiplies by the top stack value.

        forth.Interpret("TEST");
        long currentTopStackValue = vm.DataStack.PeekCell();
        Assert.Equal(5, currentTopStackValue);
    }

    // ===== Floating-Point Arithmetic Tests =====

    /// <summary>
    /// F+ Floating-point addition
    /// </summary>
    [Fact]
    public void FloatAddTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("10.5 20.3 F+");
        // Adds 10.5 and 20.3.

        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(30.8, result, 5); // 5 decimal places precision
    }

    /// <summary>
    /// F- Floating-point subtraction
    /// </summary>
    [Fact]
    public void FloatSubtractTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("50.7 20.2 F-");
        // Subtracts 20.2 from 50.7.

        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(30.5, result, 5);
    }

    /// <summary>
    /// F* Floating-point multiplication
    /// </summary>
    [Fact]
    public void FloatMultiplyTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("2.5 4.0 F*");
        // Multiplies 2.5 by 4.0.

        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(10.0, result, 5);
    }

    /// <summary>
    /// F/ Floating-point division
    /// </summary>
    [Fact]
    public void FloatDivideTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("10.0 4.0 F/");
        // Divides 10.0 by 4.0.

        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(2.5, result, 5);
    }

    /// <summary>
    /// FNEGATE Floating-point negation
    /// </summary>
    [Fact]
    public void FloatNegateTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("42.5 FNEGATE");
        // Negates 42.5.

        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(-42.5, result, 5);
    }

    /// <summary>
    /// FABS Floating-point absolute value
    /// </summary>
    [Fact]
    public void FloatAbsoluteValueTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("-42.5 FABS");
        // Gets absolute value of -42.5.

        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(42.5, result, 5);
    }

    /// <summary>
    /// FDUP Duplicate top of float stack
    /// </summary>
    [Fact]
    public void FloatDupTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("3.14 FDUP");
        // Duplicates 3.14 on float stack.

        double top = vm.FloatStack.PopDouble();
        double second = vm.FloatStack.PopDouble();
        Assert.Equal(3.14, top, 5);
        Assert.Equal(3.14, second, 5);
    }

    /// <summary>
    /// FDROP Drop top of float stack
    /// </summary>
    [Fact]
    public void FloatDropTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("1.1 2.2 3.3 FDROP");
        // Drops 3.3 from float stack.

        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(2.2, result, 5);
    }

    /// <summary>
    /// FSWAP Swap top two float stack elements
    /// </summary>
    [Fact]
    public void FloatSwapTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("1.1 2.2 FSWAP");
        // Swaps 1.1 and 2.2.

        double top = vm.FloatStack.PopDouble();
        double second = vm.FloatStack.PopDouble();
        Assert.Equal(1.1, top, 5);
        Assert.Equal(2.2, second, 5);
    }

    /// <summary>
    /// FOVER Copy second float to top
    /// </summary>
    [Fact]
    public void FloatOverTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret("1.1 2.2 FOVER");
        // Copies 1.1 to top of float stack.

        double top = vm.FloatStack.PopDouble();
        double second = vm.FloatStack.PopDouble();
        double third = vm.FloatStack.PopDouble();
        Assert.Equal(1.1, top, 5);
        Assert.Equal(2.2, second, 5);
        Assert.Equal(1.1, third, 5);
    }

    /// <summary>
    /// Test floating-point operations in colon definition
    /// </summary>
    [Fact]
    public void FloatColonDefinitionTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret(": SQUARE FDUP F* ;");
        // Defines SQUARE that duplicates and multiplies (x * x).

        forth.Interpret("5.0 SQUARE");
        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(25.0, result, 5);
    }

    /// <summary>
    /// Test complex floating-point calculation
    /// </summary>
    [Fact]
    public void FloatComplexCalculationTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        // Calculate (3.0 + 4.0) * 2.0 = 14.0
        forth.Interpret("3.0 4.0 F+ 2.0 F*");

        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(14.0, result, 5);
    }

    // ===== Control Flow Tests (IF THEN ELSE) =====

    /// <summary>
    /// Test simple IF THEN (condition true)
    /// </summary>
    [Fact]
    public void IfThenTrueTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret(": TEST 1 IF 42 THEN ;");

        forth.Interpret("TEST");
        long result = vm.DataStack.PeekCell();
        Assert.Equal(42, result);
    }

    /// <summary>
    /// Test simple IF THEN (condition false)
    /// </summary>
    [Fact]
    public void IfThenFalseTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret(": TEST 0 IF 42 THEN ;");

        forth.Interpret("TEST");
        // Stack should be empty since IF condition was false
        Assert.Equal(0, vm.DataStack.Pointer);
    }

    /// <summary>
    /// Test IF ELSE THEN (condition true)
    /// </summary>
    [Fact]
    public void IfElseThenTrueTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret(": TEST 1 IF 42 ELSE 99 THEN ;");

        forth.Interpret("TEST");
        long result = vm.DataStack.PeekCell();
        Assert.Equal(42, result);
    }

    /// <summary>
    /// Test IF ELSE THEN (condition false)
    /// </summary>
    [Fact]
    public void IfElseThenFalseTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        forth.Interpret(": TEST 0 IF 42 ELSE 99 THEN ;");

        forth.Interpret("TEST");
        long result = vm.DataStack.PeekCell();
        Assert.Equal(99, result);
    }

    /// <summary>
    /// Test MAX implementation using IF THEN
    /// </summary>
    [Fact]
    public void MaxWithIfThenTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        // MAX: ( a b -- max ) : MAX 2DUP < IF SWAP THEN DROP ;
        forth.Interpret(": MAX 2DUP < IF SWAP THEN DROP ;");

        forth.Interpret("10 42 MAX");
        long result = vm.DataStack.PeekCell();
        Assert.Equal(42, result);

        // Test the other direction
        vm.DataStack.Clear();
        forth.Interpret("42 10 MAX");
        result = vm.DataStack.PeekCell();
        Assert.Equal(42, result);
    }

    /// <summary>
    /// Test MIN implementation using IF THEN
    /// </summary>
    [Fact]
    public void MinWithIfThenTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        // MIN: ( a b -- min ) : MIN 2DUP > IF SWAP THEN DROP ;
        forth.Interpret(": MIN 2DUP > IF SWAP THEN DROP ;");

        forth.Interpret("10 42 MIN");
        long result = vm.DataStack.PeekCell();
        Assert.Equal(10, result);

        // Test the other direction
        vm.DataStack.Clear();
        forth.Interpret("42 10 MIN");
        result = vm.DataStack.PeekCell();
        Assert.Equal(10, result);
    }

    /// <summary>
    /// Test ABS implementation using IF THEN
    /// </summary>
    [Fact]
    public void AbsWithIfThenTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);
        // ABS: ( n -- |n| ) : ABS DUP 0 < IF NEGATE THEN ;
        forth.Interpret(": ABS DUP 0 < IF NEGATE THEN ;");

        forth.Interpret("-42 ABS");
        long result = vm.DataStack.PeekCell();
        Assert.Equal(42, result);

        // Test with positive number
        vm.DataStack.Clear();
        forth.Interpret("42 ABS");
        result = vm.DataStack.PeekCell();
        Assert.Equal(42, result);
    }
}
