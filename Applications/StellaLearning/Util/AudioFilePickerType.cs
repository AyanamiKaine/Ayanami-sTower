
using Avalonia.Platform.Storage;

namespace AyanamisTower.StellaLearning.Util;

/// <summary>
/// Provides a set of custom FilePickerTypes
/// </summary>
public static class CustomFilePickerTypes
{
    /// <summary>
    /// Provides a set of audio file extensions. 
    /// </summary>
    public static readonly FilePickerFileType AudioFileType = new("Audio files")
    {
        Patterns = ["*.mp3", "*.wav", "*.ogg", "*.flac", "*.aac", "*.m4a"],
    };

    /// <summary>
    /// Provides a set of video file extensions.
    /// </summary>
    public static readonly FilePickerFileType VideoFileType = new("Video files")
    {
        Patterns = [
            "*.mp4", "*.mkv", "*.avi", "*.mov", "*.wmv", "*.flv", "*.webm",
            "*.3gp", "*.m4v", "*.mpeg", "*.mpg", "*.ogv", "*.vob", "*.ts"
        ],
    };

    /// <summary>
    /// Provides a set of image file extensions.
    /// </summary>
    public static readonly FilePickerFileType ImageFileType = new("Image files")
    {
        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.tiff", "*.ico", "*.webp"],
    };
}
