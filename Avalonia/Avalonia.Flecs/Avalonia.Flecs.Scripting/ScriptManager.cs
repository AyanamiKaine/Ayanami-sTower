using System.Reflection;
using Flecs.NET.Core;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Avalonia.Flecs.Scripting;


public class GlobalData(World _world, NamedEntities _entities)
{
    public World world = _world;
    public NamedEntities entities = _entities;
}

public class CompiledScripts
{
    private readonly Dictionary<string, Script> _compiledScripts = [];

    public void Add(string name, Script script)
    {
        _compiledScripts.Add(name, script);
    }

    public Script Get(string name)
    {
        return _compiledScripts[name];
    }

    public void Remove(string name)
    {
        _compiledScripts.Remove(name);
    }

    public bool Contains(string name)
    {
        return _compiledScripts.ContainsKey(name);
    }

    public void Clear()
    {
        _compiledScripts.Clear();
    }

    public void Run(string name, GlobalData data)
    {
        var script = _compiledScripts[name];
        script.RunAsync(data);
    }

    public Script this[string name]
    {
        get { return _compiledScripts[name]; }
        set { _compiledScripts[name] = value; }
    }
}

// Define a custom exception class for script not found
public class ScriptNotFoundException(string scriptName) : Exception($"Script with name {scriptName} not found.")
{
}

/*
We need to add hooks so users can provide functions that execute before and after 
we recompile scripts that changed. Something like OnRecompliationStarted and OnRecompilationFinished.

Why?
Because lets assume we have a hotreloading scripting system, what we would like to do 
is automatically rerun certain scripts if they have changed.
*/


public class ScriptCompilationEventArgs(string scriptName) : EventArgs
{
    public string ScriptName { get; } = scriptName;
}

public delegate void ScriptCompilationStart(object sender, ScriptCompilationEventArgs e);
public delegate void ScriptCompilationFinished(object sender, ScriptCompilationEventArgs e);

public class ScriptManager
{
    public event ScriptCompilationStart? OnScriptCompilationStart;
    public event ScriptCompilationFinished? OnScriptCompilationFinished;


    public ScriptManager(World world, NamedEntities entities, bool recompileScriptsOnFileChange = true)
    {

        ScriptWatcher = InitializeScriptWatcher(recompileScriptsOnFileChange);

        Data = new GlobalData(world, entities);
        _compiledScripts = new();

        //Adding meta data seems to do nothing?
        //What does meta data even do?
        //Like this -> MetadataReference.CreateFromFile("Flecs.NET.dll"),
        Options = ScriptOptions.Default
            .AddReferences(
                typeof(object).Assembly, // Usually needed for basic types
                Assembly.Load("Flecs.NET"),
                Assembly.Load("Flecs.NET.Bindings"),
                Assembly.Load("Avalonia.Flecs.Controls"),
                Assembly.Load("Avalonia.Controls"),
                Assembly.Load("Avalonia"),
                Assembly.Load("Avalonia.Desktop"),
                Assembly.Load("Avalonia.Base")
            // Load your API assembly
            )
            .AddImports("Avalonia.Controls");
    }

    public ScriptManager(World world, NamedEntities entities, ScriptOptions options, bool recompileScriptsOnFileChange = true)
    {
        ScriptWatcher = InitializeScriptWatcher(recompileScriptsOnFileChange);

        Data = new GlobalData(world, entities);
        _compiledScripts = new();
        Options = options;
    }

    private Dictionary<string, DateTime> _lastWriteTime = [];
    /// <summary>
    /// The debounce interval for file changes determines how far apart changes
    /// to the file can be before we recompile. We are doing this because
    /// simply editing the file changes several things, like content, last write
    /// time etc. This would result in multiple recompilation that are not needed.
    /// </summary>
    private TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// File watcher for the scripts folder.
    /// </summary>  
    private FileSystemWatcher ScriptWatcher { get; set; }

    private bool _recompileScriptsOnFileChange;

    /// <summary>
    /// If set true when you edit or rename a .csx script file in the script folder
    /// it will automatically recompile.
    /// </summary>
    public bool RecompileScriptsOnFileChange
    {
        get { return _recompileScriptsOnFileChange; }
        set
        {
            _recompileScriptsOnFileChange = value;
            // Call your function here
            if (_recompileScriptsOnFileChange == true)
            {
                ScriptWatcher.Created += OnScriptAdded;
                ScriptWatcher.Changed += OnScriptChange;
                ScriptWatcher.Renamed += OnScriptRenamed;
            }
            else
            {
                ScriptWatcher.Created -= OnScriptAdded;
                ScriptWatcher.Changed -= OnScriptChange;
                ScriptWatcher.Renamed -= OnScriptRenamed;
            }
        }
    }

    public ScriptOptions Options { get; set; }

    public GlobalData Data { get; set; }

    /// <summary>
    /// List of all compiled scripts.
    /// </summary>
    private readonly CompiledScripts _compiledScripts;

