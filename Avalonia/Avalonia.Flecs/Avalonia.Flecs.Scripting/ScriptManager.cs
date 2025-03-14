﻿using System.Diagnostics;
using System.Reflection;
using System.Text;
using Avalonia.Flecs.Util;
using Flecs.NET.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Avalonia.Flecs.Scripting;

/// <summary>
/// e
/// </summary>
public class ScriptSourceRefrenceResolver : SourceReferenceResolver
{
    private readonly string _baseDirectory;
    /// <summary>
    /// Constructor for ScriptSourceRefrenceResolver
    /// </summary>
    /// <param name="baseDirectory"></param>
    public ScriptSourceRefrenceResolver(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
    }

    /// <summary>
    /// Uses the current directory as the base directory.
    /// and defines the scripts folder as the root for source files.
    /// 
    /// So if you have a script file in the scripts folder you can refrence it
    /// by its name.
    /// </summary>
    public ScriptSourceRefrenceResolver()
    {
        _baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "scripts");
    }

    /// <summary>
    /// Resolves a reference to a script file.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="baseFilePath"></param>
    /// <returns></returns>
    public override string? ResolveReference(string? path, string? baseFilePath)
    {
        // If the path is already absolute, return it as is
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        // Construct a path relative to the base directory (your scripts folder)
        var combinedPath = Path.Combine(_baseDirectory, path!);
        return Path.GetFullPath(combinedPath); // Normalize the path
    }

    /// <summary>
    ///     Opens a stream to the specified file.
    /// </summary>
    /// <param name="resolvedPath"></param>
    /// <returns></returns>
    public override Stream OpenRead(string resolvedPath)
    {
        // You might need to add error handling here in case the file doesn't exist
        return File.OpenRead(resolvedPath);
    }

    // Implement other abstract members if necessary (NormalizePath, ResolvePath, etc.)
    // but these defaults are often sufficient:
    /// <summary>
    /// Normalizes a path.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="baseFilePath"></param>
    /// <returns></returns>
    public override string? NormalizePath(string? path, string? baseFilePath) => path;

    /// <summary>
    /// Checks if the given object is equal to this object.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(object? other)
    {
        // Implementation for Equals
        if (other == null || GetType() != other.GetType())
        {
            return false;
        }
        return _baseDirectory == ((ScriptSourceRefrenceResolver)other)._baseDirectory;
    }

    /// <summary>
    /// Gets the hash code for this object.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        // Implementation for GetHashCode
        return _baseDirectory != null ? _baseDirectory.GetHashCode() : 0;
    }
}

/// <summary>
/// Global data that is accessible in the scripts.
/// We can use this to interact with the ECS.
/// </summary>
/// <param name="world"></param>
/// <param name="entities"></param>
public class GlobalData(World world, NamedEntities entities)
{
    /// <summary>
    /// The app ecs world instance.
    /// </summary>
    public World _world = world;
    /// <summary>
    /// We provide a NameEntities container where we give entities unique names
    /// by them we can refrence them better by name. Flecs provides a way to lookup
    /// entities via a path (the path is define by the given name to the entity and its parents).
    /// But because we will change the parent of the entity we will have to change the path.
    /// The simple solution is to store the entity id in a dictionary with the name as the key.
    /// </summary>
    public NamedEntities _entities = entities;
}

/// <summary>
/// Class that stores compiled scripts.
/// </summary>
public class CompiledScripts
{
    private readonly Dictionary<string, Script> _compiledScripts = [];

    /// <summary>
    /// Adds a script to the list of compiled scripts.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="script"></param>
    public void Add(string name, Script script)
    {
        _compiledScripts.Add(name, script);
    }

    /// <summary>
    /// Gets a script from the list of compiled scripts.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Script Get(string name)
    {
        return _compiledScripts[name];
    }

    /// <summary>
    /// Removes a script from the list of compiled scripts.
    /// </summary>
    /// <param name="name"></param>
    public void Remove(string name)
    {
        _compiledScripts.Remove(name);
    }

    /// <summary>
    /// Checks if a script with the given name exists in the list of compiled scripts.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool Contains(string name)
    {
        return _compiledScripts.ContainsKey(name);
    }

    /// <summary>
    /// Clears the list of compiled scripts.
    /// </summary>
    public void Clear()
    {
        _compiledScripts.Clear();
    }

    /// <summary>
    /// Runs a script from the list of compiled scripts.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="data"></param>
    public void Run(string name, GlobalData data)
    {
        var script = _compiledScripts[name];
        script.RunAsync(data);
    }

