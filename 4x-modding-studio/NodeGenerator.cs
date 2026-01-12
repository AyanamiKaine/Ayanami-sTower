using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generates GraphNode instances dynamically based on XSD element definitions.
/// This allows the system to support any XML element without hand-crafting each node.
/// </summary>
public partial class NodeGenerator : Node
{
    [Export]
    public X4DatabaseManager? DatabaseManager { get; set; }

    [Export]
    public PackedScene? BaseNodeScene { get; set; }

    /// <summary>
    /// Cache of generated node scenes by element name
    /// </summary>
    private Dictionary<string, PackedScene> _generatedSceneCache = new();

    /// <summary>
    /// Maps attribute types to appropriate control types
    /// </summary>
    private static readonly Dictionary<string, string> TypeToControlMap = new()
    {
        { "xs:string", "LineEdit" },
        { "xs:boolean", "CheckBox" },
        { "xs:integer", "SpinBox" },
        { "xs:int", "SpinBox" },
        { "xs:positiveInteger", "SpinBox" },
        { "xs:nonNegativeInteger", "SpinBox" },
        { "xs:decimal", "SpinBox" },
        { "xs:float", "SpinBox" },
        { "xs:double", "SpinBox" },
        { "expression", "LineEdit" },  // X4 expression type
        { "comment", "LineEdit" },
        { "object", "LineEdit" },
        { "boolean", "CheckBox" },
    };

    public override void _Ready()
    {
        base._Ready();

        // Load the base node scene if not set
        if (BaseNodeScene == null)
        {
            BaseNodeScene = GD.Load<PackedScene>("res://nodes/base_node.tscn");
        }
    }

    /// <summary>
    /// Creates a GraphNode instance for the given XML element name.
    /// Uses cached data if available, otherwise generates from XSD info.
    /// </summary>
    public GraphNode? CreateNodeForElement(string elementName)
    {
        if (DatabaseManager == null)
        {
            GD.PrintErr("[NodeGenerator] DatabaseManager not set!");
            return null;
        }

        // Try to get element info from database
        if (!DatabaseManager.AllElements.TryGetValue(elementName, out var elemInfo))
        {
            GD.PrintErr($"[NodeGenerator] Unknown element: {elementName}");
            return null;
        }

        return CreateNodeFromElementInfo(elemInfo);
    }

    /// <summary>
    /// Creates a GraphNode from XsdElementInfo
    /// </summary>
    public GraphNode? CreateNodeFromElementInfo(XsdElementInfo elemInfo)
    {
        if (BaseNodeScene == null)
        {
            GD.PrintErr("[NodeGenerator] BaseNodeScene not loaded!");
            return null;
        }

        // Instantiate the base node
        var node = BaseNodeScene.Instantiate<GraphNode>();
        if (node == null)
        {
            GD.PrintErr("[NodeGenerator] Failed to instantiate base node!");
            return null;
        }

        // Configure the node
        ConfigureNode(node, elemInfo);

        return node;
    }

    /// <summary>
    /// Configures a GraphNode instance with element info
    /// </summary>
    private void ConfigureNode(GraphNode node, XsdElementInfo elemInfo)
    {
        // Set basic properties
        node.Title = FormatElementName(elemInfo.Name);
        node.Set("xml_element_name", elemInfo.Name);

        // Set tooltip with documentation
        if (!string.IsNullOrEmpty(elemInfo.Documentation))
        {
            node.TooltipText = elemInfo.Documentation;
        }

        // Determine node type from attribute groups
        var nodeType = DetermineNodeType(elemInfo);
        node.Set("type", nodeType);

        // Set expected child/parent types based on allowed children and parents
        SetExpectedTypes(node, elemInfo);

        // Initialize xml_attributes dictionary
        var xmlAttributes = new Godot.Collections.Dictionary();
        node.Set("xml_attributes", xmlAttributes);

        // Add attribute controls
        AddAttributeControls(node, elemInfo, xmlAttributes);

        // Set slot configuration based on node type
        ConfigureSlots(node, elemInfo);
    }

