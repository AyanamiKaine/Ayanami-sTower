using SqlKata.Execution;

namespace AyanamisTower.StellaDB.Tests;

/// <summary>
/// Unit tests for the World
/// </summary>
public class WorldTests
{
    /// <summary>
    /// We want to easily create entities that are part of a world.
    /// </summary>
    [Fact]
    public void CreateEntities()
    {
        // Creating an in memory test world
        var world = new World("TEST", true);

        var _ = world.Entity();
        var e = world.Entity();

        Assert.NotEqual(0, e.Id);
    }

    /// <summary>
    /// Entites have names in a seperate name table, we should be able to define a name for an entity,
    /// and query entities by name
    /// </summary>
    [Fact]
    public void CreateEntitiesByName()
    {
        // Creating an in memory test world
        var world = new World("TEST", true);

        var _ = world.Entity();
        var e = world.Entity("Entity2");

        Assert.Equal("Entity2", e.Name);
    }

    /// <summary>
    /// Changing the entity property name should change it also in the world
    /// </summary>
    [Fact]
    public void ChangeEntityName()
    {
        // Creating an in memory test world
        var world = new World("TEST", true);

        var _ = world.Entity();
        var e = world.Entity("Entity2"); // Inital entity name

        e.Name = "ChangedName"; // new and expected entity name

        var result = world.Query("Name")
                          .Where("Value", "=", "ChangedName")
                          .Select("Value")
                          .FirstOrDefault<string?>();

        Assert.Equal("ChangedName", result);
    }


    /// <summary>
    /// Entities can have parents
    /// </summary>
    [Fact]
    public void DefineParent()
    {
        // Creating an in memory test world
        var world = new World("TEST", true);

        var parent = world.Entity("Parent");
        var child = world.Entity("Child");
        child.ParentId = parent.Id;

        var parentId = world.Query("Entity")
                        .Where("Id", child.Id)
                        .Select("ParentId")
                        .FirstOrDefault<long?>();

        Assert.Equal(parent.Id, parentId);
    }

    /// <summary>
    /// When we delete a parent it should cascade and delete all children too
    /// </summary>
    [Fact]
    public void DeleteParentDeletesAllChildren()
    {
        // Creating an in memory test world
        var world = new World("TEST", true);

        var parent = world.Entity("Parent");
        var child = world.Entity("Child");
        child.ParentId = parent.Id;

        world.Query("Entity").Where("Id", parent.Id).Delete();


        var parentId = world.Query("Entity")
                        .Where("Id", child.Id)
                        .Select("ParentId")
                        .FirstOrDefault<long>();

        // If no entity is found we expect it to returnt he default value of 0
        Assert.Equal(0, parentId);
    }

    /// <summary>
    /// We should be able to get entites by name
    /// </summary>
    [Fact]
    public void GetEntityByName()
    {
        // Creating an in memory test world
        var world = new World("TEST", true);

        var _ = world.Entity();
        var e = world.Entity("Entity2"); // Inital entity name

        e.Name = "ChangedName"; // new name

        var result = world.Query("Name") // Start with the "Name" table where the 'Value' is
                                .Where("Value", e.Name)
                                .Select("EntityId")
                                .FirstOrDefault<long>();


        Assert.Equal(e, new Entity { Id = result, World = world });
    }

    /// <summary>
    /// We want to able to see all children of an entity.
    /// </summary>
    [Fact]
    public void GetChildren()
    {
        // Creating an in memory test world
        var world = new World("TEST", true);

        var parent = world.Entity("Parent");

        var childA = world.Entity("ChildA");
        childA.ParentId = parent.Id;
        var childB = world.Entity("ChildB");
        childB.ParentId = parent.Id;
        var childC = world.Entity("ChildC");
        childC.ParentId = parent.Id;
        var childD = world.Entity("ChildD");
        childD.ParentId = parent.Id;


        var childrenIds = world.Query("Entity")
                       .Where("ParentId", parent.Id) // Filter by ParentId
                       .Select("Id")                 // We only need the Ids of the children
                       .Get<long>();                 // Execute the query and get a collection of long (the Ids)

        const int expectedChildrenCount = 4;
        var actualChildrenCount = childrenIds.Count();
        Assert.Equal(expectedChildrenCount, actualChildrenCount);
    }


    /// <summary>
    /// Entities with the same id are the same entity.
    /// </summary>
    [Fact]
    public void EntityEquality()
    {
        // Creating an in memory test world
        var world = new World("TEST", true);

        var _ = world.Entity();
        var e = world.Entity("Entity2"); // Inital entity name

        e.Name = "ChangedName"; // new name

        var result = world.Query("Name") // Start with the "Name" table where the 'Value' is
                                .Where("Value", e.Name)
                                .Select("EntityId")
                                .FirstOrDefault<long>();


        Assert.Equal(e, new Entity { Id = result, World = world });

    }

