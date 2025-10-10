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

    /// <summary>
    /// Test BEGIN...UNTIL loop construct
    /// T{ : GI4 BEGIN DUP 1+ DUP 5 &gt; UNTIL ; -&gt; }T
    /// T{ 3 GI4 -&gt; 3 4 5 6 }T
    /// </summary>
    [Fact]
    public void BeginUntilLoopTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Define: : GI4 BEGIN DUP 1+ DUP 5 > UNTIL ;
        forth.Interpret(": GI4 BEGIN DUP 1+ DUP 5 > UNTIL ;");

        // Test: 3 GI4 -> 3 4 5 6
        forth.Interpret("3 GI4");

        // Stack should have: 3 4 5 6 (bottom to top)
        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(3, val1);
        Assert.Equal(4, val2);
        Assert.Equal(5, val3);
        Assert.Equal(6, val4);
    }

    /// <summary>
    /// Test BEGIN...UNTIL loop with different start values
    /// T{ 5 GI4 -&gt; 5 6 }T
    /// T{ 6 GI4 -&gt; 6 7 }T
    /// </summary>
    [Fact]
    public void BeginUntilLoopWithDifferentStartsTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        forth.Interpret(": GI4 BEGIN DUP 1+ DUP 5 > UNTIL ;");

        // Test: 5 GI4 -> 5 6
        forth.Interpret("5 GI4");
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();
        Assert.Equal(5, val1);
        Assert.Equal(6, val2);

        // Test: 6 GI4 -> 6 7
        vm.DataStack.Clear();
        forth.Interpret("6 GI4");
        val2 = vm.DataStack.PopLong();
        val1 = vm.DataStack.PopLong();
        Assert.Equal(6, val1);
        Assert.Equal(7, val2);
    }

    /// <summary>
    /// Test BEGIN...WHILE...REPEAT loop construct
    /// T{ : GI3 BEGIN DUP 5 &lt; WHILE DUP 1+ REPEAT ; -&gt; }T
    /// T{ 0 GI3 -&gt; 0 1 2 3 4 5 }T
    /// </summary>
    [Fact]
    public void BeginWhileRepeatLoopTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Define: : GI3 BEGIN DUP 5 < WHILE DUP 1+ REPEAT ;
        forth.Interpret(": GI3 BEGIN DUP 5 < WHILE DUP 1+ REPEAT ;");

        // Test: 0 GI3 -> 0 1 2 3 4 5
        forth.Interpret("0 GI3");

        long val6 = vm.DataStack.PopLong();
        long val5 = vm.DataStack.PopLong();
        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(0, val1);
        Assert.Equal(1, val2);
        Assert.Equal(2, val3);
        Assert.Equal(3, val4);
        Assert.Equal(4, val5);
        Assert.Equal(5, val6);
    }

    /// <summary>
    /// Test BEGIN...WHILE...REPEAT with different start values
    /// T{ 4 GI3 -&gt; 4 5 }T
    /// T{ 5 GI3 -&gt; 5 }T
    /// T{ 6 GI3 -&gt; 6 }T
    /// </summary>
    [Fact]
    public void BeginWhileRepeatWithDifferentStartsTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        forth.Interpret(": GI3 BEGIN DUP 5 < WHILE DUP 1+ REPEAT ;");

        // Test: 4 GI3 -> 4 5
        forth.Interpret("4 GI3");
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();
        Assert.Equal(4, val1);
        Assert.Equal(5, val2);

        // Test: 5 GI3 -> 5
        vm.DataStack.Clear();
        forth.Interpret("5 GI3");
        val1 = vm.DataStack.PopLong();
        Assert.Equal(5, val1);

        // Test: 6 GI3 -> 6
        vm.DataStack.Clear();
        forth.Interpret("6 GI3");
        val1 = vm.DataStack.PopLong();
        Assert.Equal(6, val1);
    }

    /// <summary>
    /// Test nested BEGIN...WHILE loops
    /// T{ : GI5 BEGIN DUP 2 &gt; WHILE DUP 5 &lt; WHILE DUP 1+ REPEAT 123 ELSE 345 THEN ; -&gt; }T
    /// T{ 1 GI5 -&gt; 1 345 }T
    /// T{ 2 GI5 -&gt; 2 345 }T
    /// T{ 3 GI5 -&gt; 3 4 5 123 }T
    /// </summary>
    [Fact]
    public void NestedBeginWhileLoopsTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Define: : GI5 BEGIN DUP 2 > WHILE DUP 5 < WHILE DUP 1+ REPEAT 123 ELSE 345 THEN ;
        forth.Interpret(": GI5 BEGIN DUP 2 > WHILE DUP 5 < WHILE DUP 1+ REPEAT 123 ELSE 345 THEN ;");

        // Test: 1 GI5 -> 1 345
        forth.Interpret("1 GI5");
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();
        Assert.Equal(1, val1);
        Assert.Equal(345, val2);

        // Test: 2 GI5 -> 2 345
        vm.DataStack.Clear();
        forth.Interpret("2 GI5");
        val2 = vm.DataStack.PopLong();
        val1 = vm.DataStack.PopLong();
        Assert.Equal(2, val1);
        Assert.Equal(345, val2);

        // Test: 3 GI5 -> 3 4 5 123
        vm.DataStack.Clear();
        forth.Interpret("3 GI5");
        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        val2 = vm.DataStack.PopLong();
        val1 = vm.DataStack.PopLong();
        Assert.Equal(3, val1);
        Assert.Equal(4, val2);
        Assert.Equal(5, val3);
        Assert.Equal(123, val4);
    }

    /// <summary>
    /// Test VARIABLE word
    /// T{ VARIABLE V1 -&gt; }T
    /// T{ 123 V1 ! -&gt; }T
    /// T{ V1 @ -&gt; 123 }T
    /// </summary>
    [Fact]
    public void VariableTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Create a variable
        forth.Interpret("VARIABLE V1");

        // Store 123 in the variable (using address value ! convention)
        forth.Interpret("V1 123 !");

        // Fetch the value
        forth.Interpret("V1 @");

        long result = vm.DataStack.PeekCell();
        Assert.Equal(123, result);
    }

    /// <summary>
    /// Test multiple VARIABLEs
    /// </summary>
    [Fact]
    public void MultipleVariablesTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Create multiple variables
        forth.Interpret("VARIABLE X");
        forth.Interpret("VARIABLE Y");
        forth.Interpret("VARIABLE Z");

        // Store different values (using address value ! convention)
        forth.Interpret("X 42 !");
        forth.Interpret("Y 99 !");
        forth.Interpret("Z 256 !");

        // Fetch and verify X
        forth.Interpret("X @");
        long result = vm.DataStack.PopLong();
        Assert.Equal(42, result);

        // Fetch and verify Y
        forth.Interpret("Y @");
        result = vm.DataStack.PopLong();
        Assert.Equal(99, result);

        // Fetch and verify Z
        forth.Interpret("Z @");
        result = vm.DataStack.PopLong();
        Assert.Equal(256, result);
    }

    /// <summary>
    /// Test VARIABLE in a colon definition
    /// </summary>
    [Fact]
    public void VariableInColonDefinitionTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Create a variable
        forth.Interpret("VARIABLE COUNTER");

        // Define a word that uses the variable (using address value ! convention)
        forth.Interpret(": INCR COUNTER @ 1 + COUNTER SWAP ! ;");

        // Initialize counter to 0 (using address value ! convention)
        forth.Interpret("COUNTER 0 !");

        // Increment three times
        forth.Interpret("INCR");
        forth.Interpret("INCR");
        forth.Interpret("INCR");

        // Check the result
        forth.Interpret("COUNTER @");
        long result = vm.DataStack.PeekCell();
        Assert.Equal(3, result);
    }
}

