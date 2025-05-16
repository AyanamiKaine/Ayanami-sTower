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
using System.IO;
using System.Linq;
using AyanamisTower.StellaLearning.Data;

namespace AyanamisTower.StellaLearning.Util.NoteHandler;

/// <summary>
/// Handles obsidian defined notes
/// </summary>
public static class ObsidianHandler
{
    /// <summary>
    /// Parses all .md files found at the defined root, after that
    /// it will decent down all folders to search for more .md files.
    /// Optional allows for the addition of .pdf files.
    /// </summary>
    /// <param name="pathToNoteRoot"></param>
    /// <param name="scanForPdfs"></param>
    /// <returns></returns>
    public static List<LocalFileSourceItem> ParseVault(
        string pathToNoteRoot,
        bool scanForPdfs = false
    )
    {
        if (!Directory.Exists(pathToNoteRoot))
        {
            throw new DirectoryNotFoundException(
                $"The specified vault path does not exist or is not accessible: {pathToNoteRoot}"
            );
        }

        var foundItems = new List<LocalFileSourceItem>();
        var targetExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".md" };

        if (scanForPdfs)
        {
            targetExtensions.Add(".pdf");
        }
        try
        {
            // Use EnumerateFiles for better performance with large directories
            foreach (
                string filePath in Directory.EnumerateFiles(
                    pathToNoteRoot,
                    "*.*",
                    SearchOption.AllDirectories
                )
            )
            {
                string fileExtension = Path.GetExtension(filePath);

                // Check if the file extension is one we care about
                if (targetExtensions.Contains(fileExtension))
                {
                    try
                    {
                        // Create the basic item using the constructor that sets FilePath and default Name
                        var item = new LocalFileSourceItem(filePath);

                        try
                        {
                            item.Title = Path.GetFileNameWithoutExtension(filePath);
                        }
                        catch (ArgumentException argEx)
                        {
                            // Handle cases where the path might be invalid for GetFileNameWithoutExtension
                            Console.Error.WriteLine(
                                $"Warning: Could not extract filename for Title from path '{filePath}': {argEx.Message}"
                            );
                            item.Title = "Invalid File Path"; // Set a fallback title
                        }

                        // If it's a Markdown file, try to parse front matter
                        if (fileExtension.Equals(".md", StringComparison.OrdinalIgnoreCase))
                        {
                            string fileContent = File.ReadAllText(filePath);
                            ObsidianNoteProperties properties = ObsidianNoteProperties.Parse(
                                fileContent
                            );

                            // Check if parsing yielded any data (Created is often mandatory)
                            // Use specific checks based on what Parse returns on failure (new() vs null)
                            // Since Parse returns new() on failure, check for non-default values.
                            bool parsedSuccessfully =
                                properties.Created != default
                                || (properties.Aliases?.Count > 0)
                                || (properties.Tags?.Count > 0);

                            if (parsedSuccessfully) // Or check if properties != null if Parse returned null on failure
                            {
                                /*
                                if (!string.IsNullOrWhiteSpace(properties.Title))
                                {
                                    // The title should be the file name
                                    item.Title = properties.Title;
                                }
                                */
                                // Use Year from Created date if 'Year' property exists (assuming int)
                                if (properties.Created != default)
                                {
                                    try
                                    {
                                        item.PublicationYear = properties.Created.Year;
                                    }
                                    catch (Exception ex) // Catch potential errors if Year property exists but assignment fails
                                    {
                                        Console.Error.WriteLine(
                                            $"Error assigning Year from Created date for {filePath}: {ex.Message}"
                                        );
                                    }
                                }

                                if (properties.Tags?.Count > 0)
                                {
                                    try
                                    {
                                        item.Tags = [.. properties.Tags]; //
                                        //Console.WriteLine($"Note '{item.Name}' Tags: {string.Join(", ", properties.Tags)} (No direct Keywords field to map?)");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.Error.WriteLine(
                                            $"Error assigning Tags to Keywords for {filePath}: {ex.Message}"
                                        );
                                    }
                                }

                                if (properties.Aliases?.Count > 0)
                                {
                                    item.Aliases = [.. properties.Aliases];

                                    //Console.WriteLine($"Note '{item.Name}' Aliases: {string.Join(", ", properties.Aliases)} (No field to map?)");
                                }
                            }
                        }

                        foundItems.Add(item);
                    }
                    catch (IOException ex)
                    {
                        Console.Error.WriteLine(
                            $"Error reading or processing file {filePath}: {ex.Message}"
                        );
                        // Decide whether to skip the file or rethrow
                    }
                    catch (Exception ex) // Catch other unexpected errors for a specific file
                    {
                        Console.Error.WriteLine(
                            $"An unexpected error occurred processing file {filePath}: {ex.Message}"
                        );
                        // Decide whether to skip the file
                    }
                }
                // else: File extension not targeted, skip
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine(
                $"Permission denied accessing path {pathToNoteRoot} or its subdirectories: {ex.Message}"
            );
            // Optionally rethrow or return partial list
        }
        catch (Exception ex) // Catch broader errors during enumeration
        {
            Console.Error.WriteLine($"An error occurred during vault parsing: {ex.Message}");
            // Optionally rethrow
        }
        return foundItems;
    }

    /// <summary>
    /// Recursively scans a directory for Markdown (.md) files and returns the paths
    /// of those files that are not present in the provided list of existing file paths.
    /// </summary>
    /// <param name="pathToNoteRoot">The root directory to scan.</param>
    /// <param name="existingFilePaths">
    /// A collection of file paths that are already known/processed.
    /// These paths are expected to be absolute or resolvable to absolute paths
    /// that can be directly compared with the absolute paths generated during the scan.
    /// If these paths originate from `LocalFileSourceItem.FilePath` (populated by `ParseVault`),
    /// they will typically be absolute.
    /// </param>
    /// <returns>A list of full file paths for new Markdown files found.</returns>
    /// <exception cref="ArgumentNullException">If pathToNoteRoot is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">If pathToNoteRoot does not exist.</exception>
    public static List<string> GetNewMarkdownFilePaths(
        string pathToNoteRoot,
        IEnumerable<string> existingFilePaths
    )
    {
        // 1. Input Validation
        if (string.IsNullOrWhiteSpace(pathToNoteRoot))
        {
            throw new ArgumentNullException(nameof(pathToNoteRoot), "Path to note root cannot be null or empty.");
        }
        if (!Directory.Exists(pathToNoteRoot))
        {
            throw new DirectoryNotFoundException(
                $"The specified directory does not exist or is not accessible: {pathToNoteRoot}"
            );
        }

        var newMarkdownFiles = new List<string>();

        // 2. Prepare existing file paths for efficient lookup
        // Normalize paths to full paths and use a case-insensitive comparer.
        // This assumes that paths in existingFilePaths are either absolute
        // or can be correctly resolved to absolute paths using Path.GetFullPath.
        // If they are relative paths, Path.GetFullPath resolves them against the current working directory.
        // Since Directory.EnumerateFiles returns absolute paths, consistency is key.
        // Paths from LocalFileSourceItem.FilePath (if populated by ParseVault) should be absolute.
        var existingPathsSet = new HashSet<string>(
            (existingFilePaths ?? [])                         // Handle null input gracefully
                .Where(p => !string.IsNullOrWhiteSpace(p))    // Filter out null or empty paths
                .Select(Path.GetFullPath)                     // Convert to full, canonical path
                .Distinct(),                                  // Ensure uniqueness (optional, HashSet does this)
            StringComparer.OrdinalIgnoreCase                  // Use case-insensitive comparison for paths
        );

        try
        {
            // 3. File Scanning for .md files
            // Directory.EnumerateFiles returns full paths by default.
            foreach (string filePath in Directory.EnumerateFiles(
                         pathToNoteRoot,
                         "*.md", // Filter specifically for Markdown files
                         SearchOption.AllDirectories // Recursively search subdirectories
                     ))
            {
                // Normalize the found path for consistent comparison.
                // Path.GetFullPath also handles canonicalization (e.g. C:\folder\.\file.md -> C:\folder\file.md)
                string normalizedFoundPath = Path.GetFullPath(filePath);

                // 4. Filtering: Check if the normalized path is NOT in the existing set
                if (!existingPathsSet.Contains(normalizedFoundPath))
                {
                    newMarkdownFiles.Add(normalizedFoundPath); // Add to results if it's a new file
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            // Log or handle the exception as appropriate for your application
            Console.Error.WriteLine(
                $"Permission denied while scanning directory {pathToNoteRoot} or its subdirectories: {ex.Message}"
            );
            // Depending on requirements, you might rethrow, return partial results, or an empty list.
            // For this example, we'll return any files found before the error.
        }
        catch (Exception ex) // Catch other potential errors during file enumeration
        {
            Console.Error.WriteLine($"An unexpected error occurred during file scanning in {pathToNoteRoot}: {ex.Message}");
            // Handle accordingly; perhaps rethrow or log.
        }

        return newMarkdownFiles;
    }

    /// <summary>
    /// Scans a directory for new Markdown (.md) files not present in the existing collection of items,
    /// creates LocalFileSourceItem objects for them, and attempts to parse their metadata.
    /// </summary>
    /// <param name="pathToNoteRoot">The root directory to scan for Markdown files.</param>
    /// <param name="currentKnownItems">An enumerable collection of LocalFileSourceItem objects already known to the application.</param>
    /// <returns>A list of new LocalFileSourceItem objects for newly discovered Markdown files. Returns an empty list if no new files are found or an error occurs.</returns>
    /// <exception cref="ArgumentNullException">If pathToNoteRoot is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">If pathToNoteRoot does not exist.</exception>
    public static List<LocalFileSourceItem> GetAndParseNewMarkdownFiles(
        string pathToNoteRoot,
        IEnumerable<LocalFileSourceItem> currentKnownItems
    )
    {
        // 1. Validate pathToNoteRoot (GetNewMarkdownFilePaths also does this, but defense in depth)
        if (string.IsNullOrWhiteSpace(pathToNoteRoot))
        {
            throw new ArgumentNullException(nameof(pathToNoteRoot), "Path to note root cannot be null or empty.");
        }
        if (!Directory.Exists(pathToNoteRoot))
        {
            throw new DirectoryNotFoundException(
                $"The specified directory does not exist or is not accessible: {pathToNoteRoot}"
            );
        }

        // 2. Extract file paths from the currently known items
        // Ensure paths are full and normalized for reliable comparison.
        var existingFilePaths = (currentKnownItems ?? [])
            .Select(item => item.FilePath) // Assumes FilePath property exists and holds the path
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(Path.GetFullPath) // Normalize for comparison
            .ToList();

        // 3. Get the list of file paths for new Markdown files
        // This leverages the previously defined method.
        List<string> newMarkdownFilePaths;
        try
        {
            newMarkdownFilePaths = GetNewMarkdownFilePaths(pathToNoteRoot, existingFilePaths);
        }
        catch (Exception ex) // Catch exceptions from GetNewMarkdownFilePaths (e.g., DirectoryNotFound, UnauthorizedAccess)
        {
            Console.Error.WriteLine($"Error obtaining new markdown file paths from '{pathToNoteRoot}': {ex.Message}");
            return []; // Return empty list on error during path discovery
        }


        var newItems = new List<LocalFileSourceItem>();

        // 4. Process each new file path to create and populate LocalFileSourceItem
        foreach (string newFilePath in newMarkdownFilePaths)
        {
            try
            {
                // Create the basic item. The constructor LocalFileSourceItem(filePath)
                // typically sets the FilePath and might set a default Name (e.g., from filename).
                var item = new LocalFileSourceItem(newFilePath);

                // Explicitly set the Title, usually to the filename without extension.
                // The Name property might be used for display, Title for metadata.
                try
                {
                    item.Title = Path.GetFileNameWithoutExtension(newFilePath);
                }
                catch (ArgumentException argEx)
                {
                    Console.Error.WriteLine(
                        $"Warning: Could not extract filename for Title from path '{newFilePath}': {argEx.Message}"
                    );
                    item.Title = "Invalid File Path"; // Fallback title
                }

                // Attempt to parse Markdown front matter, similar to the ParseVault method.
                // This section assumes the existence of an ObsidianNoteProperties class and its Parse method.
                // If this parsing logic is not required or the class is unavailable, this can be removed or kept commented.

                string fileContent = File.ReadAllText(newFilePath);
                ObsidianNoteProperties properties = ObsidianNoteProperties.Parse(fileContent);

                // Check if parsing was successful based on your criteria
                bool parsedSuccessfully = properties.Created != default ||
                                          (properties.Aliases?.Count > 0) ||
                                          (properties.Tags?.Count > 0);

                if (parsedSuccessfully)
                {
                    if (properties.Created != default)
                    {
                        try
                        {
                            item.PublicationYear = properties.Created.Year;
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Error assigning PublicationYear for {newFilePath}: {ex.Message}");
                        }
                    }
                    if (properties.Tags?.Count > 0)
                    {
                        try
                        {
                            item.Tags = [.. properties.Tags];
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Error assigning Tags for {newFilePath}: {ex.Message}");
                        }
                    }
                    if (properties.Aliases?.Count > 0)
                    {
                        try
                        {
                            item.Aliases = [.. properties.Aliases];
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Error assigning Aliases for {newFilePath}: {ex.Message}");
                        }
                    }
                }

                newItems.Add(item);
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"IO error processing new file {newFilePath}: {ex.Message}");
            }
            catch (Exception ex) // Catch other unexpected errors for a specific new file
            {
                Console.Error.WriteLine($"An unexpected error occurred processing new file {newFilePath}: {ex.Message}");
            }
        }
        return newItems;
    }
}