    /// <summary>
    /// Here we try to adding an age component to an entity,
    /// it shows how we can easily add a primitive value component (One to One Relation)
    /// One entity has one age.
    /// </summary>
    [Fact]
    public void AddAgeComponent()
    {
        // Creating an in memory test world
        var world = new World("TEST", true);

        var e = world.Entity("EntityA");

        const int expectedAge = 24;

        world.Query("Age")
             .Insert(new { EntityId = e.Id, Value = expectedAge });

        var actualAge = world.Query("Age")
                        .Where("EntityId", e.Id)
                        .Select("Value")
                        .FirstOrDefault<int>();

        Assert.Equal(expectedAge, actualAge);
    }

    // DTO for ConnectedTo table rows to avoid using 'dynamic'
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class ConnectedToDto
    {
        public long EntityId1 { get; set; }
        public long EntityId2 { get; set; }
        public double Distance { get; set; }
    }

    /// <summary>
    /// Tests creating, querying, and constraints of the ConnectedTo table.
    /// Assumes StarSystem table is created by schema (EntityId PRIMARY KEY, FOREIGN KEY (EntityId) REFERENCES Entity(Id)).
    /// This shows the many to many relationship. As many star systems can be connected to many other.
    /// </summary>
    [Fact]
    public void StarSystemConnections_ManagesAndQueriesCorrectly()
    {
        // Arrange
        var world = new World("TEST", true);

        // Helper to create StarSystems. This assumes that creating an Entity
        // and then inserting its ID into a 'StarSystem' table makes it a valid StarSystem
        // for the FOREIGN KEY constraints in 'ConnectedTo'.
        Entity CreateStarSystem(string name = "")
        {
            var entity = world.Entity(name);
            // This insert is crucial for the FOREIGN KEY in ConnectedTo to work,
            // assuming ConnectedTo.SystemId1/2 REFERENCES StarSystem.EntityId.
            world.Query("StarSystem").Insert(new { EntityId = entity.Id });
            return entity;
        }

        var systemA = CreateStarSystem("SystemA"); // e.g., Id = 1
        var systemB = CreateStarSystem("SystemB"); // e.g., Id = 2
        var systemC = CreateStarSystem("SystemC"); // e.g., Id = 3
        var systemD = CreateStarSystem("SystemD"); // e.g., Id = 4 (initially unconnected)

        // Helper to add connection, ensuring SystemId1 < SystemId2 for the CHECK constraint
        void AddConnection(long entityId1, long entityId2, double distance)
        {
            long s1 = Math.Min(entityId1, entityId2);
            long s2 = Math.Max(entityId1, entityId2);

            if (s1 == s2)
                throw new ArgumentException("Cannot connect a system to itself with different IDs.");

            world.Query("ConnectedTo").Insert(new { EntityId1 = s1, EntityId2 = s2, Distance = distance });
        }

        // Act & Assert: Create connections
        AddConnection(systemA.Id, systemB.Id, 10.5); // A(1)-B(2)
        AddConnection(systemB.Id, systemC.Id, 20.2); // B(2)-C(3)

        // Assert: Verify connections were created
        var connectionAB = world.Query("ConnectedTo")
            .Where("EntityId1", Math.Min(systemA.Id, systemB.Id))
            .Where("EntityId2", Math.Max(systemA.Id, systemB.Id))
            .FirstOrDefault<ConnectedToDto>();

        Assert.NotNull(connectionAB);
        Assert.Equal(10.5, connectionAB.Distance);

        // Act & Assert: Query all connections for a specific system (e.g., SystemB)
        long targetSystemIdB = systemB.Id;
        var connectionsOfB_Raw = world.Query("ConnectedTo")
            .Where(q => q.Where("EntityId1", targetSystemIdB).OrWhere("EntityId2", targetSystemIdB))
            .Get<ConnectedToDto>();

        var connectedSystemsToB = new List<(long connectedId, double distance)>();
        foreach (var row in connectionsOfB_Raw)
        {
            if (row.EntityId1 == targetSystemIdB)
            {
                connectedSystemsToB.Add((row.EntityId2, row.Distance));
            }
            else if (row.EntityId2 == targetSystemIdB) // Ensure it's not adding itself if SystemId1 was also target
            {
                connectedSystemsToB.Add((row.EntityId1, row.Distance));
            }
        }

        Assert.Equal(2, connectedSystemsToB.Count);
        Assert.Contains((systemA.Id, 10.5), connectedSystemsToB);
        Assert.Contains((systemC.Id, 20.2), connectedSystemsToB);
        Assert.DoesNotContain((systemD.Id, 0.0), connectedSystemsToB); // Check an unconnected system isn't there

        // Act & Assert: Check if two specific systems are connected (A and B)
        // Query respects CHECK (SystemId1 < SystemId2)
        bool areABConnected = world.Query("ConnectedTo")
            .Where("EntityId1", Math.Min(systemA.Id, systemB.Id))
            .Where("EntityId2", Math.Max(systemA.Id, systemB.Id))
            .Count<int>() > 0;
        Assert.True(areABConnected, "System A and B should be connected.");

        // Act & Assert: Check if two specific systems are NOT directly connected (A and C)
        bool areACConnected = world.Query("ConnectedTo")
            .Where("EntityId1", Math.Min(systemA.Id, systemC.Id))
            .Where("EntityId2", Math.Max(systemA.Id, systemC.Id))
            .Count<int>() > 0;
        Assert.False(areACConnected, "System A and C should NOT be directly connected.");
    }
}
