using System;
using System.Text;

namespace StellaLang;

/// <summary>
/// A host I/O implementation that captures all output into a string buffer.
/// Useful for web-based REPLs or testing scenarios where you need to capture output.
/// </summary>
public sealed class StringHostIO : IHostIO
{
    private readonly StringWriter _outWriter;
    private readonly StringWriter _errorWriter;
    private readonly StringReader _inReader;

    /// <summary>
    /// Initializes a new instance of <see cref="StringHostIO"/>.
    /// </summary>
    /// <param name="input">Optional input string. Defaults to empty string.</param>
    public StringHostIO(string input = "")
    {
        _outWriter = new StringWriter();
        _errorWriter = new StringWriter();
        _inReader = new StringReader(input);
    }

    /// <summary>
    /// Gets a <see cref="TextWriter"/> that writes to the output buffer.
    /// </summary>
    public TextWriter Out => _outWriter;

    /// <summary>
    /// Gets a <see cref="TextWriter"/> that writes to the error buffer.
    /// </summary>
    public TextWriter Error => _errorWriter;

    /// <summary>
    /// Gets a <see cref="TextReader"/> that reads from the input buffer.
    /// </summary>
    public TextReader In => _inReader;

    /// <summary>
    /// Writes the specified string to the output buffer without a newline.
    /// </summary>
    /// <param name="s">The string to write. May be <c>null</c>.</param>
    public void Write(string? s) => _outWriter.Write(s);

    /// <summary>
    /// Writes the specified string to the output buffer followed by a newline.
    /// </summary>
    /// <param name="s">The string to write. May be <c>null</c>.</param>
    public void WriteLine(string? s) => _outWriter.WriteLine(s);

    /// <summary>
    /// Reads the next line of characters from the input buffer.
    /// </summary>
    /// <returns>The next line from the input buffer, or <c>null</c> if no more lines are available.</returns>
    public string? ReadLine() => _inReader.ReadLine();

    /// <summary>
    /// Gets all text written to the output stream.
    /// </summary>
    /// <returns>The accumulated output text.</returns>
    public string GetOutput() => _outWriter.ToString();

    /// <summary>
    /// Gets all text written to the error stream.
    /// </summary>
    /// <returns>The accumulated error text.</returns>
    public string GetError() => _errorWriter.ToString();

    /// <summary>
    /// Clears the output buffer.
    /// </summary>
    public void ClearOutput()
    {
        _outWriter.GetStringBuilder().Clear();
    }

    /// <summary>
    /// Clears the error buffer.
    /// </summary>
    public void ClearError()
    {
        _errorWriter.GetStringBuilder().Clear();
    }

    /// <summary>
    /// Clears both output and error buffers.
    /// </summary>
    public void Clear()
    {
        ClearOutput();
        ClearError();
    }
}
