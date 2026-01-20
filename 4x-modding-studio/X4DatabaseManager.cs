using Godot;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

/// <summary>
/// Represents a single usage of an XML element in a file
/// </summary>
public class XmlElementUsage
{
    public string FilePath { get; set; } = "";
    public int LineNumber { get; set; } = 0;
    public string ParentElement { get; set; } = ""; // The parent element name, if any
    public System.Collections.Generic.Dictionary<string, string> Attributes { get; set; } = new(); // Attribute values at this usage

    public override string ToString() => $"{Path.GetFileName(FilePath)}:{LineNumber}";
}

/// <summary>
/// Aggregates all usages of a specific element name
/// </summary>
public class XmlElementUsageStats
{
    public string ElementName { get; set; } = "";
    public int TotalUsageCount { get; set; } = 0;
    public List<XmlElementUsage> Usages { get; set; } = new();
    public HashSet<string> FilesUsedIn { get; set; } = new();
    public HashSet<string> ObservedParents { get; set; } = new(); // Parent elements observed in actual usage
    public System.Collections.Generic.Dictionary<string, HashSet<string>> ObservedAttributeValues { get; set; } = new(); // Attr name -> observed values

    public override string ToString() => $"{ElementName}: {TotalUsageCount} usages in {FilesUsedIn.Count} files";
}

/// <summary>
/// Represents an attribute definition from an XSD schema
/// </summary>
public class XsdAttributeInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "xs:string";
    public bool IsRequired { get; set; } = false;
    public string DefaultValue { get; set; } = "";
    public string Documentation { get; set; } = "";
    public List<string> EnumValues { get; set; } = new(); // If type is an enum restriction

    public override string ToString() => $"{Name} ({Type}){(IsRequired ? " [required]" : "")}";
}

/// <summary>
/// Represents an element definition from an XSD schema
/// </summary>
public class XsdElementInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = ""; // Named type if using ref or type attribute
    public string Documentation { get; set; } = "";
    public List<XsdAttributeInfo> Attributes { get; set; } = new();
    public List<string> AttributeGroups { get; set; } = new(); // Referenced attribute groups (e.g., "action", "condition")
    public List<string> AllowedChildren { get; set; } = new(); // Element names that can be children
    public List<string> ParentElements { get; set; } = new(); // Elements that can contain this element
    public int MinOccurs { get; set; } = 1;
    public int MaxOccurs { get; set; } = 1; // -1 means unbounded
    public string SourceFile { get; set; } = ""; // Which XSD file defined this

    /// <summary>
    /// Parent complex type name if this element is defined within another type's context.
    /// Used to disambiguate elements with the same name from different contexts.
    /// </summary>
    public string ParentTypeName { get; set; } = "";

    /// <summary>
    /// A unique key that distinguishes this element from others with the same name.
    /// Format: "name" if unique, or "name@source" if disambiguation is needed.
    /// </summary>
    public string UniqueKey { get; set; } = "";

    /// <summary>
    /// Gets a display-friendly source context for this element.
    /// </summary>
    public string SourceContext => !string.IsNullOrEmpty(ParentTypeName)
        ? $"{Path.GetFileNameWithoutExtension(SourceFile)}/{ParentTypeName}"
        : Path.GetFileNameWithoutExtension(SourceFile);

    public override string ToString() => $"{Name} (attrs: {Attributes.Count}, children: {AllowedChildren.Count})";
}

