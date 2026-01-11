using Godot;
using System;
using System.Collections.Generic;
using System.Linq; // Essential for LINQ queries
using System.Xml.Linq;

public partial class XmlWriter : Node
{
    [Export]
    public GraphEdit GraphEdit { get; set; }

    /// <summary>
    /// Converts the current GraphEdit state into a hierarchical XElement.
    /// </summary>
    public XElement ConvertGraphToXMLDoc()
    {
        if (GraphEdit == null)
            return new XElement("Error", "GraphEdit not assigned");

        // 1. GATHER DATA
        // Godot stores connections as a list of Dicts: { "from_node": "NameA", "to_node": "NameB", ... }
        var connections = GraphEdit.GetConnectionList();

        // Map: ParentName -> List of ChildNames
        // This makes looking up children O(1) instead of searching the list every time.
        var adjacencyList = new Dictionary<string, List<string>>();

        // precise set to track which nodes are "targets" (children). 
        // Any node NOT in this set is a Root node.
        var nodesThatHaveParents = new HashSet<string>();

        foreach (var conn in connections)
        {
            string fromNode = (string)conn["from_node"];
            string toNode = (string)conn["to_node"];

            if (!adjacencyList.ContainsKey(fromNode))
                adjacencyList[fromNode] = new List<string>();

            adjacencyList[fromNode].Add(toNode);
            nodesThatHaveParents.Add(toNode);
        }

        // 2. LOCATE NODES
        // We need the actual GraphNode objects to read Title, Position, etc.
        // We store them in a dictionary keyed by their Name property.
        var nodeLookup = new Dictionary<string, GraphNode>();
        foreach (var child in GraphEdit.GetChildren())
        {
            if (child is GraphNode node)
            {
                nodeLookup[node.Name] = node;
            }
        }

        // 3. IDENTIFY ROOT NODE
        // The root is the FIRST node spawned in the graph (first in scene tree order)
        // that doesn't have a parent connection.
        GraphNode rootNode = null;

        foreach (var node in nodeLookup.Values)
        {
            if (!nodesThatHaveParents.Contains(node.Name))
            {
                rootNode = node;
                break; // Take the first one as the root
            }
        }

        // 4. BUILD XML
        // We create a container "mdscript" element as the root.
        var xmlRoot = new XElement("mdscript");

        // Helper set to prevent infinite recursion if users create circular loops (A->B->A)
        var visited = new HashSet<string>();

        // Only add the root node to XML
        if (rootNode != null)
        {
            xmlRoot.Add(BuildNodeRecursive(rootNode, adjacencyList, nodeLookup, visited));
        }

        return xmlRoot;
    }

    /// <summary>
    /// Recursive function to build the tree.
    /// </summary>
    private XElement BuildNodeRecursive(
        GraphNode currentNode,
        Dictionary<string, List<string>> adjacencyList,
        Dictionary<string, GraphNode> nodeLookup,
        HashSet<string> visited)
    {
        // Cycle Detection: If we've seen this node in the current stack/path, stop.
        if (visited.Contains(currentNode.Name))
        {
            return new XElement("CycleDetected", new XAttribute("ref", currentNode.Name));
        }

        // Add to visited to process this branch
        // Note: For a strict Tree structure, we track globally. 
        // For a Graph where nodes might be shared, logic varies, but global visited is safest for XML.
        visited.Add(currentNode.Name);

        // Create the Element for this Node
        // Using "Node" as the tag name, but you could use currentNode.Title if it's a valid XML tag.
        var xNode = new XElement("Node",
            // Attributes for Visuals (Look of it)
            new XAttribute("Name", currentNode.Name),
            new XAttribute("Title", currentNode.Title),
            new XAttribute("PosX", currentNode.PositionOffset.X),
            new XAttribute("PosY", currentNode.PositionOffset.Y)
        );

        // Check if this node has children (connections going OUT)
        if (adjacencyList.ContainsKey(currentNode.Name))
        {
            foreach (var childName in adjacencyList[currentNode.Name])
            {
                if (nodeLookup.TryGetValue(childName, out GraphNode childNode))
                {
                    // RECURSE: Add the result of this function as a child XElement
                    xNode.Add(BuildNodeRecursive(childNode, adjacencyList, nodeLookup, visited));
                }
            }
        }

        return xNode;
    }
}