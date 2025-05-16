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
namespace AyanamisTower.StellaLearning.Util.NoteHandler;

/// <summary>
/// Represents a local obsidian file, it does not
/// store the contents of the file only its path
/// and vault location.
/// </summary>
public class ObsidianNote
{
    ObsidianNoteProperties Properties { get; } = new();

    /// <summary>
    /// Absolute file path to the obsidian note
    /// </summary>
    string AbsoluteNoteFilePath { get; } = string.Empty;

    /// <summary>
    /// Absolute path to the obsidian vault
    /// </summary>
    string AbsoluteVaultPath { get; } = string.Empty;
}
