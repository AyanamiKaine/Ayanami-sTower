using clox;

namespace cloxUnitTests;

public class ByteChunkTest
{
    [Fact]
    public void WriteToChunk()
    {
        List<OpCode?> expectedOpcodes = [OpCode.OP_RETURN];

        var chunk = new Chunk("Test Chunk");
        chunk.Write(OpCode.OP_RETURN);
        var actualOpCodes = chunk.DisassembleAllOpcodes();

        Assert.Equal(expectedOpcodes, actualOpCodes);
    }

    [Fact]
    public void AddingConstantsToChunkTest()
    {
        List<OpCode?> expectedOpcodes = [OpCode.OP_CONSTANT, OpCode.OP_RETURN];

        var chunk = new Chunk("Test Chunk");
        var index = chunk.AddConstant(1.2);

        chunk.Write(OpCode.OP_CONSTANT);
        chunk.Write(index);
        chunk.Write(OpCode.OP_RETURN);

        var actualOpCodes = chunk.DisassembleAllOpcodes();

        Assert.Equal(expectedOpcodes, actualOpCodes);
    }
}
