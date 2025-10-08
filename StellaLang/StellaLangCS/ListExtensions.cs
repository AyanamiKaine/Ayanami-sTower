using System;
using System.Collections.Generic;

namespace StellaLang;

/// <summary>
/// Extension methods for List&lt;byte&gt; to provide stack-like functionality.
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// Pushes a byte value onto the end of the list (stack-like behavior).
    /// </summary>
    /// <param name="list">The list to push onto.</param>
    /// <param name="value">The byte value to push.</param>
    public static void Push(this List<byte> list, byte value)
    {
        list.Add(value);
    }

    /// <summary>
    /// Pops a byte value from the end of the list (stack-like behavior).
    /// </summary>
    /// <param name="list">The list to pop from.</param>
    /// <returns>The byte value that was at the end of the list.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the list is empty.</exception>
    public static byte Pop(this List<byte> list)
    {
        if (list.Count == 0)
        {
            throw new InvalidOperationException("Cannot pop from an empty list.");
        }

        int lastIndex = list.Count - 1;
        byte value = list[lastIndex];
        list.RemoveAt(lastIndex);
        return value;
    }

    /// <summary>
    /// Peeks at the byte value at the end of the list without removing it.
    /// </summary>
    /// <param name="list">The list to peek at.</param>
    /// <returns>The byte value at the end of the list.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the list is empty.</exception>
    public static byte Peek(this List<byte> list)
    {
        if (list.Count == 0)
        {
            throw new InvalidOperationException("Cannot peek at an empty list.");
        }

        return list[list.Count - 1];
    }

    /// <summary>
    /// Checks if the list is empty.
    /// </summary>
    /// <param name="list">The list to check.</param>
    /// <returns>True if the list is empty, false otherwise.</returns>
    public static bool IsEmpty(this List<byte> list)
    {
        return list.Count == 0;
    }
}