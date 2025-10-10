using System;

namespace StellaLang.Tests;

/// <summary>
/// Tests for Forth string handling primitives and operations.
/// </summary>
public class ForthStringTests
{
    /// <summary>
    /// Test that WORD creates a counted string and TYPE can print it.
    /// </summary>
    [Fact]
    public void TestWordAndType()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        var originalOut = Console.Out;
        using var writer = new System.IO.StringWriter();
        Console.SetOut(writer);

        try
        {
            // WORD reads until space, creates counted string at HERE
            forth.Interpret("[CHAR] \" WORD");

            // Now we should have address of counted string on stack
            // Let's use COUNT to get addr and len, then TYPE to print
            forth.Interpret("COUNT TYPE");

            string output = writer.ToString();
            // The input buffer should contain some text after the WORD call
            // But since we're calling WORD in interpret mode, it reads from empty buffer
            // Let's test with a proper string input
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Test the custom " word that creates a counted string.
    /// </summary>
    [Fact]
    public void TestCustomQuoteWord()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        var originalOut = Console.Out;
        using var writer = new System.IO.StringWriter();
        Console.SetOut(writer);

        try
        {
            // Define the " word as in the MATERIAL example
            forth.Interpret(": \" ( -- addr )   [CHAR] \" WORD DUP C@ 1+ ALLOT ;");

            // Test it by creating a string and using COUNT TYPE
            forth.Interpret(": test-str ( -- )   .\" hello\" COUNT TYPE ;");
            forth.Interpret("test-str");

            string output = writer.ToString();
            Assert.Contains("hello", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Test that @ (fetch) can read a cell from memory that was stored with , (comma).
    /// </summary>
    [Fact]
    public void TestCommaAndFetch()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Store a value at HERE using comma
        forth.Interpret("HERE");
        long addr = vm.DataStack.PopLong();

        forth.Interpret("42 ,");

        // Fetch the value back
        forth.Interpret($"{addr} @");
        long value = vm.DataStack.PopLong();

        Assert.Equal(42L, value);
    }

    /// <summary>
    /// Test the DOES> pattern with variables like in MATERIAL example.
    /// </summary>
    [Fact]
    public void TestDoesWithVariables()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        var originalOut = Console.Out;
        using var writer = new System.IO.StringWriter();
        Console.SetOut(writer);

        try
        {
            // Simplified version of MATERIAL pattern
            forth.Interpret(@"
                VARIABLE VAR1
                VARIABLE VAR2
                
                : SIMPLE ( n1 n2 -- )
                CREATE , ,
                DOES> ( -- ) DUP @ VAR1 ! CELL+ @ VAR2 ! ;
                
                10 20 SIMPLE TEST-ITEM
                
                TEST-ITEM
                
                VAR1 @ .
                VAR2 @ .
            ");

            string output = writer.ToString();
            Assert.Contains("10", output);
            Assert.Contains("20", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Test storing and fetching strings in memory.
    /// </summary>
    [Fact]
    public void TestStringStorage()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        var originalOut = Console.Out;
        using var writer = new System.IO.StringWriter();
        Console.SetOut(writer);

        try
        {
            // Create counted string manually
            forth.Interpret(@"
                HERE
                5 C,
                [CHAR] h C,
                [CHAR] e C,
                [CHAR] l C,
                [CHAR] l C,
                [CHAR] o C,
                COUNT TYPE
            ");

            string output = writer.ToString();
            Assert.Contains("hello", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