    /// <summary>
    /// Formats an XML element name for display (e.g., "set_value" -> "Set Value")
    /// </summary>
    private static string FormatElementName(string elementName)
    {
        if (string.IsNullOrEmpty(elementName))
            return "Unknown";

        // Replace underscores with spaces and title case
        var words = elementName.Split('_');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        return string.Join(" ", words);
    }

    /// <summary>
    /// Determines the node type based on attribute groups
    /// </summary>
    private static int DetermineNodeType(XsdElementInfo elemInfo)
    {
        // Check attribute groups to determine type
        // These match the Types.NodeType enum in GDScript
        if (elemInfo.AttributeGroups.Contains("action"))
            return 2; // ACTION
        if (elemInfo.AttributeGroups.Contains("condition"))
            return 3; // CONDITION (assuming this exists)
        if (elemInfo.Name == "cue" || elemInfo.Name == "cues")
            return 1; // CUE type
        if (elemInfo.Name.StartsWith("event_"))
            return 4; // EVENT type (assuming this exists)

        return 0; // NONE/default
    }

    /// <summary>
    /// Sets expected parent and child types based on XSD relationships
    /// </summary>
    private void SetExpectedTypes(GraphNode node, XsdElementInfo elemInfo)
    {
        // For now, set generic arrays - could be enhanced based on actual relationships
        var expectedParentTypes = new Godot.Collections.Array<int>();
        var expectedChildTypes = new Godot.Collections.Array<int>();

        // If element has allowed children, it can have child connections
        if (elemInfo.AllowedChildren.Count > 0)
        {
            // Allow various child types based on what's in AllowedChildren
            foreach (var childName in elemInfo.AllowedChildren)
            {
                if (DatabaseManager != null && DatabaseManager.AllElements.TryGetValue(childName, out var childInfo))
                {
                    var childType = DetermineNodeType(childInfo);
                    if (!expectedChildTypes.Contains(childType))
                    {
                        expectedChildTypes.Add(childType);
                    }
                }
            }
        }

        // If element has parent elements, it can be a child
        if (elemInfo.ParentElements.Count > 0)
        {
            foreach (var parentName in elemInfo.ParentElements)
            {
                if (DatabaseManager != null && DatabaseManager.AllElements.TryGetValue(parentName, out var parentInfo))
                {
                    var parentType = DetermineNodeType(parentInfo);
                    if (!expectedParentTypes.Contains(parentType))
                    {
                        expectedParentTypes.Add(parentType);
                    }
                }
            }
        }

        node.Set("expected_parent_types", expectedParentTypes);
        node.Set("expected_child_types", expectedChildTypes);
    }

    /// <summary>
    /// Adds UI controls for each attribute in the element
    /// </summary>
    private void AddAttributeControls(GraphNode node, XsdElementInfo elemInfo, Godot.Collections.Dictionary xmlAttributes)
    {
        foreach (var attr in elemInfo.Attributes)
        {
            var container = CreateAttributeControl(attr, xmlAttributes);
            if (container != null)
            {
                node.AddChild(container);
            }
        }
    }

    /// <summary>
    /// Creates a control container for an attribute
    /// </summary>
    private Control? CreateAttributeControl(XsdAttributeInfo attr, Godot.Collections.Dictionary xmlAttributes)
    {
        var hbox = new HBoxContainer();
        hbox.Name = $"Attr_{attr.Name}";

        // Create label
        var label = new Label();
        label.Text = FormatAttributeName(attr.Name) + ":";
        label.CustomMinimumSize = new Vector2(100, 0);

        // Set tooltip with documentation
        if (!string.IsNullOrEmpty(attr.Documentation))
        {
            label.TooltipText = attr.Documentation;
            hbox.TooltipText = attr.Documentation;
        }

        // Mark required attributes
        if (attr.IsRequired)
        {
            label.Text = "* " + label.Text;
            label.AddThemeColorOverride("font_color", Colors.Orange);
        }

        hbox.AddChild(label);

        // Create appropriate input control based on type
        Control inputControl;

        // Check if it's an enum type
        if (attr.EnumValues.Count > 0)
        {
            inputControl = CreateEnumControl(attr, xmlAttributes);
        }
        else
        {
            inputControl = CreateTypedControl(attr, xmlAttributes);
        }

        inputControl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.AddChild(inputControl);

        return hbox;
    }

