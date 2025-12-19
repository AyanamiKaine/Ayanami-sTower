using InvictaDB;
using StellaInvicta.Data;
using static StellaInvicta.Data.RelationshipExtensions;

namespace StellaInvicta.Tests;

/// <summary>
/// Tests for GraphExtensions and QueryBuilder functionality.
/// </summary>
public class GraphExtensionsUnitTest
{
    #region Test Setup

    private static InvictaDatabase CreateTestDatabase()
    {
        return new InvictaDatabase()
            .RegisterTable<Character>()
            .RegisterTable<Trait>()
            .RegisterTable<CharacterTrait>()
            .RegisterTable<RelationshipType>()
            .RegisterTable<Relationship>()
            .RegisterTable<Location>();
    }

    private static Character CreateCharacter(string name, int age = 25) =>
        new(name, age, 10, 10, 10, 10, new DateTime(1, 1, 1));

    #endregion

    #region Ref Resolution Tests

    /// <summary>
    /// Tests resolving a Ref to its actual entity.
    /// </summary>
    [Fact]
    public void Resolve_ReturnsEntity_WhenExists()
    {
        var db = CreateTestDatabase();
        db = db.Insert("alice", CreateCharacter("Alice"));

        var aliceRef = new Ref<Character>("alice");
        var resolved = db.Resolve(aliceRef);

        Assert.NotNull(resolved);
        Assert.Equal("Alice", resolved.Name);
    }

    /// <summary>
    /// Tests resolving a Ref returns default when entity doesn't exist.
    /// </summary>
    [Fact]
    public void Resolve_ReturnsDefault_WhenNotExists()
    {
        var db = CreateTestDatabase();

        var missingRef = new Ref<Character>("missing");
        var resolved = db.Resolve(missingRef);

        Assert.Null(resolved);
    }

    /// <summary>
    /// Tests ResolveRequired throws when entity doesn't exist.
    /// </summary>
    [Fact]
    public void ResolveRequired_Throws_WhenNotExists()
    {
        var db = CreateTestDatabase();

        var missingRef = new Ref<Character>("missing");

        Assert.Throws<KeyNotFoundException>(() => db.ResolveRequired(missingRef));
    }

