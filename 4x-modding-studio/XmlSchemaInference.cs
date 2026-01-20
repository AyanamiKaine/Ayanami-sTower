using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Godot;

/// <summary>
/// Holds the inferred schema information for a specific XML Element.
/// </summary>
public class ElementMetadata
{
    public string ElementName { get; }

    // Using HashSets to automatically handle duplicates. 
    private readonly HashSet<string> _attributes = new HashSet<string>();
    private readonly HashSet<string> _validChildren = new HashSet<string>();
    private readonly HashSet<string> _validParents = new HashSet<string>();

    public ElementMetadata(string name)
    {
        ElementName = name;
    }

    // --- Data Ingestion Methods ---

    public void AddAttribute(string attributeName)
    {
        _attributes.Add(attributeName);
    }

    public void AddChild(string childName)
    {
        _validChildren.Add(childName);
    }

    public void AddParent(string parentName)
    {
        _validParents.Add(parentName);
    }

    // --- Query Methods ---

    public IEnumerable<string> GetAttributes() => _attributes.OrderBy(x => x);
    public IEnumerable<string> GetValidChildren() => _validChildren.OrderBy(x => x);
    public IEnumerable<string> GetValidParents() => _validParents.OrderBy(x => x);
    
    public override string ToString()
    {
        return $"Element: <{ElementName}> | Attrs: {_attributes.Count} | Children: {_validChildren.Count} | Parents: {_validParents.Count}";
    }
}

public class XmlSchemaRegistry
{
    public string SourceDirectory { get; }

    // Dictionary to look up metadata by element name (Key = Element Name)
    private readonly Dictionary<string, ElementMetadata> _registry 
        = new Dictionary<string, ElementMetadata>(StringComparer.OrdinalIgnoreCase);

    public XmlSchemaRegistry(string sourceDirectory)
    {
        SourceDirectory = sourceDirectory;
    }

    /// <summary>
    /// Retrieves or creates the metadata node for a given element name.
    /// </summary>
    private ElementMetadata GetOrAddMetadata(string elementName)
    {
        if (!_registry.TryGetValue(elementName, out var metadata))
        {
            metadata = new ElementMetadata(elementName);
            _registry[elementName] = metadata;
        }
        return metadata;
    }

    /// <summary>
    /// Ingests an XML string to update the meta structure.
    /// </summary>
    public void LearnFromXml(string xmlContent, string filename = "")
    {
        try
        {
            var doc = XDocument.Parse(xmlContent);
            if (doc.Root == null) return;

            // Traverse the whole tree
            VisitElement(doc.Root, null);
        }
        catch (Exception ex)
        {
            GD.Print($"[XmlSchemaRegistry] Error parsing XML in {filename}: {ex.Message}");
        }
    }

    /// <summary>
    /// Recursive visitor to traverse the XML tree.
    /// </summary>
    private void VisitElement(XElement element, XElement? parent)
    {
        string currentName = element.Name.LocalName;
        
        // 1. Get (or create) the definition for THIS element
        var currentMeta = GetOrAddMetadata(currentName);

        // 2. Register Attributes found on this instance
        foreach (var attr in element.Attributes())
        {
            currentMeta.AddAttribute(attr.Name.LocalName);
        }

        // 3. Register Relationship with Parent (if exists)
        if (parent != null)
        {
            string parentName = parent.Name.LocalName;

            // Tell the Child (Current) who its Parent is
            currentMeta.AddParent(parentName);

            // Tell the Parent who its Child (Current) is
            var parentMeta = GetOrAddMetadata(parentName);
            parentMeta.AddChild(currentName);
        }

        // 4. Recurse into children
        foreach (var child in element.Elements())
        {
            VisitElement(child, element);
        }
    }

    // --- Public Query API ---

    public ElementMetadata? GetMetadata(string elementName)
    {
        if (_registry.TryGetValue(elementName, out var meta))
            return meta;
        
        return null;
    }

    public IEnumerable<string> GetAllKnownElements()
    {
        return _registry.Keys.OrderBy(k => k);
    }
    
    public IEnumerable<ElementMetadata> GetAllMetadata()
    {
        return _registry.Values;
    }
}
