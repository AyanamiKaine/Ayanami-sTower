using InvictaDB.Events;

namespace InvictaDB.Tests;

/// <summary>
/// Unit tests for the database event system.
/// </summary>
public class DatabaseEventUnitTest
{
    internal record Player(string Name, int Score);
    internal record GameState(string Level, int Lives);

    #region Table Registration Events

    /// <summary>
    /// Registering a table should emit a TableRegistered event.
    /// </summary>
    [Fact]
    public void RegisterTable_EmitsTableRegisteredEvent()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Player>();

        Assert.Single(db.PendingEvents);
        var evt = db.PendingEvents[0];
        Assert.Equal(DatabaseEventType.TableRegistered, evt.EventType);
        Assert.Equal(typeof(Player), evt.EntityType);
        Assert.Equal("Player", evt.TableName);
    }

    /// <summary>
    /// Registering a table with custom name should emit event with that name.
    /// </summary>
    [Fact]
    public void RegisterTableWithCustomName_EmitsEventWithCustomName()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Player>("Players");

        Assert.Single(db.PendingEvents);
        var evt = db.PendingEvents[0];
        Assert.Equal(DatabaseEventType.TableRegistered, evt.EventType);
        Assert.Equal("Players", evt.TableName);
    }

    /// <summary>
    /// Registering an already existing table should not emit an event.
    /// </summary>
    [Fact]
    public void RegisterExistingTable_DoesNotEmitEvent()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Player>();
        db = db.ClearEvents();

        db = db.RegisterTable<Player>();

        Assert.Empty(db.PendingEvents);
    }

    #endregion

    #region Insert Events

    /// <summary>
    /// Inserting a new entry should emit an Inserted event.
    /// </summary>
    [Fact]
    public void Insert_NewEntry_EmitsInsertedEvent()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Player>();
        db = db.ClearEvents();

        var player = new Player("Alice", 100);
        db = db.Insert("player1", player);

        Assert.Single(db.PendingEvents);
        var evt = db.PendingEvents[0];
        Assert.Equal(DatabaseEventType.Inserted, evt.EventType);
        Assert.Equal(typeof(Player), evt.EntityType);
        Assert.Equal("player1", evt.EntityId);
        Assert.Equal("Player", evt.TableName);
        Assert.Null(evt.OldValue);
        Assert.Equal(player, evt.NewValue);
    }

    /// <summary>
    /// Updating an existing entry should emit an Updated event.
    /// </summary>
    [Fact]
    public void Insert_ExistingEntry_EmitsUpdatedEvent()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Player>();
        var oldPlayer = new Player("Alice", 100);
        db = db.Insert("player1", oldPlayer);
        db = db.ClearEvents();

        var newPlayer = new Player("Alice", 200);
        db = db.Insert("player1", newPlayer);

        Assert.Single(db.PendingEvents);
        var evt = db.PendingEvents[0];
        Assert.Equal(DatabaseEventType.Updated, evt.EventType);
        Assert.Equal(typeof(Player), evt.EntityType);
        Assert.Equal("player1", evt.EntityId);
        Assert.Equal(oldPlayer, evt.OldValue);
        Assert.Equal(newPlayer, evt.NewValue);
    }

    #endregion

    #region Singleton Events

    /// <summary>
    /// Inserting a new singleton should emit a SingletonChanged event.
    /// </summary>
    [Fact]
    public void InsertSingleton_New_EmitsSingletonChangedEvent()
    {
        var db = new InvictaDatabase();
        var state = new GameState("Level1", 3);

        db = db.InsertSingleton(state);

        Assert.Single(db.PendingEvents);
        var evt = db.PendingEvents[0];
        Assert.Equal(DatabaseEventType.SingletonChanged, evt.EventType);
        Assert.Equal(typeof(GameState), evt.EntityType);
        Assert.Equal("GameState", evt.EntityId);
        Assert.Null(evt.OldValue);
        Assert.Equal(state, evt.NewValue);
    }

    /// <summary>
    /// Updating an existing singleton should emit a SingletonChanged event with old value.
    /// </summary>
    [Fact]
    public void InsertSingleton_Existing_EmitsSingletonChangedEventWithOldValue()
    {
        var db = new InvictaDatabase();
        var oldState = new GameState("Level1", 3);
        db = db.InsertSingleton(oldState);
        db = db.ClearEvents();

        var newState = new GameState("Level2", 2);
        db = db.InsertSingleton(newState);

        Assert.Single(db.PendingEvents);
        var evt = db.PendingEvents[0];
        Assert.Equal(DatabaseEventType.SingletonChanged, evt.EventType);
        Assert.Equal(oldState, evt.OldValue);
        Assert.Equal(newState, evt.NewValue);
    }

    /// <summary>
    /// Inserting a singleton with custom ID should emit event with that ID.
    /// </summary>
    [Fact]
    public void InsertSingletonWithId_EmitsEventWithCustomId()
    {
        var db = new InvictaDatabase();
        var state = new GameState("Level1", 3);

        db = db.InsertSingleton("CurrentGame", state);

        Assert.Single(db.PendingEvents);
        var evt = db.PendingEvents[0];
        Assert.Equal(DatabaseEventType.SingletonChanged, evt.EventType);
        Assert.Equal("CurrentGame", evt.EntityId);
    }

    #endregion

    #region Remove Events

    /// <summary>
    /// Removing an entry should emit a Removed event.
    /// </summary>
    [Fact]
    public void RemoveEntry_EmitsRemovedEvent()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Player>();
        var player = new Player("Alice", 100);
        db = db.Insert("player1", player);
        db = db.ClearEvents();

        db = db.RemoveEntry<Player>("player1");

        Assert.Single(db.PendingEvents);
        var evt = db.PendingEvents[0];
        Assert.Equal(DatabaseEventType.Removed, evt.EventType);
        Assert.Equal(typeof(Player), evt.EntityType);
        Assert.Equal("player1", evt.EntityId);
        Assert.Equal("Player", evt.TableName);
        Assert.Equal(player, evt.OldValue);
        Assert.Null(evt.NewValue);
    }

    /// <summary>
    /// Removing a non-existent entry should not emit an event.
    /// </summary>
    [Fact]
    public void RemoveEntry_NonExistent_DoesNotEmitEvent()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Player>();
        db = db.ClearEvents();

        db = db.RemoveEntry<Player>("nonexistent");

        Assert.Empty(db.PendingEvents);
    }

    /// <summary>
    /// Removing a singleton should emit a Removed event.
    /// </summary>
    [Fact]
    public void RemoveSingleton_EmitsRemovedEvent()
    {
        var db = new InvictaDatabase();
        var state = new GameState("Level1", 3);
        db = db.InsertSingleton(state);
        db = db.ClearEvents();

        db = db.RemoveSingleton<GameState>();

        Assert.Single(db.PendingEvents);
        var evt = db.PendingEvents[0];
        Assert.Equal(DatabaseEventType.Removed, evt.EventType);
        Assert.Equal(typeof(GameState), evt.EntityType);
        Assert.Equal("GameState", evt.EntityId);
        Assert.Null(evt.TableName);
        Assert.Equal(state, evt.OldValue);
    }

    /// <summary>
    /// Removing a singleton by ID should emit a Removed event.
    /// </summary>
    [Fact]
    public void RemoveSingletonById_EmitsRemovedEvent()
    {
        var db = new InvictaDatabase();
        var state = new GameState("Level1", 3);
        db = db.InsertSingleton("CurrentGame", state);
        db = db.ClearEvents();

        db = db.RemoveSingleton<GameState>("CurrentGame");

        Assert.Single(db.PendingEvents);
        var evt = db.PendingEvents[0];
        Assert.Equal(DatabaseEventType.Removed, evt.EventType);
        Assert.Equal("CurrentGame", evt.EntityId);
    }

    /// <summary>
    /// Removing a non-existent singleton should not emit an event.
    /// </summary>
    [Fact]
    public void RemoveSingleton_NonExistent_DoesNotEmitEvent()
    {
        var db = new InvictaDatabase();

        db = db.RemoveSingleton<GameState>();

        Assert.Empty(db.PendingEvents);
    }

    #endregion

    #region ClearEvents

    /// <summary>
    /// ClearEvents should return a database with no pending events.
    /// </summary>
    [Fact]
    public void ClearEvents_RemovesAllPendingEvents()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Player>();
        db = db.Insert("player1", new Player("Alice", 100));
        db = db.InsertSingleton(new GameState("Level1", 3));

        Assert.Equal(3, db.PendingEvents.Count);

        db = db.ClearEvents();

        Assert.Empty(db.PendingEvents);
    }

    /// <summary>
    /// ClearEvents should preserve database state.
    /// </summary>
    [Fact]
    public void ClearEvents_PreservesDatabaseState()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Player>();
        var player = new Player("Alice", 100);
        db = db.Insert("player1", player);

        db = db.ClearEvents();

        Assert.True(db.TableExists<Player>());
        Assert.Equal(player, db.GetEntry<Player>("player1"));
    }

    #endregion

    #region EventLog Query Methods

    /// <summary>
    /// GetEvents should filter by event type.
    /// </summary>
    [Fact]
    public void EventLog_GetEvents_FiltersByEventType()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Player>();
        db = db.Insert("player1", new Player("Alice", 100));
        db = db.Insert("player1", new Player("Alice", 200)); // Update

        var insertEvents = db.EventLog.GetEvents(DatabaseEventType.Inserted).ToList();
        var updateEvents = db.EventLog.GetEvents(DatabaseEventType.Updated).ToList();

        Assert.Single(insertEvents);
        Assert.Single(updateEvents);
    }

    /// <summary>
    /// GetEventsForType should filter by entity type.
    /// </summary>
    [Fact]
    public void EventLog_GetEventsForType_FiltersByEntityType()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Player>();
        db = db.Insert("player1", new Player("Alice", 100));
        db = db.InsertSingleton(new GameState("Level1", 3));

        var playerEvents = db.EventLog.GetEventsForType<Player>().ToList();
        var gameStateEvents = db.EventLog.GetEventsForType<GameState>().ToList();

        Assert.Equal(2, playerEvents.Count); // TableRegistered + Inserted
        Assert.Single(gameStateEvents);
    }

    /// <summary>
    /// GetEventsForEntity should filter by entity type and ID.
    /// </summary>
    [Fact]
    public void EventLog_GetEventsForEntity_FiltersByEntityAndId()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Player>();
        db = db.Insert("player1", new Player("Alice", 100));
        db = db.Insert("player2", new Player("Bob", 50));
        db = db.Insert("player1", new Player("Alice", 200)); // Update player1

        var player1Events = db.EventLog.GetEventsForEntity<Player>("player1").ToList();
        var player2Events = db.EventLog.GetEventsForEntity<Player>("player2").ToList();

        Assert.Equal(2, player1Events.Count); // Insert + Update
        Assert.Single(player2Events); // Just Insert
    }

    /// <summary>
    /// Events should have timestamps that increase.
    /// </summary>
    [Fact]
    public void Events_HaveIncreasingTimestamps()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Player>();
        db = db.Insert("player1", new Player("Alice", 100));
        db = db.Insert("player2", new Player("Bob", 50));

        var events = db.PendingEvents;
        for (int i = 1; i < events.Count; i++)
        {
            Assert.True(events[i].Timestamp >= events[i - 1].Timestamp);
        }
    }

    #endregion

    #region Immutability

    /// <summary>
    /// Original database should not be affected by operations on new database.
    /// </summary>
    [Fact]
    public void Events_ImmutableAcrossInstances()
    {
        var db1 = new InvictaDatabase();
        db1 = db1.RegisterTable<Player>();

        var db2 = db1.Insert("player1", new Player("Alice", 100));

        // db1 should only have the table registration event
        Assert.Single(db1.PendingEvents);
        Assert.Equal(DatabaseEventType.TableRegistered, db1.PendingEvents[0].EventType);

        // db2 should have both events
        Assert.Equal(2, db2.PendingEvents.Count);
    }

    #endregion
}
