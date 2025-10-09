using System;
using AyanamisTower.MemoryExtensions;

namespace StellaLang;


/*
NOTES:
We should throw exceptions for Stack overflow/underflow interrupts.
*/

/// <summary>
/// A simple stack-based virtual machine for executing StellaLang bytecode.
/// </summary>
public class VM
{
    /// <summary>
    /// A table where we map names to op codes. Same concept as in FORTH. A dictionary of words.
    /// </summary>
    public Dictionary<string, List<OPCode>> Dictionary = [];
    /// <summary>
    /// Here we define what should happen when we encounter an OP CODE.
    /// </summary>
    public Dictionary<OPCode, Action> InstructionTable = [];
    /// <summary>
    /// Data stack. Also often called parameter stack or expression stack.
    /// This is where we push and pop values for operations.
    /// </summary>
    public MemoryStack DataStack = new(1024);
    /// <summary>
    /// Handles floats/doubles.
    /// </summary>
    public MemoryStack FloatStack = new(1024);
    /// <summary>
    /// Return stack is used to store
    /// subroutine return addresses
    /// instead of instruction operands
    /// </summary>
    public MemoryStack ReturnStack = new(1024);

    /// <summary>
    /// The program counter holds the address
    /// of the next instruction to be executed.
    /// </summary>
    public int ProgramCounter = 0;

    /// <summary>
    /// Executes the next OPCode
    /// </summary>
    public void Step()
    {

    }

    /// <summary>
    /// Runs the current program until it hits a HALT OPCODE.
    /// </summary>
    public void Execute()
    {

    }

    /// <summary>
    /// Executes a set of byte codes.
    /// </summary>
    /// <param name="code"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Execute(string code)
    {
        throw new NotImplementedException();
    }

    private void GetNextInstruction()
    {

    }
}
