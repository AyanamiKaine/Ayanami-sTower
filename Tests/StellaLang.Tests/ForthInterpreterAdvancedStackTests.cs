using System;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace StellaLang.Tests;

/// <summary>
/// Tests for advanced stack manipulation words: PICK, ROLL, 2SWAP, 2OVER
/// </summary>
public class ForthInterpreterAdvancedStackTests
{
    // ===== 2DROP Tests =====

    [Fact]
    public void TwoDropBasicTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        forth.Interpret("1 2 3 4 2DROP");

        // Should have only 1 and 2 left
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(1, val1);
        Assert.Equal(2, val2);
        Assert.True(vm.DataStack.IsEmpty);
    }

    [Fact]
    public void TwoDropExactlyTwoItemsTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        forth.Interpret("10 20 2DROP");

        // Stack should be empty
        Assert.True(vm.DataStack.IsEmpty);
    }

    // ===== 2SWAP Tests =====

    [Fact]
    public void TwoSwapBasicTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // ( 1 2 3 4 -- 3 4 1 2 )
        forth.Interpret("1 2 3 4 2SWAP");

        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(3, val1);
        Assert.Equal(4, val2);
        Assert.Equal(1, val3);
        Assert.Equal(2, val4);
    }

    [Fact]
    public void TwoSwapWithDifferentValuesTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // ( 10 20 30 40 -- 30 40 10 20 )
        forth.Interpret("10 20 30 40 2SWAP");

        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(30, val1);
        Assert.Equal(40, val2);
        Assert.Equal(10, val3);
        Assert.Equal(20, val4);
    }

    [Fact]
    public void TwoSwapInColonDefinitionTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        forth.Interpret(": TESTSWAP 2SWAP ;");
        forth.Interpret("1 2 3 4 TESTSWAP");

        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(3, val1);
        Assert.Equal(4, val2);
        Assert.Equal(1, val3);
        Assert.Equal(2, val4);
    }

    // ===== 2OVER Tests =====

    [Fact]
    public void TwoOverBasicTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // ( 1 2 3 4 -- 1 2 3 4 1 2 )
        forth.Interpret("1 2 3 4 2OVER");

        long val6 = vm.DataStack.PopLong();
        long val5 = vm.DataStack.PopLong();
        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(1, val1);
        Assert.Equal(2, val2);
        Assert.Equal(3, val3);
        Assert.Equal(4, val4);
        Assert.Equal(1, val5);
        Assert.Equal(2, val6);
    }

    [Fact]
    public void TwoOverWithDifferentValuesTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // ( 10 20 30 40 -- 10 20 30 40 10 20 )
        forth.Interpret("10 20 30 40 2OVER");

        long val6 = vm.DataStack.PopLong();
        long val5 = vm.DataStack.PopLong();
        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(10, val1);
        Assert.Equal(20, val2);
        Assert.Equal(30, val3);
        Assert.Equal(40, val4);
        Assert.Equal(10, val5);
        Assert.Equal(20, val6);
    }

    [Fact]
    public void TwoOverInColonDefinitionTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        forth.Interpret(": TESTOVER 2OVER + + + + + ;");
        forth.Interpret("1 2 3 4 TESTOVER");

        // Sum: 1 + 2 + 3 + 4 + 1 + 2 = 13
        long result = vm.DataStack.PopLong();
        Assert.Equal(13, result);
    }

    // ===== PICK Tests =====

    [Fact]
    public void PickZeroEquivalentToDupTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        forth.Interpret("42 0 PICK");

        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(42, val1);
        Assert.Equal(42, val2);
    }

    [Fact]
    public void PickOneEquivalentToOverTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        forth.Interpret("10 20 1 PICK");

        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(10, val1);
        Assert.Equal(20, val2);
        Assert.Equal(10, val3);
    }

    [Fact]
    public void PickTwoTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // ( 1 2 3 2 -- 1 2 3 1 )
        forth.Interpret("1 2 3 2 PICK");

        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(1, val1);
        Assert.Equal(2, val2);
        Assert.Equal(3, val3);
        Assert.Equal(1, val4);
    }

    [Fact]
    public void PickDeepStackTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // ( 1 2 3 4 5 3 -- 1 2 3 4 5 2 )
        forth.Interpret("1 2 3 4 5 3 PICK");

        long val6 = vm.DataStack.PopLong();
        long val5 = vm.DataStack.PopLong();
        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(1, val1);
        Assert.Equal(2, val2);
        Assert.Equal(3, val3);
        Assert.Equal(4, val4);
        Assert.Equal(5, val5);
        Assert.Equal(2, val6);
    }

    [Fact]
    public void PickInColonDefinitionTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // Get third item from stack
        forth.Interpret(": THIRD 2 PICK ;");
        forth.Interpret("10 20 30 THIRD");

        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(10, val1);
        Assert.Equal(20, val2);
        Assert.Equal(30, val3);
        Assert.Equal(10, val4);
    }

    // ===== ROLL Tests =====

    [Fact]
    public void RollZeroDoesNothingTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        forth.Interpret("10 20 30 0 ROLL");

        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(10, val1);
        Assert.Equal(20, val2);
        Assert.Equal(30, val3);
    }

    [Fact]
    public void RollOneEquivalentToSwapTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // ( 10 20 1 -- 20 10 )
        forth.Interpret("10 20 1 ROLL");

        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(20, val1);
        Assert.Equal(10, val2);
    }

    [Fact]
    public void RollTwoEquivalentToRotTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // ( 1 2 3 2 -- 2 3 1 )
        forth.Interpret("1 2 3 2 ROLL");

        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(2, val1);
        Assert.Equal(3, val2);
        Assert.Equal(1, val3);
    }

    [Fact]
    public void RollThreeTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // ( 1 2 3 4 3 -- 2 3 4 1 )
        forth.Interpret("1 2 3 4 3 ROLL");

        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(2, val1);
        Assert.Equal(3, val2);
        Assert.Equal(4, val3);
        Assert.Equal(1, val4);
    }

    [Fact]
    public void RollDeepStackTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // ( 1 2 3 4 5 6 5 -- 2 3 4 5 6 1 )
        forth.Interpret("1 2 3 4 5 6 5 ROLL");

        long val6 = vm.DataStack.PopLong();
        long val5 = vm.DataStack.PopLong();
        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(2, val1);
        Assert.Equal(3, val2);
        Assert.Equal(4, val3);
        Assert.Equal(5, val4);
        Assert.Equal(6, val5);
        Assert.Equal(1, val6);
    }

    [Fact]
    public void RollInColonDefinitionTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // Bring third item to top
        forth.Interpret(": BRING3RD 2 ROLL ;");
        forth.Interpret("10 20 30 BRING3RD");

        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(20, val1);
        Assert.Equal(30, val2);
        Assert.Equal(10, val3);
    }

    // ===== Practical Usage Tests =====

    [Fact]
    public void PickAndRollTogetherTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // Use PICK to copy, then ROLL to rearrange
        forth.Interpret("1 2 3 4 5");
        forth.Interpret("3 PICK"); // Copy 2 to top: 1 2 3 4 5 2
        forth.Interpret("1 ROLL"); // Swap: 1 2 3 4 2 5

        long val6 = vm.DataStack.PopLong();
        long val5 = vm.DataStack.PopLong();
        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(1, val1);
        Assert.Equal(2, val2);
        Assert.Equal(3, val3);
        Assert.Equal(4, val4);
        Assert.Equal(2, val5);
        Assert.Equal(5, val6);
    }

    [Fact]
    public void TwoSwapAndTwoOverTogetherTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // ( 1 2 3 4 -- 3 4 1 2 -- 3 4 1 2 3 4 )
        forth.Interpret("1 2 3 4 2SWAP 2OVER");

        long val6 = vm.DataStack.PopLong();
        long val5 = vm.DataStack.PopLong();
        long val4 = vm.DataStack.PopLong();
        long val3 = vm.DataStack.PopLong();
        long val2 = vm.DataStack.PopLong();
        long val1 = vm.DataStack.PopLong();

        Assert.Equal(3, val1);
        Assert.Equal(4, val2);
        Assert.Equal(1, val3);
        Assert.Equal(2, val4);
        Assert.Equal(3, val5);
        Assert.Equal(4, val6);
    }

    [Fact]
    public void ComplexStackManipulationTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        // Define a word that uses multiple stack operations
        forth.Interpret(": COMPLEX 2OVER 2SWAP 3 PICK 2 ROLL ;");
        forth.Interpret("10 20 30 40 COMPLEX");

        // This is complex - let's just verify stack state is consistent
        int depth = vm.DataStack.Pointer / 8;
        Assert.Equal(7, depth); // Should have 7 items after all operations
    }

    // ===== Error Handling Tests =====

    [Fact]
    public void PickNegativeIndexThrowsTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        Assert.Throws<InvalidOperationException>(() =>
        {
            forth.Interpret("10 20 -1 PICK");
        });
    }

    [Fact]
    public void RollNegativeCountThrowsTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        Assert.Throws<InvalidOperationException>(() =>
        {
            forth.Interpret("10 20 -1 ROLL");
        });
    }

    [Fact]
    public void TwoSwapUnderflowTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        Assert.Throws<StackUnderflowException>(() =>
        {
            forth.Interpret("1 2 3 2SWAP"); // Only 3 items
        });
    }

    [Fact]
    public void TwoOverUnderflowTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        Assert.Throws<StackUnderflowException>(() =>
        {
            forth.Interpret("1 2 3 2OVER"); // Only 3 items
        });
    }

    [Fact]
    public void PickUnderflowTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        Assert.Throws<StackUnderflowException>(() =>
        {
            forth.Interpret("10 20 5 PICK"); // Trying to access 5th item with only 2
        });
    }

    [Fact]
    public void RollUnderflowTest()
    {
        var vm = new VM();
        using var forth = new ForthInterpreter(vm);

        Assert.Throws<StackUnderflowException>(() =>
        {
            forth.Interpret("10 20 5 ROLL"); // Trying to roll 5th item with only 2
        });
    }
}
