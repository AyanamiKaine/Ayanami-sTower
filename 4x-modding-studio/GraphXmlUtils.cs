using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

/// <summary>
/// Structure to represent a node in the graph
/// </summary>
public struct GraphNodeData
{
    /// <summary>
    /// XML element name (e.g., "cues", "cue", "conditions")
    /// </summary>
    public string ElementName;  // XML element name (e.g., "cues", "cue", "conditions")
    /// <summary>
    /// XML attributes
    /// </summary>
    public Dictionary<string, string> Attributes;  // XML attributes
    /// <summary>
    /// Child nodes
    /// </summary>
    public List<GraphNodeData> Children;  // Child nodes
}
/// <summary>
/// Utility class for working with graph XML data
/// </summary>
public partial class GraphXmlUtils : RefCounted
{
    // List of all possible node types (scene names)
    private static readonly string[] AllNodeTypes = new string[]
    {
        "Mission", "Cues", "Cue", "Conditions", "Actions", "Debug Text",
        "Delay", "Event Happened", "Set Value", "Show Help", "Write To Logbook",
        "Check Any", "Check Value"
    };

    /// <summary>
    /// XML representation of the graph 
    /// </summary>
    /// <param name="graphEdit"></param>
    /// <returns></returns>
    public static string GetGraphXmlString(GraphEdit graphEdit)
    {
        XElement element = GenerateXElement(graphEdit);
        return element.ToString();
    }

    private static XElement GenerateXElement(GraphEdit graphEdit)
    {
        if (graphEdit == null) return new XElement("Error", "Null Graph");

        // 1. Map connections to build the adjacency list (Parent -> Children)
        var connections = graphEdit.GetConnectionList();
        var adjacencyList = new Dictionary<string, List<string>>();
        var nodesThatHaveParents = new HashSet<string>();

        foreach (var conn in connections)
        {
            string fromNode = (string)conn["from_node"];
            string toNode = (string)conn["to_node"];

            if (!adjacencyList.ContainsKey(fromNode))
                adjacencyList[fromNode] = [];

            adjacencyList[fromNode].Add(toNode);
            nodesThatHaveParents.Add(toNode);
        }

        // 2. Cache all GraphNode instances for easy lookup
        var nodeLookup = new Dictionary<string, GraphNode>();
        foreach (var child in graphEdit.GetChildren())
        {
            if (child is GraphNode node) nodeLookup[node.Name] = node;
        }

        // 3. Identify Root Node (first node with no incoming connections)
        var rootNodes = nodeLookup.Values
            .Where(n => !nodesThatHaveParents.Contains(n.Name));

        // 4. GET THE FIRST ROOT NODE
        // We take only the first root node (spawned first)
        var firstRoot = rootNodes.FirstOrDefault();

        var xmlRoot = new XElement("mdscript");

        // Add XML namespace declarations properly
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        xmlRoot.Add(new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName));
        xmlRoot.Add(new XAttribute(xsi + "noNamespaceSchemaLocation", "md.xsd"));

        var visited = new HashSet<string>();

        // 5. Build the tree from the first root only
        // If the root is an mdscript node, use its attributes and children directly
        if (firstRoot != null)
        {
            // Add attributes from the root mdscript node to the outer mdscript element
            foreach (var attr in firstRoot.Get("xml_attributes").AsGodotDictionary())
            {
                xmlRoot.Add(new XAttribute(attr.Key.AsString(), attr.Value.AsString()));
            }

            visited.Add(firstRoot.Name);

            // Add only the children of the mdscript, not the mdscript itself
            if (adjacencyList.ContainsKey(firstRoot.Name))
            {
                var childNames = adjacencyList[firstRoot.Name];
                var childNodes = new List<GraphNode>();

                foreach (var name in childNames)
                {
                    if (nodeLookup.TryGetValue(name, out GraphNode? child))
                    {
                        childNodes.Add(child);
                    }
                }

                var sortedChildren = childNodes
                    .OrderBy(n => n.PositionOffset.Y)
                    .ThenBy(n => n.PositionOffset.X);

                foreach (var childNode in sortedChildren)
                {
                    xmlRoot.Add(BuildNodeRecursive(childNode, adjacencyList, nodeLookup, visited));
                }
            }
        }

