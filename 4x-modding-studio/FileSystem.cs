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

    // Context menu for right-click actions
    [Export]
    public PopupMenu? ContextMenu { get; set; }

    // Currently right-clicked item
    private TreeItem? _contextItem;

    // File system watcher for auto-refresh
    private FileSystemWatcher? _watcher;
    private bool _needsRefresh = false;
    private double _refreshCooldown = 0;
    private const double RefreshDelay = 0.5; // Wait 0.5 seconds after last change before refreshing

    // Signal emitted when a context action is selected
    [Signal]
    public delegate void ContextActionRequestedEventHandler(string action, string filePath, bool isDirectory);

    public override void _Ready()
    {
        // 1. Basic Tree Setup
        Columns = 1;
        HideRoot = false;

        // 2. Initial Refresh
        RefreshTree();

        // 3. Setup file system watcher
        SetupFileWatcher();
    }

    public override void _Process(double delta)
    {
        // Handle deferred refresh (file watcher events come from different thread)
        if (_needsRefresh)
        {
            _refreshCooldown -= delta;
            if (_refreshCooldown <= 0)
            {
                _needsRefresh = false;
                RefreshTree();
                GD.Print("File tree refreshed due to file system changes");
            }
        }
    }

    public override void _ExitTree()
    {
        // Clean up the file watcher when node is removed
        DisposeFileWatcher();
    }

    private void SetupFileWatcher()
    {
        try
        {
            // Convert Godot path to system path
            string globalPath = ProjectSettings.GlobalizePath(RootPath);

            if (!Directory.Exists(globalPath))
            {
                GD.PrintErr($"Cannot watch directory: {globalPath} does not exist");
                return;
            }

            _watcher = new FileSystemWatcher(globalPath)
            {
                NotifyFilter = NotifyFilters.FileName
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.LastWrite
                             | NotifyFilters.CreationTime,

                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            // Subscribe to events
            _watcher.Created += OnFileSystemChanged;
            _watcher.Deleted += OnFileSystemChanged;
            _watcher.Renamed += OnFileSystemRenamed;
            _watcher.Changed += OnFileSystemChanged;

            GD.Print($"File watcher started for: {globalPath}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to setup file watcher: {ex.Message}");
        }
    }

    private void DisposeFileWatcher()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnFileSystemChanged;
            _watcher.Deleted -= OnFileSystemChanged;
            _watcher.Renamed -= OnFileSystemRenamed;
            _watcher.Changed -= OnFileSystemChanged;
            _watcher.Dispose();
            _watcher = null;
            GD.Print("File watcher disposed");
        }
    }

    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        // Skip excluded files
        if (!string.IsNullOrEmpty(e.Name) && IsFileExcluded(e.Name))
            return;

        // Schedule a refresh (with cooldown to batch rapid changes)
        ScheduleRefresh();
    }

    private void OnFileSystemRenamed(object sender, RenamedEventArgs e)
    {
        // Schedule a refresh
        ScheduleRefresh();
    }

    private void ScheduleRefresh()
    {
        // Reset cooldown timer - this batches multiple rapid changes
        _refreshCooldown = RefreshDelay;
        _needsRefresh = true;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            // Check for right-click press
            if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
            {
                _contextItem = GetItemAtPosition(mouseEvent.Position);
                if (_contextItem != null)
                {
                    // Select the item that was right-clicked
                    SetSelected(_contextItem, 0);
                    ShowContextMenu(mouseEvent.Position);
                    AcceptEvent(); // Consume the event
                }
            }
        }
    }

    private void ShowContextMenu(Vector2 position)
    {
        if (ContextMenu == null)
        {
            GD.PrintErr("ContextMenu not assigned to FileSystem!");
            return;
        }

        string filePath = _contextItem!.GetMetadata(0).AsString();
        bool isDirectory = DirAccess.DirExistsAbsolute(filePath);

        // Clear and rebuild menu based on item type
        ContextMenu.Clear();

        if (isDirectory)
        {
            ContextMenu.AddItem("New File", 0);
            ContextMenu.AddItem("New Folder", 1);
            ContextMenu.AddSeparator();
            ContextMenu.AddItem("Rename", 2);
            ContextMenu.AddItem("Delete", 3);
            ContextMenu.AddSeparator();
            ContextMenu.AddItem("Refresh", 4);
            ContextMenu.AddSeparator();
            ContextMenu.AddItem("Open in File Explorer", 5);
        }
        else
        {
            ContextMenu.AddItem("Open", 0);
            ContextMenu.AddSeparator();
            ContextMenu.AddItem("Rename", 2);
            ContextMenu.AddItem("Delete", 3);
            ContextMenu.AddSeparator();
            ContextMenu.AddItem("Show in File Explorer", 5);
        }

        // Disconnect previous connection if exists, then reconnect
        if (ContextMenu.IsConnected(PopupMenu.SignalName.IdPressed, Callable.From<long>(OnContextMenuItemPressed)))
        {
            ContextMenu.Disconnect(PopupMenu.SignalName.IdPressed, Callable.From<long>(OnContextMenuItemPressed));
        }
        ContextMenu.IdPressed += OnContextMenuItemPressed;

        // Position and show the menu
        ContextMenu.Position = (Vector2I)GetGlobalMousePosition();
        ContextMenu.Popup();
    }

    private void OnContextMenuItemPressed(long id)
    {
        if (_contextItem == null) return;

        string filePath = _contextItem.GetMetadata(0).AsString();
        bool isDirectory = DirAccess.DirExistsAbsolute(filePath);

        string action = id switch
        {
            0 => isDirectory ? "new_file" : "open",
            1 => "new_folder",
            2 => "rename",
            3 => "delete",
            4 => "refresh",
            5 => "show_in_explorer",
            _ => ""
        };

        if (!string.IsNullOrEmpty(action))
        {
            EmitSignal(SignalName.ContextActionRequested, action, filePath, isDirectory);
        }
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