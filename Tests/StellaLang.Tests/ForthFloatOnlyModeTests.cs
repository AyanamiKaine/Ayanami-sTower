using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace StellaLang.Tests;

/// <summary>
/// Tests for the interpreter when running in floating-point-only numeric mode.
/// Ensures integer literals are treated as floats, arithmetic tokens map to
/// float opcodes, and compiled words use floating-point semantics.
/// </summary>
public class ForthFloatOnlyModeTests
{
    [Fact]
    public void IntegersAreTreatedAsFloatsInInterpretMode()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm, new TestHostIO(), floatOnlyMode: true);

        forth.Interpret("1 2 +");

        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(3.0, result, 8);
    }

    [Fact]
    public void CompiledDefinitionsUseFloatArithmetic()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm, new TestHostIO(), floatOnlyMode: true);

        // Define a word that uses + (which should map to FADD in float-only mode)
        forth.Interpret(": ADD-TWO + ;");
        forth.Interpret("5 7 ADD-TWO");

        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(12.0, result, 8);
    }

    [Fact]
    public void IntegerLiteralsCompileAsFloatLiterals()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm, new TestHostIO(), floatOnlyMode: true);

        // DOUBLE uses the * token which should be FMUL in float-only mode
        forth.Interpret(": DOUBLE 2 * ;");
        forth.Interpret("3 DOUBLE");

        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(6.0, result, 8);
    }

    [Fact]
    public void MixedFloatAndIntegerOperandsWork()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm, new TestHostIO(), floatOnlyMode: true);

        // Mix a float literal with an integer literal; integer should be promoted
        forth.Interpret("1.5 2 +");

        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(3.5, result, 8);
    }

    [Fact]
    public void DivisionByZeroProducesInfinityInFloatMode()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm, new TestHostIO(), floatOnlyMode: true);

        // Division by zero in floats results in Infinity rather than a VM crash
        forth.Interpret("1.0 0.0 /");

        double result = vm.FloatStack.PeekDouble();
        Assert.True(double.IsInfinity(result));
    }

    [Fact]
    public void NestedCompiledWordsUseFloatArithmetic()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm, new TestHostIO(), floatOnlyMode: true);

        // Create nested compiled words: INC increments by 1 then ADD_TWO_INC calls INC twice
        forth.Interpret(": INC 1 + ;");
        forth.Interpret(": ADD_TWO_INC INC INC ;");

        forth.Interpret("1 ADD_TWO_INC");

        double result = vm.FloatStack.PeekDouble();
        Assert.Equal(3.0, result, 8);
    }

    [Fact]
    public void SameDefinitionCompilesDifferentlyUnderModes()
    {
        // Compile and run under integer mode
        var vmInt = new VM();
        var intForth = new ForthInterpreter(vmInt, new TestHostIO(), floatOnlyMode: false);
        intForth.Interpret(": SUM 1 2 + ;");
        intForth.Interpret("SUM");
        // Result should be on the integer/data stack
        long intResult = vmInt.DataStack.PeekCell();
        Assert.Equal(3, intResult);

        // Compile and run the same source under float-only mode
        var vmFloat = new VM();
        var floatForth = new ForthInterpreter(vmFloat, new TestHostIO(), floatOnlyMode: true);
        floatForth.Interpret(": SUM 1 2 + ;");
        floatForth.Interpret("SUM");
        double floatResult = vmFloat.FloatStack.PeekDouble();
        Assert.Equal(3.0, floatResult, 8);
    }
}
