using System;

namespace StellaLang.Tests;

public sealed class TestHostIO : StellaLang.IHostIO
{
    private readonly System.IO.StringWriter _out = new();
    private readonly System.IO.StringWriter _error = new();

    public TextWriter Out => _out;
    public TextWriter Error => _error;
    public TextReader In => System.IO.TextReader.Null;

    public void Write(string? s) => _out.Write(s);
    public void WriteLine(string? s) => _out.WriteLine(s);
    public string? ReadLine() => null;

    public string GetOutput() => _out.ToString();
    public string GetError() => _error.ToString();
}
