using System;
using System.Xml.Linq;
using SqlKata.Execution;

namespace AyanamisTower.StellaDB;

/*
TODO:
There must be a way to easily add components to data. We need a generic parse method that can infere based on
the structure what tables and attributes should be used.

<StarSystem Name = "Sirius">
    <Galaxy Name = "Milky Way"/>
    <Components>
        <Position X="-60" Y="-20" Z="-20"/>
    </Components>
</StarSystem>

Here we should understand that every element in components is another table and its attributes are columns in it.

Maybe every element is a another table associated with the entity. And the attributes for an element are the columns so
we need to rewrite the xml structure like so:
<StarSystem Name = "Sirius", Galaxy = "Milky Way">
    <Position X="-60" Y="-20" Z="-20"/>
</StarSystem>

But then again its not really correct. Because correct would be saying parent. And name is not a column in entities but instead
another component.

<StarSystem Parent = "Milky Way">
    <Name>Sirius</Name>
    <Position X="-60" Y="-20" Z="-20"/>
</StarSystem>

Maybe being more explicit how entities are constructed. Its much more symetric, because this reflect the structure
we create in code, and in the database. This works because the id of entities can be queried using their unique name
so we dont have to write Parent=2231 should the id change for any reason we would need to change it here too.

<Entity Parent="Milky Way">
    <StarSystem/>
    <Name Value="Sirius"/>
    <Position X="-60" Y="-20" Z="-20"/>
</Entity>

TODO:
See the ExampleGalaxy.xml for an even better way of structuring it. Because it reflects the parent child relationship
directly in the xml.

*/

/// <summary>
/// Parses the data found.
/// </summary>
/// <summary>
/// Parses XML data files to populate the world with entities and their components.
/// This parser is designed to handle both explicit parent attributes and nested XML structures
/// to define parent-child relationships between entities.
/// </summary>
/// <summary>
/// Parses XML data files to populate the world with entities and their components.
/// This parser is designed to handle both explicit parent attributes and nested XML structures
/// to define parent-child relationships between entities.
/// </summary>
public static class DataParser
{
    /// <summary>
    /// A set of component names that should be treated as many-to-many relationship tables.
    /// This provides an explicit and robust way to differentiate them from standard one-to-one components.
    /// Names are compared case-insensitively.
    /// </summary>
    private static readonly HashSet<string> RelationTableNames =
        new(StringComparer.OrdinalIgnoreCase) { "ConnectedTo" };

    /// <summary>
    /// Parses a list of root XElements from data files and populates the world.
    /// It uses a robust two-pass approach:
    /// 1. **Discovery Pass:** All entities are identified from the XML and created with just their names.
    ///    This guarantees that any entity can be referenced by name during the second pass, regardless of order.
    /// 2. **Processing Pass:** Each entity is processed to establish parent relationships and add components,
    ///    including one-to-one data and many-to-many relationship entries.
    /// </summary>
    /// <param name="world">The world instance to populate.</param>
    /// <param name="allRootElements">A list of all root XElements from the data files (e.g., from XDocument.Root).</param>
    public static void ParseAndLoad(World world, List<XElement> allRootElements)
    {
        try
        {
            // To handle any XML structure, we first flatten the hierarchy to get a list of all <Entity> elements.
            // This allows us to process entities defined inside <Children> tags or at the root level uniformly.
            var allEntityElements = allRootElements
                .SelectMany(root =>
                    root.Name.LocalName == "Entity"
                        ? new[] { root }.Concat(root.Descendants("Entity"))
                        : root.Descendants("Entity")
                )
                .Distinct()
                .ToList();

            // Pass 1: Discover and create all entities so they can be referenced by name later.
            CreateAllEntities(world, allEntityElements);

            // Pass 2: Process each entity to set its parent, components, and relationships.
            ProcessEntitiesAndComponents(world, allEntityElements);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred during XML parsing: {ex}");
        }
    }

