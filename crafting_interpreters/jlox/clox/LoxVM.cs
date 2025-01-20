namespace clox;

public class LoxVM(Chunk chunk)
{
    private Chunk Chunk = chunk;
    public int IntructionPointer = 0;

    public void Interpret()
    {
        Run();
    }

    private void Run()
    {
        var instruction = ReadNextInstruction();
        switch (instruction)
        {
            case OpCode.OP_CONSTANT:
                LoxValue constant = ReadConstant();
                break;
            case OpCode.OP_RETURN:
                return;
            default:
                throw new Exception($"Unknown instruction {instruction}, DUMPING BYTECODE: {Chunk}");
        }

    }

    /// <summary>
    /// Reads the next byte in the code, uses its byte value as an index for the constants array in the chunk.
    /// Returns the Constant LoxValue.
    /// </summary>
    /// <returns></returns>
    private LoxValue ReadConstant()
    {
        return Chunk.Constants[ReadNextByte()];
    }

    /// <summary>
    /// Reads the next byte as an OpCode increments the instruction pointer by one and returns the OpCode.
    /// </summary>
    /// <returns></returns>
    private OpCode ReadNextInstruction()
    {
        IntructionPointer++;
        return (OpCode)Chunk.Code[IntructionPointer];
    }

    /// <summary>
    /// Reads the next byte in the code. Increments the instruction pointer by one and returns the byte.
    /// </summary>
    /// <returns></returns>
    private byte ReadNextByte()
    {
        IntructionPointer++;
        return Chunk.Code[IntructionPointer];
    }
}
