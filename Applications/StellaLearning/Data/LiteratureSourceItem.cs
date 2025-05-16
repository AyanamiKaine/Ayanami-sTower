/*
Stella Learning is a modern learning app.
Copyright (C) <2025>  <Patrick, Grohs>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel; // For ObservableObject

namespace AyanamisTower.StellaLearning.Data;

/// <summary>
/// Represents different types of literature sources that can be managed in the application.
/// </summary>
public enum LiteratureSourceType
{
    /// <summary>
    /// Represents a book literature source.
    /// </summary>
    Book,

    /// <summary>
    /// Represents a journal article literature source.
    /// </summary>
    JournalArticle,

    /// <summary>
    /// Represents a conference paper literature source.
    /// </summary>
    ConferencePaper,

    /// <summary>
    /// Represents a website literature source.
    /// </summary>
    Website,

    /// <summary>
    /// Represents a report literature source.
    /// </summary>
    Report,

    /// <summary>
    /// Represents a thesis literature source.
    /// </summary>
    Thesis,

    /// <summary>
    /// Represents a local file literature source.
    /// </summary>
    LocalFile, // Added generic local file type

    /// <summary>
    /// Represents a literature source that doesn't fit into any of the predefined categories.
    /// </summary>
    Other,
}

/// <summary>
/// Represents different academic citation styles used for formatting references.
/// </summary>
public enum CitationStyle
{
    /// <summary>
    /// American Psychological Association citation style.
    /// </summary>
    APA,

    /// <summary>
    /// Modern Language Association citation style.
    /// </summary>
    MLA,

    /// <summary>
    /// Chicago Manual of Style citation format.
    /// </summary>
    Chicago,

    /// <summary>
    /// Institute of Electrical and Electronics Engineers citation style.
    /// </summary>
    IEEE,
}

/// <summary>
/// base class representing a literature source item.
/// Contains common properties shared by all source types.
/// </summary>
public partial class LiteratureSourceItem : ObservableObject
{
    // --- Core Common Properties ---

    /// <summary>
    /// The unique identifier of the item
    /// </summary>
    public Guid Uid { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string _name = "Unnamed Source";

    [ObservableProperty]
    private List<string> _tags = [];

    [ObservableProperty]
    private List<string> _aliases = [];

    // SourceType is still useful for filtering or UI switching, set by derived classes.
    [ObservableProperty]
    private LiteratureSourceType? _sourceType;

    // --- Common Metadata Properties ---

    [ObservableProperty]
    private string _author = "";

    [ObservableProperty]
    private string _summary = "";

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private int _publicationYear = 0;

    [ObservableProperty]
    private string? _notes;

    // --- Specific Metadata (Nullable here, made non-nullable in derived classes where appropriate) ---
    // These might still be useful as nullable in the base if some derived types might optionally have them.
    // Alternatively, they can be moved entirely to the derived classes that *always* have them.
    // Let's keep some common ones here as nullable for flexibility.

    [ObservableProperty]
    private string? _publisher; // Common for Books, Reports

    [ObservableProperty]
    private string? _doi; // Common for Articles

    [ObservableProperty]
    private string? _isbn; // Common for Books

    [ObservableProperty]
    private int _pageCount;

    // --- Constructor ---
    // Protected constructor ensures it can only be called by derived classes.
    /// <summary>
    /// Initializes a new instance of the <see cref="LiteratureSourceItem"/> class with the specified source type.
    /// </summary>
    /// <param name="type">The type of literature source.</param>
    [JsonConstructor]
    public LiteratureSourceItem(LiteratureSourceType type)
    {
        _sourceType = type;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LiteratureSourceItem"/> class with default values.
    /// </summary>
    public LiteratureSourceItem() { }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Name} ({SourceType})";
    }
}
