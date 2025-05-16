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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using AyanamisTower.StellaLearning.Data;

namespace AyanamisTower.StellaLearning.Util.NoteHandler;


/// <summary>
/// Watches an Obsidian vault directory for changes to .md files and updates a
/// managed collection of LocalFileSourceItem objects accordingly.
/// </summary>
/// <summary>
/// Watches an Obsidian vault directory for changes to .md files and updates LocalFileSourceItem instances
/// within a managed collection of LiteratureSourceItem objects accordingly.
/// </summary>
public class ObsidianVaultWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    /// <summary>
    /// Current vaultpath being watched
    /// </summary>
    public string VaultPath { get; }
    // Changed to IList<LiteratureSourceItem> to manage a mixed collection
    private readonly IList<LiteratureSourceItem> _managedItems;
    private readonly Lock _lockObject = new();
    private bool _isDisposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObsidianVaultWatcher"/> class.
    /// </summary>
    /// <param name="vaultPath">The absolute path to the Obsidian vault directory to watch.</param>
    /// <param name="itemsToManage">The collection of LiteratureSourceItem objects to keep synchronized.
    /// This collection will be modified by the watcher. It can be an ObservableCollection&lt;LiteratureSourceItem&gt;.</param>
    /// <exception cref="ArgumentNullException">Thrown if vaultPath or itemsToManage is null.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown if the vaultPath does not exist.</exception>
    public ObsidianVaultWatcher(string vaultPath, IList<LiteratureSourceItem> itemsToManage)
    {
        if (string.IsNullOrWhiteSpace(vaultPath))
            throw new ArgumentNullException(nameof(vaultPath));
        if (!Directory.Exists(vaultPath))
            throw new DirectoryNotFoundException($"The vault path '{vaultPath}' does not exist.");

        VaultPath = Path.GetFullPath(vaultPath);
        _managedItems = itemsToManage ?? throw new ArgumentNullException(nameof(itemsToManage));

        _watcher = new FileSystemWatcher(VaultPath)
        {
            Filter = "*.md",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
        };

        _watcher.Created += OnFileCreated;
        _watcher.Deleted += OnFileDeleted;
        _watcher.Renamed += OnFileRenamed;
        _watcher.Error += OnWatcherError;
    }

    /// <summary>
    /// Starts the obsidian vault watcher it will try its best at syncing the new and removed contents of
    /// the vault with the literature list.
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    public void StartWatching()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Stops the file watcher.
    /// </summary>
    public void StopWatching()
    {
        if (_isDisposed) return;
        _watcher.EnableRaisingEvents = false;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        try
        {
            string fullPath = Path.GetFullPath(e.FullPath);
            Console.WriteLine($"File created: {fullPath}");

            if (!fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) return;

            lock (_lockObject)
            {
                // Check if a LocalFileSourceItem with this path already exists in the mixed collection
                if (_managedItems.OfType<LocalFileSourceItem>().Any(item => Path.GetFullPath(item.FilePath).Equals(fullPath, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine($"LocalFileSourceItem already exists for path (created): {fullPath}. Skipping.");
                    return;
                }

                var newItem = new LocalFileSourceItem(fullPath); // This is a LiteratureSourceItem
                try
                {
                    newItem.Title = Path.GetFileNameWithoutExtension(fullPath);
                }
                catch (ArgumentException argEx)
                {
                    Console.Error.WriteLine($"Warning: Could not extract filename for Title from path '{fullPath}': {argEx.Message}");
                    newItem.Title = "Invalid File Path";
                }

                // Parse front matter for the new file
                try
                {
                    string fileContent = File.ReadAllText(fullPath);
                    ObsidianNoteProperties properties = ObsidianNoteProperties.Parse(fileContent);

                    if (properties.Created != default)
                    {
                        newItem.PublicationYear = properties.Created.Year;
                    }
                    if (properties.Tags?.Count > 0)
                    {
                        newItem.Tags = new List<string>(properties.Tags);
                    }
                    if (properties.Aliases?.Count > 0)
                    {
                        newItem.Aliases = new List<string>(properties.Aliases);
                    }
                }
                catch (IOException ioEx)
                {
                    Console.Error.WriteLine($"IO Error reading file for parsing {fullPath}: {ioEx.Message}");
                }
                catch (Exception parseEx)
                {
                    Console.Error.WriteLine($"Error parsing frontmatter for {fullPath}: {parseEx.Message}");
                }

                _managedItems.Add(newItem); // Add the LocalFileSourceItem to the IList<LiteratureSourceItem>
                Console.WriteLine($"Added new item: {newItem.Title} ({newItem.FilePath})");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing file creation event for {e.FullPath}: {ex.Message}");
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        try
        {
            string fullPath = Path.GetFullPath(e.FullPath);
            Console.WriteLine($"File deleted: {fullPath}");

            if (!fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) return;

            lock (_lockObject)
            {
                // Find the LocalFileSourceItem to remove from the mixed collection
                var itemToRemove = _managedItems.OfType<LocalFileSourceItem>().FirstOrDefault(item =>
                    Path.GetFullPath(item.FilePath).Equals(fullPath, StringComparison.OrdinalIgnoreCase));

                if (itemToRemove != null)
                {
                    _managedItems.Remove(itemToRemove); // Remove the specific item
                    Console.WriteLine($"Removed item: {itemToRemove.Title} ({itemToRemove.FilePath})");
                }
                else
                {
                    Console.WriteLine($"LocalFileSourceItem not found for path (deleted): {fullPath}. No action taken.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing file deletion event for {e.FullPath}: {ex.Message}");
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        try
        {
            string oldFullPath = Path.GetFullPath(e.OldFullPath);
            string newFullPath = Path.GetFullPath(e.FullPath);
            Console.WriteLine($"File renamed: from {oldFullPath} to {newFullPath}");

            bool newIsMarkdown = newFullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

            lock (_lockObject)
            {
                // Find the LocalFileSourceItem to update in the mixed collection
                var itemToUpdate = _managedItems.OfType<LocalFileSourceItem>().FirstOrDefault(item =>
                    Path.GetFullPath(item.FilePath).Equals(oldFullPath, StringComparison.OrdinalIgnoreCase));

                if (itemToUpdate != null)
                {
                    if (newIsMarkdown)
                    {
                        itemToUpdate.FilePath = newFullPath; // Update existing item's path
                        try
                        {
                            itemToUpdate.Title = Path.GetFileNameWithoutExtension(newFullPath);

                            string fileContent = File.ReadAllText(newFullPath);
                            ObsidianNoteProperties properties = ObsidianNoteProperties.Parse(fileContent);

                            if (properties.Created != default) itemToUpdate.PublicationYear = properties.Created.Year; else itemToUpdate.PublicationYear = 0;
                            if (properties.Tags?.Count > 0) itemToUpdate.Tags = new List<string>(properties.Tags); else itemToUpdate.Tags.Clear();
                            if (properties.Aliases?.Count > 0) itemToUpdate.Aliases = new List<string>(properties.Aliases); else itemToUpdate.Aliases.Clear();

                        }
                        catch (ArgumentException argEx)
                        {
                            Console.Error.WriteLine($"Warning: Could not extract filename for Title from new path '{newFullPath}': {argEx.Message}");
                            itemToUpdate.Title = "Invalid File Path";
                        }
                        catch (IOException ioEx) { Console.Error.WriteLine($"IO Error reading renamed file for parsing {newFullPath}: {ioEx.Message}"); }
                        catch (Exception parseEx) { Console.Error.WriteLine($"Error parsing frontmatter for renamed file {newFullPath}: {parseEx.Message}"); }

                        Console.WriteLine($"Updated item: {itemToUpdate.Title} (Old: {oldFullPath}, New: {newFullPath})");
                    }
                    else // Renamed from .md to something else
                    {
                        _managedItems.Remove(itemToUpdate); // Remove it from the collection
                        Console.WriteLine($"Removed item due to rename away from .md: {itemToUpdate.Title} (was {oldFullPath})");
                    }
                }
                else if (newIsMarkdown) // A non-tracked file was renamed to .md, or a non-LocalFileSourceItem was renamed
                {
                    // Check if a LocalFileSourceItem for the new path already exists to avoid duplicates
                    if (!_managedItems.OfType<LocalFileSourceItem>().Any(item => Path.GetFullPath(item.FilePath).Equals(newFullPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        Console.WriteLine($"File renamed to .md, treating as new: {newFullPath}");
                        var newItem = new LocalFileSourceItem(newFullPath); // Create as LocalFileSourceItem
                        try
                        {
                            newItem.Title = Path.GetFileNameWithoutExtension(newFullPath);
                            string fileContent = File.ReadAllText(newFullPath);
                            ObsidianNoteProperties properties = ObsidianNoteProperties.Parse(fileContent);
                            if (properties.Created != default) newItem.PublicationYear = properties.Created.Year;
                            if (properties.Tags?.Count > 0) newItem.Tags = new List<string>(properties.Tags);
                            if (properties.Aliases?.Count > 0) newItem.Aliases = new List<string>(properties.Aliases);
                        }
                        catch (ArgumentException argEx)
                        {
                            Console.Error.WriteLine($"Warning: Could not extract filename for Title from path '{newFullPath}': {argEx.Message}");
                            newItem.Title = "Invalid File Path";
                        }
                        catch (IOException ioEx) { Console.Error.WriteLine($"IO Error reading newly named file for parsing {newFullPath}: {ioEx.Message}"); }
                        catch (Exception parseEx) { Console.Error.WriteLine($"Error parsing frontmatter for newly named file {newFullPath}: {parseEx.Message}"); }

                        _managedItems.Add(newItem); // Add to the IList<LiteratureSourceItem>
                        Console.WriteLine($"Added new item from rename: {newItem.Title} ({newItem.FilePath})");
                    }
                    else
                    {
                        Console.WriteLine($"LocalFileSourceItem already exists for path (renamed to .md): {newFullPath}. Skipping add.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing file rename event from {e.OldFullPath} to {e.FullPath}: {ex.Message}");
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        Exception ex = e.GetException();
        Console.Error.WriteLine($"FileSystemWatcher error: {ex?.Message ?? "Unknown error"}");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Diposes the file watcher
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Created -= OnFileCreated;
                    _watcher.Deleted -= OnFileDeleted;
                    _watcher.Renamed -= OnFileRenamed;
                    _watcher.Error -= OnWatcherError;
                    _watcher.Dispose();
                }
            }
            _isDisposed = true;
        }
    }
}