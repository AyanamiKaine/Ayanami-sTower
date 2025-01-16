namespace jlox;

public static class StringExtensions
{
    /// <summary>
    /// Extracts a substring from a string using start and end indices (inclusive).
    /// </summary>
    /// <param name="str">The original string.</param>
    /// <param name="startIndex">The zero-based starting index of the substring (inclusive).</param>
    /// <param name="endIndex">The zero-based ending index of the substring (inclusive).</param>
    /// <returns>The extracted substring.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the input string is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if startIndex or endIndex is out of range or if startIndex is greater than endIndex.</exception>
    public static string SubstringByIndex(this string str, int startIndex, int endIndex)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str), "Input string cannot be null.");
        }

        if (startIndex < 0 || startIndex >= str.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex must be within the bounds of the string.");
        }

        if (endIndex < 0 || endIndex >= str.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(endIndex), "endIndex must be within the bounds of the string.");
        }

        if (startIndex > endIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex), "startIndex cannot be greater than endIndex.");
        }

        // Calculate the length of the substring.
        int length = endIndex - startIndex + 1;

        // Use the built-in Substring method with start index and length.
        return str.Substring(startIndex, length);
    }
}