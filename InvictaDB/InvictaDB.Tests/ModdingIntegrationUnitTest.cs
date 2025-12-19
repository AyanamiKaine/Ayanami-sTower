using InvictaDB.Messaging;

namespace InvictaDB.Tests;

/// <summary>
/// Demonstrates how mods could use the event bus to react to game events.
/// </summary>
public class ModdingIntegrationUnitTest
{
    #region Game Event Types (Shared between game and mods)

    /// <summary>
    /// Event fired when a unit is created.
    /// </summary>
    internal record UnitCreated(string UnitId, string UnitType, string OwnerId);

    /// <summary>
    /// Event fired when a unit is destroyed.
    /// </summary>
    internal record UnitDestroyed(string UnitId, string DestroyedBy);

    /// <summary>
    /// Event fired when resources are gained.
    /// </summary>
    internal record ResourcesGained(string PlayerId, string ResourceType, int Amount);

    #endregion

    #region Game Data Types

    internal record Unit(string Id, string Type, string OwnerId, int Health);
    internal record Player(string Id, string Name, int Gold, int Kills);

    #endregion

    #region Simulated Game Systems

    /// <summary>
    /// Simulates a game system that spawns units and fires events.
    /// </summary>
    private static InvictaDatabase SpawnerSystem_Update(InvictaDatabase db, string unitId, string unitType, string ownerId)
    {
        // 1. Insert the unit into the database
        var unit = new Unit(unitId, unitType, ownerId, 100);
        db = db.Insert(unitId, unit);

        // 2. Broadcast the event so mods (and other systems) can react
        db = db.SendMessage("SpawnerSystem", new UnitCreated(unitId, unitType, ownerId));

        return db;
    }

    /// <summary>
    /// Simulates a combat system that destroys units and fires events.
    /// </summary>
    private static InvictaDatabase CombatSystem_DestroyUnit(InvictaDatabase db, string unitId, string destroyedBy)
    {
        // 1. Remove the unit from the database
        db = db.RemoveEntry<Unit>(unitId);

        // 2. Broadcast the event
        db = db.SendMessage("CombatSystem", new UnitDestroyed(unitId, destroyedBy));

        return db;
    }

    #endregion

    #region Simulated Mod Handlers

    /// <summary>
    /// A mod that listens for UnitCreated events and grants gold to the owner.
    /// </summary>
    private static InvictaDatabase Mod_GoldOnSpawn_OnTick(InvictaDatabase db)
    {
        // Read all UnitCreated events (do not consume/remove them)
        foreach (var evt in db.Messages.GetMessages<UnitCreated>())
        {
            var payload = evt.GetPayload<UnitCreated>();
            if (payload == null) continue;

            // Grant 10 gold to the owner for each unit spawned
            var player = db.Get<Player>(payload.OwnerId);
            if (player != null)
            {
                var updatedPlayer = player with { Gold = player.Gold + 10 };
                db = db.Insert(payload.OwnerId, updatedPlayer);

                // The mod can also fire its own events!
                db = db.SendMessage("GoldOnSpawnMod", new ResourcesGained(payload.OwnerId, "Gold", 10));
            }
        }

        return db;
    }

    /// <summary>
    /// A mod that tracks kill counts when units are destroyed.
    /// </summary>
    private static InvictaDatabase Mod_KillTracker_OnTick(InvictaDatabase db)
    {
        foreach (var evt in db.Messages.GetMessages<UnitDestroyed>())
        {
            var payload = evt.GetPayload<UnitDestroyed>();
            if (payload == null) continue;

            // Increment kills for the destroyer
            var killer = db.Get<Player>(payload.DestroyedBy);
            if (killer != null)
            {
                var updatedKiller = killer with { Kills = killer.Kills + 1 };
                db = db.Insert(payload.DestroyedBy, updatedKiller);
            }
        }

        return db;
    }

    #endregion

    #region Tests

