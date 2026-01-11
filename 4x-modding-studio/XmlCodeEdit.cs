using Godot;
using System;
using System.Collections.Generic;

public partial class XmlCodeEdit : CodeEdit
{
    // ------------------------------------------------------------------------------
    // CONFIGURATION
    // ------------------------------------------------------------------------------
    private readonly Color COLOR_TAG_OPEN = new("ff7b7b");   // Reddish for tags
    private readonly Color COLOR_ATTRIBUTE = new("ffcc66");  // Orange/Yellow for attributes
    private readonly Color COLOR_STRING = new("a5c261");     // Green for strings
    private readonly Color COLOR_COMMENT = new("6c757d");    // Grey for comments
    private readonly Color COLOR_BASE_TEXT = new("e0e0e0");  // Default text

    // ------------------------------------------------------------------------------
    // XML SCHEMA DEFINITION
    // ------------------------------------------------------------------------------
    // Helper class to strictly type the schema instead of using raw Dictionaries
    private class TagDefinition
    {
        public string[] Attributes { get; }
        public string[] Children { get; }

        public TagDefinition(string[] attributes, string[] children)
        {
            Attributes = attributes;
            Children = children;
        }
    }

    private readonly Dictionary<string, TagDefinition> XML_SCHEMA = new()
    {
        { "mdscript",            new TagDefinition(["name"], ["cue"]) },
        { "cue",                 new TagDefinition(["name", "instantiate"], ["conditions", "actions", "cue"]) },
        { "conditions",          new TagDefinition([], ["event_game_started", "check_value", "event_cue_signalled"]) },
        { "actions",             new TagDefinition([], ["show_help", "set_value", "debug_text", "signal_cue", "write_to_logbook"]) },
        { "event_game_started",  new TagDefinition([], []) },
        { "event_cue_signalled", new TagDefinition(["cue"], []) },
        { "check_value",         new TagDefinition(["name", "exact", "min", "max"], []) },
        { "show_help",           new TagDefinition(["position", "duration", "text"], []) },
        { "set_value",           new TagDefinition(["name", "exact", "operation"], []) },
        { "debug_text",          new TagDefinition(["text", "filter"], []) },
        { "signal_cue",          new TagDefinition(["cue"], []) },
        { "write_to_logbook",    new TagDefinition(["title", "text", "category"], []) }
    };

    // Root level tags
    private readonly string[] ROOT_TAGS = ["mdscript"];

    public override void _Ready()
    {
        SetupEditorSettings();
        SetupHighlighter();

        // Connect to text changed signal using lambda to ensure it fires
        TextChanged += () =>
        {
            string lineText = GetLine(GetCaretLine());
            int col = GetCaretColumn();

            if (col > 0 && col <= lineText.Length)
            {
                char lastChar = lineText[col - 1];
                if (lastChar == '<' || lastChar == ' ')
                {
                    OnCodeCompletionRequested();
                }
            }
        };
    }

    // ------------------------------------------------------------------------------
    // 1. SETUP & VISUALS
    // ------------------------------------------------------------------------------
    private void SetupEditorSettings()
    {
        CodeCompletionEnabled = true;

        GuttersDrawLineNumbers = true;
        GuttersDrawFoldGutter = true;
        AutoBraceCompletionEnabled = true;

        // Don't auto-close < and > as it interferes with autocompletion
        // AddAutoBraceCompletionPair("<", ">");
    }

    private void SetupHighlighter()
    {
        // Use our custom XML syntax highlighter
        var highlighter = new XmlSyntaxHighlighter();
        this.SyntaxHighlighter = highlighter;
    }

