using Godot;
using System;
using System.IO;
using System.Linq; // Required for efficient array checking

public partial class FileSystem : Tree
{
    // Set the starting path (res:// is the project folder)
    [Export]
    public string RootPath = "res://";

    // New Export: Defines which file extensions to ignore.
    // We default to .xsd and .html as requested.
    [Export]
    public string[] ExcludedExtensions = [".xsd", ".html"];

    public override void _Ready()
    {
        // 1. Basic Tree Setup
        Columns = 1;
        HideRoot = false;

        // 2. Initial Refresh
        RefreshTree();
    }

    public void RefreshTree()
    {
        Clear();

        TreeItem root = CreateItem();
        root.SetText(0, RootPath);

        PopulateTree(RootPath, root);
    }

    private void PopulateTree(string path, TreeItem parent)
    {
        using var dir = DirAccess.Open(path);

        if (dir == null)
        {
            GD.PrintErr($"Error: Could not open path {path}. Error: {DirAccess.GetOpenError()}");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        while (fileName != "")
        {
            // Skip hidden system files (like . and ..)
            if (!fileName.StartsWith("."))
            {
                // We need to check if it is a directory to decide on logic
                bool isDirectory = dir.CurrentIsDir();

                // Get the full path for recursion
                // Note: Godot 4 recommends PathJoin() over System.IO.Path.Combine for Godot paths
                string fullPath = path.PathJoin(fileName);

                // --- LOGIC CHANGE: EXCLUSION CHECK ---

                // If it is a FILE, we check if we should exclude it.
                // We generally do not filter directories by extension.
                if (!isDirectory)
                {
                    if (IsFileExcluded(fileName))
                    {
                        // Get the next file and jump to the start of the while loop
                        // effectively skipping the creation of the TreeItem.
                        fileName = dir.GetNext();
                        continue;
                    }
                }

                // Create a new child item only if it passed the exclusion check
                TreeItem child = CreateItem(parent);
                child.SetText(0, fileName);
                child.SetMetadata(0, fullPath); // Store full path for easy access

                if (isDirectory)
                {
                    // It's a directory: Color it and recurse
                    child.SetCustomColor(0, Colors.SkyBlue);
                    child.SetSelectable(0, true);

                    PopulateTree(fullPath, child);
                }
                else
                {
                    // It's a file
                    child.SetCustomColor(0, Colors.White);
                }
            }

            fileName = dir.GetNext();
        }
    }

    /// <summary>
    /// Checks if the given filename ends with any of the excluded extensions.
    /// Case-insensitive (e.g., .HTML and .html are treated the same).
    /// </summary>
    private bool IsFileExcluded(string fileName)
    {
        // If the list is empty, nothing is excluded
        if (ExcludedExtensions == null || ExcludedExtensions.Length == 0)
            return false;

        string extension = Path.GetExtension(fileName);

        // We use LINQ to check if the array contains the extension.
        // StringComparer.OrdinalIgnoreCase ensures that .HTML is caught even if the filter is .html
        return ExcludedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}