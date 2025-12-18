using InvictaDB;
using StellaInvicta.Data;
using StellaInvicta.Query;

namespace StellaInvicta.Tests;

/// <summary>
/// Tests for CharacterTraitExtensions and RelationshipQueryExtensions.
/// </summary>
public class QueryExtensionsUnitTest
{
    #region Test Setup

    private static InvictaDatabase CreateTestDatabase()
    {
        return new InvictaDatabase()
            .RegisterTable<Character>()
            .RegisterTable<Trait>()
            .RegisterTable<CharacterTrait>()
            .RegisterTable<RelationshipType>()
            .RegisterTable<Relationship>();
    }

    private static Character CreateCharacter(string name) =>
        new(name, 25, 10, 10, 10, 10, new DateTime(1, 1, 1));

    #endregion

    #region CharacterTrait Extension Tests

    /// <summary>
    /// AddTrait extension should add a trait to a character.
    /// </summary>
    [Fact]
    public void AddTrait_AddsTraitToCharacter()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("brave", new Trait("Brave", "Courageous"));

        // Use extension method
        db = db.AddTrait("alice", "brave");

        Assert.True(db.HasTrait("alice", "brave"));
    }

    /// <summary>
    /// AddTraits extension should add multiple traits at once.
    /// </summary>
    [Fact]
    public void AddTraits_AddsMultipleTraits()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("brave", new Trait("Brave", "Courageous"))
            .Insert("clever", new Trait("Clever", "Smart"))
            .Insert("strong", new Trait("Strong", "Powerful"));

        // Add multiple traits at once
        db = db.AddTraits("alice", "brave", "clever", "strong");

        Assert.True(db.HasTrait("alice", "brave"));
        Assert.True(db.HasTrait("alice", "clever"));
        Assert.True(db.HasTrait("alice", "strong"));
        Assert.Equal(3, db.CountTraits("alice"));
    }

    /// <summary>
    /// GetTraitIds should return all trait IDs for a character.
    /// </summary>
    [Fact]
    public void GetTraitIds_ReturnsAllTraitIds()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("brave", new Trait("Brave", "Courageous"))
            .Insert("clever", new Trait("Clever", "Smart"))
            .AddTrait("alice", "brave")
            .AddTrait("alice", "clever");

        var traitIds = db.GetTraitIds("alice").ToList();

        Assert.Equal(2, traitIds.Count);
        Assert.Contains("brave", traitIds);
        Assert.Contains("clever", traitIds);
    }

    /// <summary>
    /// GetTraits should return full Trait objects for a character.
    /// </summary>
    [Fact]
    public void GetTraits_ReturnsFullTraitObjects()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("brave", new Trait("Brave", "Courageous"))
            .Insert("clever", new Trait("Clever", "Smart"))
            .AddTrait("alice", "brave")
            .AddTrait("alice", "clever");

        var traits = db.GetTraits("alice").ToList();

        Assert.Equal(2, traits.Count);
        Assert.Contains(traits, t => t.Name == "Brave");
        Assert.Contains(traits, t => t.Name == "Clever");
    }

    /// <summary>
    /// GetCharactersWithTrait should return all characters that have a specific trait.
    /// </summary>
    [Fact]
    public void GetCharactersWithTrait_ReturnsMatchingCharacters()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("charlie", CreateCharacter("Charlie"))
            .Insert("brave", new Trait("Brave", "Courageous"))
            .AddTrait("alice", "brave")
            .AddTrait("charlie", "brave");

        var braveCharacters = db.GetCharactersWithTrait("brave").ToList();

        Assert.Equal(2, braveCharacters.Count);
        Assert.Contains(braveCharacters, c => c.Name == "Alice");
        Assert.Contains(braveCharacters, c => c.Name == "Charlie");
    }

    /// <summary>
    /// RemoveTrait should remove a trait from a character.
    /// </summary>
    [Fact]
    public void RemoveTrait_RemovesTraitFromCharacter()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("brave", new Trait("Brave", "Courageous"))
            .AddTrait("alice", "brave");

        Assert.True(db.HasTrait("alice", "brave"));

        db = db.RemoveTrait("alice", "brave");

        Assert.False(db.HasTrait("alice", "brave"));
    }

    /// <summary>
    /// RemoveAllTraits should remove all traits from a character.
    /// </summary>
    [Fact]
    public void RemoveAllTraits_RemovesAllTraitsFromCharacter()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("brave", new Trait("Brave", "Courageous"))
            .Insert("clever", new Trait("Clever", "Smart"))
            .AddTraits("alice", "brave", "clever");

        Assert.Equal(2, db.CountTraits("alice"));

        db = db.RemoveAllTraits("alice");

        Assert.Equal(0, db.CountTraits("alice"));
    }

    #endregion

    #region Relationship Extension Tests

    /// <summary>
    /// AddCharacterRelationship should create a relationship between characters.
    /// </summary>
    [Fact]
    public void AddCharacterRelationship_CreatesRelationship()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("friend", new RelationshipType("Friend", true));

        db = db.AddCharacterRelationship("alice", "bob", "friend", 50);

        Assert.True(db.HasRelationship("alice", "bob", "friend"));
        Assert.Equal(50, db.GetRelationshipStrength("alice", "bob", "friend"));
    }

    /// <summary>
    /// AddSymmetricRelationship should create relationships in both directions.
    /// </summary>
    [Fact]
    public void AddSymmetricRelationship_CreatesBothDirections()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("friend", new RelationshipType("Friend", true));

        db = db.AddSymmetricRelationship("alice", "bob", nameof(Character), "friend", 75);

        Assert.True(db.HasRelationship("alice", "bob", "friend"));
        Assert.True(db.HasRelationship("bob", "alice", "friend"));
    }

    /// <summary>
    /// GetOutgoingRelationships should return all outgoing relationships.
    /// </summary>
    [Fact]
    public void GetOutgoingRelationships_ReturnsCorrectRelationships()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("charlie", CreateCharacter("Charlie"))
            .Insert("friend", new RelationshipType("Friend", true))
            .AddCharacterRelationship("alice", "bob", "friend", 50)
            .AddCharacterRelationship("alice", "charlie", "friend", 60);

        var relationships = db.GetOutgoingRelationships("alice").ToList();

        Assert.Equal(2, relationships.Count);
    }

    /// <summary>
    /// GetIncomingRelationships should return all incoming relationships.
    /// </summary>
    [Fact]
    public void GetIncomingRelationships_ReturnsCorrectRelationships()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("charlie", CreateCharacter("Charlie"))
            .Insert("friend", new RelationshipType("Friend", true))
            .AddCharacterRelationship("bob", "alice", "friend", 50)
            .AddCharacterRelationship("charlie", "alice", "friend", 60);

        var relationships = db.GetIncomingRelationships("alice").ToList();

        Assert.Equal(2, relationships.Count);
    }

    /// <summary>
    /// GetRelatedCharacterIds should return only character-to-character relations.
    /// </summary>
    [Fact]
    public void GetRelatedCharacterIds_ReturnsOnlyCharacterRelations()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("charlie", CreateCharacter("Charlie"))
            .Insert("friend", new RelationshipType("Friend", true))
            .AddCharacterRelationship("alice", "bob", "friend")
            .AddCharacterRelationship("alice", "charlie", "friend");

        var friendIds = db.GetRelatedCharacterIds("alice", "friend").ToList();

        Assert.Equal(2, friendIds.Count);
        Assert.Contains("bob", friendIds);
        Assert.Contains("charlie", friendIds);
    }

    /// <summary>
    /// GetRelatedCharacters should return full Character objects.
    /// </summary>
    [Fact]
    public void GetRelatedCharacters_ReturnsFullCharacterObjects()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("friend", new RelationshipType("Friend", true))
            .AddCharacterRelationship("alice", "bob", "friend");

        var friends = db.GetRelatedCharacters("alice", "friend").ToList();

        Assert.Single(friends);
        Assert.Equal("Bob", friends[0].Name);
    }

    /// <summary>
    /// GetPositiveRelationships and GetNegativeRelationships should filter by strength.
    /// </summary>
    [Fact]
    public void GetPositiveRelationships_FiltersCorrectly()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("charlie", CreateCharacter("Charlie"))
            .Insert("opinion", new RelationshipType("Opinion", false))
            .AddCharacterRelationship("alice", "bob", "opinion", 50)
            .AddCharacterRelationship("alice", "charlie", "opinion", -30);

        var positive = db.GetPositiveRelationships("alice").ToList();
        var negative = db.GetNegativeRelationships("alice").ToList();

        Assert.Single(positive);
        Assert.Equal("bob", positive[0].TargetId);
        Assert.Single(negative);
        Assert.Equal("charlie", negative[0].TargetId);
    }

    /// <summary>
    /// GetMutualRelations should find common relations between two characters.
    /// </summary>
    [Fact]
    public void GetMutualRelations_FindsCommonRelations()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("charlie", CreateCharacter("Charlie"))
            .Insert("diana", CreateCharacter("Diana"))
            .Insert("friend", new RelationshipType("Friend", true))
            // Alice friends: Bob, Charlie
            .AddCharacterRelationship("alice", "bob", "friend")
            .AddCharacterRelationship("alice", "charlie", "friend")
            // Bob friends: Alice, Charlie, Diana
            .AddCharacterRelationship("bob", "alice", "friend")
            .AddCharacterRelationship("bob", "charlie", "friend")
            .AddCharacterRelationship("bob", "diana", "friend");

        var mutualFriends = db.GetMutualRelations("alice", "bob", "friend").ToList();

        // Only Charlie is friends with both Alice and Bob
        Assert.Single(mutualFriends);
        Assert.Contains("charlie", mutualFriends);
    }

    /// <summary>
    /// UpdateRelationshipStrength should update an existing relationship's strength.
    /// </summary>
    [Fact]
    public void UpdateRelationshipStrength_UpdatesExisting()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("friend", new RelationshipType("Friend", true))
            .AddCharacterRelationship("alice", "bob", "friend", 50);

        Assert.Equal(50, db.GetRelationshipStrength("alice", "bob", "friend"));

        db = db.UpdateRelationshipStrength("alice", "bob", "friend", 90);

        Assert.Equal(90, db.GetRelationshipStrength("alice", "bob", "friend"));
    }

    /// <summary>
    /// ModifyRelationshipStrength should apply a delta to relationship strength.
    /// </summary>
    [Fact]
    public void ModifyRelationshipStrength_AppliesDelta()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("friend", new RelationshipType("Friend", true))
            .AddCharacterRelationship("alice", "bob", "friend", 50);

        // Increase by 25
        db = db.ModifyRelationshipStrength("alice", "bob", "friend", 25);
        Assert.Equal(75, db.GetRelationshipStrength("alice", "bob", "friend"));

        // Decrease by 10
        db = db.ModifyRelationshipStrength("alice", "bob", "friend", -10);
        Assert.Equal(65, db.GetRelationshipStrength("alice", "bob", "friend"));
    }

    /// <summary>
    /// ModifyRelationshipStrength should clamp values to min/max bounds.
    /// </summary>
    [Fact]
    public void ModifyRelationshipStrength_ClampsToMinMax()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("friend", new RelationshipType("Friend", true))
            .AddCharacterRelationship("alice", "bob", "friend", 90);

        // Try to increase beyond max (100)
        db = db.ModifyRelationshipStrength("alice", "bob", "friend", 50);
        Assert.Equal(100, db.GetRelationshipStrength("alice", "bob", "friend"));

        // Try to decrease beyond min (-100)
        db = db.ModifyRelationshipStrength("alice", "bob", "friend", -300);
        Assert.Equal(-100, db.GetRelationshipStrength("alice", "bob", "friend"));
    }

    /// <summary>
    /// GetRelationshipCountsByType should group and count relationships by type.
    /// </summary>
    [Fact]
    public void GetRelationshipCountsByType_GroupsCorrectly()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("charlie", CreateCharacter("Charlie"))
            .Insert("diana", CreateCharacter("Diana"))
            .Insert("friend", new RelationshipType("Friend", true))
            .Insert("rival", new RelationshipType("Rival", false))
            .AddCharacterRelationship("alice", "bob", "friend")
            .AddCharacterRelationship("alice", "charlie", "friend")
            .AddCharacterRelationship("alice", "diana", "rival");

        var counts = db.GetRelationshipCountsByType("alice");

        Assert.Equal(2, counts["friend"]);
        Assert.Equal(1, counts["rival"]);
    }

    /// <summary>
    /// RemoveCharacterRelationship should remove a relationship.
    /// </summary>
    [Fact]
    public void RemoveCharacterRelationship_RemovesRelationship()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("friend", new RelationshipType("Friend", true))
            .AddCharacterRelationship("alice", "bob", "friend");

        Assert.True(db.HasRelationship("alice", "bob", "friend"));

        db = db.RemoveCharacterRelationship("alice", "bob", "friend");

        Assert.False(db.HasRelationship("alice", "bob", "friend"));
    }

    /// <summary>
    /// RemoveAllRelationshipsOfType should remove all relationships of a specific type.
    /// </summary>
    [Fact]
    public void RemoveAllRelationshipsOfType_RemovesAll()
    {
        var db = CreateTestDatabase()
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("charlie", CreateCharacter("Charlie"))
            .Insert("friend", new RelationshipType("Friend", true))
            .Insert("rival", new RelationshipType("Rival", false))
            .AddCharacterRelationship("alice", "bob", "friend")
            .AddCharacterRelationship("alice", "charlie", "friend")
            .AddCharacterRelationship("alice", "charlie", "rival");

        // Remove all friendships
        db = db.RemoveAllRelationshipsOfType("alice", "friend");

        Assert.False(db.HasRelationship("alice", "bob", "friend"));
        Assert.False(db.HasRelationship("alice", "charlie", "friend"));
        Assert.True(db.HasRelationship("alice", "charlie", "rival")); // Rival still exists
    }

    #endregion

    #region Fluent API Demo

    /// <summary>
    /// Demonstrates the fluent API with chained calls for setup and queries.
    /// </summary>
    [Fact]
    public void FluentApi_DemonstratesChainedCalls()
    {
        // Demonstrates the fluent API with chained calls
        var db = CreateTestDatabase()
            // Setup entities
            .Insert("alice", CreateCharacter("Alice"))
            .Insert("bob", CreateCharacter("Bob"))
            .Insert("charlie", CreateCharacter("Charlie"))
            .Insert("brave", new Trait("Brave", "Courageous"))
            .Insert("clever", new Trait("Clever", "Smart"))
            .Insert("friend", new RelationshipType("Friend", true))
            .Insert("mentor", new RelationshipType("Mentor", false))
            // Add traits fluently
            .AddTraits("alice", "brave", "clever")
            .AddTrait("bob", "brave")
            // Add relationships fluently
            .AddCharacterRelationship("alice", "bob", "friend", 80)
            .AddCharacterRelationship("alice", "charlie", "mentor", 60)
            .AddSymmetricRelationship("bob", "charlie", nameof(Character), "friend", 50);

        // Verify everything was set up correctly
        Assert.Equal(2, db.CountTraits("alice"));
        Assert.True(db.HasTrait("alice", "brave"));
        Assert.True(db.HasRelationship("alice", "bob", "friend"));
        Assert.True(db.HasRelationship("bob", "charlie", "friend"));
        Assert.True(db.HasRelationship("charlie", "bob", "friend")); // Symmetric

        // Complex query: Find Alice's brave friends
        var aliceFriendIds = db.GetRelatedCharacterIds("alice", "friend").ToHashSet();
        var braveCharacterIds = db.GetCharacterIdsWithTrait("brave").ToHashSet();
        var braveFriends = aliceFriendIds.Intersect(braveCharacterIds).ToList();

        Assert.Single(braveFriends);
        Assert.Contains("bob", braveFriends);
    }

    #endregion
}