    /// <summary>
    /// **PASS 1:** Iterates through all found entity elements and creates a basic entity record in the world.
    /// </summary>
    private static void CreateAllEntities(World world, List<XElement> allEntityElements)
    {
        Console.WriteLine("Parser First Pass: Discovering and creating all entities...");
        foreach (var entityElement in allEntityElements)
        {
            string? entityName = GetEntityName(entityElement);
            if (!string.IsNullOrEmpty(entityName))
            {
                // world.Entity() is idempotent; it creates the entity if it doesn't exist
                // or retrieves the existing one.
                world.Entity(entityName);
            }
            else
            {
                Console.WriteLine(
                    "Warning: Found an <Entity> tag without an identifiable name. It will be skipped. XML: "
                        + entityElement.ToString().Split('\n').FirstOrDefault()
                );
            }
        }
        Console.WriteLine(
            $"Parser First Pass: Completed. Discovered {allEntityElements.Count} entities."
        );
    }

    /// <summary>
    /// **PASS 2:** Iterates through all entity elements again to process their full data.
    /// </summary>
    private static void ProcessEntitiesAndComponents(World world, List<XElement> allEntityElements)
    {
        Console.WriteLine("Parser Second Pass: Processing components and relationships...");
        foreach (var entityElement in allEntityElements)
        {
            ProcessSingleEntity(world, entityElement);
        }
        Console.WriteLine("Parser Second Pass: Completed.");
    }

