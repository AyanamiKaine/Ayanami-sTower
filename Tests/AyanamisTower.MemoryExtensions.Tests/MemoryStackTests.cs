namespace AyanamisTower.MemoryExtensions.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Unit tests for the MemoryStack struct.
/// </summary>
public class MemoryStackTests
{
    [Fact]
    public void Constructor_CreatesStackWithCorrectSize()
    {
        // Arrange & Act
        var stack = new MemoryStack(1024);

        // Assert
        Assert.Equal(0, stack.Pointer);
        Assert.True(stack.IsEmpty);
        Assert.Equal(1024, stack.Memory.Length);
    }

    [Fact]
    public void Push_Pop_Byte_WorksCorrectly()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const byte value = 42;

        // Act
        stack.Push(value);
        byte result = stack.Pop();

        // Assert
        Assert.Equal(value, result);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PushInt_PopInt_WorksCorrectly()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const int value = 123456;

        // Act
        stack.PushInt(value);
        int result = stack.PopInt();

        // Assert
        Assert.Equal(value, result);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PushInt_PopInt_HandlesNegativeNumbers()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const int value = -987654;

        // Act
        stack.PushInt(value);
        int result = stack.PopInt();

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void PushShort_PopShort_WorksCorrectly()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const short value = 12345;

        // Act
        stack.PushShort(value);
        short result = stack.PopShort();

        // Assert
        Assert.Equal(value, result);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PushUInt_PopUInt_WorksCorrectly()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const uint value = 4294967295;

        // Act
        stack.PushUInt(value);
        uint result = stack.PopUInt();

        // Assert
        Assert.Equal(value, result);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PushLong_PopLong_WorksCorrectly()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const long value = 9223372036854775807;

        // Act
        stack.PushLong(value);
        long result = stack.PopLong();

        // Assert
        Assert.Equal(value, result);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PushULong_PopULong_WorksCorrectly()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const ulong value = 18446744073709551615;

        // Act
        stack.PushULong(value);
        ulong result = stack.PopULong();

        // Assert
        Assert.Equal(value, result);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PushFloat_PopFloat_WorksCorrectly()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const float value = 3.14159f;

        // Act
        stack.PushFloat(value);
        float result = stack.PopFloat();

        // Assert
        Assert.Equal(value, result, 5);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PushDouble_PopDouble_WorksCorrectly()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const double value = 3.141592653589793;

        // Act
        stack.PushDouble(value);
        double result = stack.PopDouble();

