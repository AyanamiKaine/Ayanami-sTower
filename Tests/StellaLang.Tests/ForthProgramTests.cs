using System;

namespace StellaLang.Tests;

/// <summary>
/// Here we are testing some Forth programs to ensure the interpreter and compiler work correctly.
/// These tests focus on higher-level Forth constructs rather than individual opcodes.
/// </summary>
public class ForthProgramTests
{
    /// <summary>
    /// An example taking from https://www.forth.com/starting-forth/12-forth-code-example/
    /// </summary>
    [Fact]
    public void TestMaterialExample()
    {
        const string forthCode =
        """
        VARIABLE DENSITY
        VARIABLE THETA
        VARIABLE ID

        : " ( -- addr )   [CHAR] " WORD DUP C@ 1+ ALLOT ;

        : MATERIAL ( addr n1 n2 -- )    \ addr=string, n1=density, n2=theta
        CREATE  , , , 
        DOES> ( -- )   DUP @ THETA !
        CELL+ DUP @ DENSITY !  CELL+ @ ID ! ;

        : .SUBSTANCE ( -- )   ID @ COUNT TYPE ;
        : FOOT ( n1 -- n2 )   10 * ;
        : INCH ( n1 -- n2 )   100 12 */  5 +  10 /  + ;
        : /TAN ( n1 -- n2 )   1000 THETA @ */ ;

        : PILE ( n -- )         \ n=scaled height
        DUP DUP 10 */ 1000 */  355 339 */  /TAN /TAN
        DENSITY @ 200 */  ." = " . ." tons of "  .SUBSTANCE ;

        \ table of materials
        \   string-address  density  tan[theta] 
        " cement"           131        700  MATERIAL CEMENT
        " loose gravel"      93        649  MATERIAL LOOSE-GRAVEL
        " packed gravel"    100        700  MATERIAL PACKED-GRAVEL
        " dry sand"          90        754  MATERIAL DRY-SAND
        " wet sand"         118        900  MATERIAL WET-SAND
        " clay"             120        727  MATERIAL CLAY
        
        CEMENT 10 FOOT PILE
        """;

        var interpreter = new ForthInterpreter();

        // Capture console output
        var originalOut = Console.Out;
        using var writer = new System.IO.StringWriter();
        Console.SetOut(writer);

        try
        {
            interpreter.Interpret(forthCode);

            string output = writer.ToString();
            // The program should output "138 tons of cement" (or similar calculation)
            Assert.Contains("tons of", output);
            Assert.Contains("cement", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Test Fibonacci using a single tail-recursive definition with RECURSE.
    /// </summary>
    [Fact]
    public void TestFibonacciRecursiveTail()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Tail-recursive Fibonacci in a single word expecting ( a b n -- f )
        // a = F(n), b = F(n+1). Base: n==0 -> return a. Step: (b, a+b, n-1)
        const string fibTail =
            ": fib-rec ( a b n -- f )  DUP 0= IF DROP DROP EXIT THEN  >R TUCK + R> 1- RECURSE ;";

        forth.Interpret(fibTail);

        // Validate a few values
        // Bootstrap calls with initial accumulators a=0, b=1: (0 1 n)
        forth.Interpret("0 1 0 fib-rec");
        Assert.Equal(0L, vm.DataStack.PopLong());

        forth.Interpret("0 1 1 fib-rec");
        Assert.Equal(1L, vm.DataStack.PopLong());

        forth.Interpret("0 1 2 fib-rec");
        Assert.Equal(1L, vm.DataStack.PopLong());

        forth.Interpret("0 1 5 fib-rec");
        Assert.Equal(5L, vm.DataStack.PopLong());

        forth.Interpret("0 1 10 fib-rec");
        Assert.Equal(55L, vm.DataStack.PopLong());
    }

    /// <summary>
    /// Test factorial using non-tail recursion with RECURSE.
    /// This verifies that CALL/RET work properly for recursion.
    /// </summary>
    [Fact]
    public void TestFactorialRecursive()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Factorial: factorial(n) = n <= 1 ? 1 : n * factorial(n-1)
        // Stack effect: ( n -- n! )
        const string factDef =
            ": factorial ( n -- n! )  DUP 1 <= IF DROP 1 EXIT THEN  DUP 1- RECURSE * ;";

        forth.Interpret(factDef);

        // Test factorial values
        forth.Interpret("0 factorial");
        Assert.Equal(1L, vm.DataStack.PopLong());

        forth.Interpret("1 factorial");
        Assert.Equal(1L, vm.DataStack.PopLong());

        forth.Interpret("5 factorial");
        Assert.Equal(120L, vm.DataStack.PopLong());

        forth.Interpret("10 factorial");
        Assert.Equal(3628800L, vm.DataStack.PopLong());
    }

    /// <summary>
    /// Test that demonstrates both tail-call optimization and non-tail recursion.
    /// </summary>
    [Fact]
    public void TestTailCallOptimization()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Tail-recursive countdown (RECURSE is in tail position, should use JMP)
        const string countdownDef =
            ": countdown ( n -- )  DUP 0= IF DROP EXIT THEN  1- RECURSE ;";

        forth.Interpret(countdownDef);

        // This should not overflow return stack even with large n
        forth.Interpret("1000 countdown");
        // If tail-call optimization works, return stack should be empty
        Assert.Equal(0, vm.ReturnStack.Pointer);

        // Non-tail recursive sum (RECURSE is NOT in tail position due to +)
        const string sumDef =
            ": sum ( n -- sum )  DUP 0= IF EXIT THEN  DUP 1- RECURSE + ;";

        forth.Interpret(sumDef);

        // sum(10) = 10 + 9 + 8 + ... + 1 = 55
        forth.Interpret("10 sum");
        Assert.Equal(55L, vm.DataStack.PopLong());

        // sum(5) = 5 + 4 + 3 + 2 + 1 = 15
        forth.Interpret("5 sum");
        Assert.Equal(15L, vm.DataStack.PopLong());
    }

    /// <summary>
    /// Test Fibonacci sequence calculation using iterative approach.
    /// Based on the classic Forth fibonacci implementation from literate programs.
    /// </summary>
    [Fact]
    public void TestFibonacciIterative()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Classic Forth iterative Fibonacci: fib-iter
        const string fibDef = ": fib-iter ( n -- f )  0 1 rot 0 ?do over + swap loop drop ;";

        forth.Interpret(fibDef);

        // Test first 10 Fibonacci numbers: 0, 1, 1, 2, 3, 5, 8, 13, 21, 34
        long[] expectedFib = { 0, 1, 1, 2, 3, 5, 8, 13, 21, 34 };

        for (int i = 0; i < expectedFib.Length; i++)
        {
            forth.Interpret($"{i} fib-iter");
            long result = vm.DataStack.PopLong();
            Assert.Equal(expectedFib[i], result);
        }
    }
    /// <summary>
    /// Test a simpler Fibonacci for edge cases.
    /// </summary>
    [Fact]
    public void TestFibonacciSimple()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // Simple iterative version
        const string fibDef = ": fib ( n -- fib[n] )  0 1 rot 0 ?do over + swap loop drop ;";

        forth.Interpret(fibDef);

        // Test cases
        forth.Interpret("0 fib");
        Assert.Equal(0L, vm.DataStack.PopLong());

        forth.Interpret("1 fib");
        Assert.Equal(1L, vm.DataStack.PopLong());

        forth.Interpret("5 fib");
        Assert.Equal(5L, vm.DataStack.PopLong());

        forth.Interpret("10 fib");
        Assert.Equal(55L, vm.DataStack.PopLong());
    }
}
