using InvictaDB.Events;

namespace InvictaDB.Tests;

/// <summary>
/// Unit tests for the InvictaDatabase.BatchOperations class.
/// </summary>
public class BatchOperationsUnitTest
{
    /// <summary>
    /// Simple test entity for batch operation tests.
    /// </summary>
    private record TestEntity(string Id, string Name, int Value);

    /// <summary>
    /// Simple singleton for batch operation tests.
    /// </summary>
    private record TestConfig(string Setting, int Number);

    /// <summary>
    /// Test message for batch tests.
    /// </summary>
    private record TestMessage(string Content);

    #region Basic Insert Tests

    /// <summary>
    /// Tests that batch insert adds a single entry correctly.
    /// </summary>
    [Fact]
    public void Batch_InsertSingleEntry_AddsEntryToDatabase()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities");

        // Act
        var result = db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "Test", 100));
        });

        // Assert
        var entity = result.GetEntry<TestEntity>("1");
        Assert.NotNull(entity);
        Assert.Equal("Test", entity.Name);
        Assert.Equal(100, entity.Value);
    }

    /// <summary>
    /// Tests that batch insert adds multiple entries in a single batch.
    /// </summary>
    [Fact]
    public void Batch_InsertMultipleEntries_AddsAllEntries()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities");

        // Act
        var result = db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "First", 100));
            batch.Insert("2", new TestEntity("2", "Second", 200));
            batch.Insert("3", new TestEntity("3", "Third", 300));
        });

        // Assert
        Assert.Equal(3, result.GetTable<TestEntity>().Count);
        Assert.Equal("First", result.GetEntry<TestEntity>("1")?.Name);
        Assert.Equal("Second", result.GetEntry<TestEntity>("2")?.Name);
        Assert.Equal("Third", result.GetEntry<TestEntity>("3")?.Name);
    }

    /// <summary>
    /// Tests that batch insert to unregistered table throws exception.
    /// </summary>
    [Fact]
    public void Batch_InsertToUnregisteredTable_ThrowsException()
    {
        // Arrange
        var db = new InvictaDatabase();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            db.Batch(batch =>
            {
                batch.Insert("1", new TestEntity("1", "Test", 100));
            });
        });
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Tests that batch insert updates an existing entry.
    /// </summary>
    [Fact]
    public void Batch_InsertExistingEntry_UpdatesEntry()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities")
            .Insert("1", new TestEntity("1", "Original", 100));

        // Act
        var result = db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "Updated", 200));
        });

        // Assert
        var entity = result.GetEntry<TestEntity>("1");
        Assert.NotNull(entity);
        Assert.Equal("Updated", entity.Name);
        Assert.Equal(200, entity.Value);
        Assert.Single(result.GetTable<TestEntity>());
    }

    /// <summary>
    /// Tests that multiple updates to the same entry within a batch work correctly.
    /// </summary>
    [Fact]
    public void Batch_MultipleUpdatesToSameEntry_LastUpdateWins()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities");

        // Act
        var result = db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "First", 100));
            batch.Insert("1", new TestEntity("1", "Second", 200));
            batch.Insert("1", new TestEntity("1", "Third", 300));
        });

        // Assert
        var entity = result.GetEntry<TestEntity>("1");
        Assert.NotNull(entity);
        Assert.Equal("Third", entity.Name);
        Assert.Equal(300, entity.Value);
    }

    #endregion

    #region Remove Tests

    /// <summary>
    /// Tests that batch remove removes an existing entry.
    /// </summary>
    [Fact]
    public void Batch_RemoveExistingEntry_RemovesEntry()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities")
            .Insert("1", new TestEntity("1", "Test", 100));

        // Act
        var result = db.Batch(batch =>
        {
            batch.RemoveEntry<TestEntity>("1");
        });

        // Assert
        Assert.Null(result.GetEntry<TestEntity>("1"));
        Assert.Empty(result.GetTable<TestEntity>());
    }

    /// <summary>
    /// Tests that batch remove of non-existing entry does nothing.
    /// </summary>
    [Fact]
    public void Batch_RemoveNonExistingEntry_DoesNothing()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities")
            .Insert("1", new TestEntity("1", "Test", 100));

        // Act
        var result = db.Batch(batch =>
        {
            batch.RemoveEntry<TestEntity>("nonexistent");
        });

        // Assert
        Assert.Single(result.GetTable<TestEntity>());
        Assert.NotNull(result.GetEntry<TestEntity>("1"));
    }

    /// <summary>
    /// Tests that insert followed by remove in same batch results in no entry.
    /// </summary>
    [Fact]
    public void Batch_InsertThenRemove_EntryNotPresent()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities");

        // Act
        var result = db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "Test", 100));
            batch.RemoveEntry<TestEntity>("1");
        });

        // Assert
        Assert.Null(result.GetEntry<TestEntity>("1"));
        Assert.Empty(result.GetTable<TestEntity>());
    }

    /// <summary>
    /// Tests that remove followed by insert in same batch results in entry present.
    /// </summary>
    [Fact]
    public void Batch_RemoveThenInsert_EntryPresent()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities")
            .Insert("1", new TestEntity("1", "Original", 100));

        // Act
        var result = db.Batch(batch =>
        {
            batch.RemoveEntry<TestEntity>("1");
            batch.Insert("1", new TestEntity("1", "New", 200));
        });

        // Assert
        var entity = result.GetEntry<TestEntity>("1");
        Assert.NotNull(entity);
        Assert.Equal("New", entity.Name);
    }

    #endregion

    #region Singleton Tests

    /// <summary>
    /// Tests that batch singleton insert adds a singleton.
    /// </summary>
    [Fact]
    public void Batch_InsertSingleton_AddsSingleton()
    {
        // Arrange
        var db = new InvictaDatabase();

        // Act
        var result = db.Batch(batch =>
        {
            batch.InsertSingleton(new TestConfig("value", 42));
        });

        // Assert
        var config = result.GetSingleton<TestConfig>();
        Assert.Equal("value", config.Setting);
        Assert.Equal(42, config.Number);
    }

    /// <summary>
    /// Tests that batch singleton insert with ID adds a singleton.
    /// </summary>
    [Fact]
    public void Batch_InsertSingletonWithId_AddsSingleton()
    {
        // Arrange
        var db = new InvictaDatabase();

        // Act
        var result = db.Batch(batch =>
        {
            batch.InsertSingleton("custom-config", new TestConfig("custom", 99));
        });

        // Assert
        var config = result.GetSingleton<TestConfig>("custom-config");
        Assert.Equal("custom", config.Setting);
        Assert.Equal(99, config.Number);
    }

    /// <summary>
    /// Tests that batch singleton update updates an existing singleton.
    /// </summary>
    [Fact]
    public void Batch_UpdateSingleton_UpdatesSingleton()
    {
        // Arrange
        var db = new InvictaDatabase()
            .InsertSingleton(new TestConfig("original", 10));

        // Act
        var result = db.Batch(batch =>
        {
            batch.InsertSingleton(new TestConfig("updated", 20));
        });

        // Assert
        var config = result.GetSingleton<TestConfig>();
        Assert.Equal("updated", config.Setting);
        Assert.Equal(20, config.Number);
    }

    /// <summary>
    /// Tests that batch singleton remove removes a singleton.
    /// </summary>
    [Fact]
    public void Batch_RemoveSingleton_RemovesSingleton()
    {
        // Arrange
        var db = new InvictaDatabase()
            .InsertSingleton(new TestConfig("test", 42));

        // Act
        var result = db.Batch(batch =>
        {
            batch.RemoveSingleton<TestConfig>();
        });

        // Assert
        Assert.Throws<InvalidOperationException>(() => result.GetSingleton<TestConfig>());
    }

    /// <summary>
    /// Tests that batch singleton remove with ID removes a singleton.
    /// </summary>
    [Fact]
    public void Batch_RemoveSingletonWithId_RemovesSingleton()
    {
        // Arrange
        var db = new InvictaDatabase()
            .InsertSingleton("custom", new TestConfig("test", 42));

        // Act
        var result = db.Batch(batch =>
        {
            batch.RemoveSingleton<TestConfig>("custom");
        });

        // Assert
        Assert.Throws<InvalidOperationException>(() => result.GetSingleton<TestConfig>("custom"));
    }

    #endregion

    #region Batch Query Tests

    /// <summary>
    /// Tests that GetEntry within batch returns newly inserted entry.
    /// </summary>
    [Fact]
    public void Batch_GetEntryAfterInsert_ReturnsInsertedEntry()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities");

        // Act
        TestEntity? queriedEntity = null;
        var result = db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "Test", 100));
            queriedEntity = batch.GetEntry<TestEntity>("1");
        });

        // Assert
        Assert.NotNull(queriedEntity);
        Assert.Equal("Test", queriedEntity.Name);
    }

    /// <summary>
    /// Tests that GetEntry within batch returns null after remove.
    /// </summary>
    [Fact]
    public void Batch_GetEntryAfterRemove_ReturnsNull()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities")
            .Insert("1", new TestEntity("1", "Test", 100));

        // Act
        TestEntity? queriedEntity = null;
        db.Batch(batch =>
        {
            batch.RemoveEntry<TestEntity>("1");
            queriedEntity = batch.GetEntry<TestEntity>("1");
        });

        // Assert
        Assert.Null(queriedEntity);
    }

    /// <summary>
    /// Tests that Exists within batch returns true for inserted entry.
    /// </summary>
    [Fact]
    public void Batch_ExistsAfterInsert_ReturnsTrue()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities");

        // Act
        bool exists = false;
        db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "Test", 100));
            exists = batch.Exists<TestEntity>("1");
        });

        // Assert
        Assert.True(exists);
    }

    /// <summary>
    /// Tests that Exists within batch returns false after remove.
    /// </summary>
    [Fact]
    public void Batch_ExistsAfterRemove_ReturnsFalse()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities")
            .Insert("1", new TestEntity("1", "Test", 100));

        // Act
        bool exists = true;
        db.Batch(batch =>
        {
            batch.RemoveEntry<TestEntity>("1");
            exists = batch.Exists<TestEntity>("1");
        });

        // Assert
        Assert.False(exists);
    }

    /// <summary>
    /// Tests that GetSingleton within batch returns updated singleton.
    /// </summary>
    [Fact]
    public void Batch_GetSingletonAfterInsert_ReturnsInsertedSingleton()
    {
        // Arrange
        var db = new InvictaDatabase();

        // Act
        TestConfig? queriedConfig = null;
        db.Batch(batch =>
        {
            batch.InsertSingleton(new TestConfig("test", 42));
            queriedConfig = batch.GetSingleton<TestConfig>();
        });

        // Assert
        Assert.NotNull(queriedConfig);
        Assert.Equal("test", queriedConfig.Setting);
    }

    #endregion

    #region Event Log Tests

    /// <summary>
    /// Tests that batch insert creates insert event.
    /// </summary>
    [Fact]
    public void Batch_Insert_CreatesInsertEvent()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities")
            .ClearEvents();

        // Act
        var result = db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "Test", 100));
        });

        // Assert
        var events = result.EventLog.Events;
        Assert.Single(events);
        Assert.Equal(Events.DatabaseEventType.Inserted, events[0].EventType);
        Assert.Equal("entities", events[0].TableName);
        Assert.Equal("1", events[0].EntityId);
    }

    /// <summary>
    /// Tests that batch update creates update event.
    /// </summary>
    [Fact]
    public void Batch_Update_CreatesUpdateEvent()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities")
            .Insert("1", new TestEntity("1", "Original", 100));

        // Clear the initial insert event by getting a fresh log
        db = db.ClearEvents();

        // Act
        var result = db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "Updated", 200));
        });

        // Assert
        var events = result.EventLog.Events;
        Assert.Single(events);
        Assert.Equal(Events.DatabaseEventType.Updated, events[0].EventType);
    }

    /// <summary>
    /// Tests that batch remove creates remove event.
    /// </summary>
    [Fact]
    public void Batch_Remove_CreatesRemoveEvent()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities")
            .Insert("1", new TestEntity("1", "Test", 100))
            .ClearEvents();

        // Act
        var result = db.Batch(batch =>
        {
            batch.RemoveEntry<TestEntity>("1");
        });

        // Assert
        var events = result.EventLog.Events;
        Assert.Single(events);
        Assert.Equal(Events.DatabaseEventType.Removed, events[0].EventType);
    }

    /// <summary>
    /// Tests that batch creates multiple events for multiple operations.
    /// </summary>
    [Fact]
    public void Batch_MultipleOperations_CreatesMultipleEvents()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities")
            .ClearEvents();

        // Act
        var result = db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "First", 100));
            batch.Insert("2", new TestEntity("2", "Second", 200));
            batch.Insert("3", new TestEntity("3", "Third", 300));
        });

        // Assert
        var events = result.EventLog.Events;
        Assert.Equal(3, events.Count);
        Assert.All(events, e => Assert.Equal(Events.DatabaseEventType.Inserted, e.EventType));
    }

    #endregion

    #region Message Queue Tests

    /// <summary>
    /// Tests that batch SendMessage adds message to queue.
    /// </summary>
    [Fact]
    public void Batch_SendMessage_AddsMessageToQueue()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities");

        // Act
        var result = db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "Test", 100));
            batch.SendMessage("TestSystem", new TestMessage("Hello"));
        });

        // Assert
        Assert.False(result.Messages.IsEmpty);
        var message = result.Messages.Peek();
        Assert.NotNull(message);
        Assert.Equal("Hello", message.GetPayload<TestMessage>()?.Content);
    }

    #endregion

    #region Immutability Tests

    /// <summary>
    /// Tests that batch operation returns new database instance.
    /// </summary>
    [Fact]
    public void Batch_ReturnsNewInstance_OriginalUnchanged()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities");

        // Act
        var result = db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "Test", 100));
        });

        // Assert
        Assert.NotSame(db, result);
        Assert.Null(db.GetEntry<TestEntity>("1"));
        Assert.NotNull(result.GetEntry<TestEntity>("1"));
    }

    /// <summary>
    /// Tests that empty batch returns equivalent database.
    /// </summary>
    [Fact]
    public void Batch_EmptyBatch_ReturnsEquivalentDatabase()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities")
            .Insert("1", new TestEntity("1", "Test", 100));

        // Act
        var result = db.Batch(batch => { });

        // Assert
        Assert.NotSame(db, result);
        Assert.Single(result.GetTable<TestEntity>());
        Assert.NotNull(result.GetEntry<TestEntity>("1"));
    }

    #endregion

    #region Multiple Table Tests

    /// <summary>
    /// Simple second entity type for multi-table tests.
    /// </summary>
    private record SecondEntity(string Id, string Description);

    /// <summary>
    /// Tests that batch can operate on multiple tables.
    /// </summary>
    [Fact]
    public void Batch_MultipleTableOperations_WorksCorrectly()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities")
            .RegisterTable<SecondEntity>("second");

        // Act
        var result = db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "First", 100));
            batch.Insert("a", new SecondEntity("a", "Alpha"));
            batch.Insert("2", new TestEntity("2", "Second", 200));
            batch.Insert("b", new SecondEntity("b", "Beta"));
        });

        // Assert
        Assert.Equal(2, result.GetTable<TestEntity>().Count);
        Assert.Equal(2, result.GetTable<SecondEntity>().Count);
        Assert.Equal("First", result.GetEntry<TestEntity>("1")?.Name);
        Assert.Equal("Alpha", result.GetEntry<SecondEntity>("a")?.Description);
    }

    /// <summary>
    /// Tests that batch can mix inserts and removes across tables.
    /// </summary>
    [Fact]
    public void Batch_MixedOperationsAcrossTables_WorksCorrectly()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities")
            .RegisterTable<SecondEntity>("second")
            .Insert("1", new TestEntity("1", "Original", 100))
            .Insert("a", new SecondEntity("a", "Alpha"));

        // Act
        var result = db.Batch(batch =>
        {
            batch.Insert("1", new TestEntity("1", "Updated", 200));  // Update
            batch.RemoveEntry<SecondEntity>("a");                     // Remove
            batch.Insert("2", new TestEntity("2", "New", 300));       // Insert
        });

        // Assert
        Assert.Equal(2, result.GetTable<TestEntity>().Count);
        Assert.Empty(result.GetTable<SecondEntity>());
        Assert.Equal("Updated", result.GetEntry<TestEntity>("1")?.Name);
        Assert.Equal("New", result.GetEntry<TestEntity>("2")?.Name);
    }

    #endregion

    #region Performance-Oriented Tests

    /// <summary>
    /// Tests that batch can handle large number of inserts.
    /// </summary>
    [Fact]
    public void Batch_LargeNumberOfInserts_WorksCorrectly()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities");

        const int count = 1000;

        // Act
        var result = db.Batch(batch =>
        {
            for (int i = 0; i < count; i++)
            {
                batch.Insert(i.ToString(), new TestEntity(i.ToString(), $"Entity{i}", i));
            }
        });

        // Assert
        Assert.Equal(count, result.GetTable<TestEntity>().Count);
        Assert.Equal("Entity500", result.GetEntry<TestEntity>("500")?.Name);
    }

    /// <summary>
    /// Tests that batch can handle interleaved insert and update operations.
    /// </summary>
    [Fact]
    public void Batch_InterleavedInsertAndUpdate_WorksCorrectly()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>("entities");

        // Act
        var result = db.Batch(batch =>
        {
            // Insert
            batch.Insert("1", new TestEntity("1", "Original", 100));

            // Update via insert
            batch.Insert("1", new TestEntity("1", "Updated1", 150));

            // Insert another
            batch.Insert("2", new TestEntity("2", "Second", 200));

            // Update first again
            batch.Insert("1", new TestEntity("1", "Updated2", 175));
        });

        // Assert
        Assert.Equal(2, result.GetTable<TestEntity>().Count);
        Assert.Equal("Updated2", result.GetEntry<TestEntity>("1")?.Name);
        Assert.Equal(175, result.GetEntry<TestEntity>("1")?.Value);
    }

    #endregion
}
