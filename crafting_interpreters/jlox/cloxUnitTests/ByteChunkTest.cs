using clox;

namespace cloxUnitTests;

public class ByteChunkTest
{
    [Fact]
    public void WriteToChunk()
    {
        var expectedOpcode = OpCode.OP_RETURN;

        var chunk = new Chunk("Test Chunk");
        chunk.Write(OpCode.OP_RETURN);
        var actualOpCode = chunk.Disassemble();

        Assert.Equal(expectedOpcode, actualOpCode);
    }

    [Fact]
    public void AddingConstantsToChunkTest1()
    {
        var expectedOpcode = OpCode.OP_CONSTANT;

        var chunk = new Chunk("Test Chunk");
        var index = chunk.AddConstant(1.2);

        chunk.Write(OpCode.OP_CONSTANT);
        var actualOpCode = chunk.Disassemble();

        chunk.Write(index);
        chunk.Write(OpCode.OP_RETURN);


        Assert.Equal(expectedOpcode, actualOpCode);
    }

    [Fact]
    public void AddingConstantsToChunkTest2()
    {
        var expectedOpcode = OpCode.OP_RETURN;

        var chunk = new Chunk("Test Chunk");
        var index = chunk.AddConstant(1.2);

        chunk.Write(OpCode.OP_CONSTANT);

        chunk.Write(index);
        chunk.Write(OpCode.OP_RETURN);
        var actualOpCode = chunk.Disassemble();


        Assert.Equal(expectedOpcode, actualOpCode);
    }

    [Fact]
    public void AddingConstantsToChunkTest3()
    {
        var expectedOpcode = OpCode.OP_CONSTANT;

        var chunk = new Chunk("Test Chunk");
        var index = chunk.AddConstant(1.2);

        chunk.Write(OpCode.OP_CONSTANT);

        chunk.Write(index);
        var actualOpCode = chunk.Disassemble();

        chunk.Write(OpCode.OP_RETURN);


        Assert.Equal(expectedOpcode, actualOpCode);
    }
}