/// <summary>
/// Represents a complete XSD schema with all its definitions
/// </summary>
public class XsdSchemaInfo
{
    public string FilePath { get; set; } = "";
    public string TargetNamespace { get; set; } = "";
    public System.Collections.Generic.Dictionary<string, XsdElementInfo> Elements { get; set; } = new(); // Only xs:elements
    public System.Collections.Generic.Dictionary<string, XsdElementInfo> ComplexTypes { get; set; } = new(); // xs:complexType
    public System.Collections.Generic.Dictionary<string, XsdElementInfo> Groups { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, XsdElementInfo> AttributeGroups { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, XsdAttributeInfo> GlobalAttributes { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, List<string>> SimpleTypeEnums { get; set; } = new(); // Type name -> enum values
    public List<string> Imports { get; set; } = new(); // Other schemas this one imports
}

/// <summary>
/// Cache data structure for XSD schemas
/// </summary>
public class XsdCacheData
{
    public DateTime CacheCreatedAt { get; set; } = DateTime.MinValue;
    public string SourceDirectory { get; set; } = "";
    public System.Collections.Generic.Dictionary<string, DateTime> FileModificationTimes { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, XsdElementInfo> AllElements { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, List<XsdElementInfo>> ElementsByName { get; set; } = new();
    public List<string> AllAttributeNames { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, List<string>> AllEnumTypes { get; set; } = new();
}

/// <summary>
/// Serializable version of XmlElementUsageStats (HashSets converted to Lists for JSON)
/// </summary>
public class XmlUsageStatsSerialized
{
    public string ElementName { get; set; } = "";
    public int TotalUsageCount { get; set; } = 0;
    public List<string> FilesUsedIn { get; set; } = new();
    public List<string> ObservedParents { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, List<string>> ObservedAttributeValues { get; set; } = new();
    // Note: We don't cache individual Usages to keep cache size reasonable
}

/// <summary>
/// Cache data structure for XML usage statistics
/// </summary>
public class XmlUsageCacheData
{
    public DateTime CacheCreatedAt { get; set; } = DateTime.MinValue;
    public string SourceDirectory { get; set; } = "";
    public int TotalXmlFilesScanned { get; set; } = 0;
    public System.Collections.Generic.Dictionary<string, DateTime> FileModificationTimes { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, XmlUsageStatsSerialized> ElementUsageStats { get; set; } = new();
}

public partial class X4DatabaseManager : Node
{
    // SIGNAL: Emitted when the background thread finishes and data is ready.
    // Connect to this in other scripts instead of checking _Ready.
    [Signal]
    public delegate void SearchCompletedEventHandler();

    [Export]
    public string SearchDirectory { get; set; } = @"C:\Your\Project\Path";

    [Export]
    public string CacheDirectory { get; set; } = @"user://cache";

    [Export]
    public bool UseCache { get; set; } = true;

    private const string XSD_CACHE_FILE = "xsd_cache.json";
    private const string XML_USAGE_CACHE_FILE = "xml_usage_cache.json";

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

    // --- XSD Schema Data ---
    /// <summary>All parsed XSD schemas keyed by file path</summary>
    public System.Collections.Generic.Dictionary<string, XsdSchemaInfo> ParsedSchemas { get; private set; } = new();

    /// <summary>All unique element definitions across all schemas, keyed by UniqueKey</summary>
    public System.Collections.Generic.Dictionary<string, XsdElementInfo> AllElements { get; private set; } = new();

    /// <summary>Elements grouped by base name (for looking up all variants of an element)</summary>
    public System.Collections.Generic.Dictionary<string, List<XsdElementInfo>> ElementsByName { get; private set; } = new();

    /// <summary>All unique attribute names found across all schemas</summary>
    public HashSet<string> AllAttributeNames { get; private set; } = new();

    /// <summary>All simple type enumerations (e.g., allowed string values)</summary>
    public System.Collections.Generic.Dictionary<string, List<string>> AllEnumTypes { get; private set; } = new();

    // --- XML Usage Data ---
    /// <summary>Usage statistics for each element, keyed by element name</summary>
    public System.Collections.Generic.Dictionary<string, XmlElementUsageStats> ElementUsageStats { get; private set; } = new();

    /// <summary>Total number of XML files scanned</summary>
    public int TotalXmlFilesScanned { get; private set; } = 0;

    // --- GDScript-callable helper methods ---
    // C# properties with complex types aren't directly accessible from GDScript

    /// <summary>Gets the number of elements in the database (callable from GDScript)</summary>
    public int GetElementCount() => AllElements.Count;

    /// <summary>Checks if the database has loaded elements (callable from GDScript)</summary>
    public bool IsElementDatabaseReady() => AllElements.Count > 0;

    /// <summary>Gets all element names as a Godot array (callable from GDScript)</summary>
    public Godot.Collections.Array<string> GetAllElementNames()
    {
        var result = new Godot.Collections.Array<string>();
        foreach (var name in AllElements.Keys)
        {
            result.Add(name);
        }
        return result;
    }

    /// <summary>
    /// Gets all variants of an element by base name.
    /// Returns multiple elements if the same name exists in different contexts.
    /// </summary>
    public List<XsdElementInfo> GetElementVariants(string elementName)
    {
        if (ElementsByName.TryGetValue(elementName, out var variants))
            return variants;
        return new List<XsdElementInfo>();
    }

    /// <summary>
    /// Gets an element by its unique key or falls back to name lookup.
    /// If looking up by name and multiple variants exist, returns the first one.
    /// </summary>
    public XsdElementInfo? GetElement(string keyOrName)
    {
        // Try unique key first
        if (AllElements.TryGetValue(keyOrName, out var elem))
            return elem;

        // Fall back to name lookup (returns first variant if multiple exist)
        if (ElementsByName.TryGetValue(keyOrName, out var variants) && variants.Count > 0)
            return variants[0];

        return null;
    }

    /// <summary>
    /// Checks if an element name has multiple variants from different sources.
    /// </summary>
    public bool HasMultipleVariants(string elementName)
    {
        return ElementsByName.TryGetValue(elementName, out var variants) && variants.Count > 1;
    }


    public override void _Ready()
    {
        base._Ready();

        // We discard the Task (_) because _Ready cannot be awaited.
        // This kicks off the process and lets Godot continue immediately.
        _ = RunDatabaseScanAsync();
        //_ = RunXsdScanAsync(); // Replaced by inference
        _ = RunSchemaInferenceAsync();
        _ = RunXmlUsageScanAsync();
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

    // =========================================================================
    // CACHE MANAGEMENT
    // =========================================================================

    /// <summary>
    /// Gets the absolute path to the cache directory, creating it if necessary.
    /// </summary>
    private string GetCacheDirectoryPath()
    {
        string cachePath;
        if (CacheDirectory.StartsWith("user://"))
        {
            // Convert Godot user:// path to absolute path
            cachePath = ProjectSettings.GlobalizePath(CacheDirectory);
        }
        else
        {
            cachePath = CacheDirectory;
        }

        if (!Directory.Exists(cachePath))
        {
            Directory.CreateDirectory(cachePath);
        }

        return cachePath;
    }

    /// <summary>
    /// Gets modification times for all files matching a pattern in a directory.
    /// </summary>
    static System.Collections.Generic.Dictionary<string, DateTime> GetFileModificationTimes(string rootPath, string pattern)
    {
        var times = new System.Collections.Generic.Dictionary<string, DateTime>();

        if (!Directory.Exists(rootPath))
            return times;

        var enumOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false
        };

        foreach (var file in Directory.EnumerateFiles(rootPath, pattern, enumOptions))
        {
            try
            {
                times[file] = File.GetLastWriteTimeUtc(file);
            }
            catch { /* Ignore inaccessible files */ }
        }

        return times;
    }

    /// <summary>
    /// Checks if a cache is still valid by comparing file modification times.
    /// </summary>
    static bool IsCacheValid(System.Collections.Generic.Dictionary<string, DateTime> cachedTimes, System.Collections.Generic.Dictionary<string, DateTime> currentTimes)
    {
        // If file counts differ, cache is invalid
        if (cachedTimes.Count != currentTimes.Count)
            return false;

        // Check each file's modification time
        foreach (var kvp in currentTimes)
        {
            if (!cachedTimes.TryGetValue(kvp.Key, out var cachedTime))
                return false; // New file not in cache

            // Allow 1 second tolerance for filesystem precision
            if (Math.Abs((kvp.Value - cachedTime).TotalSeconds) > 1)
                return false; // File was modified
        }

        return true;
    }

    /// <summary>
    /// Saves XSD cache data to disk.
    /// </summary>
    void SaveXsdCache(System.Collections.Generic.Dictionary<string, XsdElementInfo> elements,
                      System.Collections.Generic.Dictionary<string, List<XsdElementInfo>> elementsByName,
                      HashSet<string> attributes,
                      System.Collections.Generic.Dictionary<string, List<string>> enums,
                      System.Collections.Generic.Dictionary<string, DateTime> fileTimes)
    {
        try
        {
            var cacheData = new XsdCacheData
            {
                CacheCreatedAt = DateTime.UtcNow,
                SourceDirectory = SearchDirectory,
                FileModificationTimes = fileTimes,
                AllElements = elements,
                ElementsByName = elementsByName,
                AllAttributeNames = attributes.ToList(),
                AllEnumTypes = enums
            };

            var cachePath = Path.Combine(GetCacheDirectoryPath(), XSD_CACHE_FILE);
            var options = new JsonSerializerOptions { WriteIndented = false };
            var json = JsonSerializer.Serialize(cacheData, options);
            File.WriteAllText(cachePath, json);

            GD.Print($"[Cache] Saved XSD cache ({elements.Count} elements) to {cachePath}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Cache] Failed to save XSD cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads XSD cache data from disk if valid.
    /// </summary>
    XsdCacheData? LoadXsdCache(System.Collections.Generic.Dictionary<string, DateTime> currentFileTimes)
    {
        try
        {
            var cachePath = Path.Combine(GetCacheDirectoryPath(), XSD_CACHE_FILE);
            if (!File.Exists(cachePath))
                return null;

            var json = File.ReadAllText(cachePath);
            var cacheData = JsonSerializer.Deserialize<XsdCacheData>(json);

            if (cacheData == null)
                return null;

            // Verify cache is for the same directory
            if (cacheData.SourceDirectory != SearchDirectory)
            {
                GD.Print("[Cache] XSD cache is for different directory, ignoring");
                return null;
            }

            // Verify files haven't changed
            if (!IsCacheValid(cacheData.FileModificationTimes, currentFileTimes))
            {
                GD.Print("[Cache] XSD files have changed, cache invalid");
                return null;
            }

            GD.Print($"[Cache] Loaded valid XSD cache ({cacheData.AllElements.Count} elements)");
            return cacheData;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Cache] Failed to load XSD cache: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Saves XML usage cache data to disk.
    /// </summary>
    void SaveXmlUsageCache(System.Collections.Generic.Dictionary<string, XmlElementUsageStats> stats,
                           int fileCount,
                           System.Collections.Generic.Dictionary<string, DateTime> fileTimes)
    {
        try
        {
            // Convert to serializable format
            var serializedStats = new System.Collections.Generic.Dictionary<string, XmlUsageStatsSerialized>();
            foreach (var kvp in stats)
            {
                serializedStats[kvp.Key] = new XmlUsageStatsSerialized
                {
                    ElementName = kvp.Value.ElementName,
                    TotalUsageCount = kvp.Value.TotalUsageCount,
                    FilesUsedIn = kvp.Value.FilesUsedIn.ToList(),
                    ObservedParents = kvp.Value.ObservedParents.ToList(),
                    ObservedAttributeValues = kvp.Value.ObservedAttributeValues
                        .ToDictionary(x => x.Key, x => x.Value.ToList())
                };
            }

            var cacheData = new XmlUsageCacheData
            {
                CacheCreatedAt = DateTime.UtcNow,
                SourceDirectory = SearchDirectory,
                TotalXmlFilesScanned = fileCount,
                FileModificationTimes = fileTimes,
                ElementUsageStats = serializedStats
            };

            var cachePath = Path.Combine(GetCacheDirectoryPath(), XML_USAGE_CACHE_FILE);
            var options = new JsonSerializerOptions { WriteIndented = false };
            var json = JsonSerializer.Serialize(cacheData, options);
            File.WriteAllText(cachePath, json);

            GD.Print($"[Cache] Saved XML usage cache ({stats.Count} elements) to {cachePath}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Cache] Failed to save XML usage cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads XML usage cache data from disk if valid.
    /// </summary>
    XmlUsageCacheData? LoadXmlUsageCache(System.Collections.Generic.Dictionary<string, DateTime> currentFileTimes)
    {
        try
        {
            var cachePath = Path.Combine(GetCacheDirectoryPath(), XML_USAGE_CACHE_FILE);
            if (!File.Exists(cachePath))
                return null;

            var json = File.ReadAllText(cachePath);
            var cacheData = JsonSerializer.Deserialize<XmlUsageCacheData>(json);

            if (cacheData == null)
                return null;

            // Verify cache is for the same directory
            if (cacheData.SourceDirectory != SearchDirectory)
            {
                GD.Print("[Cache] XML usage cache is for different directory, ignoring");
                return null;
            }

            // Verify files haven't changed
            if (!IsCacheValid(cacheData.FileModificationTimes, currentFileTimes))
            {
                GD.Print("[Cache] XML files have changed, cache invalid");
                return null;
            }

            GD.Print($"[Cache] Loaded valid XML usage cache ({cacheData.ElementUsageStats.Count} elements)");
            return cacheData;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Cache] Failed to load XML usage cache: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Converts cached usage stats back to the runtime format.
    /// </summary>
    static System.Collections.Generic.Dictionary<string, XmlElementUsageStats> ConvertFromCachedUsageStats(
        System.Collections.Generic.Dictionary<string, XmlUsageStatsSerialized> cached)
    {
        var result = new System.Collections.Generic.Dictionary<string, XmlElementUsageStats>();

        foreach (var kvp in cached)
        {
            result[kvp.Key] = new XmlElementUsageStats
            {
                ElementName = kvp.Value.ElementName,
                TotalUsageCount = kvp.Value.TotalUsageCount,
                FilesUsedIn = kvp.Value.FilesUsedIn.ToHashSet(),
                ObservedParents = kvp.Value.ObservedParents.ToHashSet(),
                ObservedAttributeValues = kvp.Value.ObservedAttributeValues
                    .ToDictionary(x => x.Key, x => x.Value.ToHashSet()),
                Usages = new List<XmlElementUsage>() // Not cached to save space
            };
        }

        return result;
    }

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    public void ClearCache()
    {
        try
        {
            var cacheDir = GetCacheDirectoryPath();
            var xsdCache = Path.Combine(cacheDir, XSD_CACHE_FILE);
            var xmlCache = Path.Combine(cacheDir, XML_USAGE_CACHE_FILE);

            if (File.Exists(xsdCache)) File.Delete(xsdCache);
            if (File.Exists(xmlCache)) File.Delete(xmlCache);

            GD.Print("[Cache] Cache cleared successfully");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Cache] Failed to clear cache: {ex.Message}");
        }
    }

    // =========================================================================
    // XSD SCHEMA PARSING
    // =========================================================================

    private static readonly XNamespace xs = "http://www.w3.org/2001/XMLSchema";

    /// <summary>
    /// Handles the UI/Main Thread coordination for XSD scanning.
    /// </summary>
    private async Task RunXsdScanAsync()
    {
        GD.Print($"[XSD Scanner] Starting schema scan in: {SearchDirectory}");

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string path = SearchDirectory;

            // Get current file modification times for cache validation
            var currentFileTimes = await Task.Run(() => GetFileModificationTimes(path, "*.xsd"));

            // Try to load from cache first
            if (UseCache)
            {
                var cachedData = LoadXsdCache(currentFileTimes);
                if (cachedData != null)
                {
                    sw.Stop();
                    AllElements = cachedData.AllElements;
                    ElementsByName = cachedData.ElementsByName ?? new();
                    AllAttributeNames = cachedData.AllAttributeNames.ToHashSet();
                    AllEnumTypes = cachedData.AllEnumTypes;
                    ParsedSchemas = new(); // Not cached, but rarely needed after load

                    GD.Print($"\n--- XSD LOADED FROM CACHE ({sw.ElapsedMilliseconds}ms) ---");
                    GD.Print($"Found {AllElements.Count} unique elements ({ElementsByName.Count} base names)");
                    GD.Print($"Found {AllAttributeNames.Count} unique attribute names");
                    return;
                }
            }

            // Cache miss or disabled - do full scan
            var (schemas, elements, elementsByName, attributes, enums) = await Task.Run(() => ScanXsdSchemas(path));

            sw.Stop();

            ParsedSchemas = schemas;
            AllElements = elements;
            ElementsByName = elementsByName;
            AllAttributeNames = attributes;
            AllEnumTypes = enums;

            // Save to cache for next time
            if (UseCache)
            {
                SaveXsdCache(elements, elementsByName, attributes, enums, currentFileTimes);
            }

            GD.Print($"\n--- XSD SCAN COMPLETE ({sw.ElapsedMilliseconds}ms) ---");
            GD.Print($"Found {ParsedSchemas.Count} schema files");
            GD.Print($"Found {AllElements.Count} unique elements ({ElementsByName.Count} base names)");
            GD.Print($"Found {AllAttributeNames.Count} unique attribute names");
            GD.Print($"Found {AllEnumTypes.Count} enum types");

            // Documentation coverage stats
            var documentedElements = AllElements.Values.Count(e => !string.IsNullOrEmpty(e.Documentation));
            var totalAttributes = AllElements.Values.Sum(e => e.Attributes.Count);
            var documentedAttributes = AllElements.Values.Sum(e => e.Attributes.Count(a => !string.IsNullOrEmpty(a.Documentation)));

            GD.Print($"\n--- DOCUMENTATION COVERAGE ---");
            GD.Print($"Elements with docs: {documentedElements}/{AllElements.Count} ({(AllElements.Count > 0 ? (100.0 * documentedElements / AllElements.Count) : 0):F1}%)");
            GD.Print($"Attributes with docs: {documentedAttributes}/{totalAttributes} ({(totalAttributes > 0 ? (100.0 * documentedAttributes / totalAttributes) : 0):F1}%)");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[XSD Scanner] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the UI/Main Thread coordination for Schema Inference.
    /// Replaces RunXsdScanAsync.
    /// </summary>
    private async Task RunSchemaInferenceAsync()
    {
        GD.Print($"[SchemaInference] Starting inference in: {SearchDirectory}");

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string path = SearchDirectory;

            // 1. Run inference on background thread
            // We use a lambda to run the static method
            var (elements, elementsByName, attributes) = await Task.Run(() => InferSchemaFromXmlFiles(path));

            sw.Stop();

            // 2. Update State
            AllElements = elements;
            ElementsByName = elementsByName;
            AllAttributeNames = attributes;
            // Clear other fields related to XSD that we aren't using anymore
            ParsedSchemas = new();
            AllEnumTypes = new();

            GD.Print($"\n--- INFERENCE COMPLETE ({sw.ElapsedMilliseconds}ms) ---");
            GD.Print($"Found {AllElements.Count} unique elements ({ElementsByName.Count} base names)");
            GD.Print($"Found {AllAttributeNames.Count} unique attribute names");

            // Emit signal to notify listeners (DatabaseViewer, etc)
            // Note: RunDatabaseScanAsync also emits this, so we might emit it twice?
            // Existing code emits SearchCompleted from RunDatabaseScanAsync.
            // If we want to be sure, we can emit it here too, or rely on the other one.
            // Let's emit it to be safe, assuming listeners handle multiple signals gracefully.
            // Actually, better to define a new signal or just let the UI update when it can.
            // But for now, let's leave it as is.
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SchemaInference] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Static worker method for inference
    /// </summary>
    static (System.Collections.Generic.Dictionary<string, XsdElementInfo>, System.Collections.Generic.Dictionary<string, List<XsdElementInfo>>, HashSet<string>) InferSchemaFromXmlFiles(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return (new(), new(), new());
        }

        // 1. Find all subdirectories
        // Note: This can be expensive for huge trees, but acceptable for this scope
        var allDirectories = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories)
                                .Concat(new[] { rootPath })
                                .ToList();

        var globalRegistry = new System.Collections.Generic.Dictionary<string, XmlSchemaRegistry>(StringComparer.OrdinalIgnoreCase);

        // 2. Scan each directory
        foreach (var dir in allDirectories)
        {
            try
            {
                var xmlFiles = Directory.GetFiles(dir, "*.xml");
                if (xmlFiles.Any())
                {
                    var dirName = new DirectoryInfo(dir).Name;

                    // Scope by directory name (e.g., md, aiscripts)
                    if (!globalRegistry.TryGetValue(dirName, out var registry))
                    {
                        registry = new XmlSchemaRegistry(dirName);
                        globalRegistry[dirName] = registry;
                    }

                    foreach (var file in xmlFiles)
                    {
                        try
                        {
                            string content = File.ReadAllText(file);
                            registry.LearnFromXml(content, Path.GetFileName(file));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[SchemaInference] Error reading file '{file}': {ex.Message}");
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"[SchemaInference] Unauthorized access scanning directory '{dir}': {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SchemaInference] Error scanning directory '{dir}': {ex.Message}");
            }
        }

        // 3. Convert to X4DatabaseManager structures
        var allElements = new System.Collections.Generic.Dictionary<string, XsdElementInfo>();
        var elementsByName = new System.Collections.Generic.Dictionary<string, List<XsdElementInfo>>(StringComparer.OrdinalIgnoreCase);
        var allAttributeNames = new HashSet<string>();

        foreach (var kvp in globalRegistry)
        {
            string context = kvp.Key; // Directory name
            var registry = kvp.Value;

            foreach (var meta in registry.GetAllMetadata())
            {
                var xsdInfo = ConvertToXsdInfo(meta, context);

                // Add to flattened dict
                allElements[xsdInfo.UniqueKey] = xsdInfo;

                // Add to name lookup
                if (!elementsByName.TryGetValue(xsdInfo.Name, out var list))
                {
                    list = new List<XsdElementInfo>();
                    elementsByName[xsdInfo.Name] = list;
                }
                list.Add(xsdInfo);

                // Collect attributes
                foreach (var attr in xsdInfo.Attributes)
                {
                    allAttributeNames.Add(attr.Name);
                }
            }
        }

        return (allElements, elementsByName, allAttributeNames);
    }

    /// <summary>
    /// Converts ElementMetadata to XsdElementInfo
    /// </summary>
    static XsdElementInfo ConvertToXsdInfo(ElementMetadata meta, string context)
    {
        var info = new XsdElementInfo
        {
            Name = meta.ElementName,
            // Use context (directory name) as source identification
            SourceFile = context,
            ParentTypeName = context, // Hack to make context visible in searcher
            UniqueKey = $"{meta.ElementName}@{context}", // Ensure uniqueness across folders
            AllowedChildren = meta.GetValidChildren().ToList(),
            ParentElements = meta.GetValidParents().ToList(),
            Attributes = meta.GetAttributes().Select(a => new XsdAttributeInfo
            {
                Name = a,
                Type = "xs:string", // Default for now, as we don't infer types yet
                Documentation = $"Inferred from {context}"
            }).ToList()
        };
        return info;
    }

    /// <summary>
    /// Determines if newElement should replace existingElement based on completeness.
    /// Prefers elements with documentation, more attributes, and more allowed children.
    /// IMPORTANT: Never replaces an attributeGroup with a regular element - they serve different purposes.
    /// </summary>
    static bool ShouldReplaceElement(XsdElementInfo existing, XsdElementInfo newElem)
    {
        // Never replace an attributeGroup with a non-attributeGroup (or vice versa)
        // They serve different purposes even if they share the same name
        bool existingIsAttrGroup = existing.Type == "attributeGroup";
        bool newIsAttrGroup = newElem.Type == "attributeGroup";

        if (existingIsAttrGroup != newIsAttrGroup)
        {
            // If existing is an attributeGroup, keep it (don't replace with element)
            // If new is an attributeGroup and existing is an element, don't replace
            // Essentially: first one wins when types differ
            return false;
        }

        // Calculate content score (children + attributes = actual structure)
        int existingContentScore = existing.Attributes.Count + existing.AllowedChildren.Count;
        int newContentScore = newElem.Attributes.Count + newElem.AllowedChildren.Count;

        // If existing has actual content (children/attributes) and new one doesn't,
        // NEVER replace - structure is more important than documentation
        if (existingContentScore > 0 && newContentScore == 0)
        {
            return false;
        }

        // If new has more content, prefer it
        if (newContentScore > existingContentScore)
        {
            return true;
        }

        // If content is equal, then consider documentation
        bool existingHasDoc = !string.IsNullOrEmpty(existing.Documentation);
        bool newHasDoc = !string.IsNullOrEmpty(newElem.Documentation);

        if (newContentScore == existingContentScore)
        {
            // If new has doc and existing doesn't, AND equal content, prefer new
            if (!existingHasDoc && newHasDoc)
                return true;

            // If both have doc or neither has doc, they're effectively equal - keep existing
        }

        // Keep existing by default
        return false;
    }

    /// <summary>
    /// Scans all XSD files in the directory and parses their structure.
    /// Pure logic - safe to run on background thread.
    /// </summary>
    static (System.Collections.Generic.Dictionary<string, XsdSchemaInfo> schemas,
            System.Collections.Generic.Dictionary<string, XsdElementInfo> elements,
            System.Collections.Generic.Dictionary<string, List<XsdElementInfo>> elementsByName,
            HashSet<string> attributes,
            System.Collections.Generic.Dictionary<string, List<string>> enums)
        ScanXsdSchemas(string rootPath)
    {
        var schemas = new ConcurrentDictionary<string, XsdSchemaInfo>();
        var allElementsList = new ConcurrentBag<XsdElementInfo>();
        var allComplexTypesList = new ConcurrentBag<XsdElementInfo>();
        var allGroups = new ConcurrentDictionary<string, XsdElementInfo>();
        var allAttributeGroups = new ConcurrentDictionary<string, XsdElementInfo>();
        var allAttributes = new ConcurrentBag<string>();
        var allEnums = new ConcurrentDictionary<string, List<string>>();

        if (!Directory.Exists(rootPath))
            return (new(), new(), new(), new(), new());

        var enumOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false
        };

        var xsdFiles = Directory.EnumerateFiles(rootPath, "*.xsd", enumOptions).ToList();

        Parallel.ForEach(xsdFiles, (xsdPath) =>
        {
            try
            {
                var schemaInfo = ParseXsdFile(xsdPath);
                schemas[xsdPath] = schemaInfo;

                // Collect all elements for later processing
                foreach (var elem in schemaInfo.Elements.Values)
                {
                    allElementsList.Add(elem);

                    foreach (var attr in elem.Attributes)
                    {
                        allAttributes.Add(attr.Name);
                    }
                }

                // Collect groups, attribute groups, and complex types
                foreach (var group in schemaInfo.Groups)
                {
                    allGroups[group.Key] = group.Value;
                }

                foreach (var attrGroup in schemaInfo.AttributeGroups)
                {
                    allAttributeGroups[attrGroup.Key] = attrGroup.Value;
                }

                foreach (var complexType in schemaInfo.ComplexTypes)
                {
                    allComplexTypesList.Add(complexType.Value);
                }

                foreach (var enumType in schemaInfo.SimpleTypeEnums)
                {
                    allEnums[enumType.Key] = enumType.Value;
                }
            }
            catch (Exception ex)
            {
                // Log but continue with other files
                Console.WriteLine($"Error parsing {xsdPath}: {ex.Message}");
            }
        });

        // Group complex types by name for context-aware resolution
        var complexTypesByName = allElementsList.Count > 0
           ? allComplexTypesList.GroupBy(t => t.Name).ToDictionary(g => g.Key, g => g.ToList())
           : new System.Collections.Generic.Dictionary<string, List<XsdElementInfo>>();

        // Group elements by name to detect duplicates
        var elementsByName = new System.Collections.Generic.Dictionary<string, List<XsdElementInfo>>();
        foreach (var elem in allElementsList)
        {
            if (!elementsByName.ContainsKey(elem.Name))
                elementsByName[elem.Name] = new List<XsdElementInfo>();
            elementsByName[elem.Name].Add(elem);
        }

        // Generate unique keys for all elements
        var allElements = new ConcurrentDictionary<string, XsdElementInfo>();

        foreach (var kvp in elementsByName)
        {
            var variants = kvp.Value;

            if (variants.Count == 1)
            {
                // Single element with this name - use name as key
                var elem = variants[0];
                elem.UniqueKey = elem.Name;
                allElements[elem.UniqueKey] = elem;
            }
            else
            {
                // Multiple elements with same name - need disambiguation
                // First, try to merge if they're truly duplicates (same source context)
                var uniqueVariants = new List<XsdElementInfo>();
                var seenContexts = new HashSet<string>();

                // Calculate score for each element to determine the "best" one
                // Priority:
                // 1. Top-level complexType definitions (Type == Name or Type is empty for actual content)
                // 2. Elements with actual content (attributes, children)
                // 3. Elements NOT inside a parent type (not nested references)
                // 4. Documentation (but should not override content)
                foreach (var elem in variants.OrderByDescending(e => CalculateElementScore(e)))
                {
                    var context = elem.SourceContext;
                    if (!seenContexts.Contains(context))
                    {
                        seenContexts.Add(context);
                        uniqueVariants.Add(elem);
                    }
                }

                // Replace the variants list with deduplicated list
                elementsByName[kvp.Key] = uniqueVariants;

                if (uniqueVariants.Count == 1)
                {
                    // After deduplication, only one remains
                    var elem = uniqueVariants[0];
                    elem.UniqueKey = elem.Name;
                    allElements[elem.UniqueKey] = elem;
                }
                else
                {
                    // Still multiple distinct variants - store each with unique key
                    foreach (var elem in uniqueVariants)
                    {
                        elem.UniqueKey = $"{elem.Name}@{elem.SourceContext}";
                        allElements[elem.UniqueKey] = elem;
                    }
                }
            }
        }

        // Resolve type references - copy attributes from complexTypes to elements that reference them
        ResolveReferences(allElements,
                         complexTypesByName,
                         new System.Collections.Generic.Dictionary<string, XsdElementInfo>(allGroups),
                         new System.Collections.Generic.Dictionary<string, XsdElementInfo>(allAttributeGroups));

        // Build parent-child relationships after resolution (so parents see expanded children)
        BuildParentChildRelationships(allElements);

        // DEBUG: Deep hierarchy diagnosis
        GD.Print("\n[Hierarchy Diagnosis]");
        if (complexTypesByName.TryGetValue("cues", out var ctCuesList))
            GD.Print($"CT 'cues' Children: [{string.Join(", ", ctCuesList[0].AllowedChildren)}]");
        else GD.Print("CT 'cues' NOT FOUND");

        if (allElements.TryGetValue("cues", out var elCues))
            GD.Print($"EL 'cues' Children: [{string.Join(", ", elCues.AllowedChildren)}] (Type={elCues.Type})");
        else GD.Print("EL 'cues' NOT FOUND");

        if (complexTypesByName.TryGetValue("cue", out var ctCueList))
            GD.Print($"CT 'cue' Children: [{string.Join(", ", ctCueList[0].AllowedChildren)}]");
        else GD.Print("CT 'cue' NOT FOUND");

        if (allElements.TryGetValue("cue", out var elCue))
        {
            GD.Print($"EL 'cue' Children: [{string.Join(", ", elCue.AllowedChildren)}] (Type={elCue.Type})");
            GD.Print($"EL 'cue' Parents: [{string.Join(", ", elCue.ParentElements)}]");
        }
        else GD.Print("EL 'cue' NOT FOUND");

        // DEBUG: Actions Diagnosis
        if (complexTypesByName.TryGetValue("actions", out var ctActionsList))
            foreach (var ct in ctActionsList)
                GD.Print($"CT 'actions' Children: [{string.Join(", ", ct.AllowedChildren)}] (Source: {ct.SourceFile})");
        else GD.Print("CT 'actions' NOT FOUND");

        if (allElements.TryGetValue("actions", out var elActions))
        {
            GD.Print($"EL 'actions' Children: [{string.Join(", ", elActions.AllowedChildren)}] (Type={elActions.Type}, Source: {elActions.SourceFile})");
            GD.Print($"EL 'actions' Attributes: [{string.Join(", ", elActions.Attributes.Select(a => a.Name))}]");
            GD.Print($"EL 'actions' Parents: [{string.Join(", ", elActions.ParentElements)}]");
        }
        else if (elementsByName.TryGetValue("actions", out var actionVariants))
        {
            GD.Print($"EL 'actions' Found in variants but not resolved unique key 'actions':");
            foreach (var v in actionVariants) GD.Print($"  - Variant Key: {v.UniqueKey}");
        }
        else GD.Print("EL 'actions' NOT FOUND");

        if (allGroups.TryGetValue("actions", out var grActions))
            GD.Print($"GR 'actions' Children: [{string.Join(", ", grActions.AllowedChildren)}] (Source: {grActions.SourceFile})");
        else GD.Print("GR 'actions' NOT FOUND");

        return (
            new System.Collections.Generic.Dictionary<string, XsdSchemaInfo>(schemas),
            new System.Collections.Generic.Dictionary<string, XsdElementInfo>(allElements),
            elementsByName,
            allAttributes.ToHashSet(),
            new System.Collections.Generic.Dictionary<string, List<string>>(allEnums)
        );
    }

    /// <summary>
    /// Calculates a score for element deduplication.
    /// Higher scores indicate more complete/authoritative definitions.
    /// </summary>
    static int CalculateElementScore(XsdElementInfo elem)
    {
        int score = 0;

        // Heavily favor top-level complexType definitions (Type == Name)
        // These are the "real" element definitions, not references
        if (elem.Type == elem.Name && !string.IsNullOrEmpty(elem.Type))
        {
            score += 100;
        }

        // Favor elements with actual content (attributes and children)
        // This is the most important indicator of a complete definition
        score += elem.Attributes.Count * 5;
        score += elem.AllowedChildren.Count * 5;

        // Penalize elements that are just references to other types
        // (they have a type but no actual content of their own)
        if (!string.IsNullOrEmpty(elem.Type) && elem.Type != elem.Name &&
            elem.Attributes.Count == 0 && elem.AllowedChildren.Count == 0)
        {
            score -= 50;
        }

        // Penalize elements inside parent types (nested references)
        if (!string.IsNullOrEmpty(elem.ParentTypeName))
        {
            score -= 30;
        }

        // Documentation is nice but shouldn't override structural content
        if (!string.IsNullOrEmpty(elem.Documentation))
        {
            score += 3;
        }

        return score;
    }

    /// <summary>
    /// Parses a single XSD file and extracts element/attribute definitions.
    /// </summary>
    static XsdSchemaInfo ParseXsdFile(string filePath)
    {
        var schemaInfo = new XsdSchemaInfo
        {
            FilePath = filePath
        };

        var doc = XDocument.Load(filePath);
        var root = doc.Root;

        if (root == null) return schemaInfo;

        schemaInfo.TargetNamespace = root.Attribute("targetNamespace")?.Value ?? "";

        // Parse imports
        foreach (var import in root.Elements(xs + "import"))
        {
            var schemaLocation = import.Attribute("schemaLocation")?.Value;
            if (!string.IsNullOrEmpty(schemaLocation))
                schemaInfo.Imports.Add(schemaLocation);
        }

        // Parse simple types (enums)
        foreach (var simpleType in root.Elements(xs + "simpleType"))
        {
            ParseSimpleType(simpleType, schemaInfo);
        }

        // Parse top-level elements
        foreach (var element in root.Elements(xs + "element"))
        {
            var elemInfo = ParseElement(element, filePath, schemaInfo);
            if (!string.IsNullOrEmpty(elemInfo.Name))
            {
                schemaInfo.Elements[elemInfo.Name] = elemInfo;
            }
        }

        // Parse complex types (which define element structures)
        foreach (var complexType in root.Elements(xs + "complexType"))
        {
            var typeName = complexType.Attribute("name")?.Value ?? "";
            if (string.IsNullOrEmpty(typeName)) continue;

            // Create a pseudo-element for the complex type
            var elemInfo = new XsdElementInfo
            {
                Name = typeName,
                Type = typeName,
                SourceFile = filePath
            };

            ParseComplexTypeContent(complexType, elemInfo, filePath, schemaInfo);
            schemaInfo.ComplexTypes[typeName] = elemInfo;
        }

        // Parse groups (xs:group) which contain reusable element definitions
        foreach (var group in root.Elements(xs + "group"))
        {
            var groupName = group.Attribute("name")?.Value ?? "";
            if (string.IsNullOrEmpty(groupName)) continue;

            // Create a pseudo-element for the group
            var groupElem = new XsdElementInfo
            {
                Name = groupName,
                Type = "group",
                SourceFile = filePath
            };

            // Parse documentation on the group itself
            var annotation = group.Element(xs + "annotation");
            var documentation = annotation?.Element(xs + "documentation");
            groupElem.Documentation = documentation?.Value?.Trim() ?? "";

            // Parse sequence/choice/all inside the group
            var sequence = group.Element(xs + "sequence");
            var choice = group.Element(xs + "choice");
            var all = group.Element(xs + "all");
            var childContainer = sequence ?? choice ?? all;

            if (childContainer != null)
            {
                ParseChildElements(childContainer, groupElem, filePath, schemaInfo);
            }

            schemaInfo.Groups[groupName] = groupElem;
        }

        // Parse attribute groups (xs:attributeGroup) which define reusable sets of attributes
        foreach (var attrGroup in root.Elements(xs + "attributeGroup"))
        {
            var groupName = attrGroup.Attribute("name")?.Value ?? "";
            if (string.IsNullOrEmpty(groupName)) continue;

            // Create a pseudo-element for the attribute group
            var groupElem = new XsdElementInfo
            {
                Name = groupName,
                Type = "attributeGroup",
                SourceFile = filePath
            };

            // Parse documentation
            var annotation = attrGroup.Element(xs + "annotation");
            var documentation = annotation?.Element(xs + "documentation");
            groupElem.Documentation = documentation?.Value?.Trim() ?? "";

            // Parse attributes in the group
            foreach (var attr in attrGroup.Elements(xs + "attribute"))
            {
                var attrInfo = ParseAttribute(attr);
                if (!string.IsNullOrEmpty(attrInfo.Name))
                {
                    groupElem.Attributes.Add(attrInfo);
                }
            }

            // Parse nested attributeGroup references
            foreach (var nestedGroup in attrGroup.Elements(xs + "attributeGroup"))
            {
                var refName = nestedGroup.Attribute("ref")?.Value;
                if (!string.IsNullOrEmpty(refName) && !groupElem.AttributeGroups.Contains(refName))
                {
                    groupElem.AttributeGroups.Add(refName);
                }
            }

            // Store in AttributeGroups dictionary
            schemaInfo.AttributeGroups[groupName] = groupElem;
        }

        return schemaInfo;
    }

    /// <summary>
    /// Parses a simpleType element, extracting enum restrictions if present.
    /// </summary>
    static void ParseSimpleType(XElement simpleType, XsdSchemaInfo schemaInfo)
    {
        var typeName = simpleType.Attribute("name")?.Value ?? "";
        if (string.IsNullOrEmpty(typeName)) return;

        var restriction = simpleType.Element(xs + "restriction");
        if (restriction == null) return;

        var enumValues = restriction.Elements(xs + "enumeration")
            .Select(e => e.Attribute("value")?.Value ?? "")
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();

        if (enumValues.Count > 0)
        {
            schemaInfo.SimpleTypeEnums[typeName] = enumValues;
        }
    }

    /// <summary>
    /// Parses an xs:element and returns its info.
    /// </summary>
    static XsdElementInfo ParseElement(XElement element, string sourceFile, XsdSchemaInfo schemaInfo)
    {
        var elemInfo = new XsdElementInfo
        {
            Name = element.Attribute("name")?.Value ?? element.Attribute("ref")?.Value ?? "",
            Type = element.Attribute("type")?.Value ?? "",
            SourceFile = sourceFile
        };

        // Parse minOccurs/maxOccurs
        var minOccurs = element.Attribute("minOccurs")?.Value;
        var maxOccurs = element.Attribute("maxOccurs")?.Value;

        elemInfo.MinOccurs = string.IsNullOrEmpty(minOccurs) ? 1 : int.Parse(minOccurs);
        elemInfo.MaxOccurs = maxOccurs == "unbounded" ? -1 :
                            (string.IsNullOrEmpty(maxOccurs) ? 1 : int.Parse(maxOccurs));

        // Parse documentation
        var annotation = element.Element(xs + "annotation");
        var documentation = annotation?.Element(xs + "documentation");
        elemInfo.Documentation = documentation?.Value?.Trim() ?? "";

        // Parse inline complexType
        var complexType = element.Element(xs + "complexType");
        if (complexType != null)
        {
            ParseComplexTypeContent(complexType, elemInfo, sourceFile, schemaInfo);
        }

        return elemInfo;
    }

    /// <summary>
    /// Parses the content of a complexType (attributes and child elements).
    /// </summary>
    static void ParseComplexTypeContent(XElement complexType, XsdElementInfo elemInfo, string sourceFile, XsdSchemaInfo schemaInfo)
    {
        // Parse attribute groups (e.g., <xs:attributeGroup ref="action" />)
        foreach (var attrGroup in complexType.Elements(xs + "attributeGroup"))
        {
            var groupRef = attrGroup.Attribute("ref")?.Value;
            if (!string.IsNullOrEmpty(groupRef) && !elemInfo.AttributeGroups.Contains(groupRef))
            {
                elemInfo.AttributeGroups.Add(groupRef);
            }
        }

        // Parse attributes
        foreach (var attr in complexType.Elements(xs + "attribute"))
        {
            var attrInfo = ParseAttribute(attr);
            if (!string.IsNullOrEmpty(attrInfo.Name))
            {
                elemInfo.Attributes.Add(attrInfo);
                if (elemInfo.Name.Contains("discovered_mission"))
                    GD.Print($"[XSD Debug] Added attribute '{attrInfo.Name}' to element '{elemInfo.Name}'");
            }
        }

        // Check for direct group reference (valid in XSD)
        // e.g. <xs:complexType name="actions"><xs:group ref="actions"/></xs:complexType>
        var directGroup = complexType.Element(xs + "group");
        if (directGroup != null)
        {
            var groupRef = directGroup.Attribute("ref")?.Value;
            if (!string.IsNullOrEmpty(groupRef))
            {
                var prefixedRef = "group:" + groupRef;
                if (!elemInfo.AllowedChildren.Contains(prefixedRef))
                {
                    elemInfo.AllowedChildren.Add(prefixedRef);
                }
            }
        }

        // Parse sequence/choice/all for child elements
        var sequence = complexType.Element(xs + "sequence");
        var choice = complexType.Element(xs + "choice");
        var all = complexType.Element(xs + "all");

        var childContainer = sequence ?? choice ?? all;
        if (childContainer != null)
        {
            ParseChildElements(childContainer, elemInfo, sourceFile, schemaInfo);
        }

        // Check for complexContent with extension
        var complexContent = complexType.Element(xs + "complexContent");
        if (complexContent != null)
        {
            var extension = complexContent.Element(xs + "extension");
            if (extension != null)
            {
                // Parse attribute groups from extension
                foreach (var attrGroup in extension.Elements(xs + "attributeGroup"))
                {
                    var groupRef = attrGroup.Attribute("ref")?.Value;
                    if (!string.IsNullOrEmpty(groupRef) && !elemInfo.AttributeGroups.Contains(groupRef))
                    {
                        elemInfo.AttributeGroups.Add(groupRef);
                    }
                }

                // Parse attributes from extension
                foreach (var attr in extension.Elements(xs + "attribute"))
                {
                    var attrInfo = ParseAttribute(attr);
                    if (!string.IsNullOrEmpty(attrInfo.Name))
                    {
                        elemInfo.Attributes.Add(attrInfo);
                    }
                }

                // Parse child elements from extension
                var extSequence = extension.Element(xs + "sequence");
                var extChoice = extension.Element(xs + "choice");
                var extAll = extension.Element(xs + "all");
                var extChildContainer = extSequence ?? extChoice ?? extAll;
                if (extChildContainer != null)
                {
                    ParseChildElements(extChildContainer, elemInfo, sourceFile, schemaInfo);
                }
            }
        }

        // Check for simpleContent with extension (for elements with text content + attributes)
        var simpleContent = complexType.Element(xs + "simpleContent");
        if (simpleContent != null)
        {
            var extension = simpleContent.Element(xs + "extension");
            if (extension != null)
            {
                foreach (var attr in extension.Elements(xs + "attribute"))
                {
                    var attrInfo = ParseAttribute(attr);
                    if (!string.IsNullOrEmpty(attrInfo.Name))
                    {
                        elemInfo.Attributes.Add(attrInfo);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Parses child elements from a sequence/choice/all container.
    /// Also adds inline element definitions to the schema.
    /// </summary>
    static void ParseChildElements(XElement container, XsdElementInfo parentElem, string sourceFile, XsdSchemaInfo schemaInfo)
    {
        foreach (var child in container.Elements())
        {
            if (child.Name == xs + "element")
            {
                var childName = child.Attribute("name")?.Value ?? child.Attribute("ref")?.Value ?? "";
                if (!string.IsNullOrEmpty(childName))
                {
                    // Add to allowed children
                    if (!parentElem.AllowedChildren.Contains(childName))
                    {
                        parentElem.AllowedChildren.Add(childName);
                    }

                    // If this is an inline element definition (has name attribute, not just ref),
                    // parse it fully and add to schema
                    var hasName = child.Attribute("name") != null;
                    if (childName == "actions")
                    {
                        GD.Print($"[XSD Debug] Parsing child 'actions'. hasName={hasName}, parent={parentElem.Name}");
                    }

                    if (hasName)
                    {
                        var inlineElemInfo = ParseElement(child, sourceFile, schemaInfo);
                        if (inlineElemInfo.Name.Contains("discovered_mission") || inlineElemInfo.Name == "actions")
                            GD.Print($"[XSD Debug] ParseChildElements: Parsed inline element '{inlineElemInfo.Name}' with {inlineElemInfo.Attributes.Count} attributes. Adding to schema.");

                        if (!string.IsNullOrEmpty(inlineElemInfo.Name))
                        {
                            // Add or replace if the new one has more info (documentation, attributes, etc.)
                            if (!schemaInfo.Elements.TryGetValue(childName, out var existing) ||
                                ShouldReplaceElement(existing, inlineElemInfo))
                            {
                                schemaInfo.Elements[inlineElemInfo.Name] = inlineElemInfo;
                                if (inlineElemInfo.Name == "actions")
                                    GD.Print($"[XSD Debug] Added/replaced element '{inlineElemInfo.Name}' in schema.");
                            }
                            else
                            {
                                if (inlineElemInfo.Name == "actions")
                                    GD.Print($"[XSD Debug] Kept existing element '{childName}' (existing has {existing.Attributes.Count} attrs, new has {inlineElemInfo.Attributes.Count})");
                            }
                        }
                    }
                }
            }
            else if (child.Name == xs + "sequence" || child.Name == xs + "choice" || child.Name == xs + "all")
            {
                // Recursively parse nested containers
                ParseChildElements(child, parentElem, sourceFile, schemaInfo);
            }
            else if (child.Name == xs + "group")
            {
                // Handle group references: <xs:group ref="groupName" />
                var groupRef = child.Attribute("ref")?.Value;
                if (!string.IsNullOrEmpty(groupRef))
                {
                    // Add the group name to AllowedChildren with a prefix to distinguish from elements
                    // It will be expanded to actual children in ResolveReferences
                    var prefixedRef = "group:" + groupRef;
                    if (!parentElem.AllowedChildren.Contains(prefixedRef))
                    {
                        parentElem.AllowedChildren.Add(prefixedRef);
                    }
                }
            }
            else if (child.Name == xs + "any")
            {
                // xs:any allows any element - mark with special value
                if (!parentElem.AllowedChildren.Contains("*"))
                {
                    parentElem.AllowedChildren.Add("*");
                }
            }
        }
    }

    /// <summary>
    /// Parses an xs:attribute element.
    /// </summary>
    static XsdAttributeInfo ParseAttribute(XElement attr)
    {
        var attrInfo = new XsdAttributeInfo
        {
            Name = attr.Attribute("name")?.Value ?? attr.Attribute("ref")?.Value ?? "",
            Type = attr.Attribute("type")?.Value ?? "xs:string",
            IsRequired = attr.Attribute("use")?.Value == "required",
            DefaultValue = attr.Attribute("default")?.Value ?? ""
        };

        // Parse documentation
        var annotation = attr.Element(xs + "annotation");
        var documentation = annotation?.Element(xs + "documentation");
        attrInfo.Documentation = documentation?.Value?.Trim() ?? "";

        // Check for inline simpleType with enum restriction
        var simpleType = attr.Element(xs + "simpleType");
        var restriction = simpleType?.Element(xs + "restriction");
        if (restriction != null)
        {
            attrInfo.EnumValues = [.. restriction.Elements(xs + "enumeration")
                .Select(e => e.Attribute("value")?.Value ?? "")
                .Where(v => !string.IsNullOrEmpty(v))];
        }

        return attrInfo;
    }

    /// <summary>
    /// Builds parent-child relationships by iterating through all elements.
    /// Note: allElements is keyed by UniqueKey, but AllowedChildren uses base names.
    /// </summary>
    static void BuildParentChildRelationships(ConcurrentDictionary<string, XsdElementInfo> allElements)
    {
        // Build a name -> element mapping for lookup (since AllowedChildren uses base names)
        var elementsByBaseName = new System.Collections.Generic.Dictionary<string, XsdElementInfo>();
        foreach (var elem in allElements.Values)
        {
            // Store by base name (not UniqueKey) - if multiple exist, the first one wins for parent tracking
            if (!elementsByBaseName.ContainsKey(elem.Name))
            {
                elementsByBaseName[elem.Name] = elem;
            }
        }

        foreach (var elem in allElements.Values)
        {
            foreach (var childName in elem.AllowedChildren)
            {
                if (childName == "*") continue; // Skip wildcard

                // Look up by base name (AllowedChildren uses base names, not UniqueKeys)
                if (elementsByBaseName.TryGetValue(childName, out var childElem))
                {
                    // Add parent by base name (not UniqueKey) for consistency
                    if (!childElem.ParentElements.Contains(elem.Name))
                    {
                        childElem.ParentElements.Add(elem.Name);
                    }
                }
            }
        }
    }

    /// <summary>
    /// ResolveReferences resolves type references, attribute groups, and group references.
    /// It uses separate dictionaries for type definitions, groups and attributeGroups.
    /// </summary>
    static void ResolveReferences(ConcurrentDictionary<string, XsdElementInfo> allElements,
                                 System.Collections.Generic.Dictionary<string, List<XsdElementInfo>> complexTypesByName,
                                 System.Collections.Generic.Dictionary<string, XsdElementInfo> groups,
                                 System.Collections.Generic.Dictionary<string, XsdElementInfo> attributeGroups)
    {
        // Phase 1: Resolve Groups and AttributeGroups within the ComplexTypes themselves
        // We iterate ALL variants of complex types
        foreach (var variantList in complexTypesByName.Values)
        {
            foreach (var typeInfo in variantList)
            {
                // Resolve attribute groups in complex types
                foreach (var groupName in typeInfo.AttributeGroups.ToList())
                {
                    ResolveAttributeGroupRecursive(typeInfo, groupName, attributeGroups, new HashSet<string>());
                }

                // Iteratively resolve group references in complex types (to handle nested groups)
                bool groupsChanged;
                do
                {
                    groupsChanged = false;
                    var groupsToExpand = new List<string>();

                    // Identify groups to expand
                    foreach (var childName in typeInfo.AllowedChildren.ToList())
                    {
                        if (childName.StartsWith("group:"))
                        {
                            groupsToExpand.Add(childName);
                        }
                    }

                    // Expand them
                    foreach (var prefixedName in groupsToExpand)
                    {
                        typeInfo.AllowedChildren.Remove(prefixedName);
                        var groupName = prefixedName.Substring(6); // Remove "group:" prefix

                        if (groups.TryGetValue(groupName, out var groupElem))
                        {
                            groupsChanged = true;
                            foreach (var childName in groupElem.AllowedChildren)
                            {
                                if (!typeInfo.AllowedChildren.Contains(childName))
                                {
                                    typeInfo.AllowedChildren.Add(childName);
                                }
                            }
                        }
                    }
                } while (groupsChanged);
            }

            // Phase 2: Resolve Elements (Type References, then Groups/AttrGroups)
            foreach (var elem in allElements.Values)
            {
                // If element has a type reference, look it up in ComplexTypes
                if (!string.IsNullOrEmpty(elem.Type))
                {
                    // Remove xs: prefix if present
                    var typeName = elem.Type;
                    if (typeName.StartsWith("xs:") || typeName.StartsWith("xsd:"))
                        goto ProcessAttributeGroups; // Built-in types like xs:string don't have attributes to copy

                    // Look up in ComplexTypes using context-aware resolution
                    var typeInfo = FindBestTypeMatch(typeName, elem.SourceFile, complexTypesByName);
                    if (typeInfo != null)
                    {
                        // Copy attributes from the type to the element if element has none
                        if (elem.Attributes.Count == 0 && typeInfo.Attributes.Count > 0)
                        {
                            foreach (var attr in typeInfo.Attributes)
                            {
                                elem.Attributes.Add(attr);
                            }
                        }

                        // Copy allowed children if element has none
                        if (elem.AllowedChildren.Count == 0 && typeInfo.AllowedChildren.Count > 0)
                        {
                            foreach (var child in typeInfo.AllowedChildren)
                            {
                                if (!elem.AllowedChildren.Contains(child))
                                    elem.AllowedChildren.Add(child);
                            }
                        }

                        // Copy attribute groups if element has none
                        if (elem.AttributeGroups.Count == 0 && typeInfo.AttributeGroups.Count > 0)
                        {
                            foreach (var group in typeInfo.AttributeGroups)
                            {
                                if (!elem.AttributeGroups.Contains(group))
                                    elem.AttributeGroups.Add(group);
                            }
                        }

                        // Copy documentation if element has none
                        if (string.IsNullOrEmpty(elem.Documentation) && !string.IsNullOrEmpty(typeInfo.Documentation))
                        {
                            elem.Documentation = typeInfo.Documentation;
                        }
                    }
                }

            ProcessAttributeGroups:
                // Also resolve attribute groups - copy attributes from referenced groups
                // Look up in attributeGroups dictionary
                foreach (var groupName in elem.AttributeGroups.ToList())
                {
                    ResolveAttributeGroupRecursive(elem, groupName, attributeGroups, new HashSet<string>());
                }
            }

            // Second pass: Resolve xs:group references in AllowedChildren
            foreach (var elem in allElements.Values)
            {
                // Debug logging for cue
                bool isCue = elem.Name == "cue";
                if (isCue)
                {
                    GD.Print($"[ResolveReferences] Processing 'cue' element:");
                    GD.Print($"  - Type: {elem.Type}");
                    GD.Print($"  - AllowedChildren before: [{string.Join(", ", elem.AllowedChildren)}]");
                }

                // Iteratively expand group references (nested groups)
                bool elemGroupsChanged;
                do
                {
                    elemGroupsChanged = false;
                    var groupsToExpand = new List<string>();

                    foreach (var childName in elem.AllowedChildren.ToList())
                    {
                        if (childName.StartsWith("group:"))
                        {
                            groupsToExpand.Add(childName);
                            if (isCue) GD.Print($"  - Found group reference: {childName}");
                        }
                    }

                    foreach (var prefixedName in groupsToExpand)
                    {
                        elem.AllowedChildren.Remove(prefixedName);
                        var groupName = prefixedName.Substring(6);

                        if (groups.TryGetValue(groupName, out var groupElem))
                        {
                            if (isCue) GD.Print($"  - Expanding group '{groupName}' with {groupElem.AllowedChildren.Count} children");
                            elemGroupsChanged = true;

                            foreach (var childName in groupElem.AllowedChildren)
                            {
                                if (!elem.AllowedChildren.Contains(childName))
                                {
                                    elem.AllowedChildren.Add(childName);
                                }
                            }
                        }
                    }
                } while (elemGroupsChanged);

                if (isCue)
                {
                    GD.Print($"  - AllowedChildren after: [{string.Join(", ", elem.AllowedChildren)}]");
                }
            }
        }

    }

    /// <summary>
    /// Finds the best matching complexType for a given name, prioritizing definitions
    /// from the same file or directory as the referencing element.
    /// </summary>
    static XsdElementInfo? FindBestTypeMatch(string typeName, string elementSourceFile, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<XsdElementInfo>> complexTypesByName)
    {
        if (!complexTypesByName.TryGetValue(typeName, out var variants))
            return null;

        if (variants.Count == 1)
            return variants[0];

        // If multiple variants, prioritize context

        // 1. Exact Same File
        var sameFile = variants.FirstOrDefault(v => v.SourceFile == elementSourceFile);
        if (sameFile != null) return sameFile;

        // 2. Same Directory (e.g. libraries/md.xsd and libraries/common.xsd)
        if (!string.IsNullOrEmpty(elementSourceFile))
        {
            try
            {
                var elementDir = System.IO.Path.GetDirectoryName(elementSourceFile);
                var sameDir = variants.FirstOrDefault(v =>
                    !string.IsNullOrEmpty(v.SourceFile) &&
                    System.IO.Path.GetDirectoryName(v.SourceFile) == elementDir);
                if (sameDir != null) return sameDir;
            }
            catch { } // Ignore path errors
        }

        // 3. Score-based fallback (e.g. prefer ones with content)
        return variants.OrderByDescending(v => CalculateElementScore(v)).FirstOrDefault();
    }

    /// <summary>
    /// Recursively resolves an attribute group, copying its attributes to the element.
    /// Handles nested attribute group references.
    /// </summary>
    static void ResolveAttributeGroupRecursive(XsdElementInfo elem, string groupName, System.Collections.Generic.Dictionary<string, XsdElementInfo> attributeGroups, HashSet<string> visited)
    {
        // Prevent infinite loops
        if (visited.Contains(groupName))
            return;
        visited.Add(groupName);

        if (attributeGroups.TryGetValue(groupName, out var groupInfo))
        {
            // Copy attributes from the group if they don't already exist
            foreach (var attr in groupInfo.Attributes)
            {
                if (!elem.Attributes.Any(a => a.Name == attr.Name))
                {
                    elem.Attributes.Add(attr);
                }
            }

            // Recursively resolve nested attribute groups
            foreach (var nestedGroupName in groupInfo.AttributeGroups)
            {
                ResolveAttributeGroupRecursive(elem, nestedGroupName, attributeGroups, visited);
            }
        }
    }


    // =========================================================================
    // XML USAGE SCANNING
    // =========================================================================

    /// <summary>
    /// Handles the UI/Main Thread coordination for XML usage scanning.
    /// </summary>
    private async Task RunXmlUsageScanAsync()
    {
        GD.Print($"[XML Usage Scanner] Starting XML file scan in: {SearchDirectory}");

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string path = SearchDirectory;

            // Get current file modification times for cache validation
            var currentFileTimes = await Task.Run(() => GetFileModificationTimes(path, "*.xml"));

            // Try to load from cache first
            if (UseCache)
            {
                var cachedData = LoadXmlUsageCache(currentFileTimes);
                if (cachedData != null)
                {
                    sw.Stop();
                    ElementUsageStats = ConvertFromCachedUsageStats(cachedData.ElementUsageStats);
                    TotalXmlFilesScanned = cachedData.TotalXmlFilesScanned;

                    GD.Print($"\n--- XML USAGE LOADED FROM CACHE ({sw.ElapsedMilliseconds}ms) ---");
                    GD.Print($"Scanned {TotalXmlFilesScanned} XML files");
                    GD.Print($"Found {ElementUsageStats.Count} unique element names in use");
                    return;
                }
            }

            // Cache miss or disabled - do full scan
            var (usageStats, fileCount) = await Task.Run(() => ScanXmlUsage(path));

            sw.Stop();

            ElementUsageStats = usageStats;
            TotalXmlFilesScanned = fileCount;

            // Save to cache for next time
            if (UseCache)
            {
                SaveXmlUsageCache(usageStats, fileCount, currentFileTimes);
            }

            GD.Print($"\n--- XML USAGE SCAN COMPLETE ({sw.ElapsedMilliseconds}ms) ---");
            GD.Print($"Scanned {TotalXmlFilesScanned} XML files");
            GD.Print($"Found {ElementUsageStats.Count} unique element names in use");

            // Show top 10 most used elements
            var topElements = ElementUsageStats.Values
                .OrderByDescending(e => e.TotalUsageCount)
                .Take(10)
                .ToList();

            GD.Print($"\n--- TOP 10 MOST USED ELEMENTS ---");
            foreach (var elem in topElements)
            {
                GD.Print($"  {elem.ElementName}: {elem.TotalUsageCount} usages in {elem.FilesUsedIn.Count} files");
            }

            // Show elements defined in XSD but never used
            var unusedElements = AllElements.Keys
                .Where(name => !ElementUsageStats.ContainsKey(name))
                .Take(20)
                .ToList();

            if (unusedElements.Count > 0)
            {
                GD.Print("\n--- ELEMENTS DEFINED BUT NOT USED (first 20) ---");
                GD.Print($"  {string.Join(", ", unusedElements)}");
            }

            // Show elements used but not defined in XSD
            var undefinedElements = ElementUsageStats.Keys
                .Where(name => !AllElements.ContainsKey(name))
                .Take(20)
                .ToList();

            if (undefinedElements.Count > 0)
            {
                GD.Print($"\n--- ELEMENTS USED BUT NOT IN XSD (first 20) ---");
                GD.Print($"  {string.Join(", ", undefinedElements)}");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[XML Usage Scanner] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Scans all XML files and collects element usage statistics.
    /// Pure logic - safe to run on background thread.
    /// </summary>
    static (System.Collections.Generic.Dictionary<string, XmlElementUsageStats> stats, int fileCount)
        ScanXmlUsage(string rootPath)
    {
        var usageStats = new ConcurrentDictionary<string, XmlElementUsageStats>();
        int fileCount = 0;

        if (!Directory.Exists(rootPath))
            return (new(), 0);

        var enumOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false
        };

        var xmlFiles = Directory.EnumerateFiles(rootPath, "*.xml", enumOptions).ToList();
        fileCount = xmlFiles.Count;

        Parallel.ForEach(xmlFiles, (xmlPath) =>
        {
            try
            {
                ParseXmlFileForUsage(xmlPath, usageStats);
            }
            catch (Exception ex)
            {
                // Log but continue with other files
                Console.WriteLine($"Error parsing {xmlPath}: {ex.Message}");
            }
        });

        return (
            new System.Collections.Generic.Dictionary<string, XmlElementUsageStats>(usageStats),
            fileCount
        );
    }

    /// <summary>
    /// Parses a single XML file and records element usage.
    /// Uses XmlReader for line number tracking.
    /// </summary>
    static void ParseXmlFileForUsage(string filePath, ConcurrentDictionary<string, XmlElementUsageStats> usageStats)
    {
        // Read file content to get line information
        var lines = File.ReadAllLines(filePath);

        // Use regex to find XML elements with line numbers
        // This approach gives us accurate line numbers
        var elementPattern = new Regex(@"<([a-zA-Z_][a-zA-Z0-9_\-\.]*)\s*([^>]*)(?:/>|>)", RegexOptions.Compiled);
        var attributePattern = new Regex(@"([a-zA-Z_][a-zA-Z0-9_\-\.]*)\s*=\s*""([^""]*)""", RegexOptions.Compiled);

        // Track parent elements using a stack
        var parentStack = new Stack<string>();

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            int lineNumber = lineIndex + 1; // 1-based line numbers

            // Check for closing tags to pop parent stack
            var closingTagPattern = new Regex(@"</([a-zA-Z_][a-zA-Z0-9_\-\.]*)>");
            foreach (Match closeMatch in closingTagPattern.Matches(line))
            {
                string closingName = closeMatch.Groups[1].Value;
                if (parentStack.Count > 0 && parentStack.Peek() == closingName)
                {
                    parentStack.Pop();
                }
            }

            // Find element usages
            foreach (Match match in elementPattern.Matches(line))
            {
                string elementName = match.Groups[1].Value;
                string attributesPart = match.Groups[2].Value;
                bool isSelfClosing = match.Value.EndsWith("/>");

                // Skip XML declarations and comments
                if (elementName.StartsWith("?") || elementName.StartsWith("!"))
                    continue;

                // Get or create usage stats for this element
                var stats = usageStats.GetOrAdd(elementName, _ => new XmlElementUsageStats
                {
                    ElementName = elementName
                });

                // Create usage record
                var usage = new XmlElementUsage
                {
                    FilePath = filePath,
                    LineNumber = lineNumber,
                    ParentElement = parentStack.Count > 0 ? parentStack.Peek() : ""
                };

                // Parse attributes
                foreach (Match attrMatch in attributePattern.Matches(attributesPart))
                {
                    string attrName = attrMatch.Groups[1].Value;
                    string attrValue = attrMatch.Groups[2].Value;
                    usage.Attributes[attrName] = attrValue;

                    // Track observed attribute values (limit to prevent memory explosion)
                    lock (stats.ObservedAttributeValues)
                    {
                        if (!stats.ObservedAttributeValues.TryGetValue(attrName, out HashSet<string>? value))
                        {
                            value = [];
                            stats.ObservedAttributeValues[attrName] = value;
                        }
                        if (value.Count < 100) // Limit unique values stored
                        {
                            value.Add(attrValue);
                        }
                    }
                }

                // Update stats (thread-safe)
                lock (stats)
                {
                    stats.TotalUsageCount++;
                    stats.FilesUsedIn.Add(filePath);
                    if (!string.IsNullOrEmpty(usage.ParentElement))
                    {
                        stats.ObservedParents.Add(usage.ParentElement);
                    }

                    // Only store first 1000 usages to prevent memory issues
                    if (stats.Usages.Count < 1000)
                    {
                        stats.Usages.Add(usage);
                    }
                }

                // Push to parent stack if not self-closing
                if (!isSelfClosing)
                {
                    parentStack.Push(elementName);
                }
            }
        }
    }
}