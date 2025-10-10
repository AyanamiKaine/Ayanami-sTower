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
        var forthCode =
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
}
