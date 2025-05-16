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
using YamlDotNet.Serialization;

namespace AyanamisTower.StellaLearning.Util.NoteHandler;

/// <summary>
/// Represents the properties found in an obsidian note
/// </summary>
public class ObsidianNoteProperties
{
    /// <summary>
    /// When the obsidian note was created
    /// </summary>
    [YamlMember(Alias = "Created")]
    public DateTime Created { get; set; } // Made setter public for deserialization

    /// <summary>
    /// Defines obsidian aliases
    /// </summary>
    [YamlMember(Alias = "aliases")]
    public List<string> Aliases { get; set; } // Made setter public for deserialization

    /// <summary>
    /// Defined obsidian tags
    /// </summary>
    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; set; } // Made setter public for deserialization

    /// <summary>
    /// Parameterless constructor needed for YamlDotNet deserialization.
    /// </summary>
    public ObsidianNoteProperties()
    {
        Aliases = [];
        Tags = [];
    }

    /// <summary>
    /// Created
    /// </summary>
    /// <param name="created"></param>
    public ObsidianNoteProperties(DateTime created)
        : this()
    {
        Created = created;
    }

    /// <summary>
    /// Parsers the properties of an obsidian note
    /// </summary>
    public static ObsidianNoteProperties Parse(string obsidianNote)
    {
        {
            // Trim leading/trailing whitespace from the whole content first
            var trimmedContent = obsidianNote.Trim();

            // Check if it starts with the front matter marker
            if (!trimmedContent.StartsWith("---"))
            {
                return new(); // No front matter detected
            }

            var endMarkerIndex = trimmedContent.IndexOf("---", 3);
            if (endMarkerIndex == -1)
            {
                return new();
            }

            var startOfYaml = trimmedContent.IndexOf('\n', 3);
            if (startOfYaml == -1 || startOfYaml >= endMarkerIndex)
            {
                startOfYaml = 3;
            }
            else
            {
                startOfYaml++;
            }

            if (startOfYaml >= endMarkerIndex)
            {
                return new(); // No actual content between markers
            }

            var yamlContent = trimmedContent
                .Substring(startOfYaml, endMarkerIndex - startOfYaml)
                .Trim();

            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                return new();
            }

            var deserializer = new DeserializerBuilder().Build();

            try
            {
                // Deserialize the YAML content into our object
                return deserializer.Deserialize<ObsidianNoteProperties>(yamlContent);
            }
            catch (YamlDotNet.Core.YamlException ex)
            {
                // Handle potential YAML parsing errors
                Console.Error.WriteLine($"Error parsing YAML front matter: {ex.Message}");
                return new();
            }
            catch (Exception ex) // Catch other potential exceptions during deserialization
            {
                Console.Error.WriteLine(
                    $"An unexpected error occurred during deserialization: {ex.Message}"
                );
                return new();
            }
        }
    }
}