    /// <summary>
    /// Indexer for the list of compiled scripts.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Script this[string name]
    {
        get
        {
            return _compiledScripts[name];
        }
        set
        {
            _compiledScripts[name] = value;
        }
    }
}

// Define a custom exception class for script not found
/// <summary>
/// Exception thrown when a script is not found.
/// </summary>
public class ScriptNotFoundException : Exception
{
    /// <summary>
    /// Constructor for ScriptNotFoundException
    /// </summary>
    public ScriptNotFoundException() : base()
    {
    }
    /// <summary>
    /// Constructor for ScriptNotFoundException
    /// </summary>
    public ScriptNotFoundException(string scriptName) : base($"Script with name {scriptName} not found.")
    {
    }
    /// <summary>
    /// Constructor for ScriptNotFoundException
    /// </summary>
    public ScriptNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/*
We need to add hooks so users can provide functions that execute before and after 
we recompile scripts that changed. Something like OnRecompliationStarted and OnRecompilationFinished.

Why?
Because lets assume we have a hotreloading scripting system, what we would like to do 
is automatically rerun certain scripts if they have changed.
*/

/*
One major problem I have is. While we can quite easily destroy all entities and rerun the script that creates them, i would like to also be able to only change entities that need to change. Conceptually this might be
easy to implement but practically i have no clue how it would fit into the ECS pattern and the Flecs library.
*/

/// <summary>
/// Event arguments for script compilation.
/// </summary>
/// <param name="scriptName"></param>
public class ScriptEventArgs(string scriptName) : EventArgs
{
    /// <summary>
    /// The name of the script that is being compiled.
    /// </summary>
    public string ScriptName { get; } = scriptName;
}

/// <summary>
/// Class that manages scripts and their compilation.
/// </summary>
public class ScriptManager
{
    /// <summary>
    /// If set true #r directives are removed from the code.
    /// </summary>
    public bool RemoveScriptDirective { get; set; } = true;
    /// <summary>
    /// Event that is triggered when a script compilation starts.
    /// </summary>
    public event EventHandler<ScriptEventArgs>? OnScriptCompilationStart;
    /// <summary>
    /// Event that is triggered when a script compilation finishes.
    /// </summary>
    public event EventHandler<ScriptEventArgs>? OnScriptCompilationFinished;

    /// <summary>
    /// Event that is triggered when a script is added to the compiled scripts.
    /// </summary>
    public event EventHandler<ScriptEventArgs>? OnCompiledScriptAdded;

    /// <summary>
    /// Event that is triggered when a script is removed from the compiled scripts.
    /// </summary>
    public event EventHandler<ScriptEventArgs>? OnCompiledScriptRemoved;

    /// <summary>
    /// Event that is triggered when a script is changed in the compiled scripts.
    /// So the contents of the script is changed most likely because it was recompiled
    /// but its name is still the same. A script will always be first added before the changed
    /// event is triggered.
    /// </summary>
    public event EventHandler<ScriptEventArgs>? OnCompiledScriptChanged;

    /// <summary>
    /// Makes the refrenced world and named entities avalaiable as globals to all scripts.
    /// as _world and _entities respectively.
    /// </summary>
    /// <param name="world">can be refrenced in a script using _world</param>
    /// <param name="entities">can be refrenced in a script using _entities</param>
    /// <param name="recompileScriptsOnFileChange"></param>
    /// <param name="removeScriptDirective"></param>
    public ScriptManager(World world, NamedEntities entities, bool recompileScriptsOnFileChange = true, bool removeScriptDirective = true)
    {
        RemoveScriptDirective = removeScriptDirective;
        ScriptWatcher = InitializeScriptWatcher(recompileScriptsOnFileChange);
        ReplaceBuildScriptWatcherWithDevelopmentScriptWatcher();

        Data = new GlobalData(world, entities);
        CompiledScripts = new();

        string baseDirectoryForScripts = Path.Combine(AppContext.BaseDirectory, "../../../../", "scripts");


        //Adding meta data seems to do nothing?
        //What does meta data even do?
        //Like this -> MetadataReference.CreateFromFile("Flecs.NET.dll"),
        Options = ScriptOptions.Default
            .WithSourceResolver(new ScriptSourceRefrenceResolver(baseDirectoryForScripts))
            .AddReferences(
                typeof(object).Assembly, // Usually needed for basic types
                Assembly.Load("Flecs.NET"),
                Assembly.Load("Flecs.NET.Bindings"),
                Assembly.Load("Avalonia.Flecs.Controls"),
                Assembly.Load("Avalonia.Controls"),
                Assembly.Load("Avalonia.Flecs.Scripting"),
                Assembly.Load("Avalonia"),
                Assembly.Load("Avalonia.Desktop"),
                Assembly.Load("Avalonia.Base")
            // Load your API assembly
            )
            .AddImports("Avalonia.Controls")
            .AddImports("Avalonia.Flecs.Controls.ECS")
            .AddImports("Avalonia.Flecs.Scripting")
            .AddImports("Flecs.NET.Core");
    }

