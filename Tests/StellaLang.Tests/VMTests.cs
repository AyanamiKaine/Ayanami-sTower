namespace StellaLang.Tests;

/// <summary>
/// Here we are testing the instructions of the VM
/// </summary>
public class VMTests
{
    /// <summary>
    /// Simple test to check if the VM can execute a simple bytecode that pushes two numbers and adds them
    /// </summary>
    [Fact]
    public void TestPushAndAdd()
    {
        var vm = new VMActor();
        // Bytecode: PUSH 5, PUSH 10, ADD, PRINT
        var bytecode = new List<byte> { 0x01, 5, 0x01, 10, 0x02, 0x06 };
        vm.LoadBytecode(bytecode);
        vm.Execute();
    }

    /// <summary>
    /// Test to check if the VM can execute a bytecode that pushes two numbers, subtracts them and prints the result
    /// </summary>
    [Fact]
    public void LoadConst_PushesConstantToStack()
    {
        var vm = new VMActor();
        var constants = new List<object> { 42, "test" };
        var bytecode = new List<byte> { 0x07, 0x00 }; // LOAD_CONST 0
        vm.LoadBytecode(bytecode, constants);
        vm.Execute();
        Assert.Equal(42, vm.GetStackTop()); // Assuming you add a GetStackTop method
    }

    /// <summary>
    /// Test to check if the VM throws an exception when trying to load a constant with an invalid index
    /// </summary>
    [Fact]
    public void LoadConst_InvalidIndex_ThrowsException()
    {
        var vm = new VMActor();
        var constants = new List<object> { 42 };
        var bytecode = new List<byte> { 0x07, 0x01 }; // Invalid index
        vm.LoadBytecode(bytecode, constants);
        Assert.Throws<Exception>(() => vm.Execute());
    }

    /// <summary>
    /// Test RET with empty call stack throws exception.
    /// </summary>
    [Fact]
    public void Ret_EmptyCallStack_ThrowsException()
    {
        var vm = new VMActor();
        var bytecode = new List<byte> { 0x09 }; // RET without CALL
        vm.LoadBytecode(bytecode, []);
        Assert.Throws<Exception>(() => vm.Execute());
    }

    /// <summary>
    /// Test CALL with invalid offset throws exception.
    /// </summary>
    [Fact]
    public void Call_InvalidOffset_ThrowsException()
    {
        var vm = new VMActor();
        var bytecode = new List<byte> { 0x08, 0xFF }; // Offset beyond bytecode
        vm.LoadBytecode(bytecode, []);
        Assert.Throws<Exception>(() => vm.Execute());
    }

    /// <summary>
    /// Test calling a registered C# function.
    /// </summary>
    [Fact]
    public void CallCSharp_CallsRegisteredFunction()
    {
        var vm = new VMActor();
        vm.RegisterFunction("Add", args => (dynamic)args[0] + (dynamic)args[1]);
        var constants = new List<object> { "Add" };
        var bytecode = new List<byte> { 0x01, 5, 0x01, 10, 0x0A, 0x00, 0x02 }; // PUSH 5, PUSH 10, CALL_CSHARP "Add" 2 args
        vm.LoadBytecode(bytecode, constants);
        vm.Execute();
        Assert.Equal(15, vm.GetStackTop());
    }

    /// <summary>
    /// Test CALL_CSHARP with unregistered function throws exception.
    /// </summary>
    [Fact]
    public void CallCSharp_UnregisteredFunction_ThrowsException()
    {
        var vm = new VMActor();
        var constants = new List<object> { "Unknown" };
        var bytecode = new List<byte> { 0x0A, 0x00, 0x00 }; // CALL_CSHARP "Unknown" 0 args
        vm.LoadBytecode(bytecode, constants);
        Assert.Throws<Exception>(() => vm.Execute());
    }

    /// <summary>
    /// Test CALL_CSHARP with insufficient arguments throws exception.
    /// </summary>
    [Fact]
    public void CallCSharp_InsufficientArgs_ThrowsException()
    {
        var vm = new VMActor();
        vm.RegisterFunction("Test", args => args.Length);
        var constants = new List<object> { "Test" };
        var bytecode = new List<byte> { 0x0A, 0x00, 0x02 }; // CALL_CSHARP "Test" 2 args, but stack empty
        vm.LoadBytecode(bytecode, constants);
        Assert.Throws<Exception>(() => vm.Execute());
    }
}