        // Assert
        Assert.Equal(value, result, 10);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void MultipleOperations_MaintainCorrectOrder()
    {
        // Arrange
        var stack = new MemoryStack(1024);

        // Act - Push multiple values
        stack.PushInt(100);
        stack.PushInt(200);
        stack.PushInt(300);

        // Pop in reverse order
        int third = stack.PopInt();
        int second = stack.PopInt();
        int first = stack.PopInt();

        // Assert - LIFO order
        Assert.Equal(300, third);
        Assert.Equal(200, second);
        Assert.Equal(100, first);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void MixedTypes_WorkCorrectly()
    {
        // Arrange
        var stack = new MemoryStack(1024);

        // Act - Push different types
        stack.PushInt(42);
        stack.PushFloat(3.14f);
        stack.PushLong(999L);

        // Pop in reverse order
        long longResult = stack.PopLong();
        float floatResult = stack.PopFloat();
        int intResult = stack.PopInt();

        // Assert
        Assert.Equal(999L, longResult);
        Assert.Equal(3.14f, floatResult, 5);
        Assert.Equal(42, intResult);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void Pointer_IncrementsCorrectly()
    {
        // Arrange
        var stack = new MemoryStack(1024);

        // Act & Assert
        Assert.Equal(0, stack.Pointer);

        stack.Push(1);
        Assert.Equal(1, stack.Pointer);

        stack.PushInt(100);
        Assert.Equal(5, stack.Pointer); // 1 byte + 4 bytes

        stack.Pop();
        stack.Pop();
        stack.Pop();
        stack.Pop();
        stack.Pop();
        Assert.Equal(0, stack.Pointer);
    }

    [Fact]
    public void IsEmpty_ReflectsStackState()
    {
        // Arrange
        var stack = new MemoryStack(1024);

        // Assert initial state
        Assert.True(stack.IsEmpty);

        // Act - Push
        stack.PushInt(42);
        Assert.False(stack.IsEmpty);

        // Act - Pop
        stack.PopInt();
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void Clear_ResetsStack()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        stack.PushInt(100);
        stack.PushInt(200);
        stack.PushInt(300);

        // Act
        stack.Clear();

        // Assert
        Assert.Equal(0, stack.Pointer);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void Pop_OnEmptyStack_ThrowsException()
    {
        // Arrange
        var stack = new MemoryStack(1024);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => stack.Pop());
    }

    [Fact]
    public void PopInt_OnEmptyStack_ThrowsException()
    {
        // Arrange
        var stack = new MemoryStack(1024);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => stack.PopInt());
    }

    [Fact]
    public void Push_BeyondCapacity_ThrowsException()
    {
        // Arrange - Create small stack
        var stack = new MemoryStack(4);

        // Act - Fill stack
        stack.PushInt(42); // Uses 4 bytes

        // Assert - Next push should overflow
        Assert.Throws<InvalidOperationException>(() => stack.Push(1));
    }

    [Fact]
    public void EdgeCase_MinMaxValues()
    {
        // Arrange
        var stack = new MemoryStack(1024);

        // Act & Assert - Int min/max
        stack.PushInt(int.MaxValue);
        Assert.Equal(int.MaxValue, stack.PopInt());

        stack.PushInt(int.MinValue);
        Assert.Equal(int.MinValue, stack.PopInt());

        // Act & Assert - Long min/max
        stack.PushLong(long.MaxValue);
        Assert.Equal(long.MaxValue, stack.PopLong());

        stack.PushLong(long.MinValue);
        Assert.Equal(long.MinValue, stack.PopLong());
    }

    [Fact]
    public void EdgeCase_ZeroValues()
    {
        // Arrange
        var stack = new MemoryStack(1024);

        // Act
        stack.PushInt(0);
        stack.PushFloat(0.0f);
        stack.PushDouble(0.0);

        // Assert
        Assert.Equal(0.0, stack.PopDouble());
        Assert.Equal(0.0f, stack.PopFloat());
        Assert.Equal(0, stack.PopInt());
    }

    [Fact]
    public void RegressionTest_ConsecutivePushPop()
    {
        // Arrange
        var stack = new MemoryStack(1024);

        // Act - Interleaved push/pop operations
        stack.PushInt(1);
        Assert.Equal(1, stack.PopInt());

        stack.PushInt(2);
        stack.PushInt(3);
        Assert.Equal(3, stack.PopInt());
        Assert.Equal(2, stack.PopInt());

        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void Peek_Byte_ReturnsValueWithoutRemoving()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const byte value = 42;

        // Act
        stack.Push(value);
        byte peeked = stack.Peek();
        byte popped = stack.Pop();

        // Assert
        Assert.Equal(value, peeked);
        Assert.Equal(value, popped);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PeekInt_ReturnsValueWithoutRemoving()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const int value = 123456;

        // Act
        stack.PushInt(value);
        int peeked = stack.PeekInt();
        int popped = stack.PopInt();

        // Assert
        Assert.Equal(value, peeked);
        Assert.Equal(value, popped);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PeekShort_ReturnsValueWithoutRemoving()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const short value = 12345;

        // Act
        stack.PushShort(value);
        short peeked = stack.PeekShort();
        short popped = stack.PopShort();

        // Assert
        Assert.Equal(value, peeked);
        Assert.Equal(value, popped);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PeekUInt_ReturnsValueWithoutRemoving()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const uint value = 4294967295;

        // Act
        stack.PushUInt(value);
        uint peeked = stack.PeekUInt();
        uint popped = stack.PopUInt();

        // Assert
        Assert.Equal(value, peeked);
        Assert.Equal(value, popped);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PeekLong_ReturnsValueWithoutRemoving()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const long value = 9223372036854775807;

        // Act
        stack.PushLong(value);
        long peeked = stack.PeekLong();
        long popped = stack.PopLong();

        // Assert
        Assert.Equal(value, peeked);
        Assert.Equal(value, popped);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PeekULong_ReturnsValueWithoutRemoving()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const ulong value = 18446744073709551615;

        // Act
        stack.PushULong(value);
        ulong peeked = stack.PeekULong();
        ulong popped = stack.PopULong();

        // Assert
        Assert.Equal(value, peeked);
        Assert.Equal(value, popped);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PeekFloat_ReturnsValueWithoutRemoving()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const float value = 3.14159f;

        // Act
        stack.PushFloat(value);
        float peeked = stack.PeekFloat();
        float popped = stack.PopFloat();

        // Assert
        Assert.Equal(value, peeked);
        Assert.Equal(value, popped);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void PeekDouble_ReturnsValueWithoutRemoving()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const double value = 3.141592653589793;

        // Act
        stack.PushDouble(value);
        double peeked = stack.PeekDouble();
        double popped = stack.PopDouble();

        // Assert
        Assert.Equal(value, peeked);
        Assert.Equal(value, popped);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void Peek_OnEmptyStack_ThrowsException()
    {
        // Arrange
        var stack = new MemoryStack(1024);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => stack.Peek());
    }

    [Fact]
    public void PeekInt_WithInsufficientData_ThrowsException()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        stack.Push(1);
        stack.Push(2);

        // Act & Assert (only 2 bytes, need 4 for int)
        Assert.Throws<InvalidOperationException>(() => stack.PeekInt());
    }

