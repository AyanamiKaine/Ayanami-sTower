using System;
using Xunit;

namespace StellaLang.Tests;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Tests for the STRUCT/FIELD syntactic sugar implementation.
/// Demonstrates how to use structs in FORTH for structured data access.
/// </summary>
public class ForthStructTests
{
    [Fact]
    public void TestBasicStructDefinition()
    {
        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Define a simple structure with three fields
        // STRUCT starts with offset 0
        // >w is a 2-byte field at offset 0
        // >l is a 4-byte field at offset 2
        // >b is a 1-byte field at offset 6
        // /foo is a constant holding the total size (7)
        interpreter.Interpret("STRUCT 2 FIELD >w 4 FIELD >l 1 FIELD >b CONSTANT /foo");

        // Act - Test that field words work correctly
        // Push a base address (100) and use >w to get the field address
        interpreter.Interpret("100 >w");
        var result_w = vm.DataStack.PopLong();

        interpreter.Interpret("100 >l");
        var result_l = vm.DataStack.PopLong();

        interpreter.Interpret("100 >b");
        var result_b = vm.DataStack.PopLong();

        interpreter.Interpret("/foo");
        var total_size = vm.DataStack.PopLong();

        // Assert
        Assert.Equal(100, result_w);  // >w adds 0 to base
        Assert.Equal(102, result_l);  // >l adds 2 to base
        Assert.Equal(106, result_b);  // >b adds 6 to base
        Assert.Equal(7, total_size);  // total structure size
    }

    [Fact]
    public void TestStructWithVariableAllocation()
    {
        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Define a Point structure with x and y coordinates (each 8 bytes)
        interpreter.Interpret("STRUCT 8 FIELD >x 8 FIELD >y CONSTANT /Point");

        // Create a variable to hold a Point instance
        interpreter.Interpret("VARIABLE myPoint");

        // Act - Store values into the Point structure
        // VARIABLE pushes its address directly, so we use myPoint not myPoint @
        // Store 42 into the x field
        interpreter.Interpret("42 myPoint >x !");
        // Store 73 into the y field
        interpreter.Interpret("73 myPoint >y !");

        // Read back the values
        interpreter.Interpret("myPoint >x @");
        var x_value = vm.DataStack.PopLong();

        interpreter.Interpret("myPoint >y @");
        var y_value = vm.DataStack.PopLong();

        // Assert
        Assert.Equal(42, x_value);
        Assert.Equal(73, y_value);
    }

    [Fact]
    public void TestNestedFieldAccess()
    {
        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Define a Rectangle structure
        // First define Point
        interpreter.Interpret("STRUCT 8 FIELD >x 8 FIELD >y CONSTANT /Point");
        
        // Rectangle has two Points: top-left and bottom-right
        // We need to define field words for nested access
        interpreter.Interpret("STRUCT /Point FIELD >topLeft /Point FIELD >bottomRight CONSTANT /Rectangle");

        // Act - Verify offsets
        interpreter.Interpret("100 >topLeft");
        var topLeft = vm.DataStack.PopLong();

        interpreter.Interpret("100 >bottomRight");
        var bottomRight = vm.DataStack.PopLong();

        interpreter.Interpret("/Rectangle");
        var rectSize = vm.DataStack.PopLong();

        // Assert
        Assert.Equal(100, topLeft);        // offset 0
        Assert.Equal(116, bottomRight);    // offset 16 (after first Point)
        Assert.Equal(32, rectSize);        // total: 2 Points × 16 bytes each
    }

    [Fact]
    public void TestStructEquivalenceToManualDefinition()
    {
        // This test demonstrates that STRUCT/FIELD is pure syntactic sugar
        // by showing equivalent manual definitions produce the same results

        // Arrange - Using STRUCT/FIELD
        var vm1 = new VM();
        var forth1 = new ForthInterpreter(vm1);
        forth1.Interpret("STRUCT 2 FIELD >w 4 FIELD >l 1 FIELD >b CONSTANT /foo");

        // Arrange - Manual definition
        var vm2 = new VM();
        var forth2 = new ForthInterpreter(vm2);
        forth2.Interpret(": >w 0 + ;");
        forth2.Interpret(": >l 2 + ;");
        forth2.Interpret(": >b 6 + ;");
        forth2.Interpret("7 CONSTANT /foo");

        // Act - Test both with the same inputs
        forth1.Interpret("100 >w");
        var result1_w = vm1.DataStack.PopLong();
        forth2.Interpret("100 >w");
        var result2_w = vm2.DataStack.PopLong();

        forth1.Interpret("100 >l");
        var result1_l = vm1.DataStack.PopLong();
        forth2.Interpret("100 >l");
        var result2_l = vm2.DataStack.PopLong();

        forth1.Interpret("100 >b");
        var result1_b = vm1.DataStack.PopLong();
        forth2.Interpret("100 >b");
        var result2_b = vm2.DataStack.PopLong();

        forth1.Interpret("/foo");
        var result1_size = vm1.DataStack.PopLong();
        forth2.Interpret("/foo");
        var result2_size = vm2.DataStack.PopLong();

        // Assert - Both approaches yield identical results
        Assert.Equal(result2_w, result1_w);
        Assert.Equal(result2_l, result1_l);
        Assert.Equal(result2_b, result1_b);
        Assert.Equal(result2_size, result1_size);
    }