        return xmlRoot;
    }

    private static XElement BuildNodeRecursive(
        GraphNode currentNode,
        Dictionary<string, List<string>> adjacencyList,
        Dictionary<string, GraphNode> nodeLookup,
        HashSet<string> visited)
    {
        // Cycle Detection: Prevent infinite recursion if the graph loops back on itself
        if (visited.Contains(currentNode.Name))
            return new XElement("Cycle", new XAttribute("ref", currentNode.Name));

        visited.Add(currentNode.Name);

        var xNode = new XElement(currentNode.Get("xml_element_name").AsString());

        foreach (var xmlAttribute in currentNode.Get("xml_attributes").AsGodotDictionary())
        {
            xNode.Add(new XAttribute(xmlAttribute.Key.AsString(), xmlAttribute.Value.AsString()));
        }

        // Check if this node has children
        if (adjacencyList.ContainsKey(currentNode.Name))
        {
            // Get the list of child names
            var childNames = adjacencyList[currentNode.Name];

            // Map names to actual GraphNode objects for sorting
            var childNodes = new List<GraphNode>();
            foreach (var name in childNames)
            {
                if (nodeLookup.TryGetValue(name, out GraphNode? child))
                {
                    childNodes.Add(child);
                }
            }

            // 6. SORT CHILD NODES
            // Before adding them to XML, sort them visually (Top-to-Bottom)
            var sortedChildren = childNodes
                .OrderBy(n => n.PositionOffset.Y)
                .ThenBy(n => n.PositionOffset.X);

            foreach (var childNode in sortedChildren)
            {
                xNode.Add(BuildNodeRecursive(childNode, adjacencyList, nodeLookup, visited));
            }
        }

        // Backtrack: Remove current node from visited so it can be used in other branches 
        // (if the graph structure allows shared children, though XML is usually strictly hierarchical)
        visited.Remove(currentNode.Name);

        return xNode;
    }

    /// <summary>
    /// Parse XML and return a nested array structure compatible with GDScript
    /// Format: { "element_name": string, "attributes": Dictionary, "children": Array }
    /// </summary>
    public static Godot.Collections.Dictionary ParseGraphFromXmlAsDict(string xmlString)
    {
        try
        {
            XElement root = XElement.Parse(xmlString);
            return BuildDictFromElement(root);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error parsing XML: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Recursively convert XElement to Dictionary
    /// </summary>
    private static Godot.Collections.Dictionary BuildDictFromElement(XElement element)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["element_name"] = element.Name.LocalName
        };

        // Add attributes
        var attributes = new Godot.Collections.Dictionary();
        foreach (var attr in element.Attributes())
        {
            attributes[attr.Name.LocalName] = attr.Value;
        }
        dict["attributes"] = attributes;

        // Add children
        var children = new Godot.Collections.Array();
        foreach (var child in element.Elements())
        {
            children.Add(BuildDictFromElement(child));
        }
        dict["children"] = children;

        return dict;
    }

    /// <summary>
    /// Get the node scene name for a given XML element name by checking each node's xml_element_name property
    /// </summary>
    public static string? GetNodeNameFromElementName(string elementName)
    {
        foreach (var nodeName in AllNodeTypes)
        {
            var nodePath = $"res://nodes/{nodeName.ToLower().Replace(" ", "_")}_node.tscn";
            var nodeResource = GD.Load<PackedScene>(nodePath);

            if (nodeResource == null)
                continue;

            var instance = nodeResource.Instantiate();
            if (instance == null)
                continue;

            try
            {
                var xmlElementNameVariant = instance.Get("xml_element_name");
                string xmlElementName = xmlElementNameVariant.AsString();

                if (xmlElementName == elementName)
                {
                    instance.QueueFree();
                    return nodeName;
                }
            }
            catch
            {
                // Ignore errors, continue to next node
            }

            instance.QueueFree();
        }

        return null;  // Unknown element
    }

    /// <summary>
    /// Recursively flatten the graph structure into a list of (node_name, attributes, parent_element_name) tuples
    /// for easier processing
    /// </summary>
    public static List<(string nodeName, Dictionary<string, string> attributes, string parentElementName)> FlattenGraphStructure(GraphNodeData rootData, string parentName = "")
    {
        var result = new List<(string, Dictionary<string, string>, string)>();

        // Map element name to node name
        string nodeName = GetNodeNameFromElementName(rootData.ElementName)!;
        if (nodeName != null)
        {
            result.Add((nodeName, rootData.Attributes, parentName));
        }

        // Process children
        foreach (var child in rootData.Children)
        {
            result.AddRange(FlattenGraphStructure(child, rootData.ElementName));
        }

        return result;
    }

    /// <summary>
    /// Report validation errors to the corresponding nodes in the graph
    /// Parses error messages, identifies which nodes caused errors, and marks only those as invalid
    /// </summary>
    public static void ReportValidationErrorsToNodes(string validationResult, GraphEdit graphEdit, string xmlString)
    {
        if (graphEdit == null)
            return;

        // Only process if there are actual validation errors
        if (!validationResult.Contains("ERROR") && !validationResult.Contains("Validation Errors"))
        {
            // Clear invalid state from all nodes if validation succeeded
            foreach (var child in graphEdit.GetChildren())
            {
                if (child is Node node && node.Get("invalid") is Variant invalidVar)
                {
                    if (invalidVar.AsBool())
                    {
                        node.Set("invalid", false);
                    }
                }
            }
            return;
        }

        // Parse XML to find line number to element name mapping
        try
        {
            XDocument doc = XDocument.Parse(xmlString, LoadOptions.SetLineInfo);
            var lineToElementMap = new Dictionary<int, string>();

            // Build a map of line numbers to XML element names
            foreach (var element in doc.Root!.DescendantsAndSelf())
            {
                var lineInfo = (IXmlLineInfo)element;
                if (lineInfo.HasLineInfo())
                {
                    lineToElementMap[lineInfo.LineNumber] = element.Name.LocalName;
                }
            }

            // Extract line numbers from validation errors
            var errorLines = new HashSet<int>();
            foreach (var line in validationResult.Split('\n'))
            {
                if (line.Contains("Line"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"Line (\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int lineNum))
                    {
                        errorLines.Add(lineNum);
                    }
                }
            }

            // Get all nodes and their element names
            var nodeLookup = new Dictionary<string, List<Node>>();
            foreach (var child in graphEdit.GetChildren())
            {
                if (child is Node node && node.Get("xml_element_name") is Variant elemName)
                {
                    string elementName = elemName.AsString();
                    if (!nodeLookup.ContainsKey(elementName))
                        nodeLookup[elementName] = new List<Node>();
                    nodeLookup[elementName].Add(node);
                }
            }

            // Find the root node (node with no parents)
            var connections = graphEdit.GetConnectionList();
            var nodesThatHaveParents = new HashSet<string>();
            foreach (var conn in connections)
            {
                nodesThatHaveParents.Add((string)conn["to_node"]);
            }

            Node? rootNode = null;
            foreach (var child in graphEdit.GetChildren())
            {
                if (child is GraphNode node && !nodesThatHaveParents.Contains(node.Name))
                {
                    rootNode = node;
                    break;
                }
            }

            // Mark nodes invalid if their elements appear in error lines
            var invalidatedNodes = new HashSet<Node>();
            foreach (var errorLine in errorLines)
            {
                if (lineToElementMap.TryGetValue(errorLine, out string? elementName))
                {
                    // Special case: if the element is "mdscript" (the root), mark the root node as invalid
                    if (elementName == "mdscript" && rootNode != null)
                    {
                        rootNode.Set("invalid", true);
                        invalidatedNodes.Add(rootNode);
                        GD.Print($"Marked root node as invalid due to mdscript error on line {errorLine}");
                    }
                    else if (nodeLookup.TryGetValue(elementName, out var nodes))
                    {
                        foreach (var node in nodes)
                        {
                            node.Set("invalid", true);
                            invalidatedNodes.Add(node);
                            GD.Print($"Marked {node.Name} ({elementName}) as invalid due to error on line {errorLine}");
                        }
                    }
                    else
                    {
                        GD.PrintErr($"No node found with xml_element_name '{elementName}' for error on line {errorLine}");
                    }
                }
            }

            // Clear invalid state from nodes that don't have errors
            foreach (var child in graphEdit.GetChildren())
            {
                if (child is Node node && !invalidatedNodes.Contains(node))
                {
                    if (node.Get("invalid") is Variant invalidVar && invalidVar.AsBool())
                    {
                        node.Set("invalid", false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error reporting validation errors to nodes: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate XML against its referenced XSD schema
    /// Extracts schema location from xsi:noNamespaceSchemaLocation attribute
    /// Returns validation errors as a string
    /// </summary>
    public static string ValidateXmlAgainstSchema(string xmlString)
    {
        try
        {
            XDocument doc = XDocument.Parse(xmlString);
            XElement root = doc.Root!;

            // Get the schema location from xsi:noNamespaceSchemaLocation
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            var schemaLocationAttr = root.Attribute(xsi + "noNamespaceSchemaLocation");

            if (schemaLocationAttr == null)
            {
                return "Error: No xsi:noNamespaceSchemaLocation attribute found in root element";
            }

            string schemaLocation = schemaLocationAttr.Value;

            // Build the full path to the schema file (relative to project root)
            string projectPath = ProjectSettings.GlobalizePath("res://Schemas/");
            string schemaPath = Path.Combine(projectPath, schemaLocation);

            if (!File.Exists(schemaPath))
            {
                return $"Error: Schema file not found at {schemaPath}";
            }

            // Create XmlSchemaSet with schema location set to the directory
            // This helps resolve relative imports in the schema
            XmlSchemaSet schemaSet = new();
            string? schemaDirectory = Path.GetDirectoryName(schemaPath);
            schemaSet.XmlResolver = new XmlUrlResolver();

            try
            {
                schemaSet.Add(null, schemaPath);
            }
            catch (XmlSchemaException schemaEx)
            {
                GD.PrintErr($"Error loading schema: {schemaEx.Message}");
                if (schemaEx.InnerException != null)
                {
                    GD.PrintErr($"Inner exception: {schemaEx.InnerException.Message}");
                }
                return $"Error loading schema file: {schemaEx.Message}";
            }

            // Create validation errors list
            List<string> validationErrors = [];
            List<string> schemaErrors = [];

            // Create XML reader settings with schema validation
            var settings = new XmlReaderSettings();
            settings.Schemas = schemaSet;
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;

            // Add validation event handler for both schema and instance validation
            settings.ValidationEventHandler += (sender, e) =>
            {
                string severity = e.Severity == XmlSeverityType.Error ? "ERROR" : "WARNING";
                string message = $"{severity} (Line {e.Exception?.LineNumber ?? 0}, Column {e.Exception?.LinePosition ?? 0}): {e.Message}";

                if (e.Exception?.Message.Contains("model group") == true || e.Exception?.Message.Contains("undeclared") == true)
                {
                    schemaErrors.Add(message);
                }
                else
                {
                    validationErrors.Add(message);
                }
            };

            // Validate using XmlReader
            using (XmlReader reader = XmlReader.Create(new StringReader(xmlString), settings))
            {
                try
                {
                    while (reader.Read()) { }
                }
                catch (XmlException xmlEx)
                {
                    return $"XML parsing error: {xmlEx.Message}";
                }
            }

            // Check for schema definition errors first
            if (schemaErrors.Count > 0)
            {
                return "⚠ Schema Definition Issues (not your XML):\n" + string.Join("\n", schemaErrors) +
                       "\n\nYour XML might be valid, but the XSD schema file has issues. Check if:\n" +
                       "- The XSD file references other XSD files that don't exist\n" +
                       "- There are typos in the XSD declarations (like 'commonconditions_nonevent')\n" +
                       "- The XSD file needs to be updated";
            }

            if (validationErrors.Count == 0)
            {
                return "✓ XML validation successful! No errors found.";
            }
            else
            {
                return "Validation Errors:\n" + string.Join("\n", validationErrors);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Validation exception: {ex}");
            return $"Error during validation: {ex.Message}\n\nNote: If this is a schema definition error, check your XSD files.";
        }
    }
}