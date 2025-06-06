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

*/

/// <summary>
/// Parses the data found.
/// </summary>
public static class DataParser
{
    /// <summary>
    /// Parses a consolidated list of entity XElements and populates the world.
    /// It uses a two-pass approach: first creating all entities, then setting up components and relationships.
    /// </summary>
    /// <param name="world">The world instance to populate.</param>
    /// <param name="allEntityElements">A list of all Entity XElements from all data files.</param>
    public static void ParseAndLoad(World world, List<XElement> allEntityElements)
    {
        try
        {
            // First pass: create all entities to ensure they exist before setting up parent relationships.
            // This pass guarantees that any entity can be found by name in the second pass, regardless of file order.
            Console.WriteLine("Parser First Pass: Creating all entities...");
            foreach (XElement entityElement in allEntityElements)
            {
                var nameElement = entityElement.Element("Name");
                string? entityName = null;

                if (nameElement != null)
                {
                    entityName = (string?)nameElement.Attribute("Value") ?? nameElement.Value;
                }

                if (string.IsNullOrEmpty(entityName))
                {
                    var firstComponent = entityElement.Elements().FirstOrDefault();
                    if (firstComponent != null)
                    {
                        var keyAttribute = firstComponent.Attribute("Key");
                        if (keyAttribute != null)
                        {
                            entityName = keyAttribute.Value;
                        }
                    }
                }

                // Create the entity if it has a name. world.Entity handles duplicates.
                if (!string.IsNullOrEmpty(entityName))
                {
                    world.Entity(entityName);
                }
            }
            Console.WriteLine("Parser First Pass: Completed.");

            // Second pass: process components and parent relationships now that all entities are guaranteed to exist.
            Console.WriteLine("Parser Second Pass: Processing components and relationships...");
            foreach (XElement entityElement in allEntityElements)
            {
                var nameElement = entityElement.Element("Name");
                string? entityName = null;
                if (nameElement != null)
                {
                    entityName = (string?)nameElement.Attribute("Value") ?? nameElement.Value;
                }

                if (string.IsNullOrEmpty(entityName))
                {
                    var firstComponent = entityElement.Elements().FirstOrDefault();
                    if (firstComponent != null)
                    {
                        var keyAttribute = firstComponent.Attribute("Key");
                        if (keyAttribute != null)
                        {
                            entityName = keyAttribute.Value;
                        }
                    }
                }

                if (string.IsNullOrEmpty(entityName))
                {
                    Console.WriteLine("Warning: Skipping an <Entity> in second pass because it has no identifiable name or key.");
                    continue;
                }

                var entity = world.GetEntityByName(entityName);
                if (entity == null)
                {
                    Console.WriteLine($"Error: Could not retrieve entity '{entityName}' during the second pass. It may not have been created correctly in the first pass.");
                    continue;
                }

                // Handle Parent relationship
                var parentAttribute = entityElement.Attribute("Parent");
                if (parentAttribute != null && !string.IsNullOrEmpty(parentAttribute.Value))
                {
                    var parentEntity = world.GetEntityByName(parentAttribute.Value);
                    if (parentEntity != null)
                    {
                        entity.ParentId = parentEntity.Id;
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Parent entity '{parentAttribute.Value}' not found for entity '{entityName}'.");
                    }
                }

                // Iterate over all child elements (components) of the <Entity>
                foreach (XElement componentElement in entityElement.Elements())
                {
                    string tableName = componentElement.Name.LocalName;

                    if (tableName == "Name")
                    {
                        continue;
                    }

                    var componentData = new Dictionary<string, object>
                        {
                            { "EntityId", entity.Id }
                        };

                    foreach (XAttribute attribute in componentElement.Attributes())
                    {
                        componentData[attribute.Name.LocalName] = attribute.Value;
                    }

                    try
                    {
                        if (!entity.Has(tableName))
                        {
                            world.Query(tableName).Insert(componentData);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error inserting component '{tableName}' for entity '{entityName}': {ex.Message}");
                    }
                }
            }
            Console.WriteLine("Parser Second Pass: Completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred during XML parsing: {ex.Message}");
        }
    }
}
