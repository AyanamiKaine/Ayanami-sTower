using Godot;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public override string ToString() => $"{Name} (attrs: {Attributes.Count}, children: {AllowedChildren.Count})";
}

/// <summary>
/// Represents a complete XSD schema with all its definitions
/// </summary>
public class XsdSchemaInfo
{
    public string FilePath { get; set; } = "";
    public string TargetNamespace { get; set; } = "";
    public System.Collections.Generic.Dictionary<string, XsdElementInfo> Elements { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, XsdAttributeInfo> GlobalAttributes { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, List<string>> SimpleTypeEnums { get; set; } = new(); // Type name -> enum values
    public List<string> Imports { get; set; } = new(); // Other schemas this one imports
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

    /// <summary>All unique element definitions across all schemas, keyed by element name</summary>
    public System.Collections.Generic.Dictionary<string, XsdElementInfo> AllElements { get; private set; } = new();

    /// <summary>All unique attribute names found across all schemas</summary>
    public HashSet<string> AllAttributeNames { get; private set; } = new();

    /// <summary>All simple type enumerations (e.g., allowed string values)</summary>
    public System.Collections.Generic.Dictionary<string, List<string>> AllEnumTypes { get; private set; } = new();

    // --- XML Usage Data ---
    /// <summary>Usage statistics for each element, keyed by element name</summary>
    public System.Collections.Generic.Dictionary<string, XmlElementUsageStats> ElementUsageStats { get; private set; } = new();

    /// <summary>Total number of XML files scanned</summary>
    public int TotalXmlFilesScanned { get; private set; } = 0;

    public override void _Ready()
    {
        base._Ready();

        // We discard the Task (_) because _Ready cannot be awaited.
        // This kicks off the process and lets Godot continue immediately.
        _ = RunDatabaseScanAsync();
        _ = RunXsdScanAsync();
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

            var (schemas, elements, attributes, enums) = await Task.Run(() => ScanXsdSchemas(path));

            sw.Stop();

            ParsedSchemas = schemas;
            AllElements = elements;
            AllAttributeNames = attributes;
            AllEnumTypes = enums;

            GD.Print($"\n--- XSD SCAN COMPLETE ({sw.ElapsedMilliseconds}ms) ---");
            GD.Print($"Found {ParsedSchemas.Count} schema files");
            GD.Print($"Found {AllElements.Count} unique elements");
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
    /// Determines if newElement should replace existingElement based on completeness.
    /// Prefers elements with documentation, more attributes, and more allowed children.
    /// </summary>
    static bool ShouldReplaceElement(XsdElementInfo existing, XsdElementInfo newElem)
    {
        // If existing has no documentation but new one does, replace
        bool existingHasDoc = !string.IsNullOrEmpty(existing.Documentation);
        bool newHasDoc = !string.IsNullOrEmpty(newElem.Documentation);

        if (!existingHasDoc && newHasDoc)
            return true;

        // If both have or both lack documentation, prefer the one with more info
        if (existingHasDoc == newHasDoc)
        {
            // Calculate "completeness score"
            int existingScore = existing.Attributes.Count + existing.AllowedChildren.Count +
                               existing.Attributes.Count(a => !string.IsNullOrEmpty(a.Documentation));
            int newScore = newElem.Attributes.Count + newElem.AllowedChildren.Count +
                          newElem.Attributes.Count(a => !string.IsNullOrEmpty(a.Documentation));

            return newScore > existingScore;
        }

        // Existing has doc, new doesn't - keep existing
        return false;
    }

    /// <summary>
    /// Scans all XSD files in the directory and parses their structure.
    /// Pure logic - safe to run on background thread.
    /// </summary>
    static (System.Collections.Generic.Dictionary<string, XsdSchemaInfo> schemas,
            System.Collections.Generic.Dictionary<string, XsdElementInfo> elements,
            HashSet<string> attributes,
            System.Collections.Generic.Dictionary<string, List<string>> enums)
        ScanXsdSchemas(string rootPath)
    {
        var schemas = new ConcurrentDictionary<string, XsdSchemaInfo>();
        var allElements = new ConcurrentDictionary<string, XsdElementInfo>();
        var allAttributes = new ConcurrentBag<string>();
        var allEnums = new ConcurrentDictionary<string, List<string>>();

        if (!Directory.Exists(rootPath))
            return (new(), new(), new(), new());

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

                // Merge into global collections
                foreach (var elem in schemaInfo.Elements)
                {
                    // Prefer elements with documentation, more attributes, or more children
                    allElements.AddOrUpdate(
                        elem.Key,
                        elem.Value,
                        (key, existing) => ShouldReplaceElement(existing, elem.Value) ? elem.Value : existing
                    );

                    foreach (var attr in elem.Value.Attributes)
                    {
                        allAttributes.Add(attr.Name);
                    }
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

        // Build parent-child relationships after all elements are parsed
        BuildParentChildRelationships(allElements);

        // Resolve type references - copy attributes from complexTypes to elements that reference them
        ResolveTypeReferences(allElements);

        return (
            new System.Collections.Generic.Dictionary<string, XsdSchemaInfo>(schemas),
            new System.Collections.Generic.Dictionary<string, XsdElementInfo>(allElements),
            allAttributes.ToHashSet(),
            new System.Collections.Generic.Dictionary<string, List<string>>(allEnums)
        );
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
            schemaInfo.Elements[typeName] = elemInfo;
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

            schemaInfo.Elements[groupName] = groupElem;
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

            schemaInfo.Elements[groupName] = groupElem;
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
                    if (hasName)
                    {
                        var inlineElemInfo = ParseElement(child, sourceFile, schemaInfo);
                        if (!string.IsNullOrEmpty(inlineElemInfo.Name))
                        {
                            // Add or replace if the new one has more info (documentation, attributes, etc.)
                            if (!schemaInfo.Elements.TryGetValue(childName, out var existing) ||
                                ShouldReplaceElement(existing, inlineElemInfo))
                            {
                                schemaInfo.Elements[inlineElemInfo.Name] = inlineElemInfo;
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
    /// </summary>
    static void BuildParentChildRelationships(ConcurrentDictionary<string, XsdElementInfo> allElements)
    {
        foreach (var elem in allElements.Values)
        {
            foreach (var childName in elem.AllowedChildren)
            {
                if (childName == "*") continue; // Skip wildcard

                if (allElements.TryGetValue(childName, out var childElem))
                {
                    if (!childElem.ParentElements.Contains(elem.Name))
                    {
                        childElem.ParentElements.Add(elem.Name);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Resolves type references by copying attributes, children, and other info from
    /// referenced complexTypes/groups to elements that reference them via type attribute.
    /// </summary>
    static void ResolveTypeReferences(ConcurrentDictionary<string, XsdElementInfo> allElements)
    {
        foreach (var elem in allElements.Values)
        {
            // Skip if this element is itself a type definition (no need to resolve)
            if (elem.Type == elem.Name || elem.Type == "group")
                continue;

            // If element has a type reference, look it up
            if (!string.IsNullOrEmpty(elem.Type))
            {
                // Remove xs: prefix if present
                var typeName = elem.Type;
                if (typeName.StartsWith("xs:") || typeName.StartsWith("xsd:"))
                    continue; // Built-in types like xs:string don't have attributes to copy

                if (allElements.TryGetValue(typeName, out var typeInfo))
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

            // Also resolve attribute groups - copy attributes from referenced groups
            foreach (var groupName in elem.AttributeGroups.ToList())
            {
                ResolveAttributeGroupRecursive(elem, groupName, allElements, new HashSet<string>());
            }
        }
    }

    /// <summary>
    /// Recursively resolves an attribute group, copying its attributes to the element.
    /// Handles nested attribute group references.
    /// </summary>
    static void ResolveAttributeGroupRecursive(XsdElementInfo elem, string groupName, ConcurrentDictionary<string, XsdElementInfo> allElements, HashSet<string> visited)
    {
        // Prevent infinite loops
        if (visited.Contains(groupName))
            return;
        visited.Add(groupName);

        if (allElements.TryGetValue(groupName, out var groupInfo))
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
                ResolveAttributeGroupRecursive(elem, nestedGroupName, allElements, visited);
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

            var (usageStats, fileCount) = await Task.Run(() => ScanXmlUsage(path));

            sw.Stop();

            ElementUsageStats = usageStats;
            TotalXmlFilesScanned = fileCount;

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