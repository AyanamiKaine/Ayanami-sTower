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

    [Fact]
    public void AddingAddInstructionToChunkTest()
    {
        List<OpCode?> expectedOpcodes = [OpCode.OP_CONSTANT, OpCode.OP_CONSTANT, OpCode.OP_ADD, OpCode.OP_RETURN];

        var chunk = new Chunk("Test Chunk");
        var index1 = chunk.AddConstant(1.2);
        var index2 = chunk.AddConstant(5);

        chunk.Write(OpCode.OP_CONSTANT);
        chunk.Write(index1);
        chunk.Write(OpCode.OP_CONSTANT);
        chunk.Write(index2);
        chunk.Write(OpCode.OP_ADD);
        chunk.Write(OpCode.OP_RETURN);

        var actualOpCodes = chunk.DisassembleAllOpcodes();
        Assert.Equal(expectedOpcodes, actualOpCodes);
    }

    [Fact]
    public void AddingNegateOpCodeToChunkTest()
    {
        List<OpCode?> expectedOpcodes = [OpCode.OP_CONSTANT, OpCode.OP_NEGATE, OpCode.OP_RETURN];

        var chunk = new Chunk("Test Chunk");
        var index = chunk.AddConstant(1.2);

        chunk.Write(OpCode.OP_CONSTANT);
        chunk.Write(index);
        chunk.Write(OpCode.OP_NEGATE);
        chunk.Write(OpCode.OP_RETURN);

        var actualOpCodes = chunk.DisassembleAllOpcodes();
        Assert.Equal(expectedOpcodes, actualOpCodes);
    }

    [Fact]
    public void AddingConstantsLongToChunkTest()
    {
        List<OpCode?> expectedOpcodes = [OpCode.OP_CONSTANT_LONG, OpCode.OP_RETURN];

        var chunk = new Chunk("Test Chunk");
        var index = chunk.AddConstant(1.2);

        chunk.Write(OpCode.OP_CONSTANT_LONG);
        chunk.Write(index);
        chunk.Write(OpCode.OP_RETURN);

        var actualOpCodes = chunk.DisassembleAllOpcodes();
        Assert.Equal(expectedOpcodes, actualOpCodes);
    }
}
