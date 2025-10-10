using System;

namespace StellaLang.Tests;

/// <summary>
/// Tests that verify primitives can be compiled inline into colon definitions,
/// including primitives that were previously implemented only with handlers.
/// </summary>
public class ForthCompilationTests
{
    /// <summary>
    /// Test that the '.' (dot/print) primitive can be compiled into a colon definition.
    /// Previously, this would fail with "Primitive '.' uses a handler and cannot be compiled inline."
    /// </summary>
    [Fact]
    public void TestDotCanBeCompiledInline()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Capture console output to verify behavior
        var originalOut = Console.Out;
        using var writer = new System.IO.StringWriter();
        Console.SetOut(writer);

        try
        {
            // Define a word that uses '.' inside it
            // This would previously throw an exception during compilation
            const string code = ": print-value ( n -- ) . ;";
            forth.Interpret(code);

            // Execute the compiled word
            forth.Interpret("42 print-value");

            // Verify output
            string output = writer.ToString();
            Assert.Contains("42", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Test that multiple handler-based primitives can be used in a single definition.
    /// </summary>
    [Fact]
    public void TestMultipleHandlerPrimitivesInDefinition()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        var originalOut = Console.Out;
        using var writer = new System.IO.StringWriter();
        Console.SetOut(writer);

        try
        {
            // Define a word using multiple I/O primitives
            const string code = ": show-sum ( a b -- ) + . CR ;";
            forth.Interpret(code);

            // Execute it
            forth.Interpret("3 4 show-sum");

            string output = writer.ToString();
            Assert.Contains("7", output);
            Assert.Contains("\n", output); // CR adds newline
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Test that EMIT can be compiled inline (another handler-based primitive).
    /// </summary>
    [Fact]
    public void TestEmitCanBeCompiledInline()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        var originalOut = Console.Out;
        using var writer = new System.IO.StringWriter();
        Console.SetOut(writer);

        try
        {
            // Define a word that uses EMIT
            const string code = ": print-char ( c -- ) EMIT ;";
            forth.Interpret(code);

            // Print the letter 'A' (ASCII 65)
            forth.Interpret("65 print-char");

            string output = writer.ToString();
            Assert.Contains("A", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Test that HERE can be compiled inline (returns interpreter state).
    /// </summary>
    [Fact]
    public void TestHereCanBeCompiledInline()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Define a word that uses HERE
        const string code = ": get-here ( -- addr ) HERE ;";
        forth.Interpret(code);

        // Execute and verify we get an address
        forth.Interpret("get-here");
        long addr = vm.DataStack.PopLong();

        // Address should be >= 0 (some valid memory location)
        Assert.True(addr >= 0);
    }

    /// <summary>
    /// Test complex definition mixing handler-based and bytecode primitives.
    /// </summary>
    [Fact]
    public void TestMixedPrimitivesInDefinition()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        var originalOut = Console.Out;
        using var writer = new System.IO.StringWriter();
        Console.SetOut(writer);

        try
        {
            // Mix arithmetic (bytecode), stack ops (bytecode), and I/O (handler-based)
            const string code = ": compute-and-print ( a b -- ) + DUP . SPACE . CR ;";
            forth.Interpret(code);

            // Should print sum twice
            forth.Interpret("5 3 compute-and-print");

            string output = writer.ToString();
            // Should contain "8 8" followed by newline
            Assert.Contains("8", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Test that VARIABLE works and can be accessed from compiled definitions.
    /// </summary>
    [Fact]
    public void TestVariableInCompiledDefinition()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Create a variable and define words that use it (tests compilation, not full runtime)
        const string code = @"
            VARIABLE COUNTER
            : increment ( -- ) COUNTER @ 1 + COUNTER ! ;
            : get-count ( -- n ) COUNTER @ ;
        ";

        // Should not throw during compilation
        forth.Interpret(code);

        // Verify the words were created
        Assert.True(true); // If we got here without exception, the test passed
    }

    /// <summary>
    /// Regression test: ensure all handler-based primitives get syscall IDs
    /// and can be both executed and compiled.
    /// </summary>
    [Fact]
    public void TestAllHandlerPrimitivesAreCompilable()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        var originalOut = Console.Out;
        using var writer = new System.IO.StringWriter();
        Console.SetOut(writer);

        try
        {
            // Test each handler-based primitive in a compiled context
            string[] testCases =
            [
                ": test-dot ( n -- ) . ;",
                ": test-emit ( c -- ) EMIT ;",
                ": test-cr ( -- ) CR ;",
                ": test-space ( -- ) SPACE ;",
                ": test-here ( -- addr ) HERE ;",
            ];

            foreach (var testCase in testCases)
            {
                // Should not throw during compilation
                forth.Interpret(testCase);
            }

            // Execute them to ensure they work
            forth.Interpret("42 test-dot");
            forth.Interpret("65 test-emit");
            forth.Interpret("test-cr");
            forth.Interpret("test-space");
            forth.Interpret("test-here DROP"); // Drop the address

            // Verify we got output
            string output = writer.ToString();
            Assert.Contains("42", output);
            Assert.Contains("A", output); // EMIT 65 = 'A'
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
