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
        """;

        var interpreter = new ForthInterpreter();
        interpreter.Interpret(forthCode);
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
