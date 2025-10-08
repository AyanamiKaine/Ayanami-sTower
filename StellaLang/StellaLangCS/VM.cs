using System;

namespace StellaLang;

/// <summary>
/// A simple stack-based virtual machine for executing StellaLang bytecode.
/// </summary>
public class VM
{
    /// <summary>
    /// A table where we map byte codes to instructions actions.
    /// </summary>
    public Dictionary<byte, Action> InstructionTable = [];
    /// <summary>
    /// Current position of our virtual machines execution point. 
    /// </summary>
    public int InstructionPointer;

    /// <summary>
    /// Current program code to be executed.
    /// </summary>
    public List<byte> Code = [];
    /// <summary>
    /// Defined constants
    /// </summary>
    public List<dynamic> Constants = [];
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
}