    /// <summary>
    /// Processes a single entity element to set its parent and add all its components.
    /// </summary>
    private static void ProcessSingleEntity(World world, XElement entityElement)
    {
        string? entityName = GetEntityName(entityElement);
        if (string.IsNullOrEmpty(entityName))
        {
            return; // Skip entities without a name, already warned in Pass 1.
        }

        var entity = world.GetEntityByName(entityName);
        if (entity == null)
        {
            Console.WriteLine(
                $"Critical Error: Entity '{entityName}' was not found during the second pass. This should not happen."
            );
            return;
        }

        // Establish parent-child relationship.
        long? parentId = FindParentId(world, entityElement);
        if (parentId.HasValue)
        {
            entity.ParentId = parentId.Value;
        }

        // Process all child elements of <Entity> as components.
        foreach (XElement componentElement in entityElement.Elements())
        {
            string tableName = componentElement.Name.LocalName;

            // Skip special tags that are not components.
            if (
                tableName.Equals("Name", StringComparison.OrdinalIgnoreCase)
                || tableName.Equals("Children", StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            ProcessComponent(world, entity, componentElement, tableName);
        }
    }

    /// <summary>
    /// Processes a single component, including standard components and many-to-many relations.
    /// It now uses a predefined set of table names to identify many-to-many relationships, which is more robust.
    /// </summary>
    private static void ProcessComponent(
        World world,
        Entity entity,
        XElement componentElement,
        string tableName
    )
    {
        var dataToInsert = new Dictionary<string, object>();
        // Determine the component type based on its tag name, not its attributes.
        bool isRelation = RelationTableNames.Contains(tableName);

        // Populate data from attributes, resolving any entity references by name.
        foreach (XAttribute attribute in componentElement.Attributes())
        {
            string columnName = attribute.Name.LocalName;
            string value = attribute.Value;

            // Convention: If an attribute name ends in "ID", it is treated as a reference to another entity's name.
            if (columnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
            {
                var referencedEntity = world.GetEntityByName(value);
                if (referencedEntity != null)
                {
                    dataToInsert[columnName] = referencedEntity.Id;
                }
                else if (!string.IsNullOrEmpty(value)) // Don't warn for empty reference values
                {
                    Console.WriteLine(
                        $"Warning: Could not resolve entity name '{value}' for attribute '{columnName}' in component '{tableName}' of entity '{entity.Name}'. Skipping attribute."
                    );
                }
            }
            else
            {
                dataToInsert[attribute.Name.LocalName] = attribute.Value;
            }
        }

        // Finalize and insert the data based on whether it's a relation or a standard component.
        if (isRelation)
        {
            // The owner entity is implicitly EntityId1 if not provided in the XML.
            if (!dataToInsert.ContainsKey("EntityId1"))
            {
                dataToInsert["EntityId1"] = entity.Id;
            }

            // Ensure both IDs are present before trying to sort or insert.
            if (
                dataToInsert.TryGetValue("EntityId1", out object? id1Value)
                && dataToInsert.TryGetValue("EntityId2", out object? id2Value)
                && id1Value is long id1
                && id2Value is long id2
            )
            {
                // Ensure IDs are ordered to satisfy a potential CHECK constraint (e.g., EntityId1 < EntityId2).
                if (id1 > id2)
                {
                    dataToInsert["EntityId1"] = id2;
                    dataToInsert["EntityId2"] = id1;
                }

                try
                {
                    world.Query(tableName).Insert(dataToInsert);
                }
                catch (Exception ex)
                {
                    string data = string.Join(
                        ", ",
                        dataToInsert.Select(kvp => $"{kvp.Key}: {kvp.Value}")
                    );
                    Console.WriteLine(
                        $"Error inserting relation '{tableName}' for entity '{entity.Name}'. Data: [{data}]. Error: {ex.Message}"
                    );
                }
            }
            else
            {
                Console.WriteLine(
                    $"Warning: Skipping relation '{tableName}' for entity '{entity.Name}' because one or both entity IDs are missing or invalid."
                );
            }
        }
        else // It's a standard one-to-one component.
        {
            dataToInsert["EntityId"] = entity.Id;

            try
            {
                if (!entity.Has(tableName))
                {
                    world.Query(tableName).Insert(dataToInsert);
                }
            }
            catch (Exception ex)
            {
                string data = string.Join(
                    ", ",
                    dataToInsert.Select(kvp => $"{kvp.Key}: {kvp.Value}")
                );
                Console.WriteLine(
                    $"Error inserting component '{tableName}' for entity '{entity.Name}'. Data: [{data}]. Error: {ex.Message}"
                );
            }
        }
    }

    /// <summary>
    /// Determines the parent ID of an entity by checking for implicit nesting first,
    /// then for an explicit "Parent" attribute.
    /// </summary>
    /// <returns>The parent's entity ID, or null if no parent is found.</returns>
    private static long? FindParentId(World world, XElement entityElement)
    {
        // Priority 1: Implicit parent via nested <Entity><Children><Entity> structure.
        // The structure is: parent <Entity> -> <Children> -> current <Entity>.
        var parentOfChildren = entityElement.Parent;
        if (
            parentOfChildren != null
            && parentOfChildren.Name.LocalName.Equals(
                "Children",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            var parentEntityElement = parentOfChildren.Parent;
            if (
                parentEntityElement != null
                && parentEntityElement.Name.LocalName.Equals(
                    "Entity",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                string? parentName = GetEntityName(parentEntityElement);
                if (!string.IsNullOrEmpty(parentName))
                {
                    var parentEntity = world.GetEntityByName(parentName);
                    return parentEntity?.Id;
                }
            }
        }

        // Priority 2: Explicit Parent="Name" attribute on the <Entity> tag.
        var parentAttribute = entityElement.Attribute("Parent");
        if (parentAttribute != null && !string.IsNullOrEmpty(parentAttribute.Value))
        {
            var parentEntity = world.GetEntityByName(parentAttribute.Value);
            if (parentEntity == null)
            {
                Console.WriteLine(
                    $"Warning: Parent entity '{parentAttribute.Value}' not found for entity '{GetEntityName(entityElement)}'."
                );
            }
            return parentEntity?.Id;
        }

        return null;
    }

    /// <summary>
    /// Extracts the name of an entity from its corresponding Name element.
    /// It prefers a "Value" attribute but falls back to the element's inner text.
    /// </summary>
    /// <returns>The entity name, or null if not found.</returns>
    private static string? GetEntityName(XElement entityElement)
    {
        var nameElement = entityElement.Element("Name");
        if (nameElement != null)
        {
            return (string?)nameElement.Attribute("Value") ?? nameElement.Value;
        }
        return null;
    }
}
