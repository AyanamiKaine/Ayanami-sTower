namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class ProgramExamplesTests
{
    [Fact]
    public void RpnExpression_3_4_Add_5_Mul_Is_35()
    {
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(3)
            .Push(4)
            .Op(OpCode.ADD)
            .Push(5)
            .Op(OpCode.MUL)
            .Op(OpCode.HALT)
            .Build();

        vm.LoadBytecode(bytecode);
        vm.Run();

        Assert.Single(vm.DataStack);
        Assert.Equal(35, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void CelsiusToFahrenheit_25C_Is_77F()
    {
        // F = C * 9 / 5 + 32
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(25)
            .Push(9)
            .Op(OpCode.MUL)
            .Push(5)
            .Op(OpCode.DIV)
            .Push(32)
            .Op(OpCode.ADD)
            .Op(OpCode.HALT)
            .Build();

        vm.LoadBytecode(bytecode);
        vm.Run();

        Assert.Single(vm.DataStack);
        Assert.Equal(77, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void AverageOfThreeNumbers_10_20_25_Is_18()
    {
        // (10 + 20 + 25) / 3 = 18 (integer division)
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(10)
            .Push(20)
            .Op(OpCode.ADD)
            .Push(25)
            .Op(OpCode.ADD)
            .Push(3)
            .Op(OpCode.DIV)
            .Op(OpCode.HALT)
            .Build();

        vm.LoadBytecode(bytecode);
        vm.Run();

        Assert.Single(vm.DataStack);
        Assert.Equal(18, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void Polynomial_2x3_plus_3x2_plus_x_plus_5_at_x_2_Is_35()
    {
        // Evaluate 2x^3 + 3x^2 + x + 5 at x=2 using stack ops
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            // 2x^3
            .Push(2)            // x
            .Op(OpCode.DUP)     // x x
            .Op(OpCode.DUP)     // x x x
            .Op(OpCode.MUL)     // x x^2
            .Op(OpCode.MUL)     // x^3
            .Push(2)
            .Op(OpCode.MUL)     // 2x^3
                                // + 3x^2
            .Push(2)            // x
            .Op(OpCode.DUP)     // x x
            .Op(OpCode.MUL)     // x^2
            .Push(3)
            .Op(OpCode.MUL)     // 3x^2
            .Op(OpCode.ADD)     // 2x^3 + 3x^2
                                // + x
            .Push(2)
            .Op(OpCode.ADD)     // + x
                                // + 5
            .Push(5)
            .Op(OpCode.ADD)
            .Op(OpCode.HALT)
            .Build();

        vm.LoadBytecode(bytecode);
        vm.Run();

        Assert.Single(vm.DataStack);
        Assert.Equal(35, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void MemoryAccumulator_AddsValuesUsingUserDefinedWord()
    {
        var vm = new VMActor();
        vm.Allot(16);

        // Initialize accumulator at address 0 to 0
        vm.LoadBytecode(new BytecodeBuilder()
            .Push(0)
            .Push(0)
            .Op(OpCode.STORE)
            .Op(OpCode.HALT)
            .Build());
        vm.Run();

        // Define word INC-ACC: (value -- ) acc = acc + value
        var incAcc = new BytecodeBuilder()
            .Push(0)           // addr
            .Op(OpCode.FETCH)  // acc
            .Op(OpCode.ADD)    // acc + value
            .Push(0)           // addr
            .Op(OpCode.STORE)  // store back
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("INC-ACC", incAcc);

        // Add 5 then 10
        vm.LoadBytecode(new BytecodeBuilder().Push(5).Build());
        vm.Run();
        vm.ExecuteWord("INC-ACC");

        vm.LoadBytecode(new BytecodeBuilder().Push(10).Build());
        vm.Run();
        vm.ExecuteWord("INC-ACC");

        // Read accumulator
        vm.LoadBytecode(new BytecodeBuilder().Push(0).Op(OpCode.FETCH).Op(OpCode.HALT).Build());
        vm.Run();

        Assert.Single(vm.DataStack);
        Assert.Equal(15, vm.DataStack.First().AsInteger());
    }
}
