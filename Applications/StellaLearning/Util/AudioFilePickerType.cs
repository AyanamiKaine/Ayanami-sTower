/*
<one line to give the program's name and a brief idea of what it does.>
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
    public static readonly FilePickerFileType AudioFileType =
        new("Audio files") { Patterns = ["*.mp3", "*.wav", "*.ogg", "*.flac", "*.aac", "*.m4a"] };

    /// <summary>
    /// Provides a set of video file extensions.
    /// </summary>
    public static readonly FilePickerFileType VideoFileType =
        new("Video files")
        {
            Patterns =
            [
                "*.mp4",
                "*.mkv",
                "*.avi",
                "*.mov",
                "*.wmv",
                "*.flv",
                "*.webm",
                "*.3gp",
                "*.m4v",
                "*.mpeg",
                "*.mpg",
                "*.ogv",
                "*.vob",
                "*.ts",
            ],
        };

    /// <summary>
    /// Provides a set of image file extensions.
    /// </summary>
    public static readonly FilePickerFileType ImageFileType =
        new("Image files")
        {
            Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.tiff", "*.ico", "*.webp"],
        };
}