    [Fact]
    public void TestReservedFieldWithSkip()
    {
        // Tests the pattern of skipping fields you don't want to name
        // by using simple arithmetic instead of FIELD

        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Define a packet structure with a reserved field at the start
        // The comment is just for documentation - the 4 + actually skips the space
        interpreter.Interpret("STRUCT 4 + 4 FIELD >tx-stat 8 FIELD >payload-addr CONSTANT /packet");

        // Act
        interpreter.Interpret("200 >tx-stat");
        var txStat = vm.DataStack.PopLong();

        interpreter.Interpret("200 >payload-addr");
        var payloadAddr = vm.DataStack.PopLong();

        interpreter.Interpret("/packet");
        var packetSize = vm.DataStack.PopLong();

        // Assert
        Assert.Equal(204, txStat);        // starts at offset 4
        Assert.Equal(208, payloadAddr);   // starts at offset 8
        Assert.Equal(16, packetSize);     // total size
    }

    [Fact]
    public void TestMultipleStructInstances()
    {
        // Demonstrate that field words work with any base address

        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Define a simple structure
        interpreter.Interpret("STRUCT 8 FIELD >value CONSTANT /Counter");

        // Create two separate instances
        interpreter.Interpret("VARIABLE counter1");
        interpreter.Interpret("VARIABLE counter2");

        // Act - Set different values
        // VARIABLE pushes its address directly
        interpreter.Interpret("10 counter1 >value !");
        interpreter.Interpret("20 counter2 >value !");

        // Read back
        interpreter.Interpret("counter1 >value @");
        var value1 = vm.DataStack.PopLong();

        interpreter.Interpret("counter2 >value @");
        var value2 = vm.DataStack.PopLong();

        // Assert - Each instance maintains its own data
        Assert.Equal(10, value1);
        Assert.Equal(20, value2);
    }

    [Fact]
    public void TestStructWithByteFields()
    {
        // Test single-byte field access using C! and C@

        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Define RGB color structure with byte fields
        interpreter.Interpret("STRUCT 1 FIELD >red 1 FIELD >green 1 FIELD >blue 1 FIELD >alpha CONSTANT /Color");

        interpreter.Interpret("VARIABLE myColor");

        // Act - Store byte values
        // VARIABLE pushes its address directly
        interpreter.Interpret("255 myColor >red C!");
        interpreter.Interpret("128 myColor >green C!");
        interpreter.Interpret("64 myColor >blue C!");
        interpreter.Interpret("255 myColor >alpha C!");

        // Read back
        interpreter.Interpret("myColor >red C@");
        var red = vm.DataStack.PopLong();

        interpreter.Interpret("myColor >green C@");
        var green = vm.DataStack.PopLong();

        interpreter.Interpret("myColor >blue C@");
        var blue = vm.DataStack.PopLong();

        interpreter.Interpret("myColor >alpha C@");
        var alpha = vm.DataStack.PopLong();

        interpreter.Interpret("/Color");
        var colorSize = vm.DataStack.PopLong();

        // Assert
        Assert.Equal(255, red);
        Assert.Equal(128, green);
        Assert.Equal(64, blue);
        Assert.Equal(255, alpha);
        Assert.Equal(4, colorSize);
    }