    /// <summary>
    /// Creates a dropdown for enum attributes
    /// </summary>
    private Control CreateEnumControl(XsdAttributeInfo attr, Godot.Collections.Dictionary xmlAttributes)
    {
        var optionButton = new OptionButton();
        optionButton.Name = $"Input_{attr.Name}";

        // Add empty option if not required
        if (!attr.IsRequired)
        {
            optionButton.AddItem("(none)", 0);
        }

        // Add enum values
        int index = attr.IsRequired ? 0 : 1;
        foreach (var value in attr.EnumValues)
        {
            optionButton.AddItem(value, index++);
        }

        // Set default value if specified
        if (!string.IsNullOrEmpty(attr.DefaultValue))
        {
            for (int i = 0; i < optionButton.ItemCount; i++)
            {
                if (optionButton.GetItemText(i) == attr.DefaultValue)
                {
                    optionButton.Selected = i;
                    break;
                }
            }
        }

        // Connect signal to update xml_attributes
        string attrName = attr.Name;
        optionButton.ItemSelected += (idx) =>
        {
            var text = optionButton.GetItemText((int)idx);
            if (text != "(none)" && !string.IsNullOrEmpty(text))
            {
                xmlAttributes[attrName] = text;
            }
            else
            {
                xmlAttributes.Remove(attrName);
            }
        };

        return optionButton;
    }

    /// <summary>
    /// Creates a control based on the attribute type
    /// </summary>
    private Control CreateTypedControl(XsdAttributeInfo attr, Godot.Collections.Dictionary xmlAttributes)
    {
        string attrName = attr.Name;
        string attrType = attr.Type.ToLowerInvariant();

        // Check for boolean type
        if (attrType.Contains("boolean") || attrType == "xs:boolean")
        {
            var checkBox = new CheckBox();
            checkBox.Name = $"Input_{attr.Name}";

            if (attr.DefaultValue == "true")
            {
                checkBox.ButtonPressed = true;
                xmlAttributes[attrName] = "true";
            }

            checkBox.Toggled += (pressed) =>
            {
                if (pressed)
                {
                    xmlAttributes[attrName] = "true";
                }
                else
                {
                    // Only add "false" if it differs from default or is required
                    if (attr.IsRequired || attr.DefaultValue == "true")
                    {
                        xmlAttributes[attrName] = "false";
                    }
                    else
                    {
                        xmlAttributes.Remove(attrName);
                    }
                }
            };

            return checkBox;
        }

        // Check for numeric types
        if (attrType.Contains("integer") || attrType.Contains("int") ||
            attrType.Contains("decimal") || attrType.Contains("float") ||
            attrType.Contains("double"))
        {
            var spinBox = new SpinBox();
            spinBox.Name = $"Input_{attr.Name}";
            spinBox.MinValue = attrType.Contains("positive") ? 1 : (attrType.Contains("nonNegative") ? 0 : -999999);
            spinBox.MaxValue = 999999;
            spinBox.Step = attrType.Contains("integer") || attrType.Contains("int") ? 1 : 0.01;

            if (!string.IsNullOrEmpty(attr.DefaultValue) && double.TryParse(attr.DefaultValue, out var defaultVal))
            {
                spinBox.Value = defaultVal;
            }

            spinBox.ValueChanged += (value) =>
            {
                xmlAttributes[attrName] = value.ToString();
            };

            return spinBox;
        }

        // Default to LineEdit for string/expression/etc
        var lineEdit = new LineEdit();
        lineEdit.Name = $"Input_{attr.Name}";
        lineEdit.PlaceholderText = GetPlaceholderText(attr);

        if (!string.IsNullOrEmpty(attr.DefaultValue))
        {
            lineEdit.Text = attr.DefaultValue;
            xmlAttributes[attrName] = attr.DefaultValue;
        }

        lineEdit.TextChanged += (newText) =>
        {
            if (!string.IsNullOrEmpty(newText))
            {
                xmlAttributes[attrName] = newText;
            }
            else
            {
                xmlAttributes.Remove(attrName);
            }
        };

        return lineEdit;
    }