    /// <summary>
    /// Constructor for the script manager.
    /// </summary>
    /// <param name="world">can be refrenced in a script using _world</param>
    /// <param name="entities">can be refrenced in a script using _entities</param>
    /// <param name="options"></param>
    /// <param name="recompileScriptsOnFileChange"></param>
    /// <param name="removeScriptDirective"></param>
    public ScriptManager(World world, NamedEntities entities, ScriptOptions options, bool recompileScriptsOnFileChange = true, bool removeScriptDirective = true)
    {
        RemoveScriptDirective = removeScriptDirective;
        ScriptWatcher = InitializeScriptWatcher(recompileScriptsOnFileChange);
        ReplaceBuildScriptWatcherWithDevelopmentScriptWatcher();
        Data = new GlobalData(world, entities);
        CompiledScripts = new();
        Options = options;
    }

    /// <summary>
    /// Dictionary that stores the last write time of a script.
    /// We are doing this because we want to debounce the file changes.
    /// I.e limit the amounts of time we call the recompilation function.
    /// </summary>
    private readonly Dictionary<string, DateTime> _lastWriteTime = [];
    /// <summary>
    /// The debounce interval for file changes determines how far apart changes
    /// to the file can be before we recompile. We are doing this because
    /// simply editing the file changes several things, like content, last write
    /// time etc. This would result in multiple recompilations that are not needed.
    /// </summary>
    private readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// File watcher for a defined scripts folder.
    /// Default folder watched should be "./scripts".
    /// </summary>
    private FileSystemWatcher ScriptWatcher
    {
        get; set;
    }

    private bool _recompileScriptsOnFileChange;

    /// <summary>
    /// If set true when you edit or rename a .csx script file in the script folder
    /// it will automatically recompile.
    /// </summary>
    public bool RecompileScriptsOnFileChange
    {
        get
        {
            return _recompileScriptsOnFileChange;
        }
        set
        {
            _recompileScriptsOnFileChange = value;
            // Call your function here
            if (_recompileScriptsOnFileChange == true)
            {
                ScriptWatcher.Created += OnScriptFileAdded;
                ScriptWatcher.Changed += OnScriptChange;
                ScriptWatcher.Renamed += OnScriptRenamed;
            }
            else
            {
                ScriptWatcher.Created -= OnScriptFileAdded;
                ScriptWatcher.Changed -= OnScriptChange;
                ScriptWatcher.Renamed -= OnScriptRenamed;
            }
        }
    }

    private readonly CancellationTokenSource _replCancellationTokenSource = new();

    /// <summary>
    /// The script options that are used for compiling the scripts.
    /// </summary>
    public ScriptOptions Options
    {
        get; set;
    }

    /// <summary>
    /// The global data is accessible in the scripts and can be used to interact with the ECS.
    /// </summary>
    public GlobalData Data
    {
        get; set;
    }

    /// <summary>
    /// The compiled scripts that are stored in the script manager.
    /// </summary>
    public CompiledScripts CompiledScripts { get; }

    /// <summary>
    /// Given a name and code string, compiles the code and adds it to the list of compiled scripts.
    /// That can then be run by calling RunScriptAsync with the name given.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="code"></param>
    public void AddScript(string name, string code)
    {
        if (RemoveScriptDirective)
        {
            code = CompilerScriptDirectiveTrimming(code);
        }

        OnScriptCompilationStart?.Invoke(this, new ScriptEventArgs(name));

        var script = CSharpScript.Create(code, Options, globalsType: typeof(GlobalData));
        script.Compile();
        OnScriptCompilationFinished?.Invoke(this, new ScriptEventArgs(name));

        if (CompiledScripts.Contains(name))
            OnCompiledScriptChanged?.Invoke(this, new ScriptEventArgs(name));

        CompiledScripts[name] = script;
        OnCompiledScriptAdded?.Invoke(this, new ScriptEventArgs(name));
    }

