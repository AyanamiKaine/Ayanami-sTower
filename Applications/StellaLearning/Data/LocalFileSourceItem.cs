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
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AyanamisTower.StellaLearning.Data; // Use your appropriate namespace

/// <summary>
/// Represents a literature source that is primarily associated with a local file.
/// </summary>
public partial class LocalFileSourceItem : LiteratureSourceItem
{
    /// <summary>
    /// Gets or sets the full file path to the associated local file (e.g., PDF).
    /// This field is non-nullable for this source type.
    /// </summary>
    [ObservableProperty]
    [JsonPropertyName("FilePath")]
    private string _filePath = string.Empty; // Non-nullable

    // --- Constructor ---


    /// <summary>
    /// Parameterless constructor REQUIRED for JSON deserialization
    /// when not using a fully bindable parameterized constructor.
    /// </summary>
    [JsonConstructor] // Explicitly mark, though often implicit if public parameterless exists
    public LocalFileSourceItem()
        : base(LiteratureSourceType.LocalFile) // Set default type
    {
        // Initialize required fields/properties to safe defaults if needed.
        _filePath = string.Empty; // Or some other indicator of "not set yet" if required
        // Base properties like Name, Uid etc. will be set by deserializer via setters
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileSourceItem"/> class.
    /// This constructor is intended for use by the JSON deserializer.
    /// </summary>
    /// <param name="filePath">The full path to the local file. Must match the 'FilePath' property in JSON.</param>
    /// <param name="type">The type of literature source. Defaults to LocalFile if not present in JSON for base class.</param>
    public LocalFileSourceItem(
        string filePath,
        LiteratureSourceType type = LiteratureSourceType.LocalFile
    )
        : base(type)
    {
        // It's generally better to perform validation *after* deserialization if possible,
        // or make the property setter validate. File.Exists check here might fail
        // if the file path was valid when saved but isn't accessible now.
        // Consider if constructor validation is strictly needed *during* deserialization.
        // For now, keeping basic null check:
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));

        _filePath = filePath; // Assign directly to the backing field

        // Check if Name was already deserialized from JSON, otherwise set default
        if (string.IsNullOrWhiteSpace(Name) || Name == "Unnamed Source") // Check against base default
        {
            try
            {
                Name = Path.GetFileNameWithoutExtension(filePath); // Default name from file
            }
            catch (ArgumentException)
            {
                Name = "Invalid File Path"; // Handle cases where path is invalid for GetFileName...
            }
        }

        // Optional: Post-deserialization validation or warning for File.Exists
        if (!File.Exists(filePath))
        {
            Console.WriteLine(
                $"Warning: File specified for deserialized LocalFileSourceItem does not exist or is inaccessible: {filePath}"
            );
        }
    }

    /// <summary>
    /// Helper to build the common parts of an APA citation. Can be called by derived classes.
    /// </summary>
    protected virtual void BuildApaStyleCitationBase(StringBuilder sb)
    {
        // Author(s)
        if (!string.IsNullOrWhiteSpace(Author))
        {
            sb.Append(Author.Trim().TrimEnd('.').TrimEnd(',')).Append(". ");
        }

        // Title (Assume Title Case for Books/Reports, needs adjustment for articles if inheriting)
        if (!string.IsNullOrWhiteSpace(Title))
        {
            string formattedTitle = Title.Trim().TrimEnd('.');
            // Italicize book/report titles in APA
            if (
                SourceType == LiteratureSourceType.Book
                || SourceType == LiteratureSourceType.Report
                || SourceType == LiteratureSourceType.Thesis
            )
            {
                sb.Append('*').Append(formattedTitle).Append("*");
            }
            else
            {
                sb.Append(formattedTitle); // Non-italicized for generic local file? Adjust as needed.
            }
            sb.Append(". ");
        }

        // Publisher (Common for Books/Reports)
        if (!string.IsNullOrWhiteSpace(Publisher))
        {
            sb.Append(Publisher.Trim().TrimEnd('.')).Append(". ");
        }

        // DOI (Less common for local files unless it's an article PDF)
        if (!string.IsNullOrWhiteSpace(Doi))
        {
            string formattedDoi = Doi.Trim();
            if (!formattedDoi.StartsWith("http"))
            {
                sb.Append($"https://doi.org/{formattedDoi}");
            }
            else
            {
                sb.Append(formattedDoi);
            }
        }
        // No URL or AccessedDate typically for local files unless it's a web download backup.
    }
}