    /// <summary>
    /// Given a name and code string, compiles the code and adds it to the list of compiled scripts.
    /// That can then be run by calling RunScriptAsync with the name given.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="code"></param>
    public void AddScript(string name, string code)
    {
        OnScriptCompilationStart?.Invoke(this, new ScriptCompilationEventArgs(name));

        var script = CSharpScript.Create(code, Options, globalsType: typeof(GlobalData));
        script.Compile();
        _compiledScripts[name] = script;

        OnScriptCompilationFinished?.Invoke(this, new ScriptCompilationEventArgs(name));
    }
    /// <summary>
    /// Compiles all scripts in a given folder and adds them to the list of compiled scripts.
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public void CompileScriptsFromFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
        }

        // Get all .cs files in the folder
        var scriptFiles = Directory.GetFiles(folderPath, "*.csx");

        foreach (var file in scriptFiles)
        {
            try
            {
                // Read the code from the file
                var code = File.ReadAllTextAsync(file).Result;

                // Extract script name from filename
                var scriptName = Path.GetFileNameWithoutExtension(file);

                // Compile and add the script
                AddScript(scriptName, code);
            }
            catch (Exception ex)
            {
                // Handle exceptions like IOException, compilation errors, etc.
                Console.WriteLine($"Error compiling script {file}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Compiles all scripts async in a given folder and adds them to the list of compiled scripts.
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public async Task CompileScriptsFromFolderAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
        }

        // Get all .cs files in the folder
        var scriptFiles = Directory.GetFiles(folderPath, "*.csx");

        foreach (var file in scriptFiles)
        {
            try
            {
                // Read the code from the file
                var code = await File.ReadAllTextAsync(file);

                // Extract script name from filename
                var scriptName = Path.GetFileNameWithoutExtension(file);

                // Compile and add the script
                AddScript(scriptName, code);
            }
            catch (Exception ex)
            {
                // Handle exceptions like IOException, compilation errors, etc.
                Console.WriteLine($"Error compiling script {file}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Runs a script from the compiled scripts list.
    /// </summary>
    /// <param name="scriptName"></param>
    /// <returns></returns>
    /// <exception cref="ScriptNotFoundException"></exception>
    public async Task RunScriptAsync(string scriptName)
    {
        var script = _compiledScripts[scriptName]
        ?? throw new ScriptNotFoundException(scriptName);

        try
        {
            await script.RunAsync(Data);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error running script {scriptName}: {e.Message}");
        }
    }

    /// <summary>
    /// Activates recompilation of scripts when they change.
    /// </summary>
    public void ActivateRecompileOnFileChange()
    {
        RecompileScriptsOnFileChange = true;
    }

    /// <summary>
    /// Deactivates recompilation of scripts when they change.
    /// </summary>
    public void DeactivateRecompileOnFileChange()
    {
        RecompileScriptsOnFileChange = false;
    }

    /// <summary>
    /// If a script is changed, recompile it.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnScriptChange(object sender, FileSystemEventArgs e)
    {

        var now = DateTime.Now;
        if (_lastWriteTime.TryGetValue(e.FullPath, out var last))
        {
            if (now - last < _debounceInterval)
                return;
        }
        _lastWriteTime[e.FullPath] = now;

        Console.WriteLine($"Script {e.Name} changed. Recompiling...");

        if (e.Name is null)
            return;

        var name = Path.GetFileNameWithoutExtension(e.Name);
        var code = "";

        /*
        This ensures that the file can still be read even if it is being written(Locked) to by another process.
        */
        using (FileStream fileStream = new(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            code = new StreamReader(fileStream).ReadToEnd();
        }

        AddScript(name, code);
    }

    /// <summary>
    /// If a scipt is renamed, remove the old script and add the new one.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnScriptRenamed(object sender, RenamedEventArgs e)
    {
        Console.WriteLine($"Script {e.OldName} renamed to {e.Name}. Recompiling...");

        if (e.Name is null || e.OldName is null)
            return;

        _compiledScripts.Remove(e.OldName);

        var name = Path.GetFileNameWithoutExtension(e.Name);
        var code = File.ReadAllText(e.FullPath);
        AddScript(name, code);
    }

    /// <summary>
    /// If a script is added, compile it.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnScriptAdded(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"Script {e.Name} added. Compiling...");

        if (e.Name is null)
            return;

        var name = Path.GetFileNameWithoutExtension(e.Name);
        var code = File.ReadAllText(e.FullPath);
        AddScript(name, code);
    }

    /// <summary>
    /// Setup for the FileWatcher that watches for changes in the scripts folder.
    /// </summary>
    /// <param name="recompileScriptsOnFileChange"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private FileSystemWatcher InitializeScriptWatcher(bool recompileScriptsOnFileChange = true)
    {
        // Get absolute path for scripts folder next to executable
        string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new InvalidOperationException("Could not determine executable path");
        string scriptsPath = Path.Combine(exePath, "scripts");

        // Create scripts directory if it doesn't exist
        Directory.CreateDirectory(scriptsPath);

        ScriptWatcher = new FileSystemWatcher
        {
            Path = scriptsPath,
            NotifyFilter = NotifyFilters.LastWrite
                        | NotifyFilters.FileName
                        | NotifyFilters.DirectoryName,
            Filter = "*.csx",
            EnableRaisingEvents = true
        };

        // Add debouncing

        ScriptWatcher.Changed += (s, e) =>
        {

        };

        ScriptWatcher.Error += (s, e) =>
                {
                    Console.WriteLine($"FileSystemWatcher error: {e.GetException()}");
                };

        RecompileScriptsOnFileChange = recompileScriptsOnFileChange;
        return ScriptWatcher;
    }
}
