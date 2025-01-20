using System.Text;

namespace clox;


/// <summary>
/// In our bytecode format, each instruction has a one-byte operation code. Shortend to opcode.
/// </summary>
public enum OpCode
{
    /// <summary>
    /// When the VM executes a constant instruction, it "loads" the constant for use.
    /// </summary>
    OP_CONSTANT,
    /// <summary>
    /// Return from the current function
    /// </summary>
    OP_RETURN,

}

public struct Chunk
{
    public Chunk()
    {
        Name = "";
        _code = [];
        _constants = [];
        _lines = [];
    }

    public Chunk(string name)
    {
        Name = name;
        _code = [];
        _constants = [];
        _lines = [];
    }

    public string Name = "";
    private List<byte> _code = [];
    private List<LoxValue> _constants = [];
    private List<int> _lines = [];

    public (OpCode, int) DisassembleInstruction(int offset)
    {
        byte instruction = _code[offset];
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

    private int ConstantInstruction(int offset)
    {
        return offset + 2;
    }

    private int SimpleInstruction(int offset)
    {
        return offset + 1;
    }

    /// <summary>
    /// Returns the index where the constant was placed.
    /// </summary>
    /// <param name="constant"></param>
    /// <returns></returns>
    public int AddConstant(double constant)
    {
        // Creates a new LoxValue struct and adds it
        // to the list of constants
        _constants.Add(new(constant));
        return _constants.Count - 1;
    }

    /// <summary>
    /// Returns the opcode of the last instruction added.
    /// </summary>
    /// <returns></returns>
    public OpCode? Disassemble()
    {

        OpCode? opCode = null;
        for (int offset = 0; offset < _code.Count;)
        {
            (opCode, offset) = DisassembleInstruction(offset);
        }

        return opCode;
    }

    public void Write(byte data)
    {
        _code.Add(data);
    }

    public void Write(int data)
    {
        _code.Add((byte)data);
    }

    public void Write(OpCode opCode)
    {
        _code.Add((byte)opCode);
    }

    public void Write(string data)
    {
        byte[] ASCIIBytes = Encoding.ASCII.GetBytes(data);
        foreach (byte b in ASCIIBytes)
        {
            _code.Add(b);
        }
    }
}

