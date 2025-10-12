using System;

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
}
