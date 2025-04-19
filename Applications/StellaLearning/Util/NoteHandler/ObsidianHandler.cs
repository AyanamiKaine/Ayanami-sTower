using System;
using System.Collections.Generic;
using System.IO;
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
    public static List<LocalFileSourceItem> ParseVault(string pathToNoteRoot, bool scanForPdfs = false)
    {
        if (!Directory.Exists(pathToNoteRoot))
        {
            throw new DirectoryNotFoundException($"The specified vault path does not exist or is not accessible: {pathToNoteRoot}");
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
            foreach (string filePath in Directory.EnumerateFiles(pathToNoteRoot, "*.*", SearchOption.AllDirectories))
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
                            Console.Error.WriteLine($"Warning: Could not extract filename for Title from path '{filePath}': {argEx.Message}");
                            item.Title = "Invalid File Path"; // Set a fallback title
                        }

                        // If it's a Markdown file, try to parse front matter
                        if (fileExtension.Equals(".md", StringComparison.OrdinalIgnoreCase))
                        {
                            string fileContent = File.ReadAllText(filePath);
                            ObsidianNoteProperties properties = ObsidianNoteProperties.Parse(fileContent);

                            // Check if parsing yielded any data (Created is often mandatory)
                            // Use specific checks based on what Parse returns on failure (new() vs null)
                            // Since Parse returns new() on failure, check for non-default values.
                            bool parsedSuccessfully = properties.Created != default(DateTime) ||
                                                     (properties.Aliases?.Count > 0) ||
                                                     (properties.Tags?.Count > 0);


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
                                        Console.Error.WriteLine($"Error assigning Year from Created date for {filePath}: {ex.Message}");
                                    }
                                }

                                if (properties.Tags?.Count > 0)
                                {
                                    try
                                    {
                                        item.Tags = [.. properties.Tags]; // 
                                        Console.WriteLine($"Note '{item.Name}' Tags: {string.Join(", ", properties.Tags)} (No direct Keywords field to map?)");

                                    }
                                    catch (Exception ex)
                                    {
                                        Console.Error.WriteLine($"Error assigning Tags to Keywords for {filePath}: {ex.Message}");
                                    }
                                }

                                if (properties.Aliases?.Count > 0)
                                {
                                    item.Aliases = [.. properties.Aliases];

                                    Console.WriteLine($"Note '{item.Name}' Aliases: {string.Join(", ", properties.Aliases)} (No field to map?)");
                                }
                            }
                        }

                        foundItems.Add(item);
                    }
                    catch (IOException ex)
                    {
                        Console.Error.WriteLine($"Error reading or processing file {filePath}: {ex.Message}");
                        // Decide whether to skip the file or rethrow
                    }
                    catch (Exception ex) // Catch other unexpected errors for a specific file
                    {
                        Console.Error.WriteLine($"An unexpected error occurred processing file {filePath}: {ex.Message}");
                        // Decide whether to skip the file
                    }
                }
                // else: File extension not targeted, skip
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"Permission denied accessing path {pathToNoteRoot} or its subdirectories: {ex.Message}");
            // Optionally rethrow or return partial list
        }
        catch (Exception ex) // Catch broader errors during enumeration
        {
            Console.Error.WriteLine($"An error occurred during vault parsing: {ex.Message}");
            // Optionally rethrow
        }
        return foundItems;
    }
}