using Xunit;
using StellaLang;

namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Tests for the RPN s-expression parser.
/// RPN s-expressions match the VM's execution model: (operands... operation)
/// </summary>
public class SExpressionTests
{
    [Fact]
    public void SExpr_SimpleNumber()
    {
        // (42) should just push 42
        var vm = new VMActor();
        SExpressionParser.Execute("(42)", vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(42L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_SimpleAddition()
    {
        // (2 3 +) should push 2, push 3, then add → result 5
        var vm = new VMActor();
        SExpressionParser.Execute("(2 3 +)", vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(5L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_SimpleSubtraction()
    {
        // (10 3 -) should push 10, push 3, then subtract → result 7
        var vm = new VMActor();
        SExpressionParser.Execute("(10 3 -)", vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(7L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_SimpleMultiplication()
    {
        // (4 5 *) should push 4, push 5, then multiply → result 20
        var vm = new VMActor();
        SExpressionParser.Execute("(4 5 *)", vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(20L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_SimpleDivision()
    {
        // (20 4 /) should push 20, push 4, then divide → result 5
        var vm = new VMActor();
        SExpressionParser.Execute("(20 4 /)", vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(5L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_NestedExpression()
    {
        // (5 (2 3 +) *) should:
        // 1. Push 5
        // 2. Evaluate (2 3 +) → pushes 2, pushes 3, adds → 5
        // 3. Multiply → 5 * 5 = 25
        var vm = new VMActor();
        SExpressionParser.Execute("(5 (2 3 +) *)", vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(25L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_DeeplyNestedExpression()
    {
        // ((2 3 +) (4 5 *) +) should:
        // 1. Evaluate (2 3 +) → 5
        // 2. Evaluate (4 5 *) → 20
        // 3. Add → 5 + 20 = 25
        var vm = new VMActor();
        SExpressionParser.Execute("((2 3 +) (4 5 *) +)", vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(25L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_MultipleOperations()
    {
        // (10 2 + 3 *) should:
        // 1. Push 10
        // 2. Push 2
        // 3. Add → 12
        // 4. Push 3
        // 5. Multiply → 12 * 3 = 36
        var vm = new VMActor();
        SExpressionParser.Execute("(10 2 + 3 *)", vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(36L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_StackOperations()
    {
        // (5 DUP +) should:
        // 1. Push 5
        // 2. DUP → stack is now [5, 5]
        // 3. ADD → 5 + 5 = 10
        var vm = new VMActor();
        SExpressionParser.Execute("(5 DUP +)", vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(10L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_MemoryOperations()
    {
        // Test STORE and FETCH using RPN s-expressions
        // First allocate a variable, then store and fetch
        var vm = new VMActor();

        // Create a variable using the VARIABLE word
        vm.ExecuteWord("VARIABLE");
        vm.ExecuteWord("X");

        // (42 X STORE) should store 42 at address X
        // Note: STORE consumes both values from stack
        SExpressionParser.Execute("(42 X STORE)", vm);
        // STORE shouldn't leave anything on the stack normally,
        // but if it does, we clear it for the test
        while (vm.DataStackCount() > 0) vm.PopValue();

        // (X FETCH) should fetch the value from X
        SExpressionParser.Execute("(X FETCH)", vm);
        Assert.Single(vm.DataStack);
        Assert.Equal(42L, vm.PopValue().AsInteger());
    }
    [Fact]
    public void SExpr_CompileMethodReturnsBuilder()
    {
        // Test that Compile returns a ProgramBuilder that can be further manipulated
        var builder = SExpressionParser.Compile("(2 3 +)");

        // Add more operations to the builder
        var vm = new VMActor();
        builder.Push(10).Op(OpCode.MUL).Execute(vm);

        // Should have (2 + 3) * 10 = 50
        Assert.Single(vm.DataStack);
        Assert.Equal(50L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_ComplexExpression()
    {
        // ((10 5 -) (2 3 +) * 2 /) should:
        // 1. (10 5 -) → 5
        // 2. (2 3 +) → 5
        // 3. Multiply → 5 * 5 = 25
        // 4. Push 2
        // 5. Divide → 25 / 2 = 12 (integer division)
        var vm = new VMActor();
        SExpressionParser.Execute("((10 5 -) (2 3 +) * 2 /)", vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(12L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_WithSwap()
    {
        // (1 2 SWAP -) should:
        // 1. Push 1
        // 2. Push 2
        // 3. SWAP → stack is [2, 1]
        // 4. SUB → 2 - 1 = 1
        var vm = new VMActor();
        SExpressionParser.Execute("(1 2 SWAP -)", vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(1L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_MultipleValues()
    {
        // (1 2 3) should push three values
        var vm = new VMActor();
        SExpressionParser.Execute("(1 2 3)", vm);

        Assert.Equal(3, vm.DataStack.Count());
        Assert.Equal(3L, vm.PopValue().AsInteger());
        Assert.Equal(2L, vm.PopValue().AsInteger());
        Assert.Equal(1L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_EmptyList()
    {
        // An empty s-expression () should just do nothing
        var vm = new VMActor();
        SExpressionParser.Execute("()", vm);
        Assert.Empty(vm.DataStack);
    }

    [Fact]
    public void SExpr_UnclosedParenThrows()
    {
        // Unclosed parenthesis should throw
        var vm = new VMActor();
        Assert.Throws<InvalidOperationException>(() => SExpressionParser.Execute("(2 3 +", vm));
    }

    [Fact]
    public void SExpr_UnexpectedClosingParenThrows()
    {
        // Unexpected closing paren should throw
        var vm = new VMActor();
        Assert.Throws<InvalidOperationException>(() => SExpressionParser.Execute("2 3 +)", vm));
    }

    [Fact]
    public void SExpr_WithUserDefinedWord()
    {
        // Define a word and use it in s-expressions
        var vm = new VMActor();

        // Define SQUARE as DUP *
        var squareBytecode = new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("SQUARE", squareBytecode);

        // (5 SQUARE) should compute 5 * 5 = 25
        SExpressionParser.Execute("(5 SQUARE)", vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(25L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_ControlFlow_Using_IF()
    {
        // Test using IF/ELSE/THEN in s-expressions
        // We compile bytecode using IF/ELSE/THEN, then call it
        var vm = new VMActor();

        // Compile a word using ergonomic ProgramBuilder
        new ProgramBuilder()
            .If(
                thenBranch => thenBranch.CompilePush(100),
                elseBranch => elseBranch.CompilePush(200)
            )
            .CompileOp(OpCode.RETURN)
            .Execute(vm);

        int startAddr = 0;
        int endAddr = vm.Here;

        // Get the compiled bytecode using reflection
        var fi = typeof(VMActor).GetField("_dictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var dict = (byte[])fi.GetValue(vm)!;
        byte[] compiledCode = dict[startAddr..endAddr];

        vm.DefineWord("TEST-IF", compiledCode);

        // (1 TEST-IF) with condition true should return 100
        SExpressionParser.Execute("(1 TEST-IF)", vm);
        Assert.Single(vm.DataStack);
        Assert.Equal(100L, vm.PopValue().AsInteger());

        // (0 TEST-IF) with condition false should return 200
        vm = new VMActor();
        vm.DefineWord("TEST-IF", compiledCode);
        SExpressionParser.Execute("(0 TEST-IF)", vm);
        Assert.Single(vm.DataStack);
        Assert.Equal(200L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_ComplexCalculation_Quadratic()
    {
        // Calculate ax² + bx + c using RPN s-expressions
        // For a=2, b=3, c=5 and x=4:
        // 2*4*4 + 3*4 + 5 = 32 + 12 + 5 = 49

        var vm = new VMActor();

        // Define helper words
        // SQUARE: DUP *
        vm.DefineWord("SQUARE", new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build());

        // Now compute: ((x SQUARE a *) (x b *) + c +)
        // This reads as: (x² * a) + (x * b) + c
        SExpressionParser.Execute("((4 SQUARE 2 *) (4 3 *) + 5 +)", vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(49L, vm.PopValue().AsInteger());
    }

    [Fact]
    public void SExpr_MixedWithProgramBuilder()
    {
        // Demonstrate that compiled s-expressions return ProgramBuilder
        // which can be composed with other builder methods

        var vm = new VMActor();

        // Compile (2 3 +) and then add more operations via builder
        var result = SExpressionParser.Compile("(2 3 +)")
            .Push(2)            // Stack: [5, 2]
            .Op(OpCode.MUL)     // Stack: [10]
            .Push(5)            // Stack: [10, 5]
            .Op(OpCode.ADD);    // Stack: [15]

        result.Execute(vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(15L, vm.PopValue().AsInteger());
    }
}
