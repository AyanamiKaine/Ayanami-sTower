using Godot;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public partial class X4DatabaseManager : Node
{
    // SIGNAL: Emitted when the background thread finishes and data is ready.
    // Connect to this in other scripts instead of checking _Ready.
    [Signal]
    public delegate void SearchCompletedEventHandler();

    [Export]
    public string SearchDirectory { get; set; } = @"C:\Your\Project\Path";

    [Export]
    public string[] FileExtensions { get; set; } = ["*.cs", "*.txt", "*.json", "*.xml"];

    [Export]
    public Dictionary UniqueMacros { get; set; } = [];

    [Export]
    public Godot.Collections.Array<string> Factions { get; set; } = new() { "player", "yaki", "buccaneers", "scaleplate", "antigone", "argon", "hatikvah", "paranid", "teladi", "trinity", "xenon", "court", "fallensplit", "freesplit", "split" };

    [Export]
    public Godot.Collections.Dictionary<string, Color> FactionColors { get; set; } = new()
    {
        { "player", Colors.Blue },
        { "yaki", Colors.Purple },
        { "buccaneers", Colors.Orange },
        { "scaleplate", Colors.Brown },
        { "antigone", Colors.Cyan },
        { "argon", Colors.DodgerBlue },
        { "hatikvah", Colors.Teal },
        { "paranid", Colors.Magenta },
        { "teladi", Colors.Green },
        { "trinity", Colors.Gold },
        { "xenon", Colors.Red },
        { "court", Colors.Crimson },
        { "fallensplit", Colors.DarkOrange },
        { "freesplit", Colors.Yellow },
        { "split", Colors.Olive }
    };

    public override void _Ready()
    {
        base._Ready();

        // We discard the Task (_) because _Ready cannot be awaited.
        // This kicks off the process and lets Godot continue immediately.
        _ = RunDatabaseScanAsync();
    }

    /// <summary>
    /// Handles the UI/Main Thread coordination for the background task.
    /// </summary>
    private async Task RunDatabaseScanAsync()
    {
        GD.Print($"[Main Thread] Starting search in: {SearchDirectory}");
        GD.Print("[Main Thread] Game/Editor should remain responsive...");

        try
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            // ---------------------------------------------------------
            // 1. OFFLOAD TO BACKGROUND THREAD
            // ---------------------------------------------------------
            // We capture the parameters into local variables to be safe.
            string path = SearchDirectory;
            string[] exts = FileExtensions;

            // 'await Task.Run' yields control back to Godot immediately.
            // The code inside the lambda () => runs on a worker thread.
            var groupedMacros = await Task.Run(() => FindMacrosInDirectory(path, exts));

            // ---------------------------------------------------------
            // 2. BACK ON MAIN THREAD
            // ---------------------------------------------------------
            // Execution resumes here only after the Task above finishes.
            sw.Stop();

            UniqueMacros = groupedMacros;

            GD.Print($"\n--- SEARCH COMPLETE ({sw.ElapsedMilliseconds}ms) ---");
            GD.Print($"Found {UniqueMacros.Count} macro categories.");

            // Emit the signal so other scripts know they can now access UniqueMacros
            EmitSignal(SignalName.SearchCompleted);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Critical Error] Search failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Pure logic method. Scans files and groups macros.
    /// Safe to run on background threads because it touches no Godot Nodes.
    /// </summary>
    static Dictionary FindMacrosInDirectory(string rootPath, string[] extensions)
    {
        // 1. Setup Thread-Safe Collection
        var groupedResults = new ConcurrentDictionary<string, ConcurrentBag<string>>();

        // 2. Regex Setup
        Regex allMacrosPattern = new Regex(@"\bmacro\.[a-zA-Z0-9_]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 3. Gather Files
        // Note: Directory.EnumerateFiles is fast, but if you have millions of files, 
        // even enumeration can take time. usually it's fine to do this part in the background task.
        var files = GetFiles(rootPath, extensions);

        // 4. Parallel Processing (CPU Intensive work)
        Parallel.ForEach(files, (filePath) =>
        {
            try
            {
                string content = File.ReadAllText(filePath);
                foreach (Match match in allMacrosPattern.Matches(content))
                {
                    string fullMacro = match.Value;

                    // --- KEY GENERATION LOGIC ---
                    if (fullMacro.Length <= 6) continue;

                    string namePart = fullMacro.Substring(6); // Remove "macro."
                    int underscoreIndex = namePart.IndexOf('_');

                    // Logic: Use word before first underscore as the key
                    string macroType = (underscoreIndex > 0)
                        ? namePart.Substring(0, underscoreIndex)
                        : namePart;

                    // Add to thread-safe bag
                    var bag = groupedResults.GetOrAdd(macroType, _ => []);
                    bag.Add(fullMacro);
                }
            }
            catch (IOException) { /* File locked */ }
        });

        // 5. Marshaling back to Godot Types
        // We do this inside the background task to save the main thread from iterating huge lists.
        var result = new Dictionary();

        foreach (var kvp in groupedResults)
        {
            var sortedList = kvp.Value.Distinct().OrderBy(x => x).ToList();

            var godotArray = new Godot.Collections.Array<string>();
            godotArray.AddRange(sortedList);
            result[kvp.Key] = godotArray;
        }

        return result;
    }

    static IEnumerable<string> GetFiles(string rootPath, string[] patterns)
    {
        if (!Directory.Exists(rootPath)) yield break;

        var enumOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false
        };

        foreach (var pattern in patterns)
        {
            foreach (var file in Directory.EnumerateFiles(rootPath, pattern, enumOptions))
            {
                yield return file;
            }
        }
    }
}