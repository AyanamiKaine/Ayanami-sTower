namespace clox;
using LoxValue = double;

public class LoxVM(Chunk chunk)
{
    private Chunk Chunk = chunk;
    public int IntructionPointer = 0;
    public Stack<LoxValue> Stack = new();
    public void Interpret()
    {
        Run();
    }

    public void Interpret(string source)
    {
        Compiler.Compile(source);
    }
    private void Run()
    {
        while (true)
        {

            var instruction = ReadNextInstruction();
            switch (instruction)
            {
                case OpCode.OP_CONSTANT:
                    LoxValue constant = ReadConstant();
                    Push(constant);
                    break;
                case OpCode.OP_ADD:
                    {
                        double b = Pop();
                        double a = Pop();
                        Push(a + b);
                    }
                    break;
                case OpCode.OP_SUBTRACT:
                    {
                        double b = Pop();
                        double a = Pop();
                        Push(a - b);
                    }
                    break;
                case OpCode.OP_MULTIPLY:
                    {
                        double b = Pop();
                        double a = Pop();
                        Push(a * b);
                    }
                    break;
                case OpCode.OP_DIVIDE:
                    {
                        double b = Pop();
                        double a = Pop();
                        Push(a / b);
                    }
                    break;
                case OpCode.OP_NEGATE:
                    Push(-Pop());
                    break;
                case OpCode.OP_RETURN:
                    Pop();
                    return;
                default:
                    throw new Exception($"Unknown instruction {instruction}, DUMPING BYTECODE: {Chunk}");
            }
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
        var opCode = (OpCode)Chunk.Code[IntructionPointer];
        IntructionPointer++;
        return opCode;
    }

    /// <summary>
    /// Reads the next byte in the code. Increments the instruction pointer by one and returns the byte.
    /// </summary>
    /// <returns></returns>
    private byte ReadNextByte()
    {
        var data = Chunk.Code[IntructionPointer];
        IntructionPointer++;
        return data;
    }

    private void Push(double value)
    {
        Stack.Push(value);
    }

    private double Pop()
    {
        return Stack.Pop();
    }
}
