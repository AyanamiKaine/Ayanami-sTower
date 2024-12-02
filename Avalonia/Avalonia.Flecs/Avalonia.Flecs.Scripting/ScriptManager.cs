using System.Diagnostics;
using System.Reflection;
using Avalonia.Flecs.Controls.ECS.Events;
using Flecs.NET.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;

namespace Avalonia.Flecs.Scripting;


public class GlobalData(World _world)
{
    public World world = _world;
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

public class ScriptManager
{

    public ScriptManager(World world, bool recompileScriptsOnFileChange = true)
    {
        ScriptWatcher = new FileSystemWatcher
        {
            Path = "Scripts",
            NotifyFilter = NotifyFilters.LastWrite,
            Filter = "*.csx",
            EnableRaisingEvents = true
        };

        RecompileScriptsOnFileChange = recompileScriptsOnFileChange;

        Data = new GlobalData(world);
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

    public ScriptManager(World world, ScriptOptions options, bool recompileScriptsOnFileChange = true)
    {
        ScriptWatcher = new FileSystemWatcher
        {
            Path = "Scripts",
            NotifyFilter = NotifyFilters.LastWrite,
            Filter = "*.cs",
            EnableRaisingEvents = true
        };

        RecompileScriptsOnFileChange = recompileScriptsOnFileChange;


        Data = new GlobalData(world);
        _compiledScripts = new();
        Options = options;
    }

    /// <summary>
    /// File watcher for the scripts folder.
    /// </summary>  
    FileSystemWatcher ScriptWatcher { get; set; }

    private bool _recompileScriptsOnFileChange;

    /// <summary>
    /// If set true the scripts will be recompiled when the file changes.
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
                ScriptWatcher.Changed += OnScriptChange;
                ScriptWatcher.Renamed += OnScriptRenamed;
            }
            else
            {
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
        var script = CSharpScript.Create(code, Options, globalsType: typeof(GlobalData));
        script.Compile();
        _compiledScripts[name] = script;
    }

    /// <summary>
    /// Compiles all scripts in a given folder and adds them to the list of compiled scripts.
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public async Task CompileScriptsFromFolder(string folderPath)
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

    public void ActivateRecompileOnFileChange()
    {
        RecompileScriptsOnFileChange = true;
    }

    public void DeactivateRecompileOnFileChange()
    {
        RecompileScriptsOnFileChange = false;
    }

    private void OnScriptChange(object sender, FileSystemEventArgs e)
    {
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

}
