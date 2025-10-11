using System;

namespace StellaLang;

/// <summary>
/// Abstraction for host input/output used by the interpreter.
/// This enables tests to inject a fake writer/reader instead of using Console.
/// </summary>
public interface IHostIO
{
    TextWriter Out { get; }
    TextWriter Error { get; }
    TextReader In { get; }

    void Write(string? s);
    void WriteLine(string? s);
    string? ReadLine();
}
