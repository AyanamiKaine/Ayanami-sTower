using System;

namespace StellaLang;

/// <summary>
/// A host I/O implementation that forwards input and output to <see cref="Console"/>.
/// </summary>
public sealed class ConsoleHostIO : IHostIO
{
    /// <summary>
    /// Gets a <see cref="TextWriter"/> that writes to the standard output stream.
    /// </summary>
    public TextWriter Out => Console.Out;

    /// <summary>
    /// Gets a <see cref="TextWriter"/> that writes to the standard error stream.
    /// </summary>
    public TextWriter Error => Console.Error;

    /// <summary>
    /// Gets a <see cref="TextReader"/> that reads from the standard input stream.
    /// </summary>
    public TextReader In => Console.In;

    /// <summary>
    /// Writes the specified string to the standard output without a newline.
    /// </summary>
    /// <param name="s">The string to write. May be <c>null</c>.</param>
    public void Write(string? s) => Console.Write(s);

    /// <summary>
    /// Writes the specified string to the standard output followed by the current line terminator.
    /// </summary>
    /// <param name="s">The string to write. May be <c>null</c>.</param>
    public void WriteLine(string? s) => Console.WriteLine(s);

    /// <summary>
    /// Reads the next line of characters from the standard input stream.
    /// </summary>
    /// <returns>The next line from the input stream, or <c>null</c> if no more lines are available.</returns>
    public string? ReadLine() => Console.ReadLine();
}
