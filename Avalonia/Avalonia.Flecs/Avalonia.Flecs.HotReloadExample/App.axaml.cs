using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Flecs.Controls; // UIBuilder
using Avalonia.Flecs.Controls.ECS; // Module
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading; // Dispatcher
using Flecs.NET.Core;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Flecs.HotReloadExample;

public class App : Application, IDisposable
{
    World _world = World.Create();
    UIBuilder<Window>? _mainWindowBuilder; // Builder for the window entity
    Window? _actualMainWindow; // The actual Avalonia Window instance

#if DEBUG
    // Store the entity representing the current main content
    private Entity _currentContentEntity = default;

    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private CancellationTokenSource? _rebuildCts;
    private const int DebounceMilliseconds = 500;
#endif

    public override void Initialize()
    {
        // If you truly have NO XAML for the App itself, remove this.
        // If App.axaml ONLY contains styles, replace this with programmatic style loading.
        // If App.axaml defines the App structure, keep it.
        // Based on your watcher path later, you might be using App.axaml.cs, implying App.axaml exists.
        AvaloniaXamlLoader.Load(this);

        // If you removed AvaloniaXamlLoader.Load(this), you'll likely need this:
        // Styles.Parse("/* Avalonia.Themes.Fluent */\n@import url('avares://Avalonia.Themes.Fluent/FluentLight.xaml');");

        _world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        // Create the builder for the initial content first
        var initialContentBuilder = MainContent();
#if DEBUG
        // Store the entity ID of the initial content
        _currentContentEntity = initialContentBuilder.Entity;
        Console.WriteLine($"Stored initial content entity: {_currentContentEntity.Id}");
#endif

        // Define the main window structure and add the initial content builder as a child
        _mainWindowBuilder = _world.UI<Window>((window) =>
        {
            window.SetTitle("Avalonia.Flecs.HotReload")
                  .SetHeight(300)
                  .SetWidth(500)
                  .SetPadding(new Thickness(4));
        }).Child(initialContentBuilder); // Add the builder; Flecs observers handle control attachment
    }

    // This method defines the dynamic part of your UI
    public UIBuilder<Control> MainContent()
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Defining MainContent structure...");

        // Create the new entity hierarchy for the content
        return _world.UI<StackPanel>((stackPanel) =>
        {
            stackPanel.SetSpacing(10);

            stackPanel.Child<TextBlock>((textBlock) =>
            {
                textBlock.SetText($"Change me! Last rebuilt: {DateTime.Now:HH:mm:ss.fff}");
                //textBlock.SetText("Some other text...");
            });

            stackPanel.Child<Button>((button) =>
            {
                button.SetText("Click Me!"); // Changed text
                // button.OnClick((s, e) => Console.WriteLine("Hot Reloaded Button clicked!"));
            });

            // Maybe add a new control during reload:
            // stackPanel.Child<CheckBox>(cb => cb.SetContent("Reload Checkbox"));

        }).AsBaseBuilder<Control, StackPanel>(); // Convert builder type
    }

#if DEBUG
    private void RebuildMainWindowContent()
    {
        // Ensure we have the window, the builder, and a valid entity to replace
        if (_actualMainWindow == null || _mainWindowBuilder == null)
        {
            Console.Error.WriteLine("Cannot rebuild: Window or WindowBuilder is null.");
            return;
        }

        var mainWindowEntity = _mainWindowBuilder.Entity;
        if (!mainWindowEntity.IsAlive())
        {
            Console.Error.WriteLine("Cannot rebuild: Main window entity is not alive.");
            return;
        }


        Console.WriteLine("Executing RebuildMainWindowContent on UI thread...");

        try
        {
            // 1. Destroy the OLD content entity (if it's valid and alive)
            //    This should trigger Flecs OnRemove observers for cleanup.
            if (_currentContentEntity.IsAlive())
            {
                Console.WriteLine($"Destroying old content entity: {_currentContentEntity.Id} ({_currentContentEntity.Name()})");
                _currentContentEntity.Destruct(); // <<< KEY STEP 1
            }
            else
            {
                Console.WriteLine($"Old content entity {_currentContentEntity.Id} was already dead or invalid. Skipping destruction.");
            }

            // --- Optional Delay ---
            // Sometimes it might help to let Flecs/Avalonia process the destruction. Test if needed.
            // await Task.Delay(20);
            // --- End Optional Delay ---

            // 2. Create the NEW content entity hierarchy by calling MainContent()
            Console.WriteLine("Creating new content definition...");
            var newContentBuilder = MainContent();
            _currentContentEntity = newContentBuilder.Entity; // Update the tracker <<< KEY STEP 2
            Console.WriteLine($"Created new content entity: {_currentContentEntity.Id} ({_currentContentEntity.Name()})");

            // 3. Attach the NEW entity as a child of the main window entity.
            //    The Flecs 'ControlToParentAdder' observer should handle adding the
            //    new Avalonia Control to the Window's Content/Children.
            Console.WriteLine($"Attaching new content entity {_currentContentEntity.Id} to window entity {mainWindowEntity.Id}");
            // Use the builder's Child method to establish the Flecs relationship
            _mainWindowBuilder.Child(newContentBuilder); // <<< KEY STEP 3
            // Or equivalently, directly modify the Flecs hierarchy:
            // _currentContentEntity.ChildOf(mainWindowEntity);

            Console.WriteLine("Rebuild process completed.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during hot reload rebuild: {ex}");
            // Reset the tracker on error to avoid trying to destroy an invalid entity next time
            _currentContentEntity = default;
        }
    }
#endif

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Get the actual Window instance from the Flecs entity
            _actualMainWindow = _mainWindowBuilder!.Get<Window>(); // Assumes Get<T> retrieves the control instance
            desktop.MainWindow = _actualMainWindow;

