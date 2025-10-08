using System;

namespace StellaLang;

/// <summary>
/// Simple builder abstraction to build up a set of opcodes.
/// </summary>
public class OpCodeBuilder
{
    private readonly List<byte> _code = [];

    /// <summary>
    /// Adds one op code.
    /// </summary>
    /// <param name="opCode"></param>
    public OpCodeBuilder AddOPCode(OPCode opCode)
    {
        _code.Add((byte)opCode);
        return this;
    }

    /// <summary>
    /// Adds a constant, it should automatically turn values into the right number of bytes.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public OpCodeBuilder AddConstant()
    {
        return this;
    }

    /// <summary>
    /// Adds an integer constant
    /// </summary>
    /// <param name="integer"></param>
    /// <exception cref="NotImplementedException"></exception>
    public OpCodeBuilder AddConstant(int integer)
    {
        return this;
    }

    /// <summary>
    /// Adds an integer constant
    /// </summary>
    /// <param name="b"></param>
    /// <exception cref="NotImplementedException"></exception>
    public OpCodeBuilder AddConstant(byte b)
    {
        return this;
    }

    /// <summary>
    /// Returns a list of bytes
    /// </summary>
    /// <returns></returns>
    public List<byte> Build()
    {
        return _code;
    }
}
