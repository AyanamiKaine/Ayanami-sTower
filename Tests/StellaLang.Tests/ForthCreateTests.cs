using System;

namespace StellaLang.Tests;

/// <summary>
/// Tests for the CREATE word and related defining words.
/// </summary>
public class ForthCreateTests
{
    /// <summary>
    /// Test basic CREATE functionality - creates a word that returns its data field address.
    /// </summary>
    [Fact]
    public void TestCreateBasic()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // CREATE a word
        forth.Interpret("CREATE MYDATA");

        // Execute it - should push data field address
        forth.Interpret("MYDATA");
        long addr1 = vm.DataStack.PopLong();

        // Execute again - should push same address
        forth.Interpret("MYDATA");
        long addr2 = vm.DataStack.PopLong();

        Assert.Equal(addr1, addr2);
        Assert.True(addr1 >= 0);
    }

    /// <summary>
    /// Test CREATE with ALLOT - allocate data space after CREATE.
    /// </summary>
    [Fact]
    public void TestCreateWithAllot()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // CREATE a word and allocate 16 bytes for it
        forth.Interpret("CREATE BUFFER 16 ALLOT");

        // Get the buffer address
        forth.Interpret("BUFFER");
        long bufferAddr = vm.DataStack.PopLong();

        // Store values in the buffer (standard Forth: value addr !)
        forth.Interpret($"42 {bufferAddr} !");
        forth.Interpret($"99 {bufferAddr} 8 + !");

        // Read them back
        forth.Interpret($"{bufferAddr} @");
        Assert.Equal(42L, vm.DataStack.PopLong());

        forth.Interpret($"{bufferAddr} 8 + @");
        Assert.Equal(99L, vm.DataStack.PopLong());
    }

    /// <summary>
    /// Test multiple CREATE words - each should have distinct data field.
    /// </summary>
    [Fact]
    public void TestMultipleCreate()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // CREATE with space allocation to ensure different addresses
        forth.Interpret("CREATE DATA1 8 ALLOT");
        forth.Interpret("CREATE DATA2 8 ALLOT");
        forth.Interpret("CREATE DATA3 8 ALLOT");

        // Get all addresses
        forth.Interpret("DATA1");
        long addr1 = vm.DataStack.PopLong();

        forth.Interpret("DATA2");
        long addr2 = vm.DataStack.PopLong();

        forth.Interpret("DATA3");
        long addr3 = vm.DataStack.PopLong();

        // All addresses should be different
        Assert.NotEqual(addr1, addr2);
        Assert.NotEqual(addr2, addr3);
        Assert.NotEqual(addr1, addr3);
    }

    /// <summary>
    /// Test CREATE in a colon definition.
    /// </summary>
    [Fact]
    public void TestCreateInDefinition()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Define a word that uses a created data structure
        forth.Interpret("CREATE COUNTER 0 ,");
        forth.Interpret(": INCREMENT COUNTER @ 1+ COUNTER ! ;");
        forth.Interpret(": GET-COUNT COUNTER @ ;"); ;

        // Initial value
        forth.Interpret("GET-COUNT");
        Assert.Equal(0L, vm.DataStack.PopLong());

        // Increment
        forth.Interpret("INCREMENT");
        forth.Interpret("GET-COUNT");
        Assert.Equal(1L, vm.DataStack.PopLong());

        // Increment again
        forth.Interpret("INCREMENT INCREMENT");
        forth.Interpret("GET-COUNT");
        Assert.Equal(3L, vm.DataStack.PopLong());
    }

    /// <summary>
    /// Test CREATE with comma (,) to initialize data.
    /// </summary>
    [Fact]
    public void TestCreateWithComma()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // CREATE and initialize with values
        forth.Interpret("CREATE TABLE 10 , 20 , 30 ,");

        // Read the values
        forth.Interpret("TABLE @");
        Assert.Equal(10L, vm.DataStack.PopLong());

        forth.Interpret("TABLE 8 + @");
        Assert.Equal(20L, vm.DataStack.PopLong());

        forth.Interpret("TABLE 16 + @");
        Assert.Equal(30L, vm.DataStack.PopLong());
    }

    /// <summary>
    /// Test data-space alignment with CREATE.
    /// CREATE should align the data-space pointer.
    /// </summary>
    [Fact]
    public void TestCreateAlignment()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Create some unaligned data with C,
        forth.Interpret("CREATE BYTES 1 C, 2 C, 3 C,");

        // Now CREATE should align before creating the next word
        forth.Interpret("CREATE ALIGNED");

        forth.Interpret("ALIGNED");
        long alignedAddr = vm.DataStack.PopLong();

        // Address should be 8-byte aligned
        Assert.Equal(0, alignedAddr % 8);
    }

    /// <summary>
    /// Test using CREATE to build a simple data structure (array).
    /// </summary>
    [Fact]
    public void TestCreateArray()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Define an array word that creates an array of n cells
        forth.Interpret(": ARRAY CREATE CELLS ALLOT ;");

        // Create a 5-element array
        forth.Interpret("5 ARRAY MYARRAY");

        // Get base address
        forth.Interpret("MYARRAY");
        long baseAddr = vm.DataStack.PopLong();

        // Fill the array (standard Forth: value addr !)
        for (int i = 0; i < 5; i++)
        {
            forth.Interpret($"{i * 10} {baseAddr + i * 8} !");
        }

        // Read back
        for (int i = 0; i < 5; i++)
        {
            forth.Interpret($"{baseAddr + i * 8} @");
            Assert.Equal(i * 10L, vm.DataStack.PopLong());
        }
    }

    /// <summary>
    /// Test CREATE with CONSTANT-like behavior.
    /// </summary>
    [Fact]
    public void TestCreateConstant()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Define a custom CONSTANT using CREATE
        forth.Interpret(": MYCONSTANT CREATE , ;");

        // Create a constant
        forth.Interpret("42 MYCONSTANT ANSWER");

        // Read it
        forth.Interpret("ANSWER @");
        Assert.Equal(42L, vm.DataStack.PopLong());
    }
}
