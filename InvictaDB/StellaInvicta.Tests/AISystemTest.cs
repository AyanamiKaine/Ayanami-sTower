using System;
using FluidHTN;
using InvictaDB;
using StellaInvicta.AI;
using StellaInvicta.Data;
using Xunit;

namespace StellaInvicta.Tests;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// Test data records for AI scenarios
public record GoldItem(int Amount);
public record Weapon(string Name, int Damage);
public record Ship(string Name, string CurrentLocation, string? Destination, int CargoCapacity, int CurrentCargo);
public record TradeGood(string Name, int Quantity, int Value);

public class AISystemTest
{
    [Fact]
    public void AISystem_RunsHTNPlanner_AndAppliesMutations()
    {
        // 1. Setup Database
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<GoldItem>();

        // Create a character
        var charId = "char_1";
        var character = new Character("Test Char", 20, 5, 5, 5, 5, DateTime.Now);
        db = db.Insert(charId, character);

        // Create AIAgentData
        // DomainName = "Worker", CurrentStateLabel = "Idle"
        var agentData = new AIAgentData("Worker", "Idle");
        db = db.Insert(charId, agentData);

        // 2. Setup AI System
        var aiSystem = new AISystem();
        db = aiSystem.Initialize(db);

        // 3. Define Domain
        // We want the agent to "Work" if they don't have credits (simulated by WorldState).
        // The action will add a "Gold" item to the DB.

        var domainBuilder = new DomainBuilder<SimulationContext>("Worker");
        var domain = domainBuilder
            .Select("Work Selector")
                .Condition("Has No Credits", (ctx) => !ctx.GetWorldState(WorldState.HasCredits))
                .Action("Work")
                    .Do((ctx) =>
                    {
                        // Queue a mutation to add gold
                        ctx.ApplyChange(d => d.Insert("gold_1", new GoldItem(100)));
                        return FluidHTN.TaskStatus.Success;
                    })
                    .Effect("Earned Credits", EffectType.PlanAndExecute, (ctx, type) => ctx.SetWorldState(WorldState.HasCredits, true))
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Worker", domain);

        // 4. Run AI System
        // The planner should see !HasCredits, plan "Work", execute it, and queue the mutation.
        var nextDb = aiSystem.Run(db);

        // 5. Assert
        // Check if "gold_1" exists in nextDb
        var goldTable = nextDb.GetTable<GoldItem>();
        Assert.True(goldTable.ContainsKey("gold_1"));
        Assert.Equal(100, goldTable["gold_1"].Amount);
    }