    /// <summary>
    /// Gets placeholder text based on attribute type
    /// </summary>
    private static string GetPlaceholderText(XsdAttributeInfo attr)
    {
        string type = attr.Type.ToLowerInvariant();

        if (type.Contains("expression"))
            return "e.g., player.ship";
        if (type.Contains("object"))
            return "object reference";
        if (type.Contains("comment"))
            return "comment text";

        return attr.Type;
    }

    /// <summary>
    /// Formats an attribute name for display
    /// </summary>
    private static string FormatAttributeName(string attrName)
    {
        // Simple formatting - could be enhanced
        return attrName;
    }

    /// <summary>
    /// Configures input/output slots for the node
    /// </summary>
    private void ConfigureSlots(GraphNode node, XsdElementInfo elemInfo)
    {
        bool hasParents = elemInfo.ParentElements.Count > 0;
        bool hasChildren = elemInfo.AllowedChildren.Count > 0 || elemInfo.AllowedChildren.Contains("*");

        // Configure slot 0: left (input from parent), right (output to children)
        node.SetSlot(0,
            hasParents,      // Enable left (input)
            0,               // Type left
            Colors.White,    // Color left
            hasChildren,     // Enable right (output)
            0,               // Type right
            Colors.White);   // Color right
    }

    /// <summary>
    /// Gets a list of all available element names that can be created as nodes
    /// </summary>
    public List<string> GetAvailableElements()
    {
        if (DatabaseManager == null)
            return new List<string>();

        return DatabaseManager.AllElements.Keys.OrderBy(x => x).ToList();
    }

    /// <summary>
    /// Gets element info for display in a node searcher
    /// Returns: (elementName, displayName, documentation, category)
    /// </summary>
    public List<(string elementName, string displayName, string documentation, string category)> GetElementsForSearcher()
    {
        if (DatabaseManager == null)
            return new List<(string, string, string, string)>();

        var result = new List<(string elementName, string displayName, string documentation, string category)>();

        foreach (var kvp in DatabaseManager.AllElements)
        {
            var elemInfo = kvp.Value;
            string category = DetermineCategory(elemInfo);
            string displayName = FormatElementName(elemInfo.Name);
            string doc = elemInfo.Documentation ?? "";

            result.Add((elemInfo.Name, displayName, doc, category));
        }

        return result.OrderBy(x => x.category).ThenBy(x => x.displayName).ToList();
    }

    /// <summary>
    /// Gets element info for display in a node searcher - GDScript compatible version.
    /// Returns a Godot Array of Godot Arrays: [[elementName, displayName, documentation, category], ...]
    /// </summary>
    public Godot.Collections.Array<Godot.Collections.Array<string>> GetElementsForSearcherGodot()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Array<string>>();

        if (DatabaseManager == null)
        {
            GD.Print("[NodeGenerator] GetElementsForSearcherGodot: DatabaseManager is null");
            return result;
        }

        GD.Print($"[NodeGenerator] GetElementsForSearcherGodot: Processing {DatabaseManager.AllElements.Count} elements");

        var sortedElements = DatabaseManager.AllElements.Values
            .Select(elemInfo => new
            {
                Info = elemInfo,
                Category = DetermineCategory(elemInfo),
                DisplayName = FormatElementName(elemInfo.Name)
            })
            .OrderBy(x => x.Category)
            .ThenBy(x => x.DisplayName)
            .ToList();