    /// <summary>
    /// Removes a script from the list of compiled scripts.
    /// </summary>
    /// <param name="scriptName"></param>
    public void RemoveScript(string scriptName)
    {
        if (CompiledScripts.Contains(scriptName))
        {
            CompiledScripts.Remove(scriptName);
            OnCompiledScriptRemoved?.Invoke(this, new ScriptEventArgs(scriptName));
        }
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

                if (RemoveScriptDirective)
                {
                    code = CompilerScriptDirectiveTrimming(code);
                }

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

                if (RemoveScriptDirective)
                {
                    code = CompilerScriptDirectiveTrimming(code);
                }

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
        var script = CompiledScripts[scriptName]
        ?? throw new ScriptNotFoundException(scriptName);

        try
        {
            await script.RunAsync(Data);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error running script '{scriptName}': {e.Message}");
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
    /// If a script is changed, recompile it. We try around 
    /// 50 times to read the file, because it might be locked by another process.
    /// We try to wait around 100ms for every try.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnScriptChange(object sender, FileSystemEventArgs e)
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

        /*
        This ensures that the file can still be read even if it is being written(Locked) to by another process.
        */
        await Task.Delay(250); // Wait for 200 milliseconds

        int retryCount = 0;
        // Even though the max retries is quite high, it still can happen that 
        // it is blocked, maybe make the retries infinit?, but we really dont want that this 
        // blocks
        const int maxRetries = 50;
        string code = "";

        /*
        The editor used to edit the script might still be writing to the file.
        What happens is that if the file is locked StreamReader returns an empty string
        This is not what we want, so we retry a few times before giving up.

        I should really make it async. Also we should add more error handling.
        */

        while (string.IsNullOrEmpty(code) && retryCount < maxRetries)
        {
            try
            {
                await using FileStream fileStream = new(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = new(fileStream);
                code = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(code))
                {
                    retryCount++;
                    await Task.Delay(100);
                }
            }
            catch (IOException)
            {
                retryCount++;
                await Task.Delay(100);
            }
        }
        Console.WriteLine($"Retry count: {retryCount}, because of the file was locked.");

        if (!string.IsNullOrEmpty(code))
        {
            AddScript(name, code);
        }
        else
        {
            Console.WriteLine($"Failed to read script {name} after {maxRetries} retries");
        }
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

        CompiledScripts.Remove(e.OldName);

        var name = Path.GetFileNameWithoutExtension(e.Name);
        var code = File.ReadAllText(e.FullPath);
        AddScript(name, code);
    }

    /// <summary>
    /// If a script is added to the script folder, automatically compile it.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnScriptFileAdded(object sender, FileSystemEventArgs e)
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

        ScriptWatcher.Error += (s, e) => Console.WriteLine($"FileSystemWatcher error: {e.GetException()}");

        RecompileScriptsOnFileChange = recompileScriptsOnFileChange;
        return ScriptWatcher;
    }