    [Fact]
    public void Peek_MultipleTimes_ReturnsSameValue()
    {
        // Arrange
        var stack = new MemoryStack(1024);
        const int value = 999;

        // Act
        stack.PushInt(value);
        int peek1 = stack.PeekInt();
        int peek2 = stack.PeekInt();
        int peek3 = stack.PeekInt();

        // Assert
        Assert.Equal(value, peek1);
        Assert.Equal(value, peek2);
        Assert.Equal(value, peek3);
        Assert.Equal(4, stack.Pointer); // Pointer unchanged
    }

    [Fact]
    public void FromKilobytes_CreatesCorrectSize()
    {
        // Arrange & Act
        var stack = MemoryStack.FromKilobytes(5);

        // Assert
        Assert.Equal(5 * 1024, stack.Memory.Length);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void FromMegabytes_CreatesCorrectSize()
    {
        // Arrange & Act
        var stack = MemoryStack.FromMegabytes(5);

        // Assert
        Assert.Equal(5 * 1024 * 1024, stack.Memory.Length);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void FromKilobytes_WithZero_CreatesEmptyStack()
    {
        // Arrange & Act
        var stack = MemoryStack.FromKilobytes(0);

        // Assert
        Assert.Equal(0, stack.Memory.Length);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void FromMegabytes_WithZero_CreatesEmptyStack()
    {
        // Arrange & Act
        var stack = MemoryStack.FromMegabytes(0);

        // Assert
        Assert.Equal(0, stack.Memory.Length);
        Assert.True(stack.IsEmpty);
    }

    [Fact]
    public void FromKilobytes_WithNegative_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => MemoryStack.FromKilobytes(-1));
    }

    [Fact]
    public void FromMegabytes_WithNegative_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => MemoryStack.FromMegabytes(-1));
    }

    [Fact]
    public void FromKilobytes_CanPushAndPopValues()
    {
        // Arrange
        var stack = MemoryStack.FromKilobytes(1);
        const int value = 42;

        // Act
        stack.PushInt(value);
        int result = stack.PopInt();

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void FromMegabytes_CanPushAndPopValues()
    {
        // Arrange
        var stack = MemoryStack.FromMegabytes(1);
        const long value = 123456789L;

        // Act
        stack.PushLong(value);
        long result = stack.PopLong();

        // Assert
        Assert.Equal(value, result);
    }
}