    [Fact]
    public void TestStructArrayIndexing()
    {
        // Demonstrate calculating addresses in an array of structs

        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Define a Person struct
        interpreter.Interpret("STRUCT 8 FIELD >age 8 FIELD >id CONSTANT /Person");

        // Create base address (simulating an array)
        interpreter.Interpret("VARIABLE arrayBase");
        interpreter.Interpret("1000 arrayBase !");

        // Act - Calculate address of person at index 2
        // Formula: base + (index * /Person)
        interpreter.Interpret("arrayBase @ 2 /Person * +");
        var person2Addr = vm.DataStack.PopLong();

        // Now use that calculated address with the >age field
        interpreter.Interpret("arrayBase @ 2 /Person * + >age");
        var person2AgeAddr = vm.DataStack.PopLong();

        // Assert
        Assert.Equal(1032, person2Addr);     // 1000 + (2 * 16) = 1032
        Assert.Equal(1032, person2AgeAddr);  // >age adds 0 to the address
    }

    [Fact]
    public void TestComplexNestedStructure()
    {
        // Test a more complex nested structure (3 levels deep)

        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Level 1: Point
        interpreter.Interpret("STRUCT 8 FIELD >x 8 FIELD >y CONSTANT /Point");

        // Level 2: Line (two points)
        interpreter.Interpret("STRUCT /Point FIELD >start /Point FIELD >end CONSTANT /Line");

        // Level 3: Triangle (three lines)
        interpreter.Interpret("STRUCT /Line FIELD >side1 /Line FIELD >side2 /Line FIELD >side3 CONSTANT /Triangle");

        // Act - Navigate through nested structure
        interpreter.Interpret("5000 >side2 >end >y");
        var nestedAddr = vm.DataStack.PopLong();

        interpreter.Interpret("/Triangle");
        var triangleSize = vm.DataStack.PopLong();

        // Assert
        // >side2 adds 32 (one line), >end adds 16 (one point), >y adds 8
        Assert.Equal(5056, nestedAddr);  // 5000 + 32 + 16 + 8
        Assert.Equal(96, triangleSize);  // 3 lines × 32 bytes each
    }

    [Fact]
    public void TestStructWithHelperWords()
    {
        // Test defining helper words that work with structures

        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Define Point structure
        interpreter.Interpret("STRUCT 8 FIELD >x 8 FIELD >y CONSTANT /Point");

        // Define helper to set both coordinates at once
        interpreter.Interpret(": POINT! ( x y addr -- ) >R R@ >y ! R> >x ! ;");

        // Define helper to get both coordinates
        interpreter.Interpret(": POINT@ ( addr -- x y ) DUP >x @ SWAP >y @ ;");

        // Create a point
        interpreter.Interpret("VARIABLE pt");

        // Act - Use helpers
        // VARIABLE pushes its address directly
        interpreter.Interpret("42 73 pt POINT!");
        interpreter.Interpret("pt POINT@");

        var y = vm.DataStack.PopLong();
        var x = vm.DataStack.PopLong();

        // Assert
        Assert.Equal(42, x);
        Assert.Equal(73, y);
    }

    [Fact]
    public void TestStructSizeCalculation()
    {
        // Verify that the size constant correctly represents total structure size

        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Define various structures and verify their sizes
        interpreter.Interpret("STRUCT 1 FIELD >a 1 FIELD >b 1 FIELD >c CONSTANT /Bytes3");
        interpreter.Interpret("STRUCT 8 FIELD >a 8 FIELD >b CONSTANT /Cells2");
        interpreter.Interpret("STRUCT 2 FIELD >w 4 FIELD >l 2 FIELD >w2 CONSTANT /Mixed");

        // Act
        interpreter.Interpret("/Bytes3");
        var bytes3Size = vm.DataStack.PopLong();

        interpreter.Interpret("/Cells2");
        var cells2Size = vm.DataStack.PopLong();

        interpreter.Interpret("/Mixed");
        var mixedSize = vm.DataStack.PopLong();

        // Assert
        Assert.Equal(3, bytes3Size);
        Assert.Equal(16, cells2Size);
        Assert.Equal(8, mixedSize);
    }

    [Fact]
    public void TestFieldWordBehaviorDetail()
    {
        // Test the detailed behavior of FIELD word creation
        // This verifies the CREATE/DOES> mechanism works correctly

        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Start with STRUCT to get initial offset of 0
        interpreter.Interpret("STRUCT");
        
        // Create a single field
        interpreter.Interpret("8 FIELD >first");

        // Stack should now have 8 (the updated offset)
        var newOffset = vm.DataStack.PopLong();
        Assert.Equal(8, newOffset);

        // Now test that >first works as expected
        interpreter.Interpret("1000 >first");
        var result = vm.DataStack.PopLong();
        Assert.Equal(1000, result);  // >first adds 0 (the offset when it was created)

        // Continue building from where we left off (8 on stack)
        interpreter.Interpret("8 8 FIELD >second");
        newOffset = vm.DataStack.PopLong();
        Assert.Equal(16, newOffset);

        interpreter.Interpret("1000 >second");
        result = vm.DataStack.PopLong();
        Assert.Equal(1008, result);  // >second adds 8
    }

