#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Xunit;
using System.Linq;
using AyanamisTower.StellaEcs;
using System.Collections.Generic;

namespace AyanamisTower.StellaEcs.Tests;

// --- Test Relationship Structs ---
// A standard, one-way relationship
public struct IsChildOf : IRelationship { }
// A one-way relationship
public struct IsFriendOf : IRelationship { }
// A new, two-way relationship
public struct IsMarriedTo : IBidirectionalRelationship { }
public struct FriendsWith : IRelationship
{
    public int Strength;
}
public class RelationshipTests
{
    private readonly World _world;

    public RelationshipTests()
    {
        _world = new World();
        _world.RegisterComponent<Position>();
        _world.RegisterRelationship<IsChildOf>();
        _world.RegisterRelationship<IsFriendOf>();
        // Register the new bidirectional relationship
        _world.RegisterRelationship<IsMarriedTo>();
        _world.RegisterRelationship<FriendsWith>();
    }

    [Fact]
    public void Add_And_Has_Relationship_Succeeds()
    {
        // Arrange
        var parent = _world.CreateEntity();
        var child = _world.CreateEntity();

        // Act
        _world.AddRelationship<IsChildOf>(child, parent);

        // Assert
        Assert.True(_world.HasRelationship<IsChildOf>(child, parent));
    }

    [Fact]
    public void Has_ForNonExistentRelationship_ReturnsFalse()
    {
        // Arrange
        var parent = _world.CreateEntity();
        var child = _world.CreateEntity();
        var other = _world.CreateEntity();
        _world.AddRelationship<IsChildOf>(child, parent);

        // Assert
        Assert.False(_world.HasRelationship<IsChildOf>(parent, child)); // Wrong direction
        Assert.False(_world.HasRelationship<IsChildOf>(child, other));  // Wrong target
        Assert.False(_world.HasRelationship<IsFriendOf>(child, parent)); // Wrong type
    }

    [Fact]
    public void Remove_Relationship_Succeeds()
    {
        // Arrange
        var parent = _world.CreateEntity();
        var child = _world.CreateEntity();
        _world.AddRelationship<IsChildOf>(child, parent);
        Assert.True(_world.HasRelationship<IsChildOf>(child, parent), "Precondition: Relationship should exist.");

        // Act
        _world.RemoveRelationship<IsChildOf>(child, parent);

        // Assert
        Assert.False(_world.HasRelationship<IsChildOf>(child, parent));
    }

    [Fact]
    public void GetRelationshipTargets_ReturnsCorrectEntities()
    {
        // Arrange
        var source = _world.CreateEntity();
        var target1 = _world.CreateEntity();
        var target2 = _world.CreateEntity();
        var other = _world.CreateEntity();

        _world.AddRelationship<IsFriendOf>(source, target1);
        _world.AddRelationship<IsFriendOf>(source, target2);

        // Act
        var targets = _world.GetRelationshipTargets<IsFriendOf>(source).ToList();

        // Assert
        Assert.Equal(2, targets.Count);
        Assert.Contains(target1, targets);
        Assert.Contains(target2, targets);
        Assert.DoesNotContain(other, targets);
    }

    [Fact]
    public void GetSources_ReturnsCorrectEntities()
    {
        // Arrange
        var target = _world.CreateEntity();
        var source1 = _world.CreateEntity();
        var source2 = _world.CreateEntity();
        var other = _world.CreateEntity();

        _world.AddRelationship<IsChildOf>(source1, target);
        _world.AddRelationship<IsChildOf>(source2, target);

        // Act
        // We need to get the storage to test GetSources, as it's not exposed on World
        var storage = _world.GetRelationshipStorageUnsafe(typeof(IsChildOf));
        var sources = storage.GetSources(target).ToList();

        // Assert
        Assert.Equal(2, sources.Count);
        // Note: We check by ID because the returned entity from GetSources might have a different generation
        // if the original source entity was destroyed and recycled. The current implementation correctly stores
        // the full handle, so this test is robust.
        Assert.Contains(sources, s => s.Id == source1.Id);
        Assert.Contains(sources, s => s.Id == source2.Id);
        Assert.DoesNotContain(sources, s => s.Id == other.Id);
    }

