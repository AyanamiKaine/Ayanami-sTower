using System;
using System.Collections.Generic;
using System.Linq;

namespace StellaLang;

/// <summary>
/// A FORTH-style dictionary implementation that maintains insertion order and supports
/// case-insensitive lookups, redefinition, and the FORGET operation.
/// 
/// Features:
/// - Case-insensitive word lookups (FORTH standard behavior)
/// - Maintains definition order via linked list
/// - Supports word redefinition (newer definition shadows older)
/// - FORGET operation to remove words and all words defined after them
/// - Iteration in definition order
/// </summary>
public class ForthDictionary
{
    /// <summary>
    /// Linked list maintaining words in definition order (oldest to newest).
    /// </summary>
    private readonly LinkedList<WordDefinition> _words = new();

    /// <summary>
    /// Index for fast lookup by name. Maps normalized names to the LAST (most recent)
    /// definition of that word in the linked list.
    /// </summary>
    private readonly Dictionary<string, LinkedListNode<WordDefinition>> _index =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the total number of words in the dictionary.
    /// Note: This counts all definitions, including shadowed ones.
    /// </summary>
    public int Count => _words.Count;

    /// <summary>
    /// Gets the number of unique words (not counting redefinitions).
    /// </summary>
    public int UniqueWordCount => _index.Count;

    /// <summary>
    /// Adds a new word definition to the dictionary.
    /// If a word with the same name already exists, the new definition shadows the old one.
    /// </summary>
    /// <param name="word">The word definition to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if word or word.Name is null.</exception>
    public void Add(WordDefinition word)
    {
        if (word == null)
            throw new ArgumentNullException(nameof(word));
        if (string.IsNullOrWhiteSpace(word.Name))
            throw new ArgumentException("Word name cannot be null or whitespace", nameof(word));

        // Add to the end of the linked list (newest definition)
        var node = _words.AddLast(word);

        // Update index to point to this newest definition
        // This automatically shadows any previous definition
        _index[word.Name] = node;
    }

    /// <summary>
    /// Finds a word by name (case-insensitive).
    /// Returns the most recent definition if multiple exist.
    /// </summary>
    /// <param name="name">The name of the word to find.</param>
    /// <returns>The word definition if found, null otherwise.</returns>
    public WordDefinition? Find(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return _index.TryGetValue(name, out var node) ? node.Value : null;
    }

    /// <summary>
    /// Checks if a word exists in the dictionary (case-insensitive).
    /// </summary>
    /// <param name="name">The name of the word to check.</param>
    /// <returns>True if the word exists, false otherwise.</returns>
    public bool Contains(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && _index.ContainsKey(name);
    }

    /// <summary>
    /// FORGET operation: Removes a word and all words defined after it.
    /// This is a traditional FORTH operation for managing dictionary space.
    /// </summary>
    /// <param name="name">The name of the word to forget.</param>
    /// <returns>The number of words removed (0 if word not found).</returns>
    public int Forget(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !_index.TryGetValue(name, out var targetNode))
            return 0;

        int removedCount = 0;
        var currentNode = targetNode;

        // Remove all nodes from the target word to the end
        while (currentNode != null)
        {
            var nodeToRemove = currentNode;
            currentNode = currentNode.Next;

            // Remove from index if this is the indexed definition
            if (_index.TryGetValue(nodeToRemove.Value.Name, out var indexedNode) &&
                indexedNode == nodeToRemove)
            {
                _index.Remove(nodeToRemove.Value.Name);

                // Check if there's an earlier definition to restore to the index
                var previousNode = nodeToRemove.Previous;
                while (previousNode != null)
                {
                    if (previousNode.Value.Name.Equals(nodeToRemove.Value.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        _index[nodeToRemove.Value.Name] = previousNode;
                        break;
                    }
                    previousNode = previousNode.Previous;
                }
            }

            _words.Remove(nodeToRemove);
            removedCount++;
        }

        return removedCount;
    }

    /// <summary>
    /// Removes a specific word definition.
    /// Only removes the most recent definition; earlier definitions remain if they exist.
    /// </summary>
    /// <param name="name">The name of the word to remove.</param>
    /// <returns>True if the word was found and removed, false otherwise.</returns>
    public bool Remove(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !_index.TryGetValue(name, out var node))
            return false;