    [Fact]
    public void TestEmptyStruct()
    {
        // Test edge case of a structure with no fields

        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Define empty structure (just STRUCT followed by CONSTANT)
        interpreter.Interpret("STRUCT CONSTANT /Empty");

        // Act
        interpreter.Interpret("/Empty");
        var emptySize = vm.DataStack.PopLong();

        // Assert
        Assert.Equal(0, emptySize);
    }

    [Fact]
    public void TestLargeStructure()
    {
        // Test a structure with many fields to ensure no limitations

        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Define a structure with 10 fields
        interpreter.Interpret(@"
            STRUCT
            8 FIELD >f0
            8 FIELD >f1
            8 FIELD >f2
            8 FIELD >f3
            8 FIELD >f4
            8 FIELD >f5
            8 FIELD >f6
            8 FIELD >f7
            8 FIELD >f8
            8 FIELD >f9
            CONSTANT /Large
        ");

        // Act - Test first, middle, and last fields
        interpreter.Interpret("2000 >f0");
        var f0 = vm.DataStack.PopLong();

        interpreter.Interpret("2000 >f5");
        var f5 = vm.DataStack.PopLong();

        interpreter.Interpret("2000 >f9");
        var f9 = vm.DataStack.PopLong();

        interpreter.Interpret("/Large");
        var largeSize = vm.DataStack.PopLong();

        // Assert
        Assert.Equal(2000, f0);   // offset 0
        Assert.Equal(2040, f5);   // offset 40 (5 * 8)
        Assert.Equal(2072, f9);   // offset 72 (9 * 8)
        Assert.Equal(80, largeSize);
    }

    [Fact]
    public void TestStructInColonDefinition()
    {
        // Test that struct field words can be used inside colon definitions

        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // First define the structure outside
        interpreter.Interpret("STRUCT 8 FIELD >px 8 FIELD >py CONSTANT /MyPoint");

        // Define a word that uses the structure
        interpreter.Interpret(": SET-POINT ( x y addr -- ) >R R@ >py ! R> >px ! ;");
        interpreter.Interpret(": GET-X ( addr -- x ) >px @ ;");
        interpreter.Interpret(": GET-Y ( addr -- y ) >py @ ;");

        // Create a point variable
        interpreter.Interpret("VARIABLE myPt");

        // Act - Use the words
        interpreter.Interpret("123 456 myPt SET-POINT");
        
        interpreter.Interpret("myPt GET-X");
        var x = vm.DataStack.PopLong();

        interpreter.Interpret("myPt GET-Y");
        var y = vm.DataStack.PopLong();

        // Assert
        Assert.Equal(123, x);
        Assert.Equal(456, y);
    }

    [Fact]
    public void TestMultipleStructDefinitions()
    {
        // Ensure multiple struct definitions don't interfere with each other

        // Arrange
        var vm = new VM();
        var interpreter = new ForthInterpreter(vm);

        // Define first struct
        interpreter.Interpret("STRUCT 4 FIELD >width 4 FIELD >height CONSTANT /Size");

        // Define second struct with same-sized fields
        interpreter.Interpret("STRUCT 4 FIELD >red 4 FIELD >green CONSTANT /Color2");

        // Define third struct with different sizes
        interpreter.Interpret("STRUCT 8 FIELD >id 2 FIELD >type CONSTANT /Header");

        // Act - Test all three
        interpreter.Interpret("100 >width");
        var width = vm.DataStack.PopLong();

        interpreter.Interpret("100 >red");
        var red = vm.DataStack.PopLong();

        interpreter.Interpret("100 >id");
        var id = vm.DataStack.PopLong();

        interpreter.Interpret("100 >type");
        var type = vm.DataStack.PopLong();

        // Assert - Each field has correct offset for its own structure
        Assert.Equal(100, width);  // >width offset 0 in /Size
        Assert.Equal(100, red);    // >red offset 0 in /Color2
        Assert.Equal(100, id);     // >id offset 0 in /Header
        Assert.Equal(108, type);   // >type offset 8 in /Header
    }
}