    [Fact]
    public void DestroyEntity_AsSource_RemovesRelationship()
    {
        // Arrange
        var source = _world.CreateEntity();
        var target = _world.CreateEntity();
        _world.AddRelationship<IsFriendOf>(source, target);
        Assert.True(_world.HasRelationship<IsFriendOf>(source, target), "Precondition: Relationship should exist.");

        // Act
        source.Destroy();

        // Assert
        Assert.False(source.IsAlive());
        // The HasRelationship check should fail because the source is dead
        Assert.False(_world.HasRelationship<IsFriendOf>(source, target));

        // Also check the reverse mapping is gone
        var storage = _world.GetRelationshipStorageUnsafe(typeof(IsFriendOf));
        Assert.Empty(storage.GetSources(target));
    }

    [Fact]
    public void DestroyEntity_AsTarget_RemovesRelationship()
    {
        // Arrange
        var source = _world.CreateEntity();
        var target = _world.CreateEntity();
        _world.AddRelationship<IsFriendOf>(source, target);
        Assert.True(_world.HasRelationship<IsFriendOf>(source, target), "Precondition: Relationship should exist.");

        // Act
        target.Destroy();

        // Assert
        Assert.False(target.IsAlive());
        Assert.False(_world.HasRelationship<IsFriendOf>(source, target));

        // Also check the forward mapping is gone
        Assert.Empty(_world.GetRelationshipTargets<IsFriendOf>(source));
    }

    [Fact]
    public void Query_WithRelationship_ReturnsCorrectEntities()
    {
        // Arrange
        var parent1 = _world.CreateEntity(); parent1.Add(new Position());
        var parent2 = _world.CreateEntity(); parent2.Add(new Position());

        var child1 = _world.CreateEntity(); child1.Add(new Position());
        var child2 = _world.CreateEntity(); child2.Add(new Position());
        var child3 = _world.CreateEntity(); child3.Add(new Position());

        _world.AddRelationship<IsChildOf>(child1, parent1);
        _world.AddRelationship<IsChildOf>(child2, parent2);
        _world.AddRelationship<IsChildOf>(child3, parent1);

        // Act: Find all entities with a Position that are a child of parent1
        var query = _world.Query()
            .With<Position>()
            .With<IsChildOf>(parent1)
            .Build();

        var results = new List<Entity>();
        foreach (var e in query) results.Add(e);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(child1, results);
        Assert.Contains(child3, results);
        Assert.DoesNotContain(child2, results);
    }