#if DEBUG
            SetupHotReloadWatcher();
            this.AttachDevTools();
#endif
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            // Handle single view if needed
            var mainView = _mainWindowBuilder!.Get<Window>(); // Or appropriate control type
                                                              // singleView.MainView = mainView; // Adjust as necessary
            _actualMainWindow = mainView as Window; // Store if it's a window
#if DEBUG
            SetupHotReloadWatcher();
            // DevTools might need different attach logic for single view
#endif
        }

        base.OnFrameworkInitializationCompleted();
    }

#if DEBUG
    private void SetupHotReloadWatcher()
    {
        try
        {
            string? assemblyLocation = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(assemblyLocation)) { /* Error */ return; }
            string? projectDirectory = FindProjectDirectory(Path.GetDirectoryName(assemblyLocation));
            if (projectDirectory == null) { /* Error */ return; }

            // --- !!! IMPORTANT: Point this to the file containing MainContent !!! ---
            // Usually App.cs if MainContent is in the App class.
            const string sourceFileName = "App.axaml.cs"; // <--- ADJUST IF MainContent IS ELSEWHERE
            string sourceFilePath = Path.Combine(projectDirectory, sourceFileName);

            if (!File.Exists(sourceFilePath))
            {
                Console.Error.WriteLine($"Source file NOT FOUND for watching: {sourceFilePath}. Hot Reload watcher not started.");
                return;
            }

            Console.WriteLine($"Setting up hot reload watcher for: {sourceFilePath}");

            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(sourceFilePath)!,
                Filter = Path.GetFileName(sourceFilePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName, // LastWrite is key
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnSourceFileChanged;
            _watcher.Created += OnSourceFileChanged;
            _watcher.Renamed += OnSourceFileRenamed;

            Console.WriteLine("Hot Reload FileSystemWatcher started.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to set up hot reload watcher: {ex}");
        }
    }

    // FindProjectDirectory, OnSourceFileRenamed, OnSourceFileChanged,
    // TriggerRebuildWithDebounce, DebouncedRebuildCallback remain the same as before...

    // (Include the unchanged methods from the previous answer here)
    // Helper to find the project directory (heuristic)
    private string? FindProjectDirectory(string? startPath)
    {
        var currentDir = startPath;
        while (currentDir != null)
        {
            // Look for a .csproj file or a specific marker file/directory
            if (Directory.GetFiles(currentDir, "*.csproj").Length > 0 || Directory.Exists(Path.Combine(currentDir, ".git")))
            {
                return currentDir;
            }
            currentDir = Path.GetDirectoryName(currentDir);
        }
        return null; // Or return startPath as a fallback
    }


    private void OnSourceFileRenamed(object sender, RenamedEventArgs e)
    {
        // Handle if the specific file being watched is renamed
        var watchedFileFullPath = Path.Combine(_watcher!.Path, _watcher.Filter);
        bool renamedToMatch = string.Equals(e.FullPath, watchedFileFullPath, StringComparison.OrdinalIgnoreCase);
        bool renamedFromMatch = string.Equals(e.OldFullPath, watchedFileFullPath, StringComparison.OrdinalIgnoreCase);

        if (renamedToMatch || renamedFromMatch)
        {
            Console.WriteLine($"Source file {_watcher.Filter} renamed. Triggering reload.");
            // Optional: Update watcher filter if needed, though restarting might be safer
            TriggerRebuildWithDebounce();
        }
    }

    private void OnSourceFileChanged(object sender, FileSystemEventArgs e)
    {
        // Check if the change is for the file we are actually watching
        if (!string.Equals(e.Name, _watcher!.Filter, StringComparison.OrdinalIgnoreCase))
        {
            // Console.WriteLine($"Ignoring change in unrelated file: {e.Name}");
            return;
        }
        Console.WriteLine($"Source file changed: {e.FullPath}. ChangeType: {e.ChangeType}. Debouncing rebuild...");
        TriggerRebuildWithDebounce();
    }

    private void TriggerRebuildWithDebounce()
    {
        // Cancel any pending rebuild request
        _rebuildCts?.Cancel();
        _rebuildCts?.Dispose();
        _rebuildCts = new CancellationTokenSource();

        // Dispose the previous timer if it exists
        _debounceTimer?.Dispose();

        // Create a new timer that will fire once after the debounce interval
        _debounceTimer = new Timer(DebouncedRebuildCallback, _rebuildCts.Token, DebounceMilliseconds, Timeout.Infinite);
    }

    private void DebouncedRebuildCallback(object? state)
    {
        var token = (CancellationToken)state!;
        if (token.IsCancellationRequested)
        {
            Console.WriteLine("Debounced rebuild cancelled.");
            _debounceTimer?.Dispose(); _debounceTimer = null;
            _rebuildCts?.Dispose(); _rebuildCts = null;
            return;
        }

        Console.WriteLine("Debounce timer elapsed. Posting rebuild to UI thread...");
        // Execute the actual rebuild on the UI thread
        Dispatcher.UIThread.Post(RebuildMainWindowContent, DispatcherPriority.Background);

        // Clean up the timer and CTS after execution attempt is posted
        _debounceTimer?.Dispose();
        _debounceTimer = null;
        _rebuildCts?.Dispose();
        _rebuildCts = null;
    }


    public void Dispose()
    {
        // Dispose managed resources
        _watcher?.Dispose();
        _debounceTimer?.Dispose();
        _rebuildCts?.Dispose();
        _world.Dispose(); // --- IMPORTANT: Dispose the Flecs world ---
        GC.SuppressFinalize(this);
    }
#endif

}