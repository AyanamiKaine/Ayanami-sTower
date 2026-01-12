using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class DatabaseViewer : Control
{
    [Export]
    public X4DatabaseManager? X4DatabaseManager { get; set; }

    [Export]
    public ItemList? DataItemList { get; set; }

    [Export]
    public LineEdit? DataSearchLineEdit { get; set; }

    [Export]
    public Label? LoadingLabel { get; set; } // Optional loading label overlay

    [Export]
    public Label? NameLabel { get; set; }

    [Export]
    public Label? DocLabel { get; set; }

    [Export]
    public Label? TypeLabel { get; set; }

    [Export]
    public ItemList? AttributesItemList { get; set; }

    // Cached list of all element names for filtering
    private List<string> _allElementNames = new();
    private bool _isLoading = true;
    private Timer? _loadingTimer;
    private int _loadingDots = 0;

    public override void _Ready()
    {
        base._Ready();

        // Connect search LineEdit text changed signal
        if (DataSearchLineEdit != null)
        {
            DataSearchLineEdit.TextChanged += OnSearchTextChanged;
        }

        // Connect item selection signal
        if (DataItemList != null)
        {
            DataItemList.ItemSelected += OnItemSelected;
        }

        // Show loading state initially
        ShowLoadingState();

        // Try to populate immediately if data is already loaded
        TryPopulateList();

        // Also connect to signals for when data becomes available
        if (X4DatabaseManager != null)
        {
            // Poll periodically until data is ready (since scans run async)
            var timer = new Timer();
            timer.WaitTime = 0.5;
            timer.OneShot = false;
            timer.Timeout += () =>
            {
                // Update loading animation
                UpdateLoadingAnimation();

                if (X4DatabaseManager.ElementUsageStats.Count > 0 && _allElementNames.Count == 0)
                {
                    TryPopulateList();
                    HideLoadingState();
                    timer.Stop();
                    timer.QueueFree();
                }
            };
            AddChild(timer);
            timer.Start();
            _loadingTimer = timer;
        }
    }

    /// <summary>
    /// Shows loading indicator in the UI
    /// </summary>
    private void ShowLoadingState()
    {
        _isLoading = true;

        // Show in the label if available
        if (LoadingLabel != null)
        {
            LoadingLabel.Visible = true;
            LoadingLabel.Text = "Loading database...";
        }

        // Also show in ItemList
        if (DataItemList != null)
        {
            DataItemList.Clear();
            DataItemList.AddItem("⏳ Loading XSD schemas and XML files...");
            DataItemList.SetItemDisabled(0, true);
            DataItemList.SetItemCustomFgColor(0, Colors.Gray);
        }

        // Disable search while loading
        if (DataSearchLineEdit != null)
        {
            DataSearchLineEdit.Editable = false;
            DataSearchLineEdit.PlaceholderText = "Loading...";
        }
    }

    /// <summary>
    /// Updates the loading animation dots
    /// </summary>
    private void UpdateLoadingAnimation()
    {
        if (!_isLoading) return;

        _loadingDots = (_loadingDots + 1) % 4;
        string dots = new string('.', _loadingDots);
        string padding = new string(' ', 3 - _loadingDots);

        if (LoadingLabel != null)
        {
            LoadingLabel.Text = $"Loading database{dots}{padding}";
        }

        if (DataItemList != null && DataItemList.ItemCount > 0)
        {
            DataItemList.SetItemText(0, $"⏳ Loading XSD schemas and XML files{dots}{padding}");
        }
    }

    /// <summary>
    /// Hides loading indicator
    /// </summary>
    private void HideLoadingState()
    {
        _isLoading = false;

        if (LoadingLabel != null)
        {
            LoadingLabel.Visible = false;
        }

        // Re-enable search
        if (DataSearchLineEdit != null)
        {
            DataSearchLineEdit.Editable = true;
            DataSearchLineEdit.PlaceholderText = "Search elements...";
        }
    }

    /// <summary>
    /// Attempts to populate the list with element names from the database
    /// </summary>
    private void TryPopulateList()
    {
        if (X4DatabaseManager == null || DataItemList == null)
            return;

        // Get all unique element names from both XSD definitions and XML usage
        var elementNames = new HashSet<string>();

        // Add elements from XSD schemas
        foreach (var elemName in X4DatabaseManager.AllElements.Keys)
        {
            elementNames.Add(elemName);
        }

        // Add elements from actual XML usage (includes undocumented elements)
        foreach (var elemName in X4DatabaseManager.ElementUsageStats.Keys)
        {
            elementNames.Add(elemName);
        }

        // Sort and cache
        _allElementNames = elementNames.OrderBy(n => n).ToList();

        // Populate the list
        RefreshList("");

        GD.Print($"[DatabaseViewer] Loaded {_allElementNames.Count} element names");
    }

    /// <summary>
    /// Called when the search text changes
    /// </summary>
    private void OnSearchTextChanged(string newText)
    {
        RefreshList(newText);
    }

    /// <summary>
    /// Refreshes the ItemList with filtered results
    /// </summary>
    private void RefreshList(string filter)
    {
        if (DataItemList == null)
            return;

        DataItemList.Clear();

        var searchText = filter.ToLowerInvariant().Trim();

        foreach (var elementName in _allElementNames)
        {
            // Filter: show all if empty, otherwise match name or type/attributeGroup
            bool matchesFilter = string.IsNullOrEmpty(searchText);

            if (!matchesFilter)
            {
                // Check name match
                if (elementName.ToLowerInvariant().Contains(searchText))
                {
                    matchesFilter = true;
                }
                // Check type/attributeGroup match
                else if (X4DatabaseManager != null && X4DatabaseManager.AllElements.TryGetValue(elementName, out var elemInfo))
                {
                    // Match against Type
                    if (!string.IsNullOrEmpty(elemInfo.Type) && elemInfo.Type.ToLowerInvariant().Contains(searchText))
                    {
                        matchesFilter = true;
                    }
                    // Match against AttributeGroups (e.g., "action", "condition")
                    else if (elemInfo.AttributeGroups.Any(g => g.ToLowerInvariant().Contains(searchText)))
                    {
                        matchesFilter = true;
                    }
                }
            }

            if (matchesFilter)
            {
                // Build display text with type info
                string displayText = elementName;
                string typeInfo = "";

                if (X4DatabaseManager != null && X4DatabaseManager.AllElements.TryGetValue(elementName, out var elemInfo))
                {
                    if (elemInfo.AttributeGroups.Count > 0)
                    {
                        typeInfo = string.Join(", ", elemInfo.AttributeGroups);
                    }
                    else if (!string.IsNullOrEmpty(elemInfo.Type))
                    {
                        typeInfo = elemInfo.Type;
                    }
                }

                if (!string.IsNullOrEmpty(typeInfo))
                {
                    displayText = $"{elementName}  [{typeInfo}]";
                }

                int idx = DataItemList.AddItem(displayText);

                // Store the actual element name as metadata for later use
                DataItemList.SetItemMetadata(idx, elementName);

                // Color-code based on status
                if (X4DatabaseManager != null)
                {
                    bool inXsd = X4DatabaseManager.AllElements.ContainsKey(elementName);
                    bool inUsage = X4DatabaseManager.ElementUsageStats.ContainsKey(elementName);

                    if (inXsd && inUsage)
                    {
                        // Defined and used - green
                        DataItemList.SetItemCustomFgColor(idx, Colors.LightGreen);
                    }
                    else if (inXsd && !inUsage)
                    {
                        // Defined but not used - yellow
                        DataItemList.SetItemCustomFgColor(idx, Colors.Yellow);
                    }
                    else if (!inXsd && inUsage)
                    {
                        // Used but not defined - orange (undocumented)
                        DataItemList.SetItemCustomFgColor(idx, Colors.Orange);
                    }
                }
            }
        }
    }

    public override void _ExitTree()
    {
        // Disconnect signals
        if (DataSearchLineEdit != null)
        {
            DataSearchLineEdit.TextChanged -= OnSearchTextChanged;
        }

        if (DataItemList != null)
        {
            DataItemList.ItemSelected -= OnItemSelected;
        }

        base._ExitTree();
    }

    /// <summary>
    /// Called when an item is selected in the DataItemList
    /// </summary>
    private void OnItemSelected(long index)
    {
        if (DataItemList == null || X4DatabaseManager == null)
            return;

        // Get the element name from metadata
        var elementName = DataItemList.GetItemMetadata((int)index).AsString();
        if (string.IsNullOrEmpty(elementName))
            return;

        // Populate the detail view
        PopulateElementDetails(elementName);
    }

    /// <summary>
    /// Populates the detail labels and attributes list for a selected element
    /// </summary>
    private void PopulateElementDetails(string elementName)
    {
        // Clear previous data
        ClearElementDetails();

        // Set the name
        if (NameLabel != null)
        {
            NameLabel.Text = elementName;
        }

        // Try to get XSD info
        XsdElementInfo? elemInfo = null;
        if (X4DatabaseManager != null && X4DatabaseManager.AllElements.TryGetValue(elementName, out var info))
        {
            elemInfo = info;
        }

        // Set documentation
        if (DocLabel != null)
        {
            if (elemInfo != null && !string.IsNullOrEmpty(elemInfo.Documentation))
            {
                DocLabel.Text = elemInfo.Documentation;
            }
            else
            {
                DocLabel.Text = "(No documentation available)";
            }
        }

        // Set type info
        if (TypeLabel != null)
        {
            var typeInfo = new List<string>();

            if (elemInfo != null)
            {
                if (!string.IsNullOrEmpty(elemInfo.Type))
                {
                    typeInfo.Add($"Type: {elemInfo.Type}");
                }

                if (elemInfo.AttributeGroups.Count > 0)
                {
                    typeInfo.Add($"Groups: {string.Join(", ", elemInfo.AttributeGroups)}");
                }

                // Add usage stats
                if (X4DatabaseManager != null && X4DatabaseManager.ElementUsageStats.TryGetValue(elementName, out var usageStats))
                {
                    typeInfo.Add($"Usage: {usageStats.TotalUsageCount} times in {usageStats.FilesUsedIn.Count} files");
                }

                if (elemInfo.AllowedChildren.Count > 0)
                {
                    var childrenPreview = elemInfo.AllowedChildren.Take(5);
                    var childrenText = string.Join(", ", childrenPreview);
                    if (elemInfo.AllowedChildren.Count > 5)
                    {
                        childrenText += $" (+{elemInfo.AllowedChildren.Count - 5} more)";
                    }
                    typeInfo.Add($"Children: {childrenText}");
                }
            }
            else if (X4DatabaseManager != null && X4DatabaseManager.ElementUsageStats.TryGetValue(elementName, out var usageStats))
            {
                // Element not in XSD but has usage stats
                typeInfo.Add("⚠️ Not defined in XSD schema");
                typeInfo.Add($"Usage: {usageStats.TotalUsageCount} times in {usageStats.FilesUsedIn.Count} files");
            }

            TypeLabel.Text = typeInfo.Count > 0 ? string.Join("\n", typeInfo) : "";
        }

        // Populate attributes list
        if (AttributesItemList != null && elemInfo != null)
        {
            foreach (var attr in elemInfo.Attributes)
            {
                string attrText = attr.Name;

                // Add type info
                if (!string.IsNullOrEmpty(attr.Type) && attr.Type != "xs:string")
                {
                    attrText += $" : {attr.Type}";
                }

                // Add required indicator
                if (attr.IsRequired)
                {
                    attrText += " [required]";
                }

                // Add default value
                if (!string.IsNullOrEmpty(attr.DefaultValue))
                {
                    attrText += $" = \"{attr.DefaultValue}\"";
                }

                int idx = AttributesItemList.AddItem(attrText);

                // Color required attributes
                if (attr.IsRequired)
                {
                    AttributesItemList.SetItemCustomFgColor(idx, Colors.Orange);
                }

                // Store attribute info as tooltip via metadata
                if (!string.IsNullOrEmpty(attr.Documentation))
                {
                    AttributesItemList.SetItemTooltip(idx, attr.Documentation);
                }
                else if (attr.EnumValues.Count > 0)
                {
                    AttributesItemList.SetItemTooltip(idx, $"Allowed values: {string.Join(", ", attr.EnumValues.Take(10))}" +
                        (attr.EnumValues.Count > 10 ? "..." : ""));
                }
            }
        }
    }

    /// <summary>
    /// Clears the element detail view
    /// </summary>
    private void ClearElementDetails()
    {
        if (NameLabel != null) NameLabel.Text = "";
        if (DocLabel != null) DocLabel.Text = "";
        if (TypeLabel != null) TypeLabel.Text = "";
        if (AttributesItemList != null) AttributesItemList.Clear();
    }
}
