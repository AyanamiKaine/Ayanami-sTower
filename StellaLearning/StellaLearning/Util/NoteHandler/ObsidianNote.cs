namespace StellaLearning.Util.NoteHandler;

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