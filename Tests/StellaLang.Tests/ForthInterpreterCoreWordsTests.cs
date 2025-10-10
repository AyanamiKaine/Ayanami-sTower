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

    // ========== Tier 1: Essential Stack Manipulation Tests ==========

    /// <summary>
    /// Test 2DROP - drops two items from stack
    /// ( a b -- )
    /// </summary>
    [Fact]
    public void TwoDropTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        forth.Interpret("1 2 3 4 2DROP");

        // Should have only 1 and 2 left
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(1, val1);
        Assert.Equal(2, val2);
    }

    /// <summary>
    /// Test NIP - removes second item from stack
    /// ( a b -- b )
    /// </summary>
    [Fact]
    public void NipTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        forth.Interpret("10 20 NIP");

        long result = vm.DataStack.PeekCell();
        Assert.Equal(20, result);
    }

    /// <summary>
    /// Test TUCK - copies top item below second
    /// ( a b -- b a b )
    /// </summary>
    [Fact]
    public void TuckTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        forth.Interpret("10 20 TUCK");

        // Stack should be: 20 10 20 (top to bottom)
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(20, val1);
        Assert.Equal(10, val2);
        Assert.Equal(20, val3);
    }

    /// <summary>
    /// Test -ROT - rotates three items backwards
    /// ( a b c -- c a b )
    /// </summary>
    [Fact]
    public void MinusRotTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        forth.Interpret("1 2 3 -ROT");

        // Stack should be: 3 1 2 (top to bottom)
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(3, val1);
        Assert.Equal(1, val2);
        Assert.Equal(2, val3);
    }

    /// <summary>
    /// Test ?DUP - duplicates only if non-zero
    /// </summary>
    [Fact]
    public void QuestionDupTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Test with non-zero value
        forth.Interpret("5 ?DUP");
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();
        Assert.Equal(5, val1);
        Assert.Equal(5, val2);

        // Test with zero value
        vm.DataStack.Clear();
        forth.Interpret("0 ?DUP");
        long result = vm.DataStack.PeekCell();
        Assert.Equal(0, result);

        // Verify stack depth is 1 (not duplicated)
        vm.DataStack.PopLong();
        // Stack should be empty now - this will throw if there's more
    }

    // ========== Tier 2: Arithmetic and Logic Tests ==========

    /// <summary>
    /// Test SQUARE - squares a number
    /// ( n -- n*n )
    /// </summary>
    [Fact]
    public void SquareTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        forth.Interpret("5 SQUARE");
        long result = vm.DataStack.PeekCell();
        Assert.Equal(25, result);

        vm.DataStack.Clear();
        forth.Interpret("-3 SQUARE");
        result = vm.DataStack.PeekCell();
        Assert.Equal(9, result);
    }

    /// <summary>
    /// Test CLAMP - clamps value to range
    /// ( n min max -- n' ) where min &lt;= n' &lt;= max
    /// </summary>
    [Fact]
    public void ClampTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Test value within range
        forth.Interpret("5 0 10 CLAMP");
        long result = vm.DataStack.PopLong();
        Assert.Equal(5, result);

        // Test value below range
        forth.Interpret("-5 0 10 CLAMP");
        result = vm.DataStack.PopLong();
        Assert.Equal(0, result);

        // Test value above range
        forth.Interpret("15 0 10 CLAMP");
        result = vm.DataStack.PopLong();
        Assert.Equal(10, result);
    }

    /// <summary>
    /// Test SIGN - returns sign of number
    /// ( n -- -1|0|1 )
    /// </summary>
    [Fact]
    public void SignTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Positive number
        forth.Interpret("42 SIGN");
        long result = vm.DataStack.PopLong();
        Assert.Equal(1, result);

        // Negative number
        forth.Interpret("-42 SIGN");
        result = vm.DataStack.PopLong();
        Assert.Equal(-1, result);

        // Zero
        forth.Interpret("0 SIGN");
        result = vm.DataStack.PopLong();
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Test TRUE and FALSE constants
    /// </summary>
    [Fact]
    public void TrueFalseTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        forth.Interpret("TRUE");
        long result = vm.DataStack.PopLong();
        Assert.Equal(-1, result);

        forth.Interpret("FALSE");
        result = vm.DataStack.PopLong();
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Test NOT - logical negation
    /// </summary>
    [Fact]
    public void NotTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        forth.Interpret("0 NOT");
        long result = vm.DataStack.PopLong();
        Assert.NotEqual(0, result);  // Any non-zero is true

        forth.Interpret("42 NOT");
        result = vm.DataStack.PopLong();
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Test practical usage of new stack words
    /// </summary>
    [Fact]
    public void StackWordsComboTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Use TUCK and NIP together
        forth.Interpret(": TESTWORD TUCK + NIP ;");
        forth.Interpret("5 3 TESTWORD");

        long result = vm.DataStack.PeekCell();
        Assert.Equal(8, result);  // 5 + 3
    }

    /// <summary>
    /// Test ?DUP in conditional logic
    /// </summary>
    [Fact]
    public void QuestionDupInConditionTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Define a word that uses ?DUP for error checking
        forth.Interpret(": SAFE-DIV ?DUP IF / ELSE DROP -1 THEN ;");

        // Normal division
        forth.Interpret("10 2 SAFE-DIV");
        long result = vm.DataStack.PopLong();
        Assert.Equal(5, result);

        // Division by zero (returns -1)
        forth.Interpret("10 0 SAFE-DIV");
        result = vm.DataStack.PopLong();
        Assert.Equal(-1, result);
    }

    [Fact]
    public void ToRTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Move value to return stack
        forth.Interpret("42 >R");
        Assert.True(vm.DataStack.IsEmpty);
        Assert.Equal(8, vm.ReturnStack.Pointer); // One 8-byte value
        Assert.Equal(42, vm.ReturnStack.PeekLong());
    }

    [Fact]
    public void RFromTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Move to return stack and back
        forth.Interpret("42 >R R>");
        long result = vm.DataStack.PopLong();
        Assert.Equal(42, result);
        Assert.True(vm.ReturnStack.IsEmpty);
    }

    [Fact]
    public void RFetchTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Peek at return stack without removing
        forth.Interpret("42 >R R@");
        long result = vm.DataStack.PopLong();
        Assert.Equal(42, result);
        Assert.Equal(8, vm.ReturnStack.Pointer); // One 8-byte value still there
        Assert.Equal(42, vm.ReturnStack.PeekLong());
    }

    [Fact]
    public void ReturnStackComplexTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Test complex usage: ( a b -- a*a + b*b )
        forth.Interpret(": SUM-OF-SQUARES >R DUP * R> DUP * + ;");
        forth.Interpret("3 4 SUM-OF-SQUARES");
        long result = vm.DataStack.PopLong();
        Assert.Equal(25, result); // 3*3 + 4*4 = 9 + 16 = 25
    }

    // ========== Comment Tests ==========

    [Fact]
    public void ParenCommentTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Comments should be ignored
        forth.Interpret("( this is a comment ) 42");
        long result = vm.DataStack.PopLong();
        Assert.Equal(42, result);

        // Comments in definitions
        vm.DataStack.Clear();
        forth.Interpret(": TEST ( n -- n*2 ) 2 * ;");
        forth.Interpret("21 TEST");
        result = vm.DataStack.PopLong();
        Assert.Equal(42, result);
    }

    [Fact]
    public void BackslashCommentTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Line comments should ignore rest of line
        forth.Interpret("42 \\ this is ignored\n43");
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();
        Assert.Equal(42, val1);
        Assert.Equal(43, val2);
    }

    // ========== Memory Operations Tests ==========

    [Fact]
    public void CStoreAndFetchTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Store byte at address 100
        forth.Interpret("100 65 C!");  // Store 'A' (ASCII 65)

        // Fetch it back
        forth.Interpret("100 C@");
        long result = vm.DataStack.PopLong();
        Assert.Equal(65, result);
    }

    [Fact]
    public void CellPlusTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        forth.Interpret("100 CELL+");
        long result = vm.DataStack.PopLong();
        Assert.Equal(108, result);  // 100 + 8
    }

    [Fact]
    public void CellsTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        forth.Interpret("5 CELLS");
        long result = vm.DataStack.PopLong();
        Assert.Equal(40, result);  // 5 * 8
    }

    [Fact]
    public void CommaTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Get initial HERE
        forth.Interpret("HERE");
        long initialHere = vm.DataStack.PopLong();

        // Store a value with ,
        forth.Interpret("12345 ,");

        // Check HERE advanced by 8
        forth.Interpret("HERE");
        long newHere = vm.DataStack.PopLong();
        Assert.Equal(initialHere + 8, newHere);

        // Verify the stored value
        long storedValue = vm.Memory.ReadCellAt((int)initialHere);
        Assert.Equal(12345, storedValue);
    }

    [Fact]
    public void AllotTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Get initial HERE
        forth.Interpret("HERE");
        long initialHere = vm.DataStack.PopLong();

        // Allocate 24 bytes
        forth.Interpret("24 ALLOT");

        // Check HERE advanced by 24
        forth.Interpret("HERE");
        long newHere = vm.DataStack.PopLong();
        Assert.Equal(initialHere + 24, newHere);
    }

    [Fact]
    public void HereTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Get HERE
        forth.Interpret("HERE");
        long here1 = vm.DataStack.PopLong();

        // Allocate some space
        forth.Interpret("16 ALLOT");

        // Get HERE again
        forth.Interpret("HERE");
        long here2 = vm.DataStack.PopLong();

        // Should have moved
        Assert.Equal(here1 + 16, here2);
    }

    // ========== Arithmetic Tests ==========

    [Fact]
    public void StarSlashTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Test scaled arithmetic: 100 * 355 / 113 (approximation of 100*pi)
        forth.Interpret("100 355 113 */");
        long result = vm.DataStack.PopLong();
        Assert.Equal(314, result);  // 100 * 355 / 113 = 314.15...

        // Test another case: 1000 * 200 / 100
        vm.DataStack.Clear();
        forth.Interpret("1000 200 100 */");
        result = vm.DataStack.PopLong();
        Assert.Equal(2000, result);
    }

    // ========== I/O Words Tests (Output verification) ==========

    [Fact]
    public void DotPrintTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Capture console output
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            forth.Interpret("42 .");
            string output = sw.ToString();
            Assert.Contains("42", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DotQuoteTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            forth.Interpret(".\" Hello, World!\"");
            string output = sw.ToString();
            Assert.Contains("Hello, World!", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void EmitTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            forth.Interpret("65 EMIT");  // ASCII 'A'
            string output = sw.ToString();
            Assert.Contains("A", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void CRTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            forth.Interpret("CR");
            string output = sw.ToString();
            Assert.Contains(Environment.NewLine, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void SpaceTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            forth.Interpret("SPACE");
            string output = sw.ToString();
            Assert.Equal(" ", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== String/Character Handling Tests ==========

    [Fact]
    public void BracketCharTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Test in colon definition (compile-time)
        forth.Interpret(": TEST [CHAR] A ;");
        forth.Interpret("TEST");
        long result = vm.DataStack.PopLong();
        Assert.Equal(65, result);  // ASCII 'A'
    }

    [Fact]
    public void CharTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Test runtime character
        forth.Interpret("CHAR Z");
        long result = vm.DataStack.PopLong();
        Assert.Equal(90, result);  // ASCII 'Z'
    }

    [Fact]
    public void WordTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Parse word delimited by space
        forth.Interpret("32 WORD Hello");  // 32 is ASCII space
        long addr = vm.DataStack.PopLong();

        // Check it's a counted string
        byte length = vm.Memory[(int)addr];
        Assert.Equal(5, length);  // "Hello" is 5 chars

        // Check first character
        byte firstChar = vm.Memory[(int)addr + 1];
        Assert.Equal((byte)'H', firstChar);
    }

    [Fact]
    public void CountTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Create a counted string manually
        const int addr = 1000;
        vm.Memory[addr] = 3;  // length
        vm.Memory[addr + 1] = (byte)'F';
        vm.Memory[addr + 2] = (byte)'O';
        vm.Memory[addr + 3] = (byte)'O';

        // Use COUNT
        forth.Interpret($"{addr} COUNT");

        long length = vm.DataStack.PopLong();
        long strAddr = vm.DataStack.PopLong();

        Assert.Equal(3, length);
        Assert.Equal(addr + 1, strAddr);
    }

    [Fact]
    public void TypeTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Create a string in memory
        const int addr = 1000;
        const string text = "TEST";
        for (int i = 0; i < text.Length; i++)
        {
            vm.Memory[addr + i] = (byte)text[i];
        }

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            forth.Interpret($"{addr} 4 TYPE");
            string output = sw.ToString();
            Assert.Equal("TEST", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void WordCountTypeIntegrationTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Parse, count, and type a word
            forth.Interpret("32 WORD Testing COUNT TYPE");
            string output = sw.ToString();
            Assert.Contains("Testing", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== Combined Feature Tests ==========

    [Fact]
    public void ComplexMemoryManipulationTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Create a simple data structure using , and HERE
        forth.Interpret("HERE");  // Save start address
        long startAddr = vm.DataStack.PopLong();

        forth.Interpret("42 ,");   // Store first value
        forth.Interpret("99 ,");   // Store second value
        forth.Interpret("256 ,");  // Store third value

        // Read them back
        long val1 = vm.Memory.ReadCellAt((int)startAddr);
        long val2 = vm.Memory.ReadCellAt((int)startAddr + 8);
        long val3 = vm.Memory.ReadCellAt((int)startAddr + 16);

        Assert.Equal(42, val1);
        Assert.Equal(99, val2);
        Assert.Equal(256, val3);
    }

    [Fact]
    public void ByteArrayManipulationTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Store string as bytes
        const int addr = 2000;
        forth.Interpret($"{addr} 72 C!");   // 'H'
        forth.Interpret($"{addr} 1 + 69 C!");   // 'E'
        forth.Interpret($"{addr} 2 + 76 C!");   // 'L'
        forth.Interpret($"{addr} 3 + 76 C!");   // 'L'
        forth.Interpret($"{addr} 4 + 79 C!");   // 'O'

        // Read back and verify
        byte b1 = vm.Memory[addr];
        byte b2 = vm.Memory[addr + 1];
        byte b3 = vm.Memory[addr + 2];
        byte b4 = vm.Memory[addr + 3];
        byte b5 = vm.Memory[addr + 4];

        Assert.Equal((byte)'H', b1);
        Assert.Equal((byte)'E', b2);
        Assert.Equal((byte)'L', b3);
        Assert.Equal((byte)'L', b4);
        Assert.Equal((byte)'O', b5);
    }

    [Fact]
    public void ScaledArithmeticPracticalTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Calculate percentage: 75% of 200 = (200 * 75) / 100
        forth.Interpret(": PERCENT */  ;");
        forth.Interpret("200 75 100 PERCENT");

        long result = vm.DataStack.PopLong();
        Assert.Equal(150, result);
    }

    /// <summary>
    /// ?DO...LOOP - Basic counted loop that executes when start is less than limit
    /// </summary>
    [Fact]
    public void QuestionMarkDoLoopBasicTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Sum numbers 0 to 4: 0 + 1 + 2 + 3 + 4 = 10
        forth.Interpret(": SUM 0 5 0 ?DO I + LOOP ;");
        forth.Interpret("SUM");

        long result = vm.DataStack.PopLong();
        Assert.Equal(10, result);
    }

    /// <summary>
    /// ?DO...LOOP - Skip loop when start equals or exceeds limit (zero iterations)
    /// </summary>
    [Fact]
    public void QuestionMarkDoLoopSkipWhenStartEqualsLimitTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Should not execute loop body when start == limit
        forth.Interpret(": TEST 100 5 5 ?DO I + LOOP ;");
        forth.Interpret("TEST");

        long result = vm.DataStack.PopLong();
        Assert.Equal(100, result); // Should remain 100 (loop didn't execute)
    }

    /// <summary>
    /// ?DO...LOOP - Skip loop when start exceeds limit (zero iterations)
    /// </summary>
    [Fact]
    public void QuestionMarkDoLoopSkipWhenStartGreaterThanLimitTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Should not execute loop body when start > limit
        // ?DO takes ( limit start -- ), so use 5 10 for start=10, limit=5
        forth.Interpret(": TEST 42 5 10 ?DO I + LOOP ;");
        forth.Interpret("TEST");

        long result = vm.DataStack.PopLong();
        Assert.Equal(42, result); // Should remain 42 (loop didn't execute)
    }

    /// <summary>
    /// ?DO...LOOP - Loop with single iteration
    /// </summary>
    [Fact]
    public void QuestionMarkDoLoopSingleIterationTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Loop executes exactly once: i=0
        forth.Interpret(": TEST 0 1 0 ?DO I + LOOP ;");
        forth.Interpret("TEST");

        long result = vm.DataStack.PopLong();
        Assert.Equal(0, result); // Only i=0 is added
    }

    /// <summary>
    /// ?DO...LOOP - Using loop index I to access values
    /// </summary>
    [Fact]
    public void QuestionMarkDoLoopIndexAccessTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Multiply each index by 2 and sum: (0*2) + (1*2) + (2*2) + (3*2) = 0 + 2 + 4 + 6 = 12
        forth.Interpret(": TEST 0 4 0 ?DO I 2 * + LOOP ;");
        forth.Interpret("TEST");

        long result = vm.DataStack.PopLong();
        Assert.Equal(12, result);
    }

    /// <summary>
    /// ?DO...LOOP - Nested loops
    /// </summary>
    [Fact]
    public void QuestionMarkDoLoopNestedTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Outer loop: 0, 1, 2
        // Inner loop: for each outer, counts 0, 1
        // Total iterations: 3 * 2 = 6
        forth.Interpret(": NESTED 0 3 0 ?DO 2 0 ?DO 1 + LOOP LOOP ;");
        forth.Interpret("NESTED");

        long result = vm.DataStack.PopLong();
        Assert.Equal(6, result); // 6 total inner loop iterations
    }

    /// <summary>
    /// LOOP - Test that loop counter increments correctly
    /// </summary>
    [Fact]
    public void LoopIncrementTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Collect all loop indices: 3 + 4 + 5 + 6 + 7 + 8 + 9 = 42
        forth.Interpret(": TEST 0 10 3 ?DO I + LOOP ;");
        forth.Interpret("TEST");

        long result = vm.DataStack.PopLong();
        Assert.Equal(42, result);
    }

    /// <summary>
    /// ?DO...LOOP - Large range test
    /// </summary>
    [Fact]
    public void QuestionMarkDoLoopLargeRangeTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Sum 0 to 99: (99 * 100) / 2 = 4950
        forth.Interpret(": SUM100 0 100 0 ?DO I + LOOP ;");
        forth.Interpret("SUM100");

        long result = vm.DataStack.PopLong();
        Assert.Equal(4950, result);
    }

    /// <summary>
    /// ?DO...LOOP - Using I multiple times in loop body
    /// </summary>
    [Fact]
    public void QuestionMarkDoLoopMultipleIndexUsageTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Square each index and sum: 0^2 + 1^2 + 2^2 + 3^2 = 0 + 1 + 4 + 9 = 14
        forth.Interpret(": SUMSQUARES 0 4 0 ?DO I I * + LOOP ;");
        forth.Interpret("SUMSQUARES");

        long result = vm.DataStack.PopLong();
        Assert.Equal(14, result);
    }

    /// <summary>
    /// ?DO...LOOP - Edge case with negative start
    /// </summary>
    [Fact]
    public void QuestionMarkDoLoopNegativeStartTest()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Sum from -2 to 2 inclusive: -2 + -1 + 0 + 1 + 2 = 0
        forth.Interpret(": TEST 0 3 -2 ?DO I + LOOP ;");
        forth.Interpret("TEST");

        long result = vm.DataStack.PopLong();
        Assert.Equal(0, result);
    }
}