    /// <summary>
    /// Tests resolving multiple refs at once.
    /// </summary>
    [Fact]
    public void ResolveAll_ResolvesMultipleRefs()
    {
        var db = CreateTestDatabase();
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));
        db = db.Insert("charlie", CreateCharacter("Charlie"));

        var refs = new[]
        {
            new Ref<Character>("alice"),
            new Ref<Character>("bob"),
            new Ref<Character>("missing"), // This one doesn't exist
            new Ref<Character>("charlie")
        };

        var resolved = db.ResolveAll(refs).ToList();

        Assert.Equal(3, resolved.Count);
        Assert.Contains(resolved, c => c.Name == "Alice");
        Assert.Contains(resolved, c => c.Name == "Bob");
        Assert.Contains(resolved, c => c.Name == "Charlie");
    }

    /// <summary>
    /// Tests resolving refs with IDs.
    /// </summary>
    [Fact]
    public void ResolveAllWithIds_ReturnsIdsAndEntities()
    {
        var db = CreateTestDatabase();
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));

        var refs = new[]
        {
            new Ref<Character>("alice"),
            new Ref<Character>("bob")
        };

        var resolved = db.ResolveAllWithIds(refs).ToList();

        Assert.Equal(2, resolved.Count);
        Assert.Contains(resolved, x => x.Id == "alice" && x.Entity.Name == "Alice");
        Assert.Contains(resolved, x => x.Id == "bob" && x.Entity.Name == "Bob");
    }

    #endregion

    #region Graph Traversal Tests

    /// <summary>
    /// Creates a social network for graph tests.
    /// Alice -> Bob -> Charlie -> Diana
    ///   |              ^
    ///   +--------------+
    /// </summary>
    private static InvictaDatabase CreateSocialNetwork()
    {
        var db = CreateTestDatabase();

        // Characters
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));
        db = db.Insert("charlie", CreateCharacter("Charlie"));
        db = db.Insert("diana", CreateCharacter("Diana"));
        db = db.Insert("eve", CreateCharacter("Eve")); // Isolated node

        // Relationship type
        db = db.Insert("friend", new RelationshipType("Friend", IsSymmetric: false));

        // Friendships (directed):
        // alice -> bob
        // bob -> charlie
        // charlie -> diana
        // alice -> charlie (direct shortcut)
        db = db.Insert(CharacterRelationship("alice", "bob", "friend").CompositeKey, CharacterRelationship("alice", "bob", "friend"));
        db = db.Insert(CharacterRelationship("bob", "charlie", "friend").CompositeKey, CharacterRelationship("bob", "charlie", "friend"));
        db = db.Insert(CharacterRelationship("charlie", "diana", "friend").CompositeKey, CharacterRelationship("charlie", "diana", "friend"));
        db = db.Insert(CharacterRelationship("alice", "charlie", "friend").CompositeKey, CharacterRelationship("alice", "charlie", "friend"));

        return db;
    }

    /// <summary>
    /// Helper to get friend IDs for traversal.
    /// </summary>
    private static IEnumerable<string> GetFriendIds(InvictaDatabase db, string characterId)
    {
        return db.GetTable<Relationship>()
            .Where(r => r.Value.SourceId == characterId && r.Value.RelationshipTypeId == "friend")
            .Select(r => r.Value.TargetId);
    }

    /// <summary>
    /// Tests BFS traversal from a starting node.
    /// </summary>
    [Fact]
    public void TraverseBFS_FindsAllReachableNodes()
    {
        var db = CreateSocialNetwork();

        var reachable = db.TraverseBFS<Character>("alice", GetFriendIds).ToList();

        // Alice can reach: alice (0), bob (1), charlie (1), diana (2)
        Assert.Equal(4, reachable.Count);
        Assert.Contains(reachable, x => x.Id == "alice" && x.Depth == 0);
        Assert.Contains(reachable, x => x.Id == "bob" && x.Depth == 1);
        Assert.Contains(reachable, x => x.Id == "charlie" && x.Depth == 1);
        Assert.Contains(reachable, x => x.Id == "diana" && x.Depth == 2);

        // Eve is not reachable from Alice
        Assert.DoesNotContain(reachable, x => x.Id == "eve");
    }

    /// <summary>
    /// Tests BFS with max depth limit.
    /// </summary>
    [Fact]
    public void TraverseBFS_RespectsMaxDepth()
    {
        var db = CreateSocialNetwork();

        var reachable = db.TraverseBFS<Character>("alice", GetFriendIds, maxDepth: 1).ToList();

        // With maxDepth=1: alice (0), bob (1), charlie (1)
        Assert.Equal(3, reachable.Count);
        Assert.DoesNotContain(reachable, x => x.Id == "diana"); // Diana is at depth 2
    }

    /// <summary>
    /// Tests finding a path between two nodes.
    /// </summary>
    [Fact]
    public void FindPath_ReturnsShortestPath()
    {
        var db = CreateSocialNetwork();

        var path = db.FindPath<Character>("alice", "diana", GetFriendIds);

        Assert.NotNull(path);
        // Shortest path: alice -> charlie -> diana (length 3)
        Assert.Equal(3, path.Count);
        Assert.Equal("alice", path[0]);
        Assert.Equal("charlie", path[1]);
        Assert.Equal("diana", path[2]);
    }

    /// <summary>
    /// Tests FindPath returns null when no path exists.
    /// </summary>
    [Fact]
    public void FindPath_ReturnsNull_WhenNoPath()
    {
        var db = CreateSocialNetwork();

        var path = db.FindPath<Character>("alice", "eve", GetFriendIds);

        Assert.Null(path);
    }

    /// <summary>
    /// Tests finding connected components.
    /// </summary>
    [Fact]
    public void FindConnectedComponents_IdentifiesComponents()
    {
        var db = CreateSocialNetwork();

        // For undirected graph, we need bidirectional edges
        // Let's create a simpler test
        IEnumerable<string> GetBidirectionalFriends(InvictaDatabase d, string id)
        {
            var outgoing = d.GetTable<Relationship>()
                .Where(r => r.Value.SourceId == id && r.Value.RelationshipTypeId == "friend")
                .Select(r => r.Value.TargetId);
            var incoming = d.GetTable<Relationship>()
                .Where(r => r.Value.TargetId == id && r.Value.RelationshipTypeId == "friend")
                .Select(r => r.Value.SourceId);
            return outgoing.Concat(incoming).Distinct();
        }

        var components = db.FindConnectedComponents<Character>(GetBidirectionalFriends);

        // Should have 2 components: {alice, bob, charlie, diana} and {eve}
        Assert.Equal(2, components.Count);

        var mainComponent = components.First(c => c.Contains("alice"));
        var isolatedComponent = components.First(c => c.Contains("eve"));

        Assert.Equal(4, mainComponent.Count);
        Assert.Single(isolatedComponent);
    }

    /// <summary>
    /// Tests getting degree of a node.
    /// </summary>
    [Fact]
    public void GetDegree_ReturnsFriendCount()
    {
        var db = CreateSocialNetwork();

        var aliceDegree = db.GetDegree("alice", GetFriendIds);
        var bobDegree = db.GetDegree("bob", GetFriendIds);
        var eveDegree = db.GetDegree("eve", GetFriendIds);

        Assert.Equal(2, aliceDegree); // alice -> bob, alice -> charlie
        Assert.Equal(1, bobDegree);   // bob -> charlie
        Assert.Equal(0, eveDegree);   // eve has no friends
    }

    #endregion

    #region Join Tests

    /// <summary>
    /// Tests inner join between tables.
    /// </summary>
    [Fact]
    public void Join_PerformsInnerJoin()
    {
        var db = CreateTestDatabase();

        // Create characters and traits
        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));
        db = db.Insert("brave", new Trait("Brave", "Courageous"));
        db = db.Insert("clever", new Trait("Clever", "Smart"));

        // Link them
        db = db.Insert(new CharacterTrait("alice", "brave").CompositeKey, new CharacterTrait("alice", "brave"));
        db = db.Insert(new CharacterTrait("alice", "clever").CompositeKey, new CharacterTrait("alice", "clever"));
        db = db.Insert(new CharacterTrait("bob", "brave").CompositeKey, new CharacterTrait("bob", "brave"));

        // Join CharacterTrait with Trait to get full trait info
        var traitTable = db.GetTable<Trait>();
        var results = db.Join<CharacterTrait, Trait, (string CharacterId, string TraitName)>(
            leftKeySelector: (_, ct) => ct.TraitId.Id,
            rightKeySelector: (id, _) => id,
            resultSelector: (_, ct, _, trait) => (ct.CharacterId.Id, trait.Name)
        ).ToList();

        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.CharacterId == "alice" && r.TraitName == "Brave");
        Assert.Contains(results, r => r.CharacterId == "alice" && r.TraitName == "Clever");
        Assert.Contains(results, r => r.CharacterId == "bob" && r.TraitName == "Brave");
    }

    /// <summary>
    /// Tests left join between tables.
    /// </summary>
    [Fact]
    public void LeftJoin_IncludesUnmatchedLeft()
    {
        var db = CreateTestDatabase();

        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("bob", CreateCharacter("Bob"));
        db = db.Insert("castle", new Location("Castle", "A castle"));

        // Only alice has a location
        db = db.Insert(CharacterLocationRelationship("alice", "castle", "lives_at").CompositeKey,
                       CharacterLocationRelationship("alice", "castle", "lives_at"));
        db = db.Insert("lives_at", new RelationshipType("Lives At", IsSymmetric: false));

        // Left join to see all characters with optional location
        var results = db.LeftJoin<Character, Location, (string Name, string? LocationName)>(
            leftKeySelector: (id, _) =>
            {
                var rel = db.GetTable<Relationship>()
                    .FirstOrDefault(r => r.Value.SourceId == id && r.Value.RelationshipTypeId == "lives_at");
                return rel.Value?.TargetId ?? "";
            },
            rightKeySelector: (id, _) => id,
            resultSelector: (_, character, _, location) => (character.Name, location?.Name)
        ).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Name == "Alice" && r.LocationName == "Castle");
        Assert.Contains(results, r => r.Name == "Bob" && r.LocationName == null);
    }

    #endregion

    #region Aggregation Tests

    /// <summary>
    /// Tests grouping and counting entities.
    /// </summary>
    [Fact]
    public void GroupCount_CountsByKey()
    {
        var db = CreateTestDatabase();

        db = db.Insert("alice", CreateCharacter("Alice", age: 25));
        db = db.Insert("bob", CreateCharacter("Bob", age: 30));
        db = db.Insert("charlie", CreateCharacter("Charlie", age: 25));
        db = db.Insert("diana", CreateCharacter("Diana", age: 30));
        db = db.Insert("eve", CreateCharacter("Eve", age: 30));

        // Count by age
        var ageCounts = db.GroupCount<Character, int>((_, c) => c.Age);

        Assert.Equal(2, ageCounts[25]);
        Assert.Equal(3, ageCounts[30]);
    }

    /// <summary>
    /// Tests grouping and aggregating values.
    /// </summary>
    [Fact]
    public void GroupAggregate_AggregatesByKey()
    {
        var db = CreateTestDatabase();

        db = db.Insert("alice", new Character("Alice", 25, Martial: 5, Sterwardship: 15, Intrigue: 10, Learning: 10, BirthDate: new DateTime(1, 1, 1)));
        db = db.Insert("bob", new Character("Bob", 25, Martial: 15, Sterwardship: 5, Intrigue: 10, Learning: 10, BirthDate: new DateTime(1, 1, 1)));
        db = db.Insert("charlie", new Character("Charlie", 30, Martial: 10, Sterwardship: 10, Intrigue: 10, Learning: 10, BirthDate: new DateTime(1, 1, 1)));

        // Average martial by age
        var avgMartial = db.GroupAggregate<Character, int, double>(
            keySelector: (_, c) => c.Age,
            valueSelector: (_, c) => c.Martial,
            aggregator: values => values.Average()
        );

        Assert.Equal(10.0, avgMartial[25]); // (5 + 15) / 2
        Assert.Equal(10.0, avgMartial[30]); // 10 / 1
    }

    #endregion

    #region Query Helper Tests

    /// <summary>
    /// Tests Where extension.
    /// </summary>
    [Fact]
    public void Where_FiltersEntities()
    {
        var db = CreateTestDatabase();

        db = db.Insert("alice", CreateCharacter("Alice", 25));
        db = db.Insert("bob", CreateCharacter("Bob", 30));
        db = db.Insert("charlie", CreateCharacter("Charlie", 25));

        var young = db.Where<Character>((_, c) => c.Age < 30).ToList();

        Assert.Equal(2, young.Count);
        Assert.Contains(young, x => x.Entity.Name == "Alice");
        Assert.Contains(young, x => x.Entity.Name == "Charlie");
    }

    /// <summary>
    /// Tests Select extension.
    /// </summary>
    [Fact]
    public void Select_ProjectsEntities()
    {
        var db = CreateTestDatabase();

        db = db.Insert("alice", CreateCharacter("Alice", 25));
        db = db.Insert("bob", CreateCharacter("Bob", 30));

        var names = db.Select<Character, string>((_, c) => c.Name).ToList();

        Assert.Equal(2, names.Count);
        Assert.Contains("Alice", names);
        Assert.Contains("Bob", names);
    }

    /// <summary>
    /// Tests FirstOrDefault extension.
    /// </summary>
    [Fact]
    public void FirstOrDefault_ReturnsFirstMatch()
    {
        var db = CreateTestDatabase();

        db = db.Insert("alice", CreateCharacter("Alice", 25));
        db = db.Insert("bob", CreateCharacter("Bob", 30));

        var result = db.FirstOrDefault<Character>((_, c) => c.Age == 30);

        Assert.NotNull(result);
        Assert.Equal("bob", result.Value.Id);
        Assert.Equal("Bob", result.Value.Entity.Name);
    }

    /// <summary>
    /// Tests Any extension.
    /// </summary>
    [Fact]
    public void Any_ReturnsTrueWhenMatches()
    {
        var db = CreateTestDatabase();

        db = db.Insert("alice", CreateCharacter("Alice", 25));

        Assert.True(db.Any<Character>((_, c) => c.Age == 25));
        Assert.False(db.Any<Character>((_, c) => c.Age == 100));
    }

    /// <summary>
    /// Tests Count extension.
    /// </summary>
    [Fact]
    public void Count_CountsMatches()
    {
        var db = CreateTestDatabase();

        db = db.Insert("alice", CreateCharacter("Alice", 25));
        db = db.Insert("bob", CreateCharacter("Bob", 25));
        db = db.Insert("charlie", CreateCharacter("Charlie", 30));

        Assert.Equal(2, db.Count<Character>((_, c) => c.Age == 25));
        Assert.Equal(1, db.Count<Character>((_, c) => c.Age == 30));
    }

    #endregion

    #region QueryBuilder Tests

    /// <summary>
    /// Tests fluent query builder with multiple filters.
    /// </summary>
    [Fact]
    public void QueryBuilder_ChainsMultipleFilters()
    {
        var db = CreateTestDatabase();

        db = db.Insert("alice", new Character("Alice", 25, 15, 10, 10, 10, new DateTime(1, 1, 1)));
        db = db.Insert("bob", new Character("Bob", 30, 15, 5, 10, 10, new DateTime(1, 1, 1)));
        db = db.Insert("charlie", new Character("Charlie", 25, 5, 15, 10, 10, new DateTime(1, 1, 1)));
        db = db.Insert("diana", new Character("Diana", 25, 15, 15, 10, 10, new DateTime(1, 1, 1)));

        // Find young (age 25) AND skilled (martial >= 10)
        var results = db.Query<Character>()
            .Where((_, c) => c.Age == 25)
            .Where((_, c) => c.Martial >= 10)
            .ToList();

        Assert.Equal(2, results.Count());
        Assert.Contains(results, c => c.Name == "Alice");
        Assert.Contains(results, c => c.Name == "Diana");
    }

    /// <summary>
    /// Tests query builder with WhereExists for foreign key validation.
    /// </summary>
    [Fact]
    public void QueryBuilder_WhereExists_ValidatesForeignKeys()
    {
        var db = CreateTestDatabase();

        db = db.Insert("alice", CreateCharacter("Alice"));
        db = db.Insert("brave", new Trait("Brave", "Courageous"));

        // Create a join record pointing to existing trait
        db = db.Insert("valid_link", new CharacterTrait("alice", "brave"));
        // Create a "dangling" join record pointing to non-existent trait
        db = db.Insert("dangling_link", new CharacterTrait("alice", "missing_trait"));

        // Query only valid character traits (where the trait actually exists)
        var validLinks = db.Query<CharacterTrait>()
            .WhereExists<Trait>(ct => ct.TraitId.Id)
            .ToListWithIds()
            .ToList();

        Assert.Single(validLinks);
        Assert.Equal("valid_link", validLinks[0].Id);
    }

    /// <summary>
    /// Tests query builder projection.
    /// </summary>
    [Fact]
    public void QueryBuilder_Select_ProjectsResults()
    {
        var db = CreateTestDatabase();

        db = db.Insert("alice", CreateCharacter("Alice", 25));
        db = db.Insert("bob", CreateCharacter("Bob", 30));

        var summaries = db.Query<Character>()
            .Where((_, c) => c.Age >= 25)
            .Select((id, c) => $"{c.Name} ({id})")
            .ToList();

        Assert.Equal(2, summaries.Count);
        Assert.Contains("Alice (alice)", summaries);
        Assert.Contains("Bob (bob)", summaries);
    }

    /// <summary>
    /// Tests query builder FirstOrDefault.
    /// </summary>
    [Fact]
    public void QueryBuilder_FirstOrDefault_ReturnsFirst()
    {
        var db = CreateTestDatabase();

        db = db.Insert("alice", CreateCharacter("Alice", 25));
        db = db.Insert("bob", CreateCharacter("Bob", 30));

        var result = db.Query<Character>()
            .Where((_, c) => c.Age == 30)
            .FirstOrDefaultWithId();

        Assert.NotNull(result);
        Assert.Equal("bob", result.Value.Id);
    }

    /// <summary>
    /// Tests query builder Any.
    /// </summary>
    [Fact]
    public void QueryBuilder_Any_ChecksExistence()
    {
        var db = CreateTestDatabase();

        db = db.Insert("alice", CreateCharacter("Alice", 25));

        var hasYoung = db.Query<Character>()
            .Where((_, c) => c.Age < 30)
            .Any();

        var hasOld = db.Query<Character>()
            .Where((_, c) => c.Age > 50)
            .Any();

        Assert.True(hasYoung);
        Assert.False(hasOld);
    }

    #endregion
}
