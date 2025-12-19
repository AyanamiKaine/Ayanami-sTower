using System.Collections.Concurrent;

namespace InvictaDB.Tests;

/// <summary>
/// Tests demonstrating the benefits of an immutable database with structural sharing
/// </summary>
public class ImmutableBenefitsUnitTest
{
    internal record Person(string Name, int Age);
    internal record Document(string Title, string Content, int Version);

    #region Structural Sharing Tests

    /// <summary>
    /// Demonstrates that unchanged tables are shared between database versions
    /// </summary>
    [Fact]
    public void StructuralSharing_UnchangedTablesAreShared()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>()
            .RegisterTable<Document>();

        db = db.Insert("alice", new Person("Alice", 30));
        db = db.Insert("doc1", new Document("Report", "Content", 1));

        var dbBefore = db;

        // Only update Person table
        db = db.Insert("alice", new Person("Alice", 31));

        // Document table reference should be the exact same object
        var docTableBefore = dbBefore.GetTable<Document>();
        var docTableAfter = db.GetTable<Document>();

        Assert.True(ReferenceEquals(docTableBefore, docTableAfter),
            "Unchanged tables should be the same reference (structural sharing)");
    }

    /// <summary>
    /// Demonstrates that unchanged entries within a table are shared
    /// </summary>
    [Fact]
    public void StructuralSharing_UnchangedEntriesAreShared()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        var alice = new Person("Alice", 30);
        var bob = new Person("Bob", 25);

        db = db.Insert("alice", alice);
        db = db.Insert("bob", bob);

        var dbBefore = db;

        // Only update Alice
        db = db.Insert("alice", new Person("Alice", 31));

        // Bob's entry should be the exact same object
        var bobBefore = dbBefore.Get<Person>("bob");
        var bobAfter = db.Get<Person>("bob");

        Assert.True(ReferenceEquals(bobBefore, bobAfter),
            "Unchanged entries should be the same reference (structural sharing)");
    }

    /// <summary>
    /// Demonstrates memory efficiency with many versions
    /// </summary>
    [Fact]
    public void StructuralSharing_ManyVersionsShareData()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        // Insert 100 people
        for (int i = 0; i < 100; i++)
        {
            db = db.Insert($"person{i}", new Person($"Person{i}", 20 + i));
        }

        var snapshots = new List<InvictaDatabase> { db };

        // Create 50 versions, each updating only one person
        for (int i = 0; i < 50; i++)
        {
            db = db.Insert("person0", new Person("Person0", 100 + i));
            snapshots.Add(db);
        }

        // All 51 snapshots share the other 99 person entries
        // Verify person50 is the same reference across ALL snapshots
        var person50Original = snapshots[0].Get<Person>("person50");
        foreach (var snapshot in snapshots)
        {
            Assert.True(ReferenceEquals(person50Original, snapshot.Get<Person>("person50")),
                "Unchanged entries should be shared across all versions");
        }
    }

    #endregion

    #region Safe Snapshots / Time-Travel Tests

    /// <summary>
    /// Demonstrates that snapshots are completely independent
    /// </summary>
    [Fact]
    public void Snapshots_AreCompletelyIndependent()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        db = db.Insert("alice", new Person("Alice", 30));

        // Take snapshot
        var snapshot1 = db;

        // Make changes
        db = db.Insert("alice", new Person("Alice", 31));
        db = db.Insert("bob", new Person("Bob", 25));

        var snapshot2 = db;

        // More changes
        db = db.Insert("alice", new Person("Alice", 32));
        db = db.Insert("charlie", new Person("Charlie", 35));

        // Each snapshot has its own independent state
        Assert.Equal(30, snapshot1.Get<Person>("alice").Age);
        Assert.False(snapshot1.Exists<Person>("bob"));

        Assert.Equal(31, snapshot2.Get<Person>("alice").Age);
        Assert.True(snapshot2.Exists<Person>("bob"));
        Assert.False(snapshot2.Exists<Person>("charlie"));

        Assert.Equal(32, db.Get<Person>("alice").Age);
        Assert.True(db.Exists<Person>("charlie"));
    }

    /// <summary>
    /// Demonstrates undo/redo capability through snapshots
    /// </summary>
    [Fact]
    public void Snapshots_EnableUndoRedo()
    {
        var undoStack = new Stack<InvictaDatabase>();
        var redoStack = new Stack<InvictaDatabase>();

        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        db = db.Insert("alice", new Person("Alice", 30));

        // Action 1
        undoStack.Push(db);
        db = db.Insert("alice", new Person("Alice", 31));

        // Action 2
        undoStack.Push(db);
        db = db.Insert("bob", new Person("Bob", 25));

        // Action 3
        undoStack.Push(db);
        db = db.Insert("charlie", new Person("Charlie", 35));

        Assert.Equal(3, db.GetTable<Person>().Count);

        // Undo action 3
        redoStack.Push(db);
        db = undoStack.Pop();
        Assert.Equal(2, db.GetTable<Person>().Count);
        Assert.False(db.Exists<Person>("charlie"));

        // Undo action 2
        redoStack.Push(db);
        db = undoStack.Pop();
        Assert.Single(db.GetTable<Person>());

        // Redo action 2
        undoStack.Push(db);
        db = redoStack.Pop();
        Assert.Equal(2, db.GetTable<Person>().Count);
        Assert.True(db.Exists<Person>("bob"));
    }

    /// <summary>
    /// Demonstrates querying historical states (time-travel queries)
    /// </summary>
    [Fact]
    public void Snapshots_TimeTravelQueries()
    {
        var history = new Dictionary<string, InvictaDatabase>();

        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        db = db.Insert("alice", new Person("Alice", 30));
        history["2024-01-01"] = db;

        db = db.Insert("alice", new Person("Alice", 31));
        db = db.Insert("bob", new Person("Bob", 25));
        history["2024-06-01"] = db;

        db = db.Insert("alice", new Person("Alice", 32));
        db = db.Insert("charlie", new Person("Charlie", 40));
        history["2024-12-01"] = db;

        // Query: "What was Alice's age on 2024-06-01?"
        Assert.Equal(31, history["2024-06-01"].Get<Person>("alice").Age);

        // Query: "Who existed on 2024-01-01?"
        Assert.Single(history["2024-01-01"].GetTable<Person>());

        // Query: "Show Alice's age progression"
        var aliceAges = history
            .OrderBy(h => h.Key)
            .Select(h => new { Date = h.Key, Age = h.Value.Get<Person>("alice").Age })
            .ToList();

        Assert.Equal(30, aliceAges[0].Age);
        Assert.Equal(31, aliceAges[1].Age);
        Assert.Equal(32, aliceAges[2].Age);
    }

    #endregion

    #region Branching Tests

    /// <summary>
    /// Demonstrates creating branches from a common ancestor
    /// </summary>
    [Fact]
    public void Branching_CreateDivergentBranches()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        db = db.Insert("alice", new Person("Alice", 30));
        db = db.Insert("bob", new Person("Bob", 25));

        // Common ancestor
        var ancestor = db;

        // Branch A: Promote Alice
        var branchA = ancestor.Insert("alice", new Person("Alice Senior", 30));

        // Branch B: Promote Bob instead
        var branchB = ancestor.Insert("bob", new Person("Bob Senior", 25));

        // All three states exist independently
        Assert.Equal("Alice", ancestor.Get<Person>("alice").Name);
        Assert.Equal("Bob", ancestor.Get<Person>("bob").Name);

        Assert.Equal("Alice Senior", branchA.Get<Person>("alice").Name);
        Assert.Equal("Bob", branchA.Get<Person>("bob").Name);

        Assert.Equal("Alice", branchB.Get<Person>("alice").Name);
        Assert.Equal("Bob Senior", branchB.Get<Person>("bob").Name);
    }

    /// <summary>
    /// Demonstrates "what-if" scenario analysis
    /// </summary>
    [Fact]
    public void Branching_WhatIfAnalysis()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        db = db.Insert("team1", new Person("Team Lead 1", 40));
        db = db.Insert("team2", new Person("Team Lead 2", 35));

        // What-if: Scenario A - Hire 3 juniors
        var scenarioA = db
            .Insert("junior1", new Person("Junior 1", 22))
            .Insert("junior2", new Person("Junior 2", 23))
            .Insert("junior3", new Person("Junior 3", 24));

        // What-if: Scenario B - Hire 1 senior
        var scenarioB = db
            .Insert("senior1", new Person("Senior 1", 45));

        // Analyze both scenarios
        var avgAgeA = scenarioA.GetTable<Person>().Average(p => p.Value.Age);
        var avgAgeB = scenarioB.GetTable<Person>().Average(p => p.Value.Age);

        Assert.True(avgAgeB > avgAgeA, "Scenario B has higher average age");
        Assert.Equal(5, scenarioA.GetTable<Person>().Count);
        Assert.Equal(3, scenarioB.GetTable<Person>().Count);

        // Original database unchanged
        Assert.Equal(2, db.GetTable<Person>().Count);
    }

    #endregion

    #region Concurrency Safety Tests

    /// <summary>
    /// Demonstrates safe concurrent reads from different versions
    /// </summary>
    [Fact]
    public void Concurrency_SafeParallelReads()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        // Create initial state with many entries
        for (int i = 0; i < 1000; i++)
        {
            db = db.Insert($"person{i}", new Person($"Person{i}", 20 + (i % 50)));
        }

        var snapshots = new List<InvictaDatabase>();
        for (int i = 0; i < 10; i++)
        {
            db = db.Insert("person0", new Person("Person0", 100 + i));
            snapshots.Add(db);
        }

        var results = new ConcurrentBag<(int SnapshotIndex, int Person0Age, int TotalCount)>();

        // Read from all snapshots in parallel
        Parallel.For(0, 100, iteration =>
        {
            var snapshotIndex = iteration % snapshots.Count;
            var snapshot = snapshots[snapshotIndex];

            var age = snapshot.Get<Person>("person0").Age;
            var count = snapshot.GetTable<Person>().Count;

            results.Add((snapshotIndex, age, count));
        });

        // Verify all reads got consistent data
        foreach (var result in results)
        {
            Assert.Equal(100 + result.SnapshotIndex, result.Person0Age);
            Assert.Equal(1000, result.TotalCount);
        }
    }

    /// <summary>
    /// Demonstrates that writers don't affect readers
    /// </summary>
    [Fact]
    public void Concurrency_WritersDoNotAffectReaders()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        db = db.Insert("alice", new Person("Alice", 30));

        var readerSnapshot = db;
        var readResults = new ConcurrentBag<int>();
        var writeResults = new ConcurrentBag<InvictaDatabase>();

        // Parallel reads and writes
        Parallel.Invoke(
            // Reader 1: Read age 100 times from snapshot
            () =>
            {
                for (int i = 0; i < 100; i++)
                {
                    readResults.Add(readerSnapshot.Get<Person>("alice").Age);
                }
            },
            // Reader 2: Same
            () =>
            {
                for (int i = 0; i < 100; i++)
                {
                    readResults.Add(readerSnapshot.Get<Person>("alice").Age);
                }
            },
            // Writer: Create 100 new versions
            () =>
            {
                var localDb = db;
                for (int i = 0; i < 100; i++)
                {
                    localDb = localDb.Insert("alice", new Person("Alice", 100 + i));
                    writeResults.Add(localDb);
                }
            }
        );

        // All reads from the snapshot should see age 30
        Assert.All(readResults, age => Assert.Equal(30, age));

        // Writes created new versions
        Assert.Equal(100, writeResults.Count);
    }

    #endregion

    #region No Defensive Copying Tests

    /// <summary>
    /// Demonstrates that functions can't accidentally modify passed database
    /// </summary>
    [Fact]
    public void NoDefensiveCopying_FunctionsCantModifyInput()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        db = db.Insert("alice", new Person("Alice", 30));

        var originalAge = db.Get<Person>("alice").Age;

        // Pass to function that "tries" to modify
        ProcessDatabase(db);

        // Original unchanged
        Assert.Equal(originalAge, db.Get<Person>("alice").Age);
    }

    private void ProcessDatabase(InvictaDatabase db)
    {
        // This creates a new database, doesn't modify the input
        db = db.Insert("alice", new Person("Alice", 999));
        // The caller's reference is unaffected
    }

    /// <summary>
    /// Demonstrates safe sharing across components
    /// </summary>
    [Fact]
    public void NoDefensiveCopying_SafeToShareAcrossComponents()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        db = db.Insert("shared", new Person("Shared", 50));

        var component1 = new ComponentA(db);
        var component2 = new ComponentB(db);

        // Both components can read
        Assert.Equal("Shared", component1.GetPersonName("shared"));
        Assert.Equal("Shared", component2.GetPersonName("shared"));

        // Component1 "updates" (gets new version)
        var db1 = component1.UpdatePerson("shared", "Modified by A");

        // Component2 still sees original
        Assert.Equal("Shared", component2.GetPersonName("shared"));

        // Original db also unchanged
        Assert.Equal("Shared", db.Get<Person>("shared").Name);
    }

    private class ComponentA(InvictaDatabase db)
    {
        public string GetPersonName(string id) => db.Get<Person>(id).Name;
        public InvictaDatabase UpdatePerson(string id, string name) =>
            db.Insert(id, db.Get<Person>(id) with { Name = name });
    }

    private class ComponentB(InvictaDatabase db)
    {
        public string GetPersonName(string id) => db.Get<Person>(id).Name;
    }

    #endregion

    #region Audit Trail / Diff Tests

    /// <summary>
    /// Demonstrates tracking changes between versions
    /// </summary>
    [Fact]
    public void AuditTrail_TrackChangesBetweenVersions()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        db = db.Insert("alice", new Person("Alice", 30));
        db = db.Insert("bob", new Person("Bob", 25));

        var before = db;

        db = db.Insert("alice", new Person("Alice", 31)); // Updated
        db = db.Insert("charlie", new Person("Charlie", 35)); // Added

        var after = db;

        // Find changes
        var beforeKeys = before.GetTable<Person>().Keys.ToHashSet();
        var afterKeys = after.GetTable<Person>().Keys.ToHashSet();

        var added = afterKeys.Except(beforeKeys).ToList();
        var removed = beforeKeys.Except(afterKeys).ToList();
        var potentiallyModified = beforeKeys.Intersect(afterKeys).ToList();

        var modified = potentiallyModified
            .Where(key => !ReferenceEquals(
                before.Get<Person>(key),
                after.Get<Person>(key)))
            .ToList();

        Assert.Single(added);
        Assert.Contains("charlie", added);

        Assert.Empty(removed);

        Assert.Single(modified);
        Assert.Contains("alice", modified);
    }

    /// <summary>
    /// Demonstrates computing detailed diffs
    /// </summary>
    [Fact]
    public void AuditTrail_DetailedDiff()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        db = db.Insert("alice", new Person("Alice", 30));

        var v1 = db;
        db = db.Insert("alice", new Person("Alice", 31));
        var v2 = db;
        db = db.Insert("alice", new Person("Alice", 32));
        var v3 = db;

        // Build audit trail
        var versions = new[] { v1, v2, v3 };
        var auditTrail = new List<string>();

        for (int i = 1; i < versions.Length; i++)
        {
            var prev = versions[i - 1].Get<Person>("alice");
            var curr = versions[i].Get<Person>("alice");

            if (prev.Age != curr.Age)
            {
                auditTrail.Add($"v{i} -> v{i + 1}: Alice age changed from {prev.Age} to {curr.Age}");
            }
        }

        Assert.Equal(2, auditTrail.Count);
        Assert.Contains("30 to 31", auditTrail[0]);
        Assert.Contains("31 to 32", auditTrail[1]);
    }

    #endregion

    #region Transactional Semantics Tests

    /// <summary>
    /// Demonstrates atomic multi-step operations (all or nothing)
    /// </summary>
    [Fact]
    public void Transactions_AllOrNothing()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        db = db.Insert("alice", new Person("Alice", 30));
        db = db.Insert("bob", new Person("Bob", 25));

        var checkpoint = db;

        try
        {
            // Start "transaction"
            db = db.Insert("alice", new Person("Alice", 31));
            db = db.Insert("bob", new Person("Bob", 26));

            // Simulate failure
            throw new InvalidOperationException("Something went wrong!");
        }
        catch
        {
            // Rollback
            db = checkpoint;
        }

        // Both unchanged due to rollback
        Assert.Equal(30, db.Get<Person>("alice").Age);
        Assert.Equal(25, db.Get<Person>("bob").Age);
    }

    /// <summary>
    /// Demonstrates optimistic concurrency pattern
    /// </summary>
    [Fact]
    public void Transactions_OptimisticConcurrency()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Document>();

        db = db.Insert("doc1", new Document("Report", "Initial content", 1));

        // Simulate two users reading at the same time
        var user1Read = db;
        var user2Read = db;

        // User 1 makes changes
        var doc1 = user1Read.Get<Document>("doc1");
        var user1Update = user1Read.Insert("doc1",
            doc1 with { Content = "User 1 edit", Version = doc1.Version + 1 });

        // User 1 commits (succeeds - version matches)
        db = user1Update;

        // User 2 tries to make changes based on stale read
        var doc2 = user2Read.Get<Document>("doc1");
        var currentDoc = db.Get<Document>("doc1");

        // Optimistic lock check
        if (doc2.Version != currentDoc.Version)
        {
            // Conflict detected! User 2 must re-read and retry
            Assert.True(true, "Conflict detected as expected");
        }
        else
        {
            Assert.Fail("Should have detected version conflict");
        }

        // Final state has User 1's changes
        Assert.Equal("User 1 edit", db.Get<Document>("doc1").Content);
        Assert.Equal(2, db.Get<Document>("doc1").Version);
    }

    #endregion

    #region Predictable State Tests

    /// <summary>
    /// Demonstrates that state is always predictable
    /// </summary>
    [Fact]
    public void PredictableState_AlwaysConsistent()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        db = db.Insert("alice", new Person("Alice", 30));

        // Take a snapshot
        var snapshot = db;

        // No matter how many operations we do...
        for (int i = 0; i < 100; i++)
        {
            db = db.Insert("alice", new Person("Alice", i));
            db = db.Insert($"temp{i}", new Person($"Temp{i}", i));
        }

        // ...snapshot is exactly as it was
        Assert.Equal(30, snapshot.Get<Person>("alice").Age);
        Assert.Single(snapshot.GetTable<Person>());
    }

    /// <summary>
    /// Demonstrates deterministic behavior
    /// </summary>
    [Fact]
    public void PredictableState_DeterministicBehavior()
    {
        // Same operations always produce same result
        static InvictaDatabase CreateState()
        {
            return new InvictaDatabase()
                .RegisterTable<Person>()
                .Insert("a", new Person("A", 1))
                .Insert("b", new Person("B", 2))
                .Insert("a", new Person("A", 10)); // Update
        }

        var state1 = CreateState();
        var state2 = CreateState();

        // Both have identical final state
        Assert.Equal(
            state1.Get<Person>("a").Age,
            state2.Get<Person>("a").Age);

        Assert.Equal(
            state1.GetTable<Person>().Count,
            state2.GetTable<Person>().Count);
    }

    #endregion
}
