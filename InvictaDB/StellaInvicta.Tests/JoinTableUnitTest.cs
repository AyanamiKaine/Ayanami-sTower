using InvictaDB;
using StellaInvicta.Data;
using static StellaInvicta.Data.RelationshipExtensions;

namespace StellaInvicta.Tests;

/// <summary>
/// Tests demonstrating the join table approach for CharacterTrait and Relationship tables.
/// </summary>
public class JoinTableUnitTest
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

    #region CharacterTrait Tests

    /// <summary>
    /// Demonstrates adding a trait to a character using the join table.
    /// </summary>
    [Fact]
    public void CharacterTrait_AddTraitToCharacter()
    {
        var db = CreateTestDatabase();

        // Create character and trait
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("brave", new Trait("Brave", "Courageous in battle"));

        // Link them via CharacterTrait join table
        var link = new CharacterTrait("alice", "brave");
        db = db.Insert(link.CompositeKey, link);

        // Verify the link exists
        var storedLink = db.GetEntry<CharacterTrait>(link.CompositeKey);
        Assert.Equal("alice", storedLink.CharacterId.Id);
        Assert.Equal("brave", storedLink.TraitId.Id);
    }

    /// <summary>
    /// Demonstrates adding multiple traits to a single character.
    /// </summary>
    [Fact]
    public void CharacterTrait_AddMultipleTraitsToCharacter()
    {
        var db = CreateTestDatabase();

        // Create character and traits
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("brave", new Trait("Brave", "Courageous in battle"));
        db = db.Insert("clever", new Trait("Clever", "Quick-witted"));
        db = db.Insert("strong", new Trait("Strong", "Physical prowess"));

        // Add all traits to Alice
        var traits = new[] { "brave", "clever", "strong" };
        foreach (var traitId in traits)
        {
            var link = new CharacterTrait("alice", traitId);
            db = db.Insert(link.CompositeKey, link);
        }

        // Query all traits for Alice
        var aliceTraits = db.GetTable<CharacterTrait>()
            .Where(ct => ct.Value.CharacterId.Id == "alice")
            .Select(ct => ct.Value.TraitId.Id)
            .ToList();

        Assert.Equal(3, aliceTraits.Count);
        Assert.Contains("brave", aliceTraits);
        Assert.Contains("clever", aliceTraits);
        Assert.Contains("strong", aliceTraits);
    }

    /// <summary>
    /// Demonstrates querying all characters that have a specific trait.
    /// </summary>
    [Fact]
    public void CharacterTrait_QueryCharactersWithTrait()
    {
        var db = CreateTestDatabase();

        // Create characters
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));
        db = db.Insert("charlie", CreateCharacter("Charlie"));

        // Create traits
        db = db.Insert("brave", new Trait("Brave", "Courageous in battle"));
        db = db.Insert("clever", new Trait("Clever", "Quick-witted"));

        // Alice and Charlie are brave, Bob is clever
        db = db.Insert(new CharacterTrait("alice", "brave").CompositeKey, new CharacterTrait("alice", "brave"));
        db = db.Insert(new CharacterTrait("charlie", "brave").CompositeKey, new CharacterTrait("charlie", "brave"));
        db = db.Insert(new CharacterTrait("bob", "clever").CompositeKey, new CharacterTrait("bob", "clever"));

        // Query all brave characters
        var braveCharacters = db.GetTable<CharacterTrait>()
            .Where(ct => ct.Value.TraitId.Id == "brave")
            .Select(ct => ct.Value.CharacterId.Id)
            .ToList();

        Assert.Equal(2, braveCharacters.Count);
        Assert.Contains("alice", braveCharacters);
        Assert.Contains("charlie", braveCharacters);
        Assert.DoesNotContain("bob", braveCharacters);
    }

    /// <summary>
    /// Demonstrates removing a trait from a character.
    /// </summary>
    [Fact]
    public void CharacterTrait_RemoveTraitFromCharacter()
    {
        var db = CreateTestDatabase();

        // Setup
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("brave", new Trait("Brave", "Courageous in battle"));

        var link = new CharacterTrait("alice", "brave");
        db = db.Insert(link.CompositeKey, link);

        // Verify trait exists
        Assert.True(db.Exists<CharacterTrait>(link.CompositeKey));

        // Remove the trait
        db = db.RemoveEntry<CharacterTrait>(link.CompositeKey);

        // Verify trait is removed
        Assert.False(db.Exists<CharacterTrait>(link.CompositeKey));
    }

    /// <summary>
    /// Demonstrates getting full trait details for a character using a join query.
    /// </summary>
    [Fact]
    public void CharacterTrait_GetFullTraitDetailsForCharacter()
    {
        var db = CreateTestDatabase();

        // Setup
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("brave", new Trait("Brave", "Courageous in battle"));
        db = db.Insert("clever", new Trait("Clever", "Quick-witted"));

        db = db.Insert(new CharacterTrait("alice", "brave").CompositeKey, new CharacterTrait("alice", "brave"));
        db = db.Insert(new CharacterTrait("alice", "clever").CompositeKey, new CharacterTrait("alice", "clever"));

        // Join query: get full trait objects for Alice
        var traitTable = db.GetTable<Trait>();
        var aliceFullTraits = db.GetTable<CharacterTrait>()
            .Where(ct => ct.Value.CharacterId.Id == "alice")
            .Select(ct => traitTable[ct.Value.TraitId.Id])
            .ToList();

        Assert.Equal(2, aliceFullTraits.Count);
        Assert.Contains(aliceFullTraits, t => t.Name == "Brave");
        Assert.Contains(aliceFullTraits, t => t.Name == "Clever");
    }

    #endregion

    #region Relationship Tests

    /// <summary>
    /// Demonstrates creating a relationship between two characters.
    /// </summary>
    [Fact]
    public void Relationship_CreateCharacterRelationship()
    {
        var db = CreateTestDatabase();

        // Create characters
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));

        // Create relationship type
        db = db.Insert("friend", new RelationshipType("Friend", IsSymmetric: true));

        // Create relationship
        var friendship = CharacterRelationship("alice", "bob", "friend", strength: 75);
        db = db.Insert(friendship.CompositeKey, friendship);

        // Verify
        var stored = db.GetEntry<Relationship>(friendship.CompositeKey);
        Assert.Equal("alice", stored.SourceId);
        Assert.Equal("bob", stored.TargetId);
        Assert.Equal("friend", stored.RelationshipTypeId);
        Assert.Equal(75, stored.Strength);
    }

    /// <summary>
    /// Demonstrates symmetric relationships (e.g., friendship goes both ways).
    /// </summary>
    [Fact]
    public void Relationship_SymmetricRelationship()
    {
        var db = CreateTestDatabase();

        // Create characters
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));

        // Create symmetric relationship type
        db = db.Insert("friend", new RelationshipType("Friend", IsSymmetric: true));

        // For symmetric relationships, we create both directions
        var aliceToBob = CharacterRelationship("alice", "bob", "friend", strength: 80);
        var bobToAlice = CharacterRelationship("bob", "alice", "friend", strength: 80);

        db = db.Insert(aliceToBob.CompositeKey, aliceToBob);
        db = db.Insert(bobToAlice.CompositeKey, bobToAlice);

        // Query Alice's friends
        var aliceFriends = db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == "alice" && r.Value.RelationshipTypeId == "friend")
            .Select(r => r.Value.TargetId)
            .ToList();

        // Query Bob's friends
        var bobFriends = db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == "bob" && r.Value.RelationshipTypeId == "friend")
            .Select(r => r.Value.TargetId)
            .ToList();

        Assert.Contains("bob", aliceFriends);
        Assert.Contains("alice", bobFriends);
    }

    /// <summary>
    /// Demonstrates asymmetric relationships (e.g., "is parent of" is one-way).
    /// </summary>
    [Fact]
    public void Relationship_AsymmetricRelationship()
    {
        var db = CreateTestDatabase();

        // Create characters
        db = db.Insert("parent", CreateCharacter("Parent"));
        db = db.Insert("child", CreateCharacter("Child"));

        // Create asymmetric relationship type
        db = db.Insert("parent_of", new RelationshipType("Parent Of", IsSymmetric: false));

        // Parent -> Child relationship
        var parenthood = CharacterRelationship("parent", "child", "parent_of");
        db = db.Insert(parenthood.CompositeKey, parenthood);

        // Query: who are parent's children?
        var children = db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == "parent" && r.Value.RelationshipTypeId == "parent_of")
            .Select(r => r.Value.TargetId)
            .ToList();

        // Query: who are child's parents? (reverse lookup)
        var parents = db.GetTable<Relationship>()
            .Where(r => r.Value.TargetId == "child" && r.Value.RelationshipTypeId == "parent_of")
            .Select(r => r.Value.SourceId)
            .ToList();

        Assert.Single(children);
        Assert.Contains("child", children);
        Assert.Single(parents);
        Assert.Contains("parent", parents);
    }

    /// <summary>
    /// Demonstrates relationship strength for opinion tracking.
    /// </summary>
    [Fact]
    public void Relationship_TrackOpinionStrength()
    {
        var db = CreateTestDatabase();

        // Create characters
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));
        db = db.Insert("charlie", CreateCharacter("Charlie"));

        // Create relationship type for opinion
        db = db.Insert("opinion", new RelationshipType("Opinion", IsSymmetric: false));

        // Alice likes Bob (+50), dislikes Charlie (-30)
        db = db.Insert(
            CharacterRelationship("alice", "bob", "opinion", 50).CompositeKey,
            CharacterRelationship("alice", "bob", "opinion", 50));
        db = db.Insert(
            CharacterRelationship("alice", "charlie", "opinion", -30).CompositeKey,
            CharacterRelationship("alice", "charlie", "opinion", -30));

        // Query Alice's positive opinions
        var likedCharacters = db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == "alice"
                     && r.Value.RelationshipTypeId == "opinion"
                     && r.Value.Strength > 0)
            .Select(r => r.Value.TargetId)
            .ToList();

        // Query Alice's negative opinions
        var dislikedCharacters = db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == "alice"
                     && r.Value.RelationshipTypeId == "opinion"
                     && r.Value.Strength < 0)
            .Select(r => r.Value.TargetId)
            .ToList();

        Assert.Single(likedCharacters);
        Assert.Contains("bob", likedCharacters);
        Assert.Single(dislikedCharacters);
        Assert.Contains("charlie", dislikedCharacters);
    }

    /// <summary>
    /// Demonstrates querying all relationships for a character.
    /// </summary>
    [Fact]
    public void Relationship_GetAllRelationshipsForCharacter()
    {
        var db = CreateTestDatabase();

        // Create characters
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));
        db = db.Insert("charlie", CreateCharacter("Charlie"));

        // Create relationship types
        db = db.Insert("friend", new RelationshipType("Friend", IsSymmetric: true));
        db = db.Insert("rival", new RelationshipType("Rival", IsSymmetric: false));
        db = db.Insert("mentor", new RelationshipType("Mentor", IsSymmetric: false));

        // Alice is friend with Bob, rival of Charlie, mentor to Charlie
        db = db.Insert(
            CharacterRelationship("alice", "bob", "friend", 80).CompositeKey,
            CharacterRelationship("alice", "bob", "friend", 80));
        db = db.Insert(
            CharacterRelationship("alice", "charlie", "rival", -50).CompositeKey,
            CharacterRelationship("alice", "charlie", "rival", -50));
        db = db.Insert(
            CharacterRelationship("alice", "charlie", "mentor", 30).CompositeKey,
            CharacterRelationship("alice", "charlie", "mentor", 30));

        // Query all outgoing relationships for Alice
        var aliceRelationships = db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == "alice")
            .Select(r => new { r.Value.TargetId, r.Value.RelationshipTypeId, r.Value.Strength })
            .ToList();

        Assert.Equal(3, aliceRelationships.Count);
    }

    /// <summary>
    /// Demonstrates updating relationship strength.
    /// </summary>
    [Fact]
    public void Relationship_UpdateStrength()
    {
        var db = CreateTestDatabase();

        // Setup
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));
        db = db.Insert("friend", new RelationshipType("Friend", IsSymmetric: true));

        var friendship = CharacterRelationship("alice", "bob", "friend", 50);
        db = db.Insert(friendship.CompositeKey, friendship);

        // Update: friendship grows stronger!
        var updatedFriendship = friendship with { Strength = 90 };
        db = db.Insert(friendship.CompositeKey, updatedFriendship); // Same key, updated value

        var stored = db.GetEntry<Relationship>(friendship.CompositeKey);
        Assert.Equal(90, stored.Strength);
    }

    /// <summary>
    /// Demonstrates cross-entity relationships (Character to Location).
    /// </summary>
    [Fact]
    public void Relationship_CrossEntityRelationship()
    {
        var db = CreateTestDatabase()
            .RegisterTable<Location>();

        // Create character and location
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("castle", new Location("Royal Castle", "A majestic castle"));

        // Create relationship types for locations
        db = db.Insert("owns", new RelationshipType("Owns", IsSymmetric: false));
        db = db.Insert("lives_at", new RelationshipType("Lives At", IsSymmetric: false));

        // Alice owns and lives at the castle
        var ownsRelation = CharacterLocationRelationship("alice", "castle", "owns");
        var livesAtRelation = CharacterLocationRelationship("alice", "castle", "lives_at");

        db = db.Insert(ownsRelation.CompositeKey, ownsRelation);
        db = db.Insert(livesAtRelation.CompositeKey, livesAtRelation);

        // Query: what locations does Alice own?
        var aliceOwns = db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == "alice"
                     && r.Value.SourceType == nameof(Character)
                     && r.Value.TargetType == nameof(Location)
                     && r.Value.RelationshipTypeId == "owns")
            .Select(r => r.Value.TargetId)
            .ToList();

        Assert.Single(aliceOwns);
        Assert.Contains("castle", aliceOwns);
    }

    /// <summary>
    /// Demonstrates finding mutual relationships (e.g., mutual friends).
    /// </summary>
    [Fact]
    public void Relationship_FindMutualRelationships()
    {
        var db = CreateTestDatabase();

        // Create characters
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));
        db = db.Insert("charlie", CreateCharacter("Charlie"));
        db = db.Insert("diana", CreateCharacter("Diana"));

        // Create friend relationship type
        db = db.Insert("friend", new RelationshipType("Friend", IsSymmetric: true));

        // Alice is friends with Bob and Charlie
        // Bob is friends with Alice and Charlie
        // Charlie is friends with Alice, Bob, and Diana
        var friendships = new[]
        {
            ("alice", "bob"), ("bob", "alice"),
            ("alice", "charlie"), ("charlie", "alice"),
            ("bob", "charlie"), ("charlie", "bob"),
            ("charlie", "diana"), ("diana", "charlie")
        };

        foreach (var (source, target) in friendships)
        {
            var rel = CharacterRelationship(source, target, "friend", 50);
            db = db.Insert(rel.CompositeKey, rel);
        }

        // Find mutual friends of Alice and Bob
        var aliceFriends = db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == "alice" && r.Value.RelationshipTypeId == "friend")
            .Select(r => r.Value.TargetId)
            .ToHashSet();

        var bobFriends = db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == "bob" && r.Value.RelationshipTypeId == "friend")
            .Select(r => r.Value.TargetId)
            .ToHashSet();

        var mutualFriends = aliceFriends.Intersect(bobFriends).ToList();

        Assert.Single(mutualFriends);
        Assert.Contains("charlie", mutualFriends);
    }

    #endregion

    #region Complex Query Tests

    /// <summary>
    /// Demonstrates a complex query: find brave friends of a character.
    /// </summary>
    [Fact]
    public void ComplexQuery_FindBraveFriendsOfCharacter()
    {
        var db = CreateTestDatabase();

        // Create characters
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));
        db = db.Insert("charlie", CreateCharacter("Charlie"));

        // Create traits
        db = db.Insert("brave", new Trait("Brave", "Courageous in battle"));

        // Create relationship type
        db = db.Insert("friend", new RelationshipType("Friend", IsSymmetric: true));

        // Bob is brave, Charlie is not
        db = db.Insert(new CharacterTrait("bob", "brave").CompositeKey, new CharacterTrait("bob", "brave"));

        // Alice is friends with both Bob and Charlie
        db = db.Insert(
            CharacterRelationship("alice", "bob", "friend").CompositeKey,
            CharacterRelationship("alice", "bob", "friend"));
        db = db.Insert(
            CharacterRelationship("alice", "charlie", "friend").CompositeKey,
            CharacterRelationship("alice", "charlie", "friend"));

        // Query: find Alice's friends who are brave
        var aliceFriendIds = db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == "alice" && r.Value.RelationshipTypeId == "friend")
            .Select(r => r.Value.TargetId)
            .ToHashSet();

        var braveCharacterIds = db.GetTable<CharacterTrait>()
            .Where(ct => ct.Value.TraitId.Id == "brave")
            .Select(ct => ct.Value.CharacterId.Id)
            .ToHashSet();

        var braveFriends = aliceFriendIds.Intersect(braveCharacterIds).ToList();

        Assert.Single(braveFriends);
        Assert.Contains("bob", braveFriends);
    }

    /// <summary>
    /// Demonstrates counting relationships by type for a character.
    /// </summary>
    [Fact]
    public void ComplexQuery_CountRelationshipsByType()
    {
        var db = CreateTestDatabase();

        // Create characters
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));
        db = db.Insert("charlie", CreateCharacter("Charlie"));
        db = db.Insert("diana", CreateCharacter("Diana"));

        // Create relationship types
        db = db.Insert("friend", new RelationshipType("Friend", IsSymmetric: true));
        db = db.Insert("rival", new RelationshipType("Rival", IsSymmetric: false));

        // Alice has 2 friends and 1 rival
        db = db.Insert(
            CharacterRelationship("alice", "bob", "friend").CompositeKey,
            CharacterRelationship("alice", "bob", "friend"));
        db = db.Insert(
            CharacterRelationship("alice", "charlie", "friend").CompositeKey,
            CharacterRelationship("alice", "charlie", "friend"));
        db = db.Insert(
            CharacterRelationship("alice", "diana", "rival").CompositeKey,
            CharacterRelationship("alice", "diana", "rival"));

        // Count by type
        var relationshipCounts = db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == "alice")
            .GroupBy(r => r.Value.RelationshipTypeId)
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(2, relationshipCounts["friend"]);
        Assert.Equal(1, relationshipCounts["rival"]);
    }

    #endregion
}