        // Remove from list
        _words.Remove(node);

        // Remove from index
        _index.Remove(name);

        // Search backwards for an earlier definition to restore to the index
        var previousNode = node.Previous;
        while (previousNode != null)
        {
            if (previousNode.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                _index[name] = previousNode;
                break;
            }
            previousNode = previousNode.Previous;
        }

        return true;
    }

    /// <summary>
    /// Clears all words from the dictionary.
    /// </summary>
    public void Clear()
    {
        _words.Clear();
        _index.Clear();
    }

    /// <summary>
    /// Gets all words in definition order (oldest to newest).
    /// </summary>
    /// <returns>Enumerable of all word definitions in order.</returns>
    public IEnumerable<WordDefinition> GetAllWords()
    {
        return _words;
    }

    /// <summary>
    /// Gets only the visible (non-shadowed) words.
    /// </summary>
    /// <returns>Enumerable of visible word definitions.</returns>
    public IEnumerable<WordDefinition> GetVisibleWords()
    {
        return _index.Values.Select(node => node.Value);
    }

    /// <summary>
    /// Gets all definitions of a word, from oldest to newest.
    /// Useful for debugging redefinition chains.
    /// </summary>
    /// <param name="name">The name of the word.</param>
    /// <returns>Enumerable of all definitions with matching name.</returns>
    public IEnumerable<WordDefinition> GetAllDefinitions(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Enumerable.Empty<WordDefinition>();

        return _words.Where(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets diagnostic information about the dictionary state.
    /// </summary>
    /// <returns>String representation of dictionary statistics.</returns>
    public override string ToString()
    {
        int totalWords = Count;
        int uniqueWords = UniqueWordCount;
        int shadowedWords = totalWords - uniqueWords;

        return $"ForthDictionary [Total={totalWords}, Unique={uniqueWords}, Shadowed={shadowedWords}]";
    }

    /// <summary>
    /// Creates a detailed dump of the dictionary for debugging.
    /// </summary>
    /// <param name="includeTypes">Whether to include word type information.</param>
    /// <returns>Multi-line string with dictionary contents.</returns>
    public string DumpDictionary(bool includeTypes = false)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== FORTH Dictionary ===");
        sb.AppendLine(ToString());
        sb.AppendLine();

        if (_words.Count == 0)
        {
            sb.AppendLine("(empty)");
            return sb.ToString();
        }

        sb.AppendLine("Visible Words (newest definitions):");
        var visibleWords = GetVisibleWords().OrderBy(w => w.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var word in visibleWords)
        {
            if (includeTypes)
            {
                string typeInfo = word.Type.ToString();
                if (word.IsImmediate)
                    typeInfo += " [IMMEDIATE]";
                sb.AppendLine($"  {word.Name,-20} {typeInfo}");
            }
            else
            {
                sb.AppendLine($"  {word.Name}{(word.IsImmediate ? " IMMEDIATE" : "")}");
            }
        }

        // Check for shadowed definitions
        int shadowedCount = Count - UniqueWordCount;
        if (shadowedCount > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"({shadowedCount} shadowed definition(s) hidden)");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Lists all words in definition order (for WORDS command).
    /// </summary>
    /// <param name="maxPerLine">Maximum words per line (0 for no limit).</param>
    /// <returns>Formatted string of word names.</returns>
    public string ListWords(int maxPerLine = 5)
    {
        var sb = new System.Text.StringBuilder();
        var visibleWords = GetVisibleWords()
            .OrderBy(w => w.Name, StringComparer.OrdinalIgnoreCase)
            .Select(w => w.Name)
            .ToList();

        if (visibleWords.Count == 0)
            return "(no words defined)";

        if (maxPerLine <= 0)
        {
            // Single line
            sb.Append(string.Join(" ", visibleWords));
        }
        else
        {
            // Multiple lines
            for (int i = 0; i < visibleWords.Count; i++)
            {
                if (i > 0 && i % maxPerLine == 0)
                    sb.AppendLine();
                else if (i > 0)
                    sb.Append(" ");

                sb.Append(visibleWords[i]);
            }
        }

        return sb.ToString();
    }
}
