using System;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace StellaLang.Tests;
/// <summary>
/// Testing our builder
/// </summary>
public class OpCodeBuilderTests
{
    [Fact]
    public void AddingExample()
    {
        var builder = new OpCodeBuilder()
            .AddOPCode(OPCode.PUSH)
            .AddConstant(5)
            .Build;
    } 
}
