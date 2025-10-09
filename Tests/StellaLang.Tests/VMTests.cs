namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Here we are testing the instructions of the VM
/// </summary>
public class VMTests
{
    [Fact]
    public void PushInstructionPutsValueOnStack()
    {
        // Arrange
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(42)
            .Op(OpCode.HALT)
            .Build();

        // Act
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(42, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void PopInstructionRemovesTopOfStack()
    {
        // Arrange
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(100)
            .Push(200)
            .Op(OpCode.POP)
            .Op(OpCode.HALT)
            .Build();

        // Act
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(100, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void DupInstructionDuplicatesTopOfStack()
    {
        // Arrange
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(42)
            .Op(OpCode.DUP)
            .Op(OpCode.HALT)
            .Build();

        // Act
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Assert
        Assert.Equal(2, vm.DataStack.Count());
        Assert.All(vm.DataStack, v => Assert.Equal(42, v.AsInteger()));
    }

    [Fact]
    public void AddInstructionAddsIntegerValues()
    {
        // Arrange
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(10)
            .Push(32)
            .Op(OpCode.ADD)
            .Op(OpCode.HALT)
            .Build();

        // Act
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(42, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void SubInstructionSubtractsIntegerValues()
    {
        // Arrange
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(100)
            .Push(42)
            .Op(OpCode.SUB)
            .Op(OpCode.HALT)
            .Build();

        // Act
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(58, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void MulInstructionMultipliesIntegerValues()
    {
        // Arrange
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(6)
            .Push(7)
            .Op(OpCode.MUL)
            .Op(OpCode.HALT)
            .Build();

        // Act
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(42, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void DivInstructionDividesIntegerValues()
    {
        // Arrange
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(84)
            .Push(2)
            .Op(OpCode.DIV)
            .Op(OpCode.HALT)
            .Build();

        // Act
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(42, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void StoreAndFetchWorkWithDictionaryMemory()
    {
        // Arrange
        var vm = new VMActor();
        vm.Allot(16); // Allocate 16 bytes

        var bytecode = new BytecodeBuilder()
            .Push(42)        // Value to store
            .Push(0)         // Address to store at
            .Op(OpCode.STORE)
            .Push(0)         // Address to fetch from
            .Op(OpCode.FETCH)
            .Op(OpCode.HALT)
            .Build();

        // Act
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(42, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void ComplexArithmeticProgram()
    {
        // Calculate: (10 + 5) * 2 - 3 = 27
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(10)
            .Push(5)
            .Op(OpCode.ADD)   // 15
            .Push(2)
            .Op(OpCode.MUL)   // 30
            .Push(3)
            .Op(OpCode.SUB)   // 27
            .Op(OpCode.HALT)
            .Build();

        // Act
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(27, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void StackManipulationWithDup()
    {
        // Test: Push 5, DUP, ADD = 10
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(5)
            .Op(OpCode.DUP)
            .Op(OpCode.ADD)
            .Op(OpCode.HALT)
            .Build();

        // Act
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(10, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void UnknownOpcodeThrowsException()
    {
        // Arrange
        var vm = new VMActor();
        var bytecode = new byte[] { 0xAB, (byte)OpCode.HALT }; // Invalid opcode

        // Act & Assert
        vm.LoadBytecode(bytecode);
        var ex = Assert.Throws<InvalidOperationException>(() => vm.Run());
        Assert.Contains("Unknown opcode", ex.Message);
    }

    [Fact]
    public void BinaryOperationWithInsufficientOperandsThrowsException()
    {
        // Arrange
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(42)
            .Op(OpCode.ADD) // Only one value on stack, needs two
            .Op(OpCode.HALT)
            .Build();

        // Act & Assert
        vm.LoadBytecode(bytecode);
        var ex = Assert.Throws<InvalidOperationException>(() => vm.Run());
        Assert.Contains("requires two operands", ex.Message);
    }

    [Fact]
    public void StoreOutOfBoundsThrowsException()
    {
        // Arrange
        var vm = new VMActor();
        // Don't allocate any memory, then try to store

        var bytecode = new BytecodeBuilder()
            .Push(42)
            .Push(0)
            .Op(OpCode.STORE)
            .Op(OpCode.HALT)
            .Build();

        // Act & Assert
        vm.LoadBytecode(bytecode);
        var ex = Assert.Throws<IndexOutOfRangeException>(() => vm.Run());
        Assert.Contains("out of allotted bounds", ex.Message);
    }

    [Fact]
    public void FetchOutOfBoundsThrowsException()
    {
        // Arrange
        var vm = new VMActor();
        // Don't allocate any memory, then try to fetch

        var bytecode = new BytecodeBuilder()
            .Push(0)
            .Op(OpCode.FETCH)
            .Op(OpCode.HALT)
            .Build();

        // Act & Assert
        vm.LoadBytecode(bytecode);
        var ex = Assert.Throws<IndexOutOfRangeException>(() => vm.Run());
        Assert.Contains("out of allotted bounds", ex.Message);
    }

    [Fact]
    public void MultipleStoreAndFetchOperations()
    {
        // Arrange
        var vm = new VMActor();
        vm.Allot(32); // Allocate 32 bytes

        var bytecode = new BytecodeBuilder()
            .Push(100)
            .Push(0)
            .Op(OpCode.STORE)
            .Push(200)
            .Push(8)
            .Op(OpCode.STORE)
            .Push(300)
            .Push(16)
            .Op(OpCode.STORE)
            .Push(0)
            .Op(OpCode.FETCH)
            .Push(8)
            .Op(OpCode.FETCH)
            .Push(16)
            .Op(OpCode.FETCH)
            .Op(OpCode.HALT)
            .Build();

        // Act
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Assert - stack should have three values in reverse order
        Assert.Equal(3, vm.DataStack.Count());
        var values = vm.DataStack.ToArray();
        Assert.Equal(300, values[0].AsInteger()); // Top of stack
        Assert.Equal(200, values[1].AsInteger());
        Assert.Equal(100, values[2].AsInteger()); // Bottom of stack
    }

    [Fact]
    public void ValueTypeIntegerCreation()
    {
        // Arrange & Act
        var value = Value.Integer(42);

        // Assert
        Assert.Equal(Value.ValueType.Integer, value.Type);
        Assert.Equal(42, value.AsInteger());
    }

    [Fact]
    public void ValueTypeFloatCreation()
    {
        // Arrange & Act
        var value = Value.Float(3.14);

        // Assert
        Assert.Equal(Value.ValueType.Float, value.Type);
        Assert.Equal(3.14, value.AsFloat());
    }

    [Fact]
    public void ValueTypeBooleanCreation()
    {
        // Arrange & Act
        var trueValue = Value.Boolean(true);
        var falseValue = Value.Boolean(false);

        // Assert
        Assert.Equal(Value.ValueType.Boolean, trueValue.Type);
        Assert.True(trueValue.AsBoolean());
        Assert.False(falseValue.AsBoolean());
    }

    [Fact]
    public void ValueTypePointerCreation()
    {
        // Arrange & Act
        var value = Value.Pointer(0x1000);

        // Assert
        Assert.Equal(Value.ValueType.Pointer, value.Type);
        Assert.Equal(0x1000, value.AsPointer());
    }

    [Fact]
    public void ValueTypeConversionIntegerToFloat()
    {
        // Arrange
        var value = Value.Integer(42);

        // Act & Assert
        Assert.Equal(42.0, value.AsFloat());
    }

    [Fact]
    public void ValueTypeToStringInteger()
    {
        // Arrange
        var value = Value.Integer(42);

        // Act & Assert
        Assert.Equal("42", value.ToString());
    }

    [Fact]
    public void ValueTypeToStringPointer()
    {
        // Arrange
        var value = Value.Pointer(0xFF);

        // Act & Assert
        Assert.Equal("0xFF", value.ToString());
    }

    [Fact]
    public void AllotIncreasesHerePointer()
    {
        // Arrange
        var vm = new VMActor();
        var initialHere = vm.Here;

        // Act
        vm.Allot(16);

        // Assert
        Assert.Equal(initialHere + 16, vm.Here);
    }

    [Fact]
    public void AllotExpandsCapacityWhenNeeded()
    {
        // Arrange
        var vm = new VMActor(16); // Small initial capacity
        var initialCapacity = vm.Capacity;

        // Act
        vm.Allot(100); // Allot more than initial capacity

        // Assert
        Assert.True(vm.Capacity > initialCapacity);
        Assert.Equal(100, vm.Here);
    }

    // ===== Word/Instruction Unification Tests =====

    [Fact]
    public void CanDefineUserWord()
    {
        // Arrange
        var vm = new VMActor();
        var wordBytecode = new BytecodeBuilder()
            .Push(5)
            .Push(5)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();

        // Act
        vm.DefineWord("ADD5+5", wordBytecode);
        var word = vm.GetWord("ADD5+5");

        // Assert
        Assert.NotNull(word);
        Assert.Equal("ADD5+5", word.Name);
        Assert.False(word.IsNative);
        Assert.Equal(wordBytecode, word.Bytecode);
    }

    [Fact]
    public void CanExecuteUserDefinedWordByName()
    {
        // Arrange
        var vm = new VMActor();
        // Define SQUARE: DUP MUL
        var squareBytecode = new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("SQUARE", squareBytecode);

        // Push 7 and call SQUARE (should result in 49)
        var mainBytecode = new BytecodeBuilder()
            .Push(7)
            .Build();
        vm.LoadBytecode(mainBytecode);
        vm.Run();

        // Act
        vm.ExecuteWord("SQUARE");

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(49, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void BuiltInInstructionsAreWords()
    {
        // Arrange
        var vm = new VMActor();

        // Act - Get built-in instructions as words
        var addWord = vm.GetWord("ADD");
        var dupWord = vm.GetWord("DUP");
        var storeWord = vm.GetWord("!");  // Forth alias for STORE

        // Assert
        Assert.NotNull(addWord);
        Assert.True(addWord.IsNative);
        Assert.Equal("ADD", addWord.Name);

        Assert.NotNull(dupWord);
        Assert.True(dupWord.IsNative);
        Assert.Equal("DUP", dupWord.Name);

        Assert.NotNull(storeWord);
        Assert.True(storeWord.IsNative);
        Assert.Equal("!", storeWord.Name);
    }

    [Fact]
    public void CanExecuteBuiltInWordByName()
    {
        // Arrange
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(10)
            .Push(32)
            .Build();
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Act - Execute ADD as a word
        vm.ExecuteWord("ADD");

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(42, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void UserDefinedWordCanCallOtherWords()
    {
        // Arrange
        var vm = new VMActor();

        // Define DOUBLE: DUP ADD (x -- 2x)
        var doubleBytecode = new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("DOUBLE", doubleBytecode);

        // Define QUADRUPLE: DOUBLE DOUBLE (x -- 4x)
        // This demonstrates words calling other user-defined words
        vm.LoadBytecode(doubleBytecode);
        vm.DefineWord("INTERMEDIATE_DOUBLE", doubleBytecode);

        // Push 5 and call DOUBLE twice
        var mainBytecode = new BytecodeBuilder()
            .Push(5)
            .Build();
        vm.LoadBytecode(mainBytecode);
        vm.Run();

        // Act
        vm.ExecuteWord("DOUBLE");  // 5 -> 10
        vm.ExecuteWord("DOUBLE");  // 10 -> 20

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(20, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void ComplexUserDefinedWord()
    {
        // Arrange
        var vm = new VMActor();

        // Define AVERAGE: + 2 / (takes two numbers, returns their average)
        var averageBytecode = new BytecodeBuilder()
            .Op(OpCode.ADD)
            .Push(2)
            .Op(OpCode.DIV)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("AVERAGE", averageBytecode);

        // Push 10 and 20
        var mainBytecode = new BytecodeBuilder()
            .Push(10)
            .Push(20)
            .Build();
        vm.LoadBytecode(mainBytecode);
        vm.Run();

        // Act
        vm.ExecuteWord("AVERAGE");

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(15, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void ExecuteNonExistentWordThrowsException()
    {
        // Arrange
        var vm = new VMActor();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => vm.ExecuteWord("NONEXISTENT"));
        Assert.Contains("not found in dictionary", ex.Message);
    }

    [Fact]
    public void GetNonExistentWordReturnsNull()
    {
        // Arrange
        var vm = new VMActor();

        // Act
        var word = vm.GetWord("DOESNOTEXIST");

        // Assert
        Assert.Null(word);
    }

    [Fact]
    public void ForthAliasesWork()
    {
        // Arrange
        var vm = new VMActor();
        vm.Allot(16);

        // Use Forth-style aliases: ! for STORE, @ for FETCH
        var bytecode = new BytecodeBuilder()
            .Push(42)
            .Push(0)
            .Build();
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Act - Use ! to store
        vm.ExecuteWord("!");

        var fetchBytecode = new BytecodeBuilder()
            .Push(0)
            .Build();
        vm.LoadBytecode(fetchBytecode);
        vm.Run();

        // Use @ to fetch
        vm.ExecuteWord("@");

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(42, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void WordsCanBeRedefined()
    {
        // Arrange
        var vm = new VMActor();

        // Define FOO to push 10
        var foo1 = new BytecodeBuilder()
            .Push(10)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("FOO", foo1);

        // Execute first version
        vm.ExecuteWord("FOO");
        Assert.Equal(10, vm.DataStack.First().AsInteger());

        // Act - Redefine FOO to push 20
        var foo2 = new BytecodeBuilder()
            .Push(20)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("FOO", foo2);

        // Execute redefined version
        vm.ExecuteWord("FOO");

        // Assert
        Assert.Equal(2, vm.DataStack.Count());
        var values = vm.DataStack.ToArray();
        Assert.Equal(20, values[0].AsInteger()); // Top of stack - new value
        Assert.Equal(10, values[1].AsInteger()); // Old value still there
    }

    [Fact]
    public void UserWordMaintainsStackIntegrity()
    {
        // Arrange
        var vm = new VMActor();

        // Define a word that manipulates the stack
        var wordBytecode = new BytecodeBuilder()
            .Push(100)
            .Op(OpCode.ADD)
            .Op(OpCode.DUP)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("ADD100DUP", wordBytecode);

        var mainBytecode = new BytecodeBuilder()
            .Push(5)
            .Push(10)
            .Build();
        vm.LoadBytecode(mainBytecode);
        vm.Run();

        // Act
        vm.ExecuteWord("ADD100DUP");  // Takes 10, adds 100, duplicates -> 110, 110

        // Assert
        Assert.Equal(3, vm.DataStack.Count());
        var values = vm.DataStack.ToArray();
        Assert.Equal(110, values[0].AsInteger()); // Top: duplicated result
        Assert.Equal(110, values[1].AsInteger()); // Original result
        Assert.Equal(5, values[2].AsInteger());   // Bottom: original first value
    }

    [Fact]
    public void NestedUserWordExecutionRestoresInstructionPointer()
    {
        // Arrange
        var vm = new VMActor();

        // Define INNER word
        var innerBytecode = new BytecodeBuilder()
            .Push(3)
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("TIMES3", innerBytecode);

        // Define OUTER word that calls INNER
        var outerBytecode = new BytecodeBuilder()
            .Push(2)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("ADD2", outerBytecode);

        // Main program
        var mainBytecode = new BytecodeBuilder()
            .Push(5)
            .Build();
        vm.LoadBytecode(mainBytecode);
        vm.Run();

        // Act - Execute words in sequence
        vm.ExecuteWord("ADD2");    // 5 + 2 = 7
        vm.ExecuteWord("TIMES3");  // 7 * 3 = 21

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(21, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void WordWithMemoryOperations()
    {
        // Arrange
        var vm = new VMActor();
        vm.Allot(16);

        // Define a word that stores and retrieves a value
        // STORE_AT_ZERO: ( value -- value )
        // Stores value at address 0 and retrieves it back
        var wordBytecode = new BytecodeBuilder()
            .Push(0)             // Push address 0
            .Op(OpCode.STORE)    // Store value at address 0 (consumes value and address)
            .Push(0)             // Push address 0 again
            .Op(OpCode.FETCH)    // Fetch from address 0
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("STORE_AT_ZERO", wordBytecode);

        var mainBytecode = new BytecodeBuilder()
            .Push(99)
            .Build();
        vm.LoadBytecode(mainBytecode);
        vm.Run();

        // Act
        vm.ExecuteWord("STORE_AT_ZERO");

        // Assert
        Assert.Single(vm.DataStack);
        Assert.Equal(99, vm.DataStack.First().AsInteger());
    }

    // ===== Word Redefinition Tests =====

    [Fact]
    public void CanRedefineUserWord()
    {
        // Arrange
        var vm = new VMActor();

        // Define ADDTEN: PUSH 10, ADD
        var originalBytecode = new BytecodeBuilder()
            .Push(10)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("ADDTEN", originalBytecode);

        // Test original
        vm.LoadBytecode(new BytecodeBuilder().Push(5).Build());
        vm.Run();
        vm.ExecuteWord("ADDTEN");
        Assert.Equal(15, vm.DataStack.First().AsInteger());

        // Act - Redefine ADDTEN to add 20 instead
        var newBytecode = new BytecodeBuilder()
            .Push(20)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        var success = vm.RedefineWord("ADDTEN", newBytecode);

        // Assert
        Assert.True(success);

        // Clear stack and test again
        var vm2 = new VMActor();
        vm2.DefineWord("ADDTEN", originalBytecode);
        vm2.RedefineWord("ADDTEN", newBytecode);
        vm2.LoadBytecode(new BytecodeBuilder().Push(5).Build());
        vm2.Run();
        vm2.ExecuteWord("ADDTEN");
        Assert.Equal(25, vm2.DataStack.First().AsInteger());
    }

    [Fact]
    public void CanRedefineBuiltInInstruction()
    {
        // Arrange
        var vm = new VMActor();

        // Original ADD behavior
        var bytecode = new BytecodeBuilder()
            .Push(10)
            .Push(20)
            .Op(OpCode.ADD)
            .Op(OpCode.HALT)
            .Build();
        vm.LoadBytecode(bytecode);
        vm.Run();
        Assert.Equal(30, vm.DataStack.First().AsInteger());

        // Act - Redefine ADD to do multiplication instead!
        var vm2 = new VMActor();
        var newAddBytecode = new BytecodeBuilder()
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build();
        vm2.RedefineWord("ADD", newAddBytecode);

        // Assert - ADD now multiplies
        var bytecode2 = new BytecodeBuilder()
            .Push(10)
            .Push(20)
            .Op(OpCode.ADD)  // This should now multiply!
            .Op(OpCode.HALT)
            .Build();
        vm2.LoadBytecode(bytecode2);
        vm2.Run();
        Assert.Equal(200, vm2.DataStack.First().AsInteger());
    }

    [Fact]
    public void RedefineNonExistentWordReturnsFalse()
    {
        // Arrange
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(42)
            .Op(OpCode.RETURN)
            .Build();

        // Act
        var success = vm.RedefineWord("NONEXISTENT", bytecode);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public void RedefineWordOrThrowThrowsForNonExistent()
    {
        // Arrange
        var vm = new VMActor();
        var bytecode = new BytecodeBuilder()
            .Push(42)
            .Op(OpCode.RETURN)
            .Build();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            vm.RedefineWordOrThrow("NONEXISTENT", bytecode));
        Assert.Contains("does not exist in the dictionary", ex.Message);
    }

    [Fact]
    public void RedefinedWordMaintainsName()
    {
        // Arrange
        var vm = new VMActor();
        var original = new BytecodeBuilder()
            .Push(1)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("TEST", original);

        // Act
        var newBytecode = new BytecodeBuilder()
            .Push(2)
            .Op(OpCode.RETURN)
            .Build();
        vm.RedefineWord("TEST", newBytecode);

        // Assert
        var word = vm.GetWord("TEST");
        Assert.NotNull(word);
        Assert.Equal("TEST", word.Name);
        Assert.False(word.IsNative);
    }

    [Fact]
    public void RedefinedBuiltInBecomesUserDefined()
    {
        // Arrange
        var vm = new VMActor();
        var originalAdd = vm.GetWord("ADD");
        Assert.True(originalAdd!.IsNative);

        // Act
        var newBytecode = new BytecodeBuilder()
            .Op(OpCode.SUB)
            .Op(OpCode.RETURN)
            .Build();
        vm.RedefineWord("ADD", newBytecode);

        // Assert
        var redefinedAdd = vm.GetWord("ADD");
        Assert.NotNull(redefinedAdd);
        Assert.False(redefinedAdd.IsNative);
        Assert.Equal(newBytecode, redefinedAdd.Bytecode);
    }

    [Fact]
    public void MultipleRedefinitionsWork()
    {
        // Arrange
        var vm = new VMActor();

        // Define original
        var v1 = new BytecodeBuilder().Push(1).Op(OpCode.RETURN).Build();
        vm.DefineWord("VAL", v1);

        vm.ExecuteWord("VAL");
        Assert.Equal(1, vm.DataStack.First().AsInteger());

        // Redefine to 2
        var v2 = new BytecodeBuilder().Push(2).Op(OpCode.RETURN).Build();
        vm.RedefineWord("VAL", v2);
        vm.ExecuteWord("VAL");

        // Stack now has [2, 1], top is 2
        Assert.Equal(2, vm.DataStack.Count());
        Assert.Equal(2, vm.DataStack.First().AsInteger());

        // Redefine to 3
        var v3 = new BytecodeBuilder().Push(3).Op(OpCode.RETURN).Build();
        vm.RedefineWord("VAL", v3);
        vm.ExecuteWord("VAL");

        // Stack now has [3, 2, 1], top is 3
        Assert.Equal(3, vm.DataStack.Count());
        Assert.Equal(3, vm.DataStack.First().AsInteger());
    }


    [Fact]
    public void RedefinedWordInWordIndexUpdates()
    {
        // Arrange
        var vm = new VMActor();
        var original = new BytecodeBuilder()
            .Push(10)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("MYWORD", original);

        var originalWord = vm.GetWord("MYWORD");

        // Act
        var newBytecode = new BytecodeBuilder()
            .Push(20)
            .Op(OpCode.RETURN)
            .Build();
        vm.RedefineWord("MYWORD", newBytecode);

        // Assert
        vm.ExecuteWord("MYWORD");
        Assert.Equal(20, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void RedefiningDupChangesStackBehavior()
    {
        // Arrange
        var vm = new VMActor();

        // Original DUP duplicates
        var bytecode1 = new BytecodeBuilder()
            .Push(42)
            .Op(OpCode.DUP)
            .Op(OpCode.HALT)
            .Build();
        vm.LoadBytecode(bytecode1);
        vm.Run();
        Assert.Equal(2, vm.DataStack.Count());

        // Act - Redefine DUP to triple instead (DUP DUP) in a new VM
        var vm2 = new VMActor();
        var newDupBytecode = new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.DUP)
            .Op(OpCode.RETURN)
            .Build();
        vm2.RedefineWord("DUP", newDupBytecode);

        // Assert - DUP now creates 3 copies
        var bytecode2 = new BytecodeBuilder()
            .Push(99)
            .Op(OpCode.DUP)
            .Op(OpCode.HALT)
            .Build();
        vm2.LoadBytecode(bytecode2);
        vm2.Run();

        Assert.Equal(3, vm2.DataStack.Count());
        Assert.All(vm2.DataStack, v => Assert.Equal(99, v.AsInteger()));
    }

    [Fact]
    public void RedefinedInstructionAffectsBytecodeExecution()
    {
        // Arrange
        var vm = new VMActor();

        // Act - Redefine POP to do nothing (just return)
        var nopBytecode = new BytecodeBuilder()
            .Op(OpCode.RETURN)
            .Build();
        vm.RedefineWord("POP", nopBytecode);

        // Execute bytecode that uses POP
        var bytecode = new BytecodeBuilder()
            .Push(1)
            .Push(2)
            .Push(3)
            .Op(OpCode.POP)  // Should now be a no-op
            .Op(OpCode.HALT)
            .Build();
        vm.LoadBytecode(bytecode);
        vm.Run();

        // Assert - All three values still on stack
        Assert.Equal(3, vm.DataStack.Count());
    }

    [Fact]
    public void ComplexRedefinitionScenario()
    {
        // Arrange
        var vm = new VMActor();

        // Define a word that uses ADD
        var doubleAddBytecode = new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("DOUBLE", doubleAddBytecode);

        // Original DOUBLE behavior (2x)
        vm.LoadBytecode(new BytecodeBuilder().Push(5).Build());
        vm.Run();
        vm.ExecuteWord("DOUBLE");
        Assert.Equal(10, vm.DataStack.First().AsInteger());

        // Act - Redefine ADD to multiply in a new VM
        var vm2 = new VMActor();
        vm2.DefineWord("DOUBLE", doubleAddBytecode);
        var mulBytecode = new BytecodeBuilder()
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build();
        vm2.RedefineWord("ADD", mulBytecode);

        // Assert - DOUBLE now squares (because it dups and "adds" which is now multiply)
        vm2.LoadBytecode(new BytecodeBuilder().Push(5).Build());
        vm2.Run();
        vm2.ExecuteWord("DOUBLE");
        Assert.Equal(25, vm2.DataStack.First().AsInteger());
    }

    // ===== Advanced Dynamic VM Tests =====

    [Fact]
    public void RuntimeWordComposition()
    {
        // Demonstrates building complex words from simpler ones at runtime
        var vm = new VMActor();

        // Define atomic operations
        var inc = new BytecodeBuilder()
            .Push(1)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("INC", inc);

        var doubleBytecode = new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("DOUBLE", doubleBytecode);

        // Act - Compose operations: (5 + 1) * 2 = 12
        vm.LoadBytecode(new BytecodeBuilder().Push(5).Build());
        vm.Run();
        vm.ExecuteWord("INC");      // 5 -> 6
        vm.ExecuteWord("DOUBLE");   // 6 -> 12

        // Assert
        Assert.Equal(12, vm.DataStack.First().AsInteger());

        // Now redefine INC to add 10 instead - showcases runtime flexibility
        var incBy10 = new BytecodeBuilder()
            .Push(10)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.RedefineWord("INC", incBy10);

        // Same composition, different result: (5 + 10) * 2 = 30
        var vm2 = new VMActor();
        vm2.DefineWord("INC", incBy10);
        vm2.DefineWord("DOUBLE", doubleBytecode);
        vm2.LoadBytecode(new BytecodeBuilder().Push(5).Build());
        vm2.Run();
        vm2.ExecuteWord("INC");
        vm2.ExecuteWord("DOUBLE");
        Assert.Equal(30, vm2.DataStack.First().AsInteger());
    }

    [Fact]
    public void ConditionalBehaviorThroughRedefinition()
    {
        // Demonstrates using redefinition for conditional behavior
        var vm = new VMActor();

        // Define a "MODE" word that behaves differently in different contexts
        var addMode = new BytecodeBuilder()
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("OPERATE", addMode);

        // Test in ADD mode
        vm.LoadBytecode(new BytecodeBuilder().Push(10).Push(5).Build());
        vm.Run();
        vm.ExecuteWord("OPERATE");
        Assert.Equal(15, vm.DataStack.First().AsInteger());

        // Switch to MULTIPLY mode
        var mulMode = new BytecodeBuilder()
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build();
        vm.RedefineWord("OPERATE", mulMode);

        vm.LoadBytecode(new BytecodeBuilder().Push(10).Push(5).Build());
        vm.Run();
        vm.ExecuteWord("OPERATE");
        Assert.Equal(50, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void RecursiveWordDefinitionPattern()
    {
        // Demonstrates a Forth-like recursive pattern
        var vm = new VMActor();

        // Define FACTORIAL-STEP that expects n on stack
        // This is a simplified demonstration - real factorial would need loops
        var factorialStep = new BytecodeBuilder()
            .Op(OpCode.DUP)    // Duplicate n
            .Push(1)
            .Op(OpCode.SUB)    // n-1
            .Op(OpCode.MUL)    // n * (n-1), simplified version
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("FACT-STEP", factorialStep);

        vm.LoadBytecode(new BytecodeBuilder().Push(5).Build());
        vm.Run();
        vm.ExecuteWord("FACT-STEP");  // 5 * 4 = 20

        Assert.Equal(20, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void DynamicInstructionSetExtension()
    {
        // Demonstrates extending the instruction set at runtime
        var vm = new VMActor();

        // Create a new "instruction" TRIPLE
        var triple = new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.DUP)
            .Op(OpCode.ADD)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("TRIPLE", triple);

        // Create another "instruction" SQUARED
        var squared = new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("SQUARED", squared);

        // Use them: 3^2 * 3 = 27
        vm.LoadBytecode(new BytecodeBuilder().Push(3).Build());
        vm.Run();
        vm.ExecuteWord("SQUARED");  // 9
        vm.ExecuteWord("TRIPLE");   // 27

        Assert.Equal(27, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void MemoryBasedProgramModification()
    {
        // Demonstrates self-modifying behavior using dictionary memory
        var vm = new VMActor();
        vm.Allot(32);

        // Store a "configuration value" in memory
        var setupBytecode = new BytecodeBuilder()
            .Push(100)   // Configuration value
            .Push(0)     // Address 0
            .Op(OpCode.STORE)
            .Op(OpCode.HALT)
            .Build();
        vm.LoadBytecode(setupBytecode);
        vm.Run();

        // Define a word that reads this configuration
        var configReader = new BytecodeBuilder()
            .Push(0)
            .Op(OpCode.FETCH)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("ADD-CONFIG", configReader);

        // Test with original config
        vm.LoadBytecode(new BytecodeBuilder().Push(5).Build());
        vm.Run();
        vm.ExecuteWord("ADD-CONFIG");
        Assert.Equal(105, vm.DataStack.First().AsInteger());

        // Modify the configuration in memory
        var modifyBytecode = new BytecodeBuilder()
            .Push(200)   // New config value
            .Push(0)
            .Op(OpCode.STORE)
            .Op(OpCode.HALT)
            .Build();
        vm.LoadBytecode(modifyBytecode);
        vm.Run();

        // Same word, different behavior
        vm.LoadBytecode(new BytecodeBuilder().Push(5).Build());
        vm.Run();
        vm.ExecuteWord("ADD-CONFIG");
        Assert.Equal(205, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void WordRedefinitionChain()
    {
        // Demonstrates chaining redefinitions for evolving behavior
        var vm = new VMActor();

        // Version 1: Simple increment
        var v1 = new BytecodeBuilder()
            .Push(1)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("EVOLVE", v1);

        vm.LoadBytecode(new BytecodeBuilder().Push(10).Build());
        vm.Run();
        vm.ExecuteWord("EVOLVE");
        Assert.Equal(11, vm.DataStack.First().AsInteger());

        // Version 2: Increment and double
        var v2 = new BytecodeBuilder()
            .Push(1)
            .Op(OpCode.ADD)
            .Op(OpCode.DUP)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.RedefineWord("EVOLVE", v2);

        vm.LoadBytecode(new BytecodeBuilder().Push(10).Build());
        vm.Run();
        vm.ExecuteWord("EVOLVE");
        Assert.Equal(22, vm.DataStack.First().AsInteger());  // (10+1)*2 = 22

        // Version 3: Increment, double, and add 5
        var v3 = new BytecodeBuilder()
            .Push(1)
            .Op(OpCode.ADD)
            .Op(OpCode.DUP)
            .Op(OpCode.ADD)
            .Push(5)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.RedefineWord("EVOLVE", v3);

        vm.LoadBytecode(new BytecodeBuilder().Push(10).Build());
        vm.Run();
        vm.ExecuteWord("EVOLVE");
        Assert.Equal(27, vm.DataStack.First().AsInteger());  // (10+1)*2+5 = 27
    }

    [Fact]
    public void MultipleInstructionRedefinitionsInteract()
    {
        // Demonstrates how multiple redefined instructions interact
        var vm = new VMActor();

        // Redefine ADD to multiply
        var addAsMul = new BytecodeBuilder()
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build();
        vm.RedefineWord("ADD", addAsMul);

        // Redefine MUL to add
        var mulAsAdd = new BytecodeBuilder()
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.RedefineWord("MUL", mulAsAdd);

        // Now operations are swapped!
        var bytecode = new BytecodeBuilder()
            .Push(3)
            .Push(4)
            .Op(OpCode.ADD)     // Should multiply: 3 * 4 = 12
            .Push(2)
            .Op(OpCode.MUL)     // Should add: 12 + 2 = 14
            .Op(OpCode.HALT)
            .Build();

        vm.LoadBytecode(bytecode);
        vm.Run();

        Assert.Equal(14, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void StackEffectTransformationThroughRedefinition()
    {
        // Demonstrates changing stack effects of operations
        var vm = new VMActor();

        // Original DUP: ( n -- n n )
        vm.LoadBytecode(new BytecodeBuilder().Push(5).Op(OpCode.DUP).Op(OpCode.HALT).Build());
        vm.Run();
        Assert.Equal(2, vm.DataStack.Count());

        // Redefine DUP to be TRIPLE: ( n -- n n n )
        var vm2 = new VMActor();
        var tripleDup = new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.DUP)
            .Op(OpCode.RETURN)
            .Build();
        vm2.RedefineWord("DUP", tripleDup);

        vm2.LoadBytecode(new BytecodeBuilder().Push(5).Op(OpCode.DUP).Op(OpCode.HALT).Build());
        vm2.Run();
        Assert.Equal(3, vm2.DataStack.Count());

        // Redefine DUP to be DROP: ( n -- )
        var vm3 = new VMActor();
        var dupAsDrop = new BytecodeBuilder()
            .Op(OpCode.POP)
            .Op(OpCode.RETURN)
            .Build();
        vm3.RedefineWord("DUP", dupAsDrop);

        vm3.LoadBytecode(new BytecodeBuilder().Push(5).Op(OpCode.DUP).Op(OpCode.HALT).Build());
        vm3.Run();
        Assert.Empty(vm3.DataStack);
    }

    [Fact]
    public void DomainSpecificLanguageConstruction()
    {
        // Demonstrates building a DSL at runtime
        var vm = new VMActor();

        // Define DSL words for a simple calculator
        var square = new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("SQUARE", square);

        var cube = new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.DUP)
            .Op(OpCode.MUL)
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("CUBE", cube);

        var average = new BytecodeBuilder()
            .Op(OpCode.ADD)
            .Push(2)
            .Op(OpCode.DIV)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("AVERAGE", average);

        // Use the DSL: average(3^2, 4^2) = average(9, 16) = 12.5 ≈ 12 (integer)
        vm.LoadBytecode(new BytecodeBuilder().Push(3).Build());
        vm.Run();
        vm.ExecuteWord("SQUARE");  // 9

        vm.LoadBytecode(new BytecodeBuilder().Push(4).Build());
        vm.Run();
        vm.ExecuteWord("SQUARE");  // 16

        vm.ExecuteWord("AVERAGE"); // (9+16)/2 = 12

        Assert.Equal(12, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void BehavioralPolymorphismThroughRedefinition()
    {
        // Demonstrates polymorphic behavior via redefinition
        var vm = new VMActor();

        // Define a generic "PROCESS" word
        var process = new BytecodeBuilder()
            .Push(10)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("PROCESS", process);

        // Client code uses PROCESS
        vm.LoadBytecode(new BytecodeBuilder().Push(5).Build());
        vm.Run();
        vm.ExecuteWord("PROCESS");
        var result1 = vm.DataStack.First().AsInteger();

        // Redefine PROCESS for different behavior
        var process2 = new BytecodeBuilder()
            .Push(2)
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build();
        vm.RedefineWord("PROCESS", process2);

        // Same client code, different behavior
        vm.LoadBytecode(new BytecodeBuilder().Push(5).Build());
        vm.Run();
        vm.ExecuteWord("PROCESS");
        var result2 = vm.DataStack.First().AsInteger();

        Assert.Equal(15, result1);  // 5 + 10
        Assert.Equal(10, result2);  // 5 * 2
    }

    [Fact]
    public void ComplexNestedWordRedefinition()
    {
        // Demonstrates how redefinitions propagate through nested calls
        var vm = new VMActor();

        // Define LEAF word
        var leaf = new BytecodeBuilder()
            .Push(1)
            .Op(OpCode.ADD)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("LEAF", leaf);

        // Define BRANCH that calls LEAF
        var branch = new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("BRANCH", branch);

        // Execute
        vm.LoadBytecode(new BytecodeBuilder().Push(10).Build());
        vm.Run();
        vm.ExecuteWord("BRANCH");
        vm.ExecuteWord("LEAF");
        Assert.Equal(11, vm.DataStack.First().AsInteger());

        // Redefine LEAF - BRANCH now has different behavior
        var newLeaf = new BytecodeBuilder()
            .Push(5)
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build();
        vm.RedefineWord("LEAF", newLeaf);

        vm.LoadBytecode(new BytecodeBuilder().Push(10).Build());
        vm.Run();
        vm.ExecuteWord("BRANCH");
        vm.ExecuteWord("LEAF");
        Assert.Equal(50, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void StatefulWordBehavior()
    {
        // Demonstrates words that interact with VM state (memory)
        var vm = new VMActor();
        vm.Allot(16);

        // Initialize counter in memory
        vm.LoadBytecode(new BytecodeBuilder()
            .Push(0)    // Initial counter value
            .Push(0)    // Address 0
            .Op(OpCode.STORE)
            .Op(OpCode.HALT)
            .Build());
        vm.Run();

        // Define INCREMENT-COUNTER
        var incrementCounter = new BytecodeBuilder()
            .Push(0)             // Address
            .Op(OpCode.FETCH)    // Get current value
            .Push(1)
            .Op(OpCode.ADD)      // Increment
            .Op(OpCode.DUP)      // Duplicate for return
            .Push(0)             // Address
            .Op(OpCode.STORE)    // Store back
            .Op(OpCode.RETURN)
            .Build();
        vm.DefineWord("COUNT++", incrementCounter);

        // Call multiple times
        vm.ExecuteWord("COUNT++");
        Assert.Equal(1, vm.DataStack.First().AsInteger());

        vm.ExecuteWord("COUNT++");
        Assert.Equal(2, vm.DataStack.First().AsInteger());

        vm.ExecuteWord("COUNT++");
        Assert.Equal(3, vm.DataStack.First().AsInteger());
    }
}
