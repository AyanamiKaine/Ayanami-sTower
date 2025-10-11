using System;

namespace StellaLang;

public sealed class ConsoleHostIO : IHostIO
{
    public TextWriter Out => Console.Out;
    public TextWriter Error => Console.Error;
    public TextReader In => Console.In;

    public void Write(string? s) => Console.Write(s);
    public void WriteLine(string? s) => Console.WriteLine(s);
    public string? ReadLine() => Console.ReadLine();
}