        foreach (var item in sortedElements)
        {
            var elemArray = new Godot.Collections.Array<string>
            {
                item.Info.Name,
                item.DisplayName,
                item.Info.Documentation ?? "",
                item.Category
            };
            result.Add(elemArray);
        }

        GD.Print($"[NodeGenerator] GetElementsForSearcherGodot: Returning {result.Count} elements");
        return result;
    }

    /// <summary>
    /// Determines a category for the element based on its type/groups
    /// </summary>
    private static string DetermineCategory(XsdElementInfo elemInfo)
    {
        if (elemInfo.AttributeGroups.Contains("action"))
            return "Actions";
        if (elemInfo.AttributeGroups.Contains("condition"))
            return "Conditions";
        if (elemInfo.Name.StartsWith("event_"))
            return "Events";
        if (elemInfo.Name == "cue" || elemInfo.Name == "cues")
            return "Cues";
        if (elemInfo.Type == "group" || elemInfo.Type == "attributeGroup")
            return "Groups";

        return "Other";
    }

    /// <summary>
    /// Checks if an element can be a child of another element
    /// </summary>
    public bool CanBeChildOf(string childElement, string parentElement)
    {
        if (DatabaseManager == null)
            return false;

        if (!DatabaseManager.AllElements.TryGetValue(parentElement, out var parentInfo))
            return false;

        // Check if parent allows any children
        if (parentInfo.AllowedChildren.Contains("*"))
            return true;

        return parentInfo.AllowedChildren.Contains(childElement);
    }

    /// <summary>
    /// Gets the allowed children for an element
    /// </summary>
    public List<string> GetAllowedChildren(string elementName)
    {
        if (DatabaseManager == null)
            return new List<string>();

        if (!DatabaseManager.AllElements.TryGetValue(elementName, out var elemInfo))
            return new List<string>();

        // If allows any, return all elements
        if (elemInfo.AllowedChildren.Contains("*"))
            return DatabaseManager.AllElements.Keys.ToList();

        return elemInfo.AllowedChildren;
    }

    /// <summary>
    /// Gets the allowed parents for an element
    /// </summary>
    public List<string> GetAllowedParents(string elementName)
    {
        if (DatabaseManager == null)
            return new List<string>();

        if (!DatabaseManager.AllElements.TryGetValue(elementName, out var elemInfo))
            return new List<string>();

        return elemInfo.ParentElements;
    }

    // --- GDScript-callable helper methods ---

    /// <summary>
    /// Gets allowed children for an element as a Godot Array (callable from GDScript)
    /// </summary>
    public Godot.Collections.Array<string> GetAllowedChildrenGodot(string elementName)
    {
        var result = new Godot.Collections.Array<string>();
        var children = GetAllowedChildren(elementName);
        foreach (var child in children)
        {
            result.Add(child);
        }
        return result;
    }

    /// <summary>
    /// Gets allowed parents for an element as a Godot Array (callable from GDScript)
    /// </summary>
    public Godot.Collections.Array<string> GetAllowedParentsGodot(string elementName)
    {
        var result = new Godot.Collections.Array<string>();
        var parents = GetAllowedParents(elementName);
        foreach (var parent in parents)
        {
            result.Add(parent);
        }
        return result;
    }

    /// <summary>
    /// Checks if an element can have parents (callable from GDScript)
    /// Root elements return false
    /// </summary>
    public bool CanHaveParent(string elementName)
    {
        if (DatabaseManager == null)
            return true; // Default to allowing parents if we can't check

        if (!DatabaseManager.AllElements.TryGetValue(elementName, out var elemInfo))
            return true; // Unknown elements default to allowing parents

        return elemInfo.ParentElements.Count > 0;
    }

    /// <summary>
    /// Checks if an element can have children (callable from GDScript)
    /// </summary>
    public bool CanHaveChildren(string elementName)
    {
        if (DatabaseManager == null)
            return true;

        if (!DatabaseManager.AllElements.TryGetValue(elementName, out var elemInfo))
            return true;

        return elemInfo.AllowedChildren.Count > 0;
    }
}
