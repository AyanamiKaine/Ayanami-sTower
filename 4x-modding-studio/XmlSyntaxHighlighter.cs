using Godot;
using System.Collections.Generic;

public partial class XmlSyntaxHighlighter : SyntaxHighlighter
{
    private readonly Color COL_TAG = new("ff7b7b");       // Red
    private readonly Color COL_ATTR = new("ffcc66");      // Orange
    private readonly Color COL_STRING = new("a5c261");    // Green
    private readonly Color COL_COMMENT = new("6c757d");   // Grey
    private readonly Color COL_SYMBOL = new("b0bec5");    // Light Grey
    private readonly Color COL_BASE = new("e0e0e0");      // Default Text

    public override Godot.Collections.Dictionary _GetLineSyntaxHighlighting(int lineNumber)
    {
        var textEdit = GetTextEdit();
        if (textEdit == null)
            return new Godot.Collections.Dictionary();

        string text = textEdit.GetLine(lineNumber);
        var result = new Godot.Collections.Dictionary();

        if (string.IsNullOrEmpty(text))
            return result;

        // Create an array to track color for each character position
        Color[] colors = new Color[text.Length + 1];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = COL_BASE;
        }

        int idx = 0;
        while (idx < text.Length)
        {
            // Check for comment start <!--
            if (idx + 3 < text.Length && text.Substring(idx, 4) == "<!--")
            {
                int endPos = text.IndexOf("-->", idx + 4);
                if (endPos == -1)
                {
                    endPos = text.Length - 3; // Comment continues to end of line
                }
                endPos += 3; // Include -->

                for (int j = idx; j < System.Math.Min(endPos, text.Length); j++)
                {
                    colors[j] = COL_COMMENT;
                }
                idx = endPos;
                continue;
            }

            // Check for tag start <
            if (text[idx] == '<')
            {
                colors[idx] = COL_SYMBOL; // Color the <
                idx++;

                // Check for / or ? after <
                if (idx < text.Length && (text[idx] == '/' || text[idx] == '?'))
                {
                    colors[idx] = COL_SYMBOL;
                    idx++;
                }

                // Read tag name
                while (idx < text.Length && IsTagChar(text[idx]))
                {
                    colors[idx] = COL_TAG;
                    idx++;
                }

                // Inside tag - look for attributes and closing
                while (idx < text.Length && text[idx] != '>')
                {
                    // Skip whitespace
                    if (text[idx] == ' ' || text[idx] == '\t')
                    {
                        idx++;
                        continue;
                    }

                    // Check for double-quoted string (attribute value)
                    if (text[idx] == '"')
                    {
                        colors[idx] = COL_STRING;
                        idx++;
                        while (idx < text.Length && text[idx] != '"')
                        {
                            colors[idx] = COL_STRING;
                            idx++;
                        }
                        if (idx < text.Length)
                        {
                            colors[idx] = COL_STRING; // Closing quote
                            idx++;
                        }
                        continue;
                    }

                    // Check for single-quoted string
                    if (text[idx] == '\'')
                    {
                        colors[idx] = COL_STRING;
                        idx++;
                        while (idx < text.Length && text[idx] != '\'')
                        {
                            colors[idx] = COL_STRING;
                            idx++;
                        }
                        if (idx < text.Length)
                        {
                            colors[idx] = COL_STRING; // Closing quote
                            idx++;
                        }
                        continue;
                    }

                    // Check for symbols
                    if (text[idx] == '=' || text[idx] == '/' || text[idx] == '?')
                    {
                        colors[idx] = COL_SYMBOL;
                        idx++;
                        continue;
                    }

                    // Must be an attribute name
                    if (IsTagChar(text[idx]))
                    {
                        while (idx < text.Length && IsTagChar(text[idx]))
                        {
                            colors[idx] = COL_ATTR;
                            idx++;
                        }
                        continue;
                    }

                    idx++;
                }

                // Color the closing >
                if (idx < text.Length && text[idx] == '>')
                {
                    colors[idx] = COL_SYMBOL;
                    idx++;
                }
                continue;
            }

            idx++;
        }

        // Convert colors array to the dictionary format Godot expects
        Color currentColor = COL_BASE;
        for (int i = 0; i < text.Length; i++)
        {
            if (colors[i] != currentColor)
            {
                var colorDict = new Godot.Collections.Dictionary
                {
                    { "color", colors[i] }
                };
                result[i] = colorDict;
                currentColor = colors[i];
            }
        }

        return result;
    }

    private static bool IsTagChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == ':' || c == '.';
    }
}