    /// <summary>
    /// Tests a multi-step sequence: Agent needs to get a weapon before attacking.
    /// This demonstrates the power of HTN planning - it figures out prerequisites.
    /// </summary>
    [Fact]
    public void AISystem_MultiStepSequence_GetWeaponThenAttack()
    {
        // Setup
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<Weapon>();

        var guardId = "guard_1";
        db = db.Insert(guardId, new Character("Guard", 30, 10, 5, 5, 5, DateTime.Now));
        db = db.Insert(guardId, new AIAgentData("Fighter", "Idle"));

        var aiSystem = new AISystem();
        db = aiSystem.Initialize(db);

        // Track actions executed
        var actionsExecuted = new List<string>();

        // Define Fighter Domain
        // Priority-based: Attack if armed, otherwise get weapon first
        var domain = new DomainBuilder<SimulationContext>("Fighter")
            .Select("Combat Root")
                // Priority 1: Attack if we have a weapon
                .Action("Attack Enemy")
                    .Condition("Has Weapon", ctx => ctx.Db.GetTable<Weapon>().ContainsKey(ctx.Self.Id))
                    .Do(ctx =>
                    {
                        actionsExecuted.Add("Attack");
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
                // Priority 2: Get weapon if we don't have one
                .Action("Get Weapon")
                    .Condition("No Weapon", ctx => !ctx.Db.GetTable<Weapon>().ContainsKey(ctx.Self.Id))
                    .Do(ctx =>
                    {
                        actionsExecuted.Add("GetWeapon");
                        ctx.ApplyChange(d => d.Insert(ctx.Self.Id, new Weapon("Sword", 10)));
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Fighter", domain);

        // Run - First tick: should get weapon (no weapon yet)
        var nextDb = aiSystem.Run(db);

        // Assert: Guard got a weapon
        Assert.Contains("GetWeapon", actionsExecuted);
        var weapons = nextDb.GetTable<Weapon>();
        Assert.True(weapons.ContainsKey(guardId));
        Assert.Equal("Sword", weapons[guardId].Name);

        // Run again - Second tick: should attack (already has weapon)
        actionsExecuted.Clear();
        nextDb = aiSystem.Run(nextDb);

        // Assert: Guard attacked (didn't need to get weapon again)
        Assert.Contains("Attack", actionsExecuted);
        Assert.DoesNotContain("GetWeapon", actionsExecuted);
    }

    /// <summary>
    /// Tests multiple agents running simultaneously, each with different behaviors.
    /// </summary>
    [Fact]
    public void AISystem_MultipleAgents_DifferentBehaviors()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<GoldItem>();
        db = db.RegisterTable<Weapon>();

        // Create a worker and a fighter
        db = db.Insert("worker_1", new Character("Worker Bob", 25, 3, 8, 2, 4, DateTime.Now));
        db = db.Insert("worker_1", new AIAgentData("Worker", "Idle"));

        db = db.Insert("fighter_1", new Character("Fighter Jane", 28, 12, 4, 6, 3, DateTime.Now));
        db = db.Insert("fighter_1", new AIAgentData("Fighter", "Idle"));

        var aiSystem = new AISystem();
        db = aiSystem.Initialize(db);

        // Worker domain - earns gold
        var workerDomain = new DomainBuilder<SimulationContext>("Worker")
            .Select("Work")
                .Action("Earn Gold")
                    .Do(ctx =>
                    {
                        ctx.ApplyChange(d => d.Insert(ctx.Self.Id + "_gold", new GoldItem(50)));
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        // Fighter domain - gets weapon
        var fighterDomain = new DomainBuilder<SimulationContext>("Fighter")
            .Select("Fight")
                .Action("Arm Up")
                    .Do(ctx =>
                    {
                        ctx.ApplyChange(d => d.Insert(ctx.Self.Id + "_weapon", new Weapon("Axe", 15)));
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Worker", workerDomain);
        aiSystem.RegisterDomain("Fighter", fighterDomain);

        // Run
        var nextDb = aiSystem.Run(db);

        // Assert: Both agents acted according to their domains
        var goldTable = nextDb.GetTable<GoldItem>();
        var weaponTable = nextDb.GetTable<Weapon>();

        Assert.True(goldTable.ContainsKey("worker_1_gold"));
        Assert.Equal(50, goldTable["worker_1_gold"].Amount);

        Assert.True(weaponTable.ContainsKey("fighter_1_weapon"));
        Assert.Equal("Axe", weaponTable["fighter_1_weapon"].Name);
    }

    /// <summary>
    /// Tests reading entity data from the database within HTN conditions.
    /// Agent behavior changes based on their stats.
    /// </summary>
    [Fact]
    public void AISystem_ConditionsReadFromDatabase()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();

        // Create two characters with different Martial stats
        db = db.Insert("coward", new Character("Coward Carl", 20, 2, 5, 5, 5, DateTime.Now)); // Low martial
        db = db.Insert("coward", new AIAgentData("Warrior", "Idle"));

        db = db.Insert("brave", new Character("Brave Betty", 20, 15, 5, 5, 5, DateTime.Now)); // High martial
        db = db.Insert("brave", new AIAgentData("Warrior", "Idle"));

        var aiSystem = new AISystem();
        db = aiSystem.Initialize(db);

        var actionsTaken = new Dictionary<string, string>();

        // Warrior domain - behavior depends on Martial stat
        var warriorDomain = new DomainBuilder<SimulationContext>("Warrior")
            .Select("Combat Decision")
                // High martial (>= 10) → Fight
                .Action("Fight Bravely")
                    .Condition("Is Brave", ctx =>
                    {
                        var character = ctx.Db.GetEntry<Character>(ctx.Self.Id);
                        return character.Martial >= 10;
                    })
                    .Do(ctx =>
                    {
                        actionsTaken[ctx.Self.Id] = "Fight";
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
                // Low martial → Flee
                .Action("Flee")
                    .Condition("Is Coward", ctx =>
                    {
                        var character = ctx.Db.GetEntry<Character>(ctx.Self.Id);
                        return character.Martial < 10;
                    })
                    .Do(ctx =>
                    {
                        actionsTaken[ctx.Self.Id] = "Flee";
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Warrior", warriorDomain);

        // Run
        aiSystem.Run(db);

        // Assert: Each character acted according to their stats
        Assert.Equal("Flee", actionsTaken["coward"]);
        Assert.Equal("Fight", actionsTaken["brave"]);
    }

    /// <summary>
    /// Tests a trader AI that follows a trade route: 
    /// Load cargo at origin → Travel to destination → Sell cargo
    /// Uses database state instead of WorldState flags for persistence across ticks.
    /// </summary>
    [Fact]
    public void AISystem_TraderAI_CompleteTradeRoute()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<Ship>();
        db = db.RegisterTable<TradeGood>();
        db = db.RegisterTable<GoldItem>();

        var traderId = "trader_1";
        db = db.Insert(traderId, new Character("Trader Tom", 35, 3, 12, 8, 6, DateTime.Now));
        db = db.Insert(traderId, new AIAgentData("Trader", "Idle"));
        db = db.Insert(traderId, new Ship("The Merchant", "Earth", "Mars", 100, 0));

        var aiSystem = new AISystem();
        db = aiSystem.Initialize(db);

        var actionsLog = new List<string>();

        // Trader Domain - Uses DB state for conditions (persists across ticks)
        var traderDomain = new DomainBuilder<SimulationContext>("Trader")
            .Select("Trade Operations")
                // Priority 1: Sell cargo if at destination with cargo
                .Action("Sell Cargo")
                    .Condition("Has Cargo", ctx =>
                    {
                        var ship = ctx.Db.GetEntry<Ship>(ctx.Self.Id);
                        return ship.CurrentCargo > 0;
                    })
                    .Condition("At Destination", ctx =>
                    {
                        var ship = ctx.Db.GetEntry<Ship>(ctx.Self.Id);
                        return ship.CurrentLocation == ship.Destination || ship.Destination == null && ship.CurrentLocation == "Mars";
                    })
                    .Do(ctx =>
                    {
                        actionsLog.Add("SellCargo");
                        ctx.ApplyChange(d =>
                        {
                            var ship = d.GetEntry<Ship>(ctx.Self.Id);
                            d = d.Insert(ctx.Self.Id, ship with { CurrentCargo = 0, Destination = null });
                            d = d.Insert(ctx.Self.Id + "_gold", new GoldItem(500));
                            return d;
                        });
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()

                // Priority 2: Travel to destination if has cargo but not there yet
                .Action("Travel To Destination")
                    .Condition("Has Cargo", ctx =>
                    {
                        var ship = ctx.Db.GetEntry<Ship>(ctx.Self.Id);
                        return ship.CurrentCargo > 0;
                    })
                    .Condition("Not At Destination", ctx =>
                    {
                        var ship = ctx.Db.GetEntry<Ship>(ctx.Self.Id);
                        return ship.CurrentLocation != ship.Destination;
                    })
                    .Do(ctx =>
                    {
                        actionsLog.Add("TravelToDestination");
                        ctx.ApplyChange(d =>
                        {
                            var ship = d.GetEntry<Ship>(ctx.Self.Id);
                            return d.Insert(ctx.Self.Id, ship with { CurrentLocation = ship.Destination ?? "Mars" });
                        });
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()

                // Priority 3: Load cargo if at origin without cargo
                .Action("Load Cargo")
                    .Condition("No Cargo", ctx =>
                    {
                        var ship = ctx.Db.GetEntry<Ship>(ctx.Self.Id);
                        return ship.CurrentCargo == 0;
                    })
                    .Do(ctx =>
                    {
                        actionsLog.Add("LoadCargo");
                        ctx.ApplyChange(d =>
                        {
                            var ship = d.GetEntry<Ship>(ctx.Self.Id);
                            return d.Insert(ctx.Self.Id, ship with { CurrentCargo = 50 });
                        });
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Trader", traderDomain);

        // Tick 1: Should load cargo
        var nextDb = aiSystem.Run(db);
        Assert.Contains("LoadCargo", actionsLog);
        var ship = nextDb.GetEntry<Ship>(traderId);
        Assert.Equal(50, ship.CurrentCargo);

        // Tick 2: Should travel (has cargo, not at destination)
        actionsLog.Clear();
        nextDb = aiSystem.Run(nextDb);
        Assert.Contains("TravelToDestination", actionsLog);
        ship = nextDb.GetEntry<Ship>(traderId);
        Assert.Equal("Mars", ship.CurrentLocation);

        // Tick 3: Should sell cargo (at destination with cargo)
        actionsLog.Clear();
        nextDb = aiSystem.Run(nextDb);
        Assert.Contains("SellCargo", actionsLog);
        ship = nextDb.GetEntry<Ship>(traderId);
        Assert.Equal(0, ship.CurrentCargo);
        var gold = nextDb.GetTable<GoldItem>();
        Assert.True(gold.ContainsKey(traderId + "_gold"));
    }

    /// <summary>
    /// Tests that an agent with no matching domain is gracefully skipped.
    /// </summary>
    [Fact]
    public void AISystem_UnknownDomain_AgentSkipped()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();

        db = db.Insert("agent_1", new Character("Mystery Man", 30, 5, 5, 5, 5, DateTime.Now));
        db = db.Insert("agent_1", new AIAgentData("NonExistentDomain", "Idle"));

        var aiSystem = new AISystem();
        db = aiSystem.Initialize(db);

        // Should not throw, just skip the agent
        var exception = Record.Exception(() => aiSystem.Run(db));
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that actions can return Continue to indicate ongoing work.
    /// The same action will be called again on the next tick until it succeeds.
    /// </summary>
    [Fact]
    public void AISystem_ActionReturnsContinue_PlannerResumes()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();

        db = db.Insert("builder", new Character("Builder Bill", 40, 5, 10, 3, 7, DateTime.Now));
        db = db.Insert("builder", new AIAgentData("Builder", "Idle"));

        var aiSystem = new AISystem();
        db = aiSystem.Initialize(db);

        var buildProgress = 0;
        var buildComplete = false;

        // Builder domain - building takes 3 ticks
        var builderDomain = new DomainBuilder<SimulationContext>("Builder")
            .Select("Build")
                .Action("Build Structure")
                    .Do(ctx =>
                    {
                        buildProgress++;
                        if (buildProgress >= 3)
                        {
                            buildComplete = true;
                            return FluidHTN.TaskStatus.Success;
                        }
                        return FluidHTN.TaskStatus.Continue; // Still building
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Builder", builderDomain);

        // Tick 1 - Start building
        db = aiSystem.Run(db);
        Assert.Equal(1, buildProgress);
        Assert.False(buildComplete);

        // Tick 2 - Continue building
        db = aiSystem.Run(db);
        Assert.Equal(2, buildProgress);
        Assert.False(buildComplete);

        // Tick 3 - Complete building
        db = aiSystem.Run(db);
        Assert.True(buildProgress >= 3);
        Assert.True(buildComplete);
    }
}