    // Handle manual completion with Ctrl+Space
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Space && keyEvent.CtrlPressed)
            {
                OnCodeCompletionRequested();
                GetTree().Root.SetInputAsHandled();
                return;
            }
        }
        base._Input(@event);
    }

    // This is called when code completion is requested
    public override void _RequestCodeCompletion(bool force)
    {
        OnCodeCompletionRequested();
    }

    // ------------------------------------------------------------------------------
    // 2. CODE COMPLETION LOGIC
    // ------------------------------------------------------------------------------
    private void OnCodeCompletionRequested()
    {
        // Get line content up to the caret
        string lineText = GetLine(GetCaretLine());
        int col = GetCaretColumn();

        // Safety bounds check
        if (col > lineText.Length) col = lineText.Length;

        string prefix = lineText[..col];

        int lastOpen = prefix.LastIndexOf('<');
        int lastClose = prefix.LastIndexOf('>');

        // If we are inside a tag (after < but before >)
        if (lastOpen > lastClose && lastOpen != -1)
        {
            string tagContent = prefix[(lastOpen + 1)..];

            // Check if it's a closing tag </
            if (tagContent.StartsWith("/"))
            {
                SuggestClosingTags(tagContent[1..]);
                return;
            }

            // If there is a space, we are typing attributes
            if (tagContent.Contains(" "))
            {
                string tagName = tagContent.Split(' ')[0];
                SuggestAttributesForTag(tagName);
            }
            else
            {
                // We are typing the tag name - find parent context
                string parentTag = FindParentTag();
                SuggestTags(tagContent, parentTag);
            }
        }
        else if (prefix.EndsWith("<"))
        {
            // Just typed <, suggest tags for current parent
            string parentTag = FindParentTag();
            SuggestTags("", parentTag);
        }
    }

    private string FindParentTag()
    {
        // Search backwards through the document to find the current parent tag
        int caretLine = GetCaretLine();

        // Use a Stack to track nesting
        Stack<string> tagStack = new();

        // Iterate lines from 0 to caretLine
        for (int lineIdx = 0; lineIdx <= caretLine; lineIdx++)
        {
            string line = GetLine(lineIdx);
            int colLimit = line.Length;

            // On the active line, stop scanning at the caret
            if (lineIdx == caretLine)
            {
                colLimit = GetCaretColumn();
            }

            int i = 0;
            while (i < colLimit)
            {
                // Find opening bracket
                if (line[i] == '<')
                {
                    // Check for bounds
                    if (i + 1 >= line.Length) { i++; continue; }

                    char nextChar = line[i + 1];

                    // 1. Closing tag </...>
                    if (nextChar == '/')
                    {
                        int end = line.IndexOf('>', i);
                        if (end != -1 && end <= colLimit)
                        {
                            // Extract "name" from "</name>"
                            // i+2 skips "</", length is end - (i+2)
                            string tagName = line[(i + 2)..end].Trim();

                            // Pop from stack if it matches
                            if (tagStack.Count > 0 && tagStack.Peek() == tagName)
                            {
                                tagStack.Pop();
                            }
                            i = end;
                        }
                        else
                        {
                            i++;
                        }
                    }
                    // 2. Opening tag <tag...> (ignoring <! and <?)
                    else if (nextChar != '!' && nextChar != '?')
                    {
                        int end = line.IndexOf('>', i);
                        if (end != -1 && end <= colLimit)
                        {
                            // Extract content between brackets
                            string tagPart = line[(i + 1)..end];

                            // Check self-closing
                            bool isSelfClosing = tagPart.EndsWith("/");
                            if (isSelfClosing)
                            {
                                tagPart = tagPart[..^1];
                            }

                            // Get name (handles attributes by splitting space)
                            string tagName = tagPart.Split(' ')[0].Trim();

                            if (!string.IsNullOrEmpty(tagName) && !isSelfClosing)
                            {
                                tagStack.Push(tagName);
                            }
                            i = end;
                        }
                        else
                        {
                            i++;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }
        }

        return tagStack.Count > 0 ? tagStack.Peek() : "";
    }

    private void SuggestTags(string filterText, string parentTag)
    {
        List<string> availableTags = new();

        if (string.IsNullOrEmpty(parentTag))
        {
            // At root level
            availableTags.AddRange(ROOT_TAGS);
        }
        else if (XML_SCHEMA.TryGetValue(parentTag, out TagDefinition value))
        {
            // Get valid children
            availableTags.AddRange(value.Children);
        }
        else
        {
            // Unknown parent, suggest all
            availableTags.AddRange(XML_SCHEMA.Keys);
        }

        foreach (string tag in availableTags)
        {
            if (string.IsNullOrEmpty(filterText) || tag.StartsWith(filterText) || tag.Contains(filterText))
            {
                string completionText = tag;

                // Build full completion string with attributes if they exist
                if (XML_SCHEMA.TryGetValue(tag, out var schemaDef))
                {
                    if (schemaDef.Attributes.Length > 0)
                    {
                        completionText = tag;
                        foreach (string attr in schemaDef.Attributes)
                        {
                            completionText += $" {attr}=\"\"";
                        }
                    }

                    // Self-closing check
                    if (schemaDef.Children.Length == 0)
                    {
                        completionText += " />";
                    }
                    else
                    {
                        completionText += ">";
                    }
                }

                AddCodeCompletionOption(
                    CodeCompletionKind.Class,
                    tag,
                    completionText,
                    COLOR_TAG_OPEN
                );
            }
        }

        UpdateCodeCompletionOptions(true);
    }

    private void SuggestClosingTags(string filterText)
    {
        string parentTag = FindParentTag();
        if (!string.IsNullOrEmpty(parentTag))
        {
            if (string.IsNullOrEmpty(filterText) || parentTag.StartsWith(filterText))
            {
                AddCodeCompletionOption(
                    CodeCompletionKind.Class,
                    "/" + parentTag,
                    parentTag + ">",
                    COLOR_TAG_OPEN
                );
            }
        }

        UpdateCodeCompletionOptions(true);
    }

    private void SuggestAttributesForTag(string tagName)
    {
        if (!XML_SCHEMA.TryGetValue(tagName, out var schemaDef))
        {
            UpdateCodeCompletionOptions(true);
            return;
        }

        foreach (string attr in schemaDef.Attributes)
        {
            AddCodeCompletionOption(
                CodeCompletionKind.Member,
                attr,
                attr + "=\"\"",
                COLOR_ATTRIBUTE
            );
        }

        UpdateCodeCompletionOptions(true);
    }
}