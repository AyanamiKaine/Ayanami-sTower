using System;

namespace StellaLang;

/// <summary>
/// Abstraction for host input/output used by the interpreter.
/// This enables tests to inject a fake writer/reader instead of using Console.
/// </summary>
public interface IHostIO
{
    /// <summary>
    /// A <see cref="TextWriter"/> that writes to the standard output stream.
    /// </summary>
    TextWriter Out { get; }

    /// <summary>
    /// A <see cref="TextWriter"/> that writes to the standard error stream.
    /// </summary>
    TextWriter Error { get; }

    /// <summary>
    /// A <see cref="TextReader"/> that reads from the standard input stream.
    /// </summary>
    TextReader In { get; }

    /// <summary>
    /// Writes the specified string to the output without appending a newline.
    /// </summary>
    /// <param name="s">The string to write. May be <c>null</c>.</param>
    void Write(string? s);

    /// <summary>
    /// Writes the specified string to the output followed by the current line terminator.
    /// </summary>
    /// <param name="s">The string to write. May be <c>null</c>.</param>
    void WriteLine(string? s);

    /// <summary>
    /// Reads the next line of characters from the input stream.
    /// </summary>
    /// <returns>The next line from the input stream, or <c>null</c> if no more lines are available.</returns>
    string? ReadLine();
}