    /// <summary>
    /// Demonstrates a full game tick where:
    /// 1. Game systems run and emit events.
    /// 2. Mods react to those events.
    /// 3. Events are cleared at the end of the tick.
    /// </summary>
    [Fact]
    public void FullTick_ModsReactToGameEvents()
    {
        // Setup: Register tables and create initial players
        var db = new InvictaDatabase();
        db = db.RegisterTable<Unit>("Units");
        db = db.RegisterTable<Player>("Players");

        db = db.Insert("player1", new Player("player1", "Alice", 100, 0));
        db = db.Insert("player2", new Player("player2", "Bob", 100, 0));

        // === GAME TICK START ===

        // Phase 1: Game Systems run
        db = SpawnerSystem_Update(db, "unit1", "Soldier", "player1");
        db = SpawnerSystem_Update(db, "unit2", "Archer", "player1");

        // Phase 2: Mods run (they see events from Phase 1)
        db = Mod_GoldOnSpawn_OnTick(db);

        // Phase 3: Clear events at end of tick
        db = db.ClearMessages();

        // === GAME TICK END ===

        // Verify: Player1 should have gained 20 gold (10 per unit spawned)
        var player1 = db.Get<Player>("player1");
        Assert.NotNull(player1);
        Assert.Equal(120, player1.Gold); // 100 + 10 + 10

        // Verify: Events are cleared
        Assert.True(db.Messages.IsEmpty);
    }

    /// <summary>
    /// Demonstrates multiple mods reacting to the same event.
    /// </summary>
    [Fact]
    public void MultipleMods_CanReactToSameEvent()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Unit>("Units");
        db = db.RegisterTable<Player>("Players");

        db = db.Insert("player1", new Player("player1", "Alice", 100, 0));
        db = db.Insert("player2", new Player("player2", "Bob", 100, 0));

        // Spawn a unit owned by player1
        db = SpawnerSystem_Update(db, "unit1", "Soldier", "player1");

        // Destroy the unit (killed by player2)
        db = CombatSystem_DestroyUnit(db, "unit1", "player2");

        // Both mods run and see ALL events from this tick
        db = Mod_GoldOnSpawn_OnTick(db);   // Sees UnitCreated
        db = Mod_KillTracker_OnTick(db);   // Sees UnitDestroyed

        // Verify: Player1 got gold for spawning
        var player1 = db.Get<Player>("player1");
        Assert.Equal(110, player1!.Gold);

        // Verify: Player2 got a kill
        var player2 = db.Get<Player>("player2");
        Assert.Equal(1, player2!.Kills);

        // Cleanup
        db = db.ClearMessages();
    }

    /// <summary>
    /// Demonstrates that mods can emit their own events.
    /// </summary>
    [Fact]
    public void Mods_CanEmitTheirOwnEvents()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Unit>("Units");
        db = db.RegisterTable<Player>("Players");

        db = db.Insert("player1", new Player("player1", "Alice", 100, 0));

        // Game system spawns a unit
        db = SpawnerSystem_Update(db, "unit1", "Soldier", "player1");

        // Mod runs and emits its own ResourcesGained event
        db = Mod_GoldOnSpawn_OnTick(db);

        // Verify: The mod's event is in the queue
        var resourceEvents = db.Messages.GetMessages<ResourcesGained>().ToList();
        Assert.Single(resourceEvents);

        var payload = resourceEvents[0].GetPayload<ResourcesGained>();
        Assert.Equal("player1", payload!.PlayerId);
        Assert.Equal("Gold", payload.ResourceType);
        Assert.Equal(10, payload.Amount);
    }

    /// <summary>
    /// Demonstrates using Batch operations with events.
    /// </summary>
    [Fact]
    public void Batch_CanEmitMultipleEventsEfficiently()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Unit>("Units");

        // A hypothetical "mass spawn" operation using batch
        db = db.Batch(batch =>
        {
            for (int i = 0; i < 5; i++)
            {
                var unitId = $"unit_{i}";
                batch.Insert(unitId, new Unit(unitId, "Soldier", "player1", 100));
                batch.SendMessage("SpawnerSystem", new UnitCreated(unitId, "Soldier", "player1"));
            }
        });

        // Verify: All 5 events are in the queue
        var events = db.Messages.GetMessages<UnitCreated>().ToList();
        Assert.Equal(5, events.Count);

        // Verify: All 5 units exist
        var units = db.GetTable<Unit>().Values.ToList();
        Assert.Equal(5, units.Count);
    }

    #endregion
}
