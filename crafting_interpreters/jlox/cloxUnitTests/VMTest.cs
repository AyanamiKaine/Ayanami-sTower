using clox;
namespace cloxUnitTests;

public class VMTest
{
    [Fact]
    public void SimpleMathTest()
    {
        var chunk = new Chunk();

        chunk.Write(OpCode.OP_CONSTANT);
        chunk.Write(chunk.AddConstant(1.2));
        chunk.Write(OpCode.OP_CONSTANT);
        chunk.Write(chunk.AddConstant(3.4));
        chunk.Write(OpCode.OP_CONSTANT);
        chunk.Write(chunk.AddConstant(5.6));
        chunk.Write(OpCode.OP_DIVIDE);
        chunk.Write(OpCode.OP_NEGATE);
        chunk.Write(OpCode.OP_RETURN);

        var vm = new LoxVM(chunk);
        vm.Interpret();
    }
}