    /// <summary>
    /// Starts Interactive REPL with the GlobalData defined.
    /// The repl has access to _world, and _entities.
    /// as well as all refrences defined in the Options.
    /// </summary>
    /// <returns></returns>
    public async Task StartReplAsync()
    {
        Console.WriteLine("Starting REPL...");

        while (!_replCancellationTokenSource.IsCancellationRequested)
        {
            Console.Write("> ");
            string? code = await ReadLineAsync(); // Implement async input

            if (code == null || code.Trim() == "exit")
            {
                break;
            }

            try
            {
                await EvaluateAsync(code, _replCancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        Console.WriteLine("REPL exited.");
    }

    private static async Task<string?> ReadLineAsync()
    {
        // Implement asynchronous input retrieval here.
        // This could involve reading from a UI element, a message queue, etc.
        // For simplicity, we'll use Task.Run(() => Console.ReadLine());
        // BEWARE: This pattern of using Task.Run() to call synchronous methods is not recommended but
        // Console does not provide any async methods and this is the closes workaround.

        return await Task.Run(() => Console.ReadLine());
    }
    /// <summary>
    /// Represents the current state of the repl, its used
    /// to continue the repl from the previous state.
    /// </summary>
    private ScriptState<object>? _replState = null;

    /// <summary>
    /// Evaluates the given code in the REPL.
    /// </summary>
    /// <param name="code"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task EvaluateAsync(string code, CancellationToken cancellationToken)
    {
        Script<object> script = CSharpScript.Create(code, Options, globalsType: typeof(GlobalData));
        if (_replState == null)
        {
            // First execution, create a new script
            script = CSharpScript.Create(code, Options, globalsType: typeof(GlobalData));
        }
        else
        {
            // Continue from the previous state
            _replState = await _replState.ContinueWithAsync(code, Options, cancellationToken);
            return;
        }

        var compilation = script.Compile(cancellationToken);
        // Check for compilation errors
        if (compilation.Any())
        {
            foreach (var error in compilation)
            {
                Console.WriteLine($"Compilation error: {error.ToString()}");
            }
            return;
        }

        try
        {
            _replState = await script.RunAsync(Data, cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error running script: {e.Message}");
        }
    }

    /// <summary>
    /// STOP REPL
    /// </summary>
    public void StopRepl()
    {
        _replCancellationTokenSource.Cancel();
    }

    /// <summary>
    /// The main reason for this function is to remove the #r directive from the code in
    /// a script. We do this because we already have the references defined in the Options.
    /// We only have the refrences for the lsp server. Thats why we need to remove the #r directive.
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    private string CompilerScriptDirectiveTrimming(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return code; // Handle null or empty input
        }

        string[] lines = code.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        StringBuilder sb = new();

        foreach (string line in lines)
        {
            string trimmedLine = line.TrimStart(); // Trim leading whitespace
            if (!trimmedLine.StartsWith("#r")) // Skip lines starting with #r
            {
                if (!string.IsNullOrEmpty(trimmedLine) || lines.Length == 1) // prevent removing empty lines if there is only one line
                {
                    sb.AppendLine(trimmedLine);
                }
            }
        }
        return sb.ToString().TrimEnd(); // Remove any trailing newline
    }

    /// <summary>
    /// Creates a development script file watcher.
    /// That watches for changes in the scripts folder
    /// not found in the build but instead in the development
    /// directory we expect the scripts folder to be found
    /// next to the .csproj file. This function only runs
    /// in DEBUG mode.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [Conditional("DEBUG")]
    void ReplaceBuildScriptWatcherWithDevelopmentScriptWatcher()
    {
        static string? FindProjectDirectory(string startPath)
        {
            string currentDir = startPath;
            while (!string.IsNullOrEmpty(currentDir))
            {
                if (Directory.GetFiles(currentDir, "*.csproj").Any())
                {
                    return currentDir;
                }
                currentDir = Path.GetDirectoryName(currentDir) ?? "";
            }
            return null;
        }

        var developmentScriptFileWatcher = InitializeScriptWatcher(true);
        string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
             ?? throw new InvalidOperationException("Could not determine executable path");

        // Traverse up to find the .csproj file
        string? projectPath = FindProjectDirectory(exePath) ?? throw new InvalidOperationException("Could not find project directory.");
        string scriptsPath = Path.Combine(projectPath, "scripts");

        // Create scripts directory if it doesn't exist
        Directory.CreateDirectory(scriptsPath);
        developmentScriptFileWatcher.Path = scriptsPath;
        ScriptWatcher = developmentScriptFileWatcher;

        // Get the build directory (where the executable is)
        string buildDirectory = exePath;

        // Copy new created and updated files from the project
        // scripts folder to the build directory
        developmentScriptFileWatcher.Created += (s, e) =>
        {
            string relativePath = Path.GetRelativePath(Path.Combine("scripts", scriptsPath), e.FullPath);
            string destPath = Path.Combine(buildDirectory, relativePath);

            // Create directory if it doesn't exist in the build directory
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

            File.Copy(e.FullPath, destPath, true);
            Console.WriteLine($"Copied newly created file: {e.FullPath} to {destPath}");
        };

        developmentScriptFileWatcher.Changed += (s, e) =>
        {
            string relativePath = Path.GetRelativePath(scriptsPath, e.FullPath);
            string destPath = Path.Combine(buildDirectory, Path.Combine("scripts", relativePath));

            // Create directory if it doesn't exist in the build directory
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

            File.Copy(e.FullPath, destPath, true);
            Console.WriteLine($"Copied updated file: {e.FullPath} to {destPath}");
        };

        developmentScriptFileWatcher.Renamed += (s, e) =>
        {
            string oldRelativePath = Path.GetRelativePath(scriptsPath, e.OldFullPath);
            string oldDestPath = Path.Combine(buildDirectory, Path.Combine("scripts", oldRelativePath));
            string newRelativePath = Path.GetRelativePath(scriptsPath, e.FullPath);
            string newDestPath = Path.Combine(buildDirectory, Path.Combine("scripts", newRelativePath));

            // Create directory if it doesn't exist in the build directory
            Directory.CreateDirectory(Path.GetDirectoryName(newDestPath)!);

            // Delete the old file
            if (File.Exists(oldDestPath))
            {
                File.Delete(oldDestPath);
                Console.WriteLine($"Deleted old file: {oldDestPath}");
            }

            File.Copy(e.FullPath, newDestPath, true);
            Console.WriteLine($"Copied renamed file: {e.FullPath} to {newDestPath}");

        };
    }
}