    [Fact]
    public void Query_WithMultipleRelationships_ReturnsCorrectEntities()
    {
        // Arrange
        var parent = _world.CreateEntity(); parent.Add(new Position());
        var friend = _world.CreateEntity(); friend.Add(new Position());

        var entity1 = _world.CreateEntity(); entity1.Add(new Position()); // Is child of parent AND friend of friend
        var entity2 = _world.CreateEntity(); entity2.Add(new Position()); // Is child of parent only
        var entity3 = _world.CreateEntity(); entity3.Add(new Position()); // Is friend of friend only

        _world.AddRelationship<IsChildOf>(entity1, parent);
        _world.AddRelationship<IsFriendOf>(entity1, friend);

        _world.AddRelationship<IsChildOf>(entity2, parent);
        _world.AddRelationship<IsFriendOf>(entity3, friend);


        // Act: Find all entities that are a child of parent AND a friend of friend
        var query = _world.Query()
            .With<Position>()
            .With<IsChildOf>(parent)
            .With<IsFriendOf>(friend)
            .Build();

        var results = new List<Entity>();
        foreach (var e in query) results.Add(e);

        // Assert
        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void Add_BidirectionalRelationship_CreatesBothDirections()
    {
        // Arrange
        var entityA = _world.CreateEntity();
        var entityB = _world.CreateEntity();

        // Act
        _world.AddRelationship<IsMarriedTo>(entityA, entityB);

        // Assert
        Assert.True(_world.HasRelationship<IsMarriedTo>(entityA, entityB), "A -> B should exist.");
        Assert.True(_world.HasRelationship<IsMarriedTo>(entityB, entityA), "B -> A should also exist.");
    }

    [Fact]
    public void Remove_BidirectionalRelationship_RemovesBothDirections()
    {
        // Arrange
        var entityA = _world.CreateEntity();
        var entityB = _world.CreateEntity();
        _world.AddRelationship<IsMarriedTo>(entityA, entityB);
        Assert.True(_world.HasRelationship<IsMarriedTo>(entityA, entityB), "Precondition failed: A -> B should exist.");
        Assert.True(_world.HasRelationship<IsMarriedTo>(entityB, entityA), "Precondition failed: B -> A should exist.");

        // Act: Remove from one side
        _world.RemoveRelationship<IsMarriedTo>(entityA, entityB);

        // Assert
        Assert.False(_world.HasRelationship<IsMarriedTo>(entityA, entityB), "A -> B should be removed.");
        Assert.False(_world.HasRelationship<IsMarriedTo>(entityB, entityA), "B -> A should also be removed.");
    }

    [Fact]
    public void Remove_BidirectionalRelationship_FromOtherSide_RemovesBothDirections()
    {
        // Arrange
        var entityA = _world.CreateEntity();
        var entityB = _world.CreateEntity();
        _world.AddRelationship<IsMarriedTo>(entityA, entityB);
        Assert.True(_world.HasRelationship<IsMarriedTo>(entityA, entityB), "Precondition failed: A -> B should exist.");
        Assert.True(_world.HasRelationship<IsMarriedTo>(entityB, entityA), "Precondition failed: B -> A should exist.");

        // Act: Remove from the other side
        _world.RemoveRelationship<IsMarriedTo>(entityB, entityA);

        // Assert
        Assert.False(_world.HasRelationship<IsMarriedTo>(entityA, entityB), "A -> B should be removed.");
        Assert.False(_world.HasRelationship<IsMarriedTo>(entityB, entityA), "B -> A should also be removed.");
    }

    [Fact]
    public void GetTargets_ForBidirectionalRelationship_WorksBothWays()
    {
        // Arrange
        var entityA = _world.CreateEntity();
        var entityB = _world.CreateEntity();
        _world.AddRelationship<IsMarriedTo>(entityA, entityB);

        // Act
        var targetsOfA = _world.GetRelationshipTargets<IsMarriedTo>(entityA).ToList();
        var targetsOfB = _world.GetRelationshipTargets<IsMarriedTo>(entityB).ToList();

        // Assert
        Assert.Single(targetsOfA);
        Assert.Contains(entityB, targetsOfA);

        Assert.Single(targetsOfB);
        Assert.Contains(entityA, targetsOfB);
    }

    [Fact]
    public void DestroyEntity_InBidirectionalRelationship_CleansUpBothSides()
    {
        // Arrange
        var entityA = _world.CreateEntity();
        var entityB = _world.CreateEntity();
        _world.AddRelationship<IsMarriedTo>(entityA, entityB);

        // Act
        entityA.Destroy();

        // Assert
        Assert.False(entityA.IsAlive());
        Assert.False(_world.HasRelationship<IsMarriedTo>(entityA, entityB));
        Assert.False(_world.HasRelationship<IsMarriedTo>(entityB, entityA));

        // Check that B no longer has A as a target
        var targetsOfB = _world.GetRelationshipTargets<IsMarriedTo>(entityB).ToList();
        Assert.Empty(targetsOfB);
    }

    [Fact]
    public void Query_WithBidirectionalRelationship_WorksFromEitherSide()
    {
        // Arrange
        var entityA = _world.CreateEntity(); entityA.Add(new Position());
        var entityB = _world.CreateEntity(); entityB.Add(new Position());
        var entityC = _world.CreateEntity(); entityC.Add(new Position());

        _world.AddRelationship<IsMarriedTo>(entityA, entityB);

        // Act
        // Query for entities married to B
        var query1 = _world.Query()
            .With<Position>()
            .With<IsMarriedTo>(entityB)
            .Build();

        var results1 = new List<Entity>();
        foreach (var e in query1) results1.Add(e);

        // Query for entities married to A
        var query2 = _world.Query()
            .With<Position>()
            .With<IsMarriedTo>(entityA)
            .Build();

        var results2 = new List<Entity>();
        foreach (var e in query2) results2.Add(e);

        // Assert
        Assert.Single(results1);
        Assert.Contains(entityA, results1);

        Assert.Single(results2);
        Assert.Contains(entityB, results2);
    }

    [Fact]
    public void Query_RelationshipWithData()
    {
        // 3. Create entities
        var player1 = _world.CreateEntity();
        var player2 = _world.CreateEntity();

        // 4. Add the relationship with its data
        player1.AddRelationship(player2, new FriendsWith { Strength = 20 });

        // 5. Check for and retrieve the relationship data
        if (player1.TryGetRelationship(player2, out FriendsWith friendship))
        {
            Assert.Equal(20, friendship.Strength);
        }
        else
        {
            Assert.Fail("Friendship data was not found");
        }

        // You can still get targets just like before
        var friendsOfPlayer1 = player1.GetRelationshipTargets<FriendsWith>().ToList();
        Assert.Single(friendsOfPlayer1);
    }
}
