using System;

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
    /// Memory of the VM.
    /// </summary>
    public List<byte> Memory = [];

    /// <summary>
    /// Data stack.
    /// </summary>
    public List<byte> DataStack = [];
    /// <summary>
    /// Return stack is used to store
    /// subroutine return addresses
    /// instead of instruction operands
    /// </summary>
    public List<byte> ReturnStack = [];

    /// <summary>
    /// The program counter holds the address
    /// of the next instruction to be executed.
    /// </summary>
    public int ProgramCounter = 0;

    private void PreDefineWords()
    {
        DefineWord("DUP", []);
        DefineWord("DROP", []);
        DefineWord("SWAP", []);
        DefineWord("OVER", []);
        DefineWord("ROT", []);

        DefineWord("+", []);
        DefineWord("-", []);
        DefineWord("*", []);
        DefineWord("/", []);
        DefineWord("MOD", []);
        DefineWord("1+", []);
        DefineWord("1-", []);

        DefineWord("=", []);
        DefineWord(">", []);
        DefineWord("<", []);
        DefineWord("0=", []);
        DefineWord("AND", []);
        DefineWord("OR", []);
        DefineWord("NO", []);

        DefineWord("VARIABLE", []);
        DefineWord("CONSTANT", []);
        DefineWord("!", []);
        DefineWord("@", []);

        DefineWord(":", []);
        DefineWord(";", []);
        DefineWord("IMMEDIATE", []);

        DefineWord(".", []);
        DefineWord("EMIT", []);
        DefineWord("CR", []);
        DefineWord(".\"", []);

        DefineWord("IF", []);
        DefineWord("ELSE", []);
        DefineWord("THEN", []);
        DefineWord("BEGIN", []);
        DefineWord("UNTIL", []);
        DefineWord("WHILE", []);
        DefineWord("REPEAT", []);

        DefineWord("CALL", []);
        DefineWord("RETURN", []);
    }

    /// <summary>
    /// Defines a new word in the dictionary.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="opCodes"></param>
    /// <exception cref="Exception"></exception>
    public void DefineWord(string name, List<OPCode> opCodes)
    {
        if (Dictionary.ContainsKey(name))
        {
            throw new Exception($"Word {name} is already defined.");
        }
        Dictionary.Add(name, opCodes);
    }

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

    private void GetNextInstruction()
    {

    }
}
