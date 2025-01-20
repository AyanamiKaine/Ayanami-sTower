using System.Collections;
using System.Text;

namespace clox;


/// <summary>
/// In our bytecode format, each instruction has a one-byte operation code. Shortend to opcode.
/// </summary>
public enum OpCode
{
    /// <summary>
    /// When the VM executes a constant instruction, it "loads" the constant for use.
    /// With a limit of 256 different constants.
    /// </summary>
    OP_CONSTANT,
    OP_CONSTANT_LONG,
    /// <summary>
    /// Return from the current function
    /// </summary>
    OP_RETURN,

}

public struct Chunk : IEnumerable<byte>
{
    public Chunk()
    {
        Name = "";
        Code = [];
        Constants = [];
        _lines = [];
    }

    public Chunk(string name)
    {
        Name = name;
        Code = [];
        Constants = [];
        _lines = [];
    }

    public string Name = "";
    public readonly List<byte> Code = [];
    public readonly List<LoxValue> Constants = [];
    private readonly List<int> _lines = [];

    public readonly (OpCode, int) DisassembleInstruction(int offset)
    {
        byte instruction = Code[offset];
        switch (instruction)
        {
            case (byte)OpCode.OP_RETURN:
                return (OpCode.OP_RETURN, SimpleInstruction(offset));
            case (byte)OpCode.OP_CONSTANT:
                return (OpCode.OP_CONSTANT, ConstantInstruction(offset));
            default:
                byte[] byteArray = [instruction];
                throw new Exception($"Unknown Opcode {Encoding.ASCII.GetString(byteArray)}");
        }
    }

    private static int ConstantInstruction(int offset)
    {
        return offset + 2;
    }

    private static int SimpleInstruction(int offset)
    {
        return offset + 1;
    }

    /// <summary>
    /// Returns the index where the constant was placed.
    /// </summary>
    /// <param name="constant"></param>
    /// <returns></returns>
    public readonly int AddConstant(double constant)
    {
        // Creates a new LoxValue struct and adds it
        // to the list of constants
        Constants.Add(new(constant));
        return Constants.Count - 1;
    }

    /// <summary>
    /// Returns the opcode of the last instruction added.
    /// </summary>
    /// <returns></returns>
    public readonly OpCode? Disassemble()
    {

        OpCode? opCode = null;
        for (int offset = 0; offset < Code.Count;)
        {
            (opCode, offset) = DisassembleInstruction(offset);
        }

        return opCode;
    }

    /// <summary>
    /// Returns the list of all opcodes added ignoring operands of opcodes.
    /// </summary>
    /// <returns></returns>
    public readonly List<OpCode?> DisassembleAllOpcodes()
    {
        var opcodes = new List<OpCode?>();
        for (int offset = 0; offset < Code.Count;)
        {
            (var opCode, offset) = DisassembleInstruction(offset);
            opcodes.Add(opCode);
        }
        return opcodes;
    }

    public readonly void Write(byte data)
    {
        Code.Add(data);
    }

    public readonly void Write(int data)
    {
        Code.Add((byte)data);
    }

    public readonly void Write(OpCode opCode)
    {
        Code.Add((byte)opCode);
    }

    public readonly void Write(string data)
    {
        byte[] ASCIIBytes = Encoding.ASCII.GetBytes(data);
        foreach (byte b in ASCIIBytes)
        {
            Code.Add(b);
        }
    }

    public readonly IEnumerator<byte> GetEnumerator()
    {
        return Code.GetEnumerator();
    }

    readonly IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Returns the chunk of byte code as a nice representation of it
    /// think of it as pretty-print. This can be used for example when an error occurs
    /// </summary>
    /// <returns></returns>
    public override readonly string ToString()
    {
        var data = "";
        var opcodes = new List<OpCode?>();
        for (int offset = 0; offset < Code.Count;)
        {
            (var opCode, offset) = DisassembleInstruction(offset);
            opcodes.Add(opCode);


            // To correctly determine the offset we first 
            // need to subtract the added offset by 
            // DisassembleInstruction OP_CONSTANT has 
            // an offset by two so we subtract two from the current offset
            switch (opCode)
            {
                case OpCode.OP_CONSTANT:
                    data += $"offset: '{offset - 2}' opCode:{OpCode.OP_CONSTANT} ConstantIndex:{Code[offset - 1]} ConstantValue: '{Constants[Code[offset - 1]]}'\n";
                    break;
                case OpCode.OP_RETURN:
                    data += $"offset: '{offset - 1}' opCode:{OpCode.OP_RETURN}\n";
                    break;
                default:
                    // Handle default case
                    break;
            }

        }
        return data;
    }
}

