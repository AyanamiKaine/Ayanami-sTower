using System;
using FluidHTN;
using InvictaDB;
using StellaInvicta.AI;
using StellaInvicta.Data;
using StellaInvicta.System.Date;
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
        const string charId = "char_1";
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

        const string guardId = "guard_1";
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

        const string traderId = "trader_1";
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

    // ========================================================================
    // CHARACTER INTERACTION TESTS
    // ========================================================================

    // Additional test records for character interactions
    public record Health(int Current, int Max);
    public record CombatLog(string AttackerId, string DefenderId, int Damage, DateTime Timestamp);
    public record Gift(string FromId, string ToId, string ItemName, int Value);
    public record Alliance(string MemberId1, string MemberId2, string AllianceName);

    /// <summary>
    /// Tests combat between two characters. Attacker damages defender based on stats.
    /// </summary>
    [Fact]
    public void AISystem_Combat_AttackerDamagesDefender()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<Health>();
        db = db.RegisterTable<CombatLog>();

        // Create attacker (high martial) and defender
        const string attackerId = "attacker";
        const string defenderId = "defender";

        db = db.Insert(attackerId, new Character("Warrior", 25, 15, 5, 5, 5, DateTime.Now));
        db = db.Insert(attackerId, new AIAgentData("Attacker", "Idle"));
        db = db.Insert(attackerId, new Health(100, 100));

        db = db.Insert(defenderId, new Character("Peasant", 30, 3, 8, 2, 4, DateTime.Now));
        db = db.Insert(defenderId, new Health(50, 50));

        var aiSystem = new AISystem();
        db = aiSystem.Initialize(db);

        // Attacker domain - finds a target and attacks
        var attackerDomain = new DomainBuilder<SimulationContext>("Attacker")
            .Select("Combat")
                .Action("Attack Target")
                    .Condition("Target Exists", ctx =>
                    {
                        // Find any other character to attack
                        var characters = ctx.Db.GetTable<Character>();
                        return characters.Any(c => c.Key != ctx.Self.Id);
                    })
                    .Do(ctx =>
                    {
                        // Find a target (first character that isn't self)
                        var targetId = ctx.Db.GetTable<Character>()
                            .First(c => c.Key != ctx.Self.Id).Key;

                        // Calculate damage based on attacker's martial
                        var attacker = ctx.Db.GetEntry<Character>(ctx.Self.Id);
                        var damage = attacker.Martial * 2;

                        ctx.ApplyChange(d =>
                        {
                            // Reduce target's health
                            var targetHealth = d.GetEntry<Health>(targetId);
                            d = d.Insert(targetId, targetHealth with
                            {
                                Current = Math.Max(0, targetHealth.Current - damage)
                            });

                            // Log the combat
                            var logId = $"combat_{DateTime.Now.Ticks}";
                            d = d.Insert(logId, new CombatLog(ctx.Self.Id, targetId, damage, DateTime.Now));

                            return d;
                        });

                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Attacker", attackerDomain);

        // Run combat
        var nextDb = aiSystem.Run(db);

        // Assert: Defender took damage (15 martial * 2 = 30 damage)
        var defenderHealth = nextDb.GetEntry<Health>(defenderId);
        Assert.True(defenderHealth.Current < 50); // Started at 50, took damage

        // Assert: Combat was logged
        var combatLogs = nextDb.GetTable<CombatLog>();
        Assert.NotEmpty(combatLogs);
        var log = combatLogs.Values.First();
        Assert.Equal(attackerId, log.AttackerId);
        Assert.Equal(defenderId, log.DefenderId);
    }

    /// <summary>
    /// Tests a merchant giving gifts to improve relationships.
    /// </summary>
    [Fact]
    public void AISystem_Diplomacy_GiftGivingImprovesRelationship()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<GoldItem>();
        db = db.RegisterTable<Gift>();
        db = db.RegisterTable<Relationship>();

        const string merchantId = "merchant";
        const string nobleId = "noble";

        // Merchant has gold and wants to befriend the noble
        db = db.Insert(merchantId, new Character("Rich Merchant", 40, 3, 15, 10, 8, DateTime.Now));
        db = db.Insert(merchantId, new AIAgentData("Diplomat", "Idle"));
        db = db.Insert(merchantId + "_gold", new GoldItem(1000));

        db = db.Insert(nobleId, new Character("Duke", 45, 10, 12, 8, 6, DateTime.Now));

        // Initial relationship is neutral (0)
        var relationshipKey = $"Character:{merchantId}→Friendship→Character:{nobleId}";
        db = db.Insert(relationshipKey, RelationshipExtensions.CharacterRelationship(
            merchantId, nobleId, "Friendship", 0));

        var aiSystem = new AISystem();
        db = aiSystem.Initialize(db);

        // Diplomat domain - give gifts to improve relationships
        var diplomatDomain = new DomainBuilder<SimulationContext>("Diplomat")
            .Select("Diplomacy")
                .Action("Give Gift")
                    .Condition("Has Gold", ctx =>
                    {
                        var goldTable = ctx.Db.GetTable<GoldItem>();
                        return goldTable.TryGetValue(ctx.Self.Id + "_gold", out var gold) && gold.Amount >= 100;
                    })
                    .Condition("Has Target To Befriend", ctx =>
                    {
                        // Find someone with relationship < 50
                        var relationships = ctx.Db.GetTable<Relationship>();
                        return relationships.Values.Any(r =>
                            r.SourceId == ctx.Self.Id &&
                            r.RelationshipTypeId == "Friendship" &&
                            r.Strength < 50);
                    })
                    .Do(ctx =>
                    {
                        // Find the target with lowest relationship
                        var relationships = ctx.Db.GetTable<Relationship>();
                        var targetRelation = relationships.Values
                            .Where(r => r.SourceId == ctx.Self.Id && r.RelationshipTypeId == "Friendship")
                            .OrderBy(r => r.Strength)
                            .First();

                        ctx.ApplyChange(d =>
                        {
                            // Spend gold
                            var gold = d.GetEntry<GoldItem>(ctx.Self.Id + "_gold");
                            d = d.Insert(ctx.Self.Id + "_gold", gold with { Amount = gold.Amount - 100 });

                            // Record the gift
                            var giftId = $"gift_{DateTime.Now.Ticks}";
                            d = d.Insert(giftId, new Gift(ctx.Self.Id, targetRelation.TargetId, "Fine Wine", 100));

                            // Improve relationship
                            d = d.Insert(targetRelation.CompositeKey, targetRelation with
                            {
                                Strength = Math.Min(100, targetRelation.Strength + 25)
                            });

                            return d;
                        });

                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Diplomat", diplomatDomain);

        // Run diplomacy
        var nextDb = aiSystem.Run(db);

        // Assert: Gold was spent (at least 100 per gift)
        var merchantGold = nextDb.GetEntry<GoldItem>(merchantId + "_gold");
        Assert.True(merchantGold.Amount < 1000); // Started with 1000

        // Assert: Gift was recorded
        var gifts = nextDb.GetTable<Gift>();
        Assert.NotEmpty(gifts);
        Assert.Equal(merchantId, gifts.Values.First().FromId);
        Assert.Equal(nobleId, gifts.Values.First().ToId);

        // Assert: Relationship improved (started at 0)
        var relationship = nextDb.GetEntry<Relationship>(relationshipKey);
        Assert.True(relationship.Strength > 0);
    }

    /// <summary>
    /// Tests two characters forming an alliance when conditions are right.
    /// </summary>
    [Fact]
    public void AISystem_Alliance_CharactersFormAllianceWhenFriendly()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<Relationship>();
        db = db.RegisterTable<Alliance>();

        const string lordA = "lord_a";
        const string lordB = "lord_b";

        db = db.Insert(lordA, new Character("Lord Alpha", 35, 12, 10, 8, 7, DateTime.Now));
        db = db.Insert(lordA, new AIAgentData("AllianceSeeker", "Idle"));

        db = db.Insert(lordB, new Character("Lord Beta", 38, 11, 9, 10, 6, DateTime.Now));

        // They already have a good relationship
        var relationshipKey = $"Character:{lordA}→Friendship→Character:{lordB}";
        db = db.Insert(relationshipKey, RelationshipExtensions.CharacterRelationship(
            lordA, lordB, "Friendship", 75));

        var aiSystem = new AISystem();
        db = aiSystem.Initialize(db);

        // Alliance seeker domain
        var allianceDomain = new DomainBuilder<SimulationContext>("AllianceSeeker")
            .Select("Seek Alliance")
                .Action("Propose Alliance")
                    .Condition("Has Friendly Character", ctx =>
                    {
                        var relationships = ctx.Db.GetTable<Relationship>();
                        return relationships.Values.Any(r =>
                            r.SourceId == ctx.Self.Id &&
                            r.RelationshipTypeId == "Friendship" &&
                            r.Strength >= 50);
                    })
                    .Condition("Not Already Allied", ctx =>
                    {
                        var alliances = ctx.Db.GetTable<Alliance>();
                        return !alliances.Values.Any(a =>
                            a.MemberId1 == ctx.Self.Id || a.MemberId2 == ctx.Self.Id);
                    })
                    .Do(ctx =>
                    {
                        // Find the friendliest character
                        var relationships = ctx.Db.GetTable<Relationship>();
                        var bestFriend = relationships.Values
                            .Where(r => r.SourceId == ctx.Self.Id && r.RelationshipTypeId == "Friendship")
                            .OrderByDescending(r => r.Strength)
                            .First();

                        ctx.ApplyChange(d =>
                        {
                            var allianceId = $"alliance_{ctx.Self.Id}_{bestFriend.TargetId}";
                            d = d.Insert(allianceId, new Alliance(
                                ctx.Self.Id,
                                bestFriend.TargetId,
                                "Defensive Pact"
                            ));
                            return d;
                        });

                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("AllianceSeeker", allianceDomain);

        // Run
        var nextDb = aiSystem.Run(db);

        // Assert: Alliance was formed
        var alliances = nextDb.GetTable<Alliance>();
        Assert.Single(alliances);
        var alliance = alliances.Values.First();
        Assert.Equal(lordA, alliance.MemberId1);
        Assert.Equal(lordB, alliance.MemberId2);
        Assert.Equal("Defensive Pact", alliance.AllianceName);
    }

    /// <summary>
    /// Tests characters trading resources with each other.
    /// </summary>
    [Fact]
    public void AISystem_Trade_CharactersExchangeResources()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<GoldItem>();
        db = db.RegisterTable<TradeGood>();

        const string buyerId = "buyer";
        const string sellerId = "seller";

        // Buyer has gold, wants goods
        db = db.Insert(buyerId, new Character("Merchant", 30, 3, 14, 8, 6, DateTime.Now));
        db = db.Insert(buyerId, new AIAgentData("Buyer", "Idle"));
        db = db.Insert(buyerId + "_gold", new GoldItem(500));

        // Seller has goods
        db = db.Insert(sellerId, new Character("Farmer", 45, 5, 8, 3, 4, DateTime.Now));
        db = db.Insert(sellerId + "_goods", new TradeGood("Wheat", 100, 2));
        db = db.Insert(sellerId + "_gold", new GoldItem(50));

        var aiSystem = new AISystem();
        db = aiSystem.Initialize(db);

        // Buyer domain - finds sellers and buys goods
        var buyerDomain = new DomainBuilder<SimulationContext>("Buyer")
            .Select("Trading")
                .Action("Buy Goods")
                    .Condition("Has Gold", ctx =>
                    {
                        var goldTable = ctx.Db.GetTable<GoldItem>();
                        return goldTable.TryGetValue(ctx.Self.Id + "_gold", out var gold) && gold.Amount >= 100;
                    })
                    .Condition("Seller Has Goods", ctx =>
                    {
                        var goods = ctx.Db.GetTable<TradeGood>();
                        return goods.Any(g => g.Key != ctx.Self.Id + "_goods" && g.Value.Quantity > 0);
                    })
                    .Do(ctx =>
                    {
                        // Find a seller with goods
                        var goods = ctx.Db.GetTable<TradeGood>();
                        var sellerGoods = goods.First(g => g.Key != ctx.Self.Id + "_goods" && g.Value.Quantity > 0);
                        var sellerKey = sellerGoods.Key.Replace("_goods", "");

                        const int quantityToBuy = 20;
                        var totalCost = quantityToBuy * sellerGoods.Value.Value;

                        ctx.ApplyChange(d =>
                        {
                            // Buyer pays gold
                            var buyerGold = d.GetEntry<GoldItem>(ctx.Self.Id + "_gold");
                            d = d.Insert(ctx.Self.Id + "_gold", buyerGold with { Amount = buyerGold.Amount - totalCost });

                            // Seller receives gold
                            var sellerGold = d.GetEntry<GoldItem>(sellerKey + "_gold");
                            d = d.Insert(sellerKey + "_gold", sellerGold with { Amount = sellerGold.Amount + totalCost });

                            // Seller loses goods
                            var goods = d.GetEntry<TradeGood>(sellerGoods.Key);
                            d = d.Insert(sellerGoods.Key, goods with { Quantity = goods.Quantity - quantityToBuy });

                            // Buyer gains goods
                            var buyerGoodsKey = ctx.Self.Id + "_goods";
                            if (d.GetTable<TradeGood>().TryGetValue(buyerGoodsKey, out var existing))
                            {
                                d = d.Insert(buyerGoodsKey, existing with { Quantity = existing.Quantity + quantityToBuy });
                            }
                            else
                            {
                                d = d.Insert(buyerGoodsKey, new TradeGood(goods.Name, quantityToBuy, goods.Value));
                            }

                            return d;
                        });

                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Buyer", buyerDomain);

        // Run trade
        var nextDb = aiSystem.Run(db);

        // Assert: Buyer spent gold and got goods
        var buyerGold = nextDb.GetEntry<GoldItem>(buyerId + "_gold");
        Assert.True(buyerGold.Amount < 500); // Started with 500, spent some

        var buyerGoods = nextDb.GetEntry<TradeGood>(buyerId + "_goods");
        Assert.True(buyerGoods.Quantity > 0); // Got some goods
        Assert.Equal("Wheat", buyerGoods.Name);

        // Assert: Seller gained gold and lost goods
        var sellerGold = nextDb.GetEntry<GoldItem>(sellerId + "_gold");
        Assert.True(sellerGold.Amount > 50); // Started with 50, gained some

        var sellerGoods = nextDb.GetEntry<TradeGood>(sellerId + "_goods");
        Assert.True(sellerGoods.Quantity < 100); // Started with 100, sold some
    }

    /// <summary>
    /// Tests a revenge scenario: Character A attacks B, then B retaliates.
    /// </summary>
    [Fact]
    public void AISystem_Revenge_DefenderRetaliatesAfterAttack()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<Health>();
        db = db.RegisterTable<CombatLog>();

        const string aggressorId = "aggressor";
        const string defenderId = "defender";

        db = db.Insert(aggressorId, new Character("Bully", 25, 10, 5, 5, 5, DateTime.Now));
        db = db.Insert(aggressorId, new AIAgentData("Aggressor", "Idle"));
        db = db.Insert(aggressorId, new Health(80, 80));

        db = db.Insert(defenderId, new Character("Victim", 28, 12, 5, 5, 5, DateTime.Now));
        db = db.Insert(defenderId, new AIAgentData("Retaliator", "Idle"));
        db = db.Insert(defenderId, new Health(100, 100));

        var aiSystem = new AISystem();
        db = aiSystem.Initialize(db);

        // Aggressor attacks first
        var aggressorDomain = new DomainBuilder<SimulationContext>("Aggressor")
            .Select("Attack")
                .Action("Strike First")
                    .Condition("No Combat Log Yet", ctx => !ctx.Db.GetTable<CombatLog>().Any())
                    .Do(ctx =>
                    {
                        var damage = ctx.Db.GetEntry<Character>(ctx.Self.Id).Martial * 2;
                        ctx.ApplyChange(d =>
                        {
                            var targetHealth = d.GetEntry<Health>(defenderId);
                            d = d.Insert(defenderId, targetHealth with { Current = targetHealth.Current - damage });
                            d = d.Insert($"log_{ctx.Self.Id}", new CombatLog(ctx.Self.Id, defenderId, damage, DateTime.Now));
                            return d;
                        });
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        // Retaliator strikes back if attacked
        var retaliatorDomain = new DomainBuilder<SimulationContext>("Retaliator")
            .Select("Retaliate")
                .Action("Strike Back")
                    .Condition("Was Attacked", ctx =>
                    {
                        var logs = ctx.Db.GetTable<CombatLog>();
                        return logs.Values.Any(l => l.DefenderId == ctx.Self.Id);
                    })
                    .Do(ctx =>
                    {
                        // Find who attacked us
                        var logs = ctx.Db.GetTable<CombatLog>();
                        var attacker = logs.Values.First(l => l.DefenderId == ctx.Self.Id).AttackerId;

                        var damage = ctx.Db.GetEntry<Character>(ctx.Self.Id).Martial * 2;
                        ctx.ApplyChange(d =>
                        {
                            var attackerHealth = d.GetEntry<Health>(attacker);
                            d = d.Insert(attacker, attackerHealth with { Current = attackerHealth.Current - damage });
                            d = d.Insert($"log_{ctx.Self.Id}", new CombatLog(ctx.Self.Id, attacker, damage, DateTime.Now));
                            return d;
                        });
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Aggressor", aggressorDomain);
        aiSystem.RegisterDomain("Retaliator", retaliatorDomain);

        // Tick 1: Aggressor attacks (condition: no combat log yet)
        var nextDb = aiSystem.Run(db);

        var defenderHealth = nextDb.GetEntry<Health>(defenderId);
        Assert.True(defenderHealth.Current < 100); // Started at 100, took damage

        // Assert combat log was created
        var combatLogs = nextDb.GetTable<CombatLog>();
        Assert.NotEmpty(combatLogs);

        // Tick 2: Defender retaliates (sees they were attacked)
        nextDb = aiSystem.Run(nextDb);

        var aggressorHealth = nextDb.GetEntry<Health>(aggressorId);
        Assert.True(aggressorHealth.Current < 80); // Started at 80, took retaliation damage

        // Both combat logs exist
        combatLogs = nextDb.GetTable<CombatLog>();
        Assert.True(combatLogs.Count >= 2); // At least aggressor + defender logs
    }

    /// <summary>
    /// Tests characters hiring each other for services.
    /// </summary>
    [Fact]
    public void AISystem_Hiring_CharacterHiresAnother()
    {
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<GoldItem>();
        db = db.RegisterTable<Relationship>();

        const string employerId = "employer";
        const string mercenaryId = "mercenary";

        db = db.Insert(employerId, new Character("Noble", 40, 5, 15, 10, 8, DateTime.Now));
        db = db.Insert(employerId, new AIAgentData("Employer", "Idle"));
        db = db.Insert(employerId + "_gold", new GoldItem(1000));

        db = db.Insert(mercenaryId, new Character("Sellsword", 28, 16, 4, 6, 3, DateTime.Now));
        db = db.Insert(mercenaryId + "_gold", new GoldItem(10));

        var aiSystem = new AISystem();
        db = aiSystem.Initialize(db);

        // Employer domain - hires high martial characters
        var employerDomain = new DomainBuilder<SimulationContext>("Employer")
            .Select("Hire")
                .Action("Hire Mercenary")
                    .Condition("Has Gold", ctx =>
                        ctx.Db.GetTable<GoldItem>().TryGetValue(ctx.Self.Id + "_gold", out var g) && g.Amount >= 200)
                    .Condition("Skilled Fighter Available", ctx =>
                    {
                        var characters = ctx.Db.GetTable<Character>();
                        return characters.Any(c => c.Key != ctx.Self.Id && c.Value.Martial >= 15);
                    })
                    .Condition("Not Already Employed", ctx =>
                    {
                        var relationships = ctx.Db.GetTable<Relationship>();
                        return !relationships.Values.Any(r =>
                            r.SourceId == ctx.Self.Id && r.RelationshipTypeId == "Employer");
                    })
                    .Do(ctx =>
                    {
                        var target = ctx.Db.GetTable<Character>()
                            .First(c => c.Key != ctx.Self.Id && c.Value.Martial >= 15);

                        ctx.ApplyChange(d =>
                        {
                            // Pay the mercenary
                            var employerGold = d.GetEntry<GoldItem>(ctx.Self.Id + "_gold");
                            d = d.Insert(ctx.Self.Id + "_gold", employerGold with { Amount = employerGold.Amount - 200 });

                            var mercGold = d.GetEntry<GoldItem>(target.Key + "_gold");
                            d = d.Insert(target.Key + "_gold", mercGold with { Amount = mercGold.Amount + 200 });

                            // Create employment relationship
                            var rel = RelationshipExtensions.CharacterRelationship(
                                ctx.Self.Id, target.Key, "Employer", 50);
                            d = d.Insert(rel.CompositeKey, rel);

                            return d;
                        });

                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Employer", employerDomain);

        // Run
        var nextDb = aiSystem.Run(db);

        // Assert: Gold transferred (employer paid, mercenary got paid)
        Assert.True(nextDb.GetEntry<GoldItem>(employerId + "_gold").Amount < 1000); // Started with 1000
        Assert.True(nextDb.GetEntry<GoldItem>(mercenaryId + "_gold").Amount > 10); // Started with 10

        // Assert: Relationship created
        var relationships = nextDb.GetTable<Relationship>();
        Assert.NotEmpty(relationships);
        var rel = relationships.Values.First(r => r.RelationshipTypeId == "Employer");
        Assert.Equal(employerId, rel.SourceId);
        Assert.Equal(mercenaryId, rel.TargetId);
        Assert.Equal("Employer", rel.RelationshipTypeId);
    }

    // ========================================================================
    // GAME INTEGRATION TESTS
    // ========================================================================

    /// <summary>
    /// Tests that the AI system integrates with the Game class and runs during SimulateMonth.
    /// </summary>
    [Fact]
    public void AISystem_IntegratesWithGame_RunsDuringSimulation()
    {
        // 1. Setup Game with AI System
        var game = new Game();
        var dateSystem = new DateSystem();
        var aiSystem = new AISystem();

        game.AddSystem(dateSystem.Name, dateSystem);
        game.AddSystem(aiSystem.Name, aiSystem);

        // 2. Setup Database
        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<GoldItem>();

        // Create an AI-controlled worker
        var workerId = "worker_1";
        db = db.Insert(workerId, new Character("AI Worker", 25, 5, 10, 5, 5, new DateTime(2175, 6, 15)));
        db = db.Insert(workerId, new AIAgentData("Worker", "Idle"));

        // 3. Register domain with AI system (before Init is fine)
        var workerDomain = new DomainBuilder<SimulationContext>("Worker")
            .Select("Work")
                .Action("Earn Gold")
                    .Condition("Not Rich Yet", ctx =>
                    {
                        var goldTable = ctx.Db.GetTable<GoldItem>();
                        if (goldTable.TryGetValue(ctx.Self.Id + "_gold", out var gold))
                            return gold.Amount < 1000;
                        return true;
                    })
                    .Do(ctx =>
                    {
                        ctx.ApplyChange(d =>
                        {
                            var goldKey = ctx.Self.Id + "_gold";
                            var goldTable = d.GetTable<GoldItem>();
                            if (goldTable.TryGetValue(goldKey, out var existing))
                            {
                                return d.Insert(goldKey, existing with { Amount = existing.Amount + 10 });
                            }
                            return d.Insert(goldKey, new GoldItem(10));
                        });
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Worker", workerDomain);

        // 4. Initialize Game (this initializes all systems)
        db = game.Init(db);

        // 5. Simulate a month - AI should run every hour (24 * ~30 days = ~720 ticks)
        var nextDb = game.SimulateMonth(db);

        // 6. Assert: Worker accumulated gold over the month
        var goldTable = nextDb.GetTable<GoldItem>();
        Assert.True(goldTable.ContainsKey(workerId + "_gold"));
        var gold = goldTable[workerId + "_gold"];
        Assert.True(gold.Amount > 100); // Should have earned significant gold over a month
    }

    /// <summary>
    /// Tests AI agents interacting with the date system - behavior changes based on time.
    /// </summary>
    [Fact]
    public void AISystem_ReactsToGameDate_BehaviorChangesOverTime()
    {
        var game = new Game();
        var dateSystem = new DateSystem();
        var aiSystem = new AISystem();

        game.AddSystem(dateSystem.Name, dateSystem);
        game.AddSystem(aiSystem.Name, aiSystem);

        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<GoldItem>();

        var farmerId = "farmer_1";
        db = db.Insert(farmerId, new Character("Farmer", 35, 5, 12, 3, 6, new DateTime(2165, 3, 10)));
        db = db.Insert(farmerId, new AIAgentData("SeasonalWorker", "Idle"));

        // Register domain BEFORE init - the domain definition doesn't need the db
        var harvestCount = 0;
        var restCount = 0;

        // Seasonal worker - works harder in summer, rests in winter
        var seasonalDomain = new DomainBuilder<SimulationContext>("SeasonalWorker")
            .Select("Seasonal Work")
                .Action("Harvest in Summer")
                    .Condition("Is Summer", ctx =>
                    {
                        var date = ctx.Db.GetSingleton<DateTime>();
                        return date.Month >= 6 && date.Month <= 8; // June-August
                    })
                    .Do(ctx =>
                    {
                        harvestCount++;
                        ctx.ApplyChange(d =>
                        {
                            var goldKey = ctx.Self.Id + "_gold";
                            var goldTable = d.GetTable<GoldItem>();
                            if (goldTable.TryGetValue(goldKey, out var existing))
                                return d.Insert(goldKey, existing with { Amount = existing.Amount + 20 });
                            return d.Insert(goldKey, new GoldItem(20));
                        });
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
                .Action("Rest in Winter")
                    .Condition("Is Winter", ctx =>
                    {
                        var date = ctx.Db.GetSingleton<DateTime>();
                        return date.Month == 12 || date.Month <= 2; // Dec-Feb
                    })
                    .Do(ctx =>
                    {
                        restCount++;
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
                .Action("Idle")
                    .Do(_ => FluidHTN.TaskStatus.Success)
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("SeasonalWorker", seasonalDomain);

        // Initialize game (runs Initialize on all systems)
        db = game.Init(db);

        // Set start date AFTER init (DateSystem.Initialize overwrites singleton)
        db = db.InsertSingleton(new DateTime(2200, 7, 1));

        // Simulate July (summer) - should harvest
        db = game.SimulateMonth(db);

        // Farmer should have harvested in summer
        Assert.True(harvestCount > 0, $"Expected harvest in summer, got harvestCount={harvestCount}");

        // Get gold earned in summer
        var summerGold = db.GetTable<GoldItem>().TryGetValue(farmerId + "_gold", out var g) ? g.Amount : 0;
        Assert.True(summerGold > 0, $"Expected gold earned in summer, got {summerGold}");
    }

    /// <summary>
    /// Tests multiple AI agents in a full game simulation scenario.
    /// </summary>
    [Fact]
    public void AISystem_MultipleAgents_FullGameSimulation()
    {
        var game = new Game();
        var dateSystem = new DateSystem();
        var aiSystem = new AISystem();

        game.AddSystem(dateSystem.Name, dateSystem);
        game.AddSystem(aiSystem.Name, aiSystem);

        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<GoldItem>();
        db = db.RegisterTable<TradeGood>();

        // Create a small economy: miners, traders, and merchants
        db = db.Insert("miner_1", new Character("Miner Mike", 30, 8, 6, 3, 4, new DateTime(2170, 4, 20)));
        db = db.Insert("miner_1", new AIAgentData("Miner", "Idle"));
        db = db.Insert("miner_1_gold", new GoldItem(0));

        db = db.Insert("trader_1", new Character("Trader Tina", 28, 4, 14, 10, 7, new DateTime(2172, 9, 5)));
        db = db.Insert("trader_1", new AIAgentData("Trader", "Idle"));
        db = db.Insert("trader_1_gold", new GoldItem(100));

        // Miner domain - produces ore
        var minerDomain = new DomainBuilder<SimulationContext>("Miner")
            .Select("Mining")
                .Action("Mine Ore")
                    .Do(ctx =>
                    {
                        ctx.ApplyChange(d =>
                        {
                            var oreKey = ctx.Self.Id + "_ore";
                            var oreTable = d.GetTable<TradeGood>();
                            if (oreTable.TryGetValue(oreKey, out var existing))
                                return d.Insert(oreKey, existing with { Quantity = existing.Quantity + 5 });
                            return d.Insert(oreKey, new TradeGood("Iron Ore", 5, 10));
                        });
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        // Trader domain - buys ore from miners
        var traderDomain = new DomainBuilder<SimulationContext>("Trader")
            .Select("Trading")
                .Action("Buy Ore")
                    .Condition("Has Gold", ctx =>
                    {
                        var goldTable = ctx.Db.GetTable<GoldItem>();
                        return goldTable.TryGetValue(ctx.Self.Id + "_gold", out var g) && g.Amount >= 50;
                    })
                    .Condition("Ore Available", ctx =>
                    {
                        var oreTable = ctx.Db.GetTable<TradeGood>();
                        return oreTable.Any(o => o.Key.EndsWith("_ore") && o.Key != ctx.Self.Id + "_ore" && o.Value.Quantity >= 10);
                    })
                    .Do(ctx =>
                    {
                        var oreTable = ctx.Db.GetTable<TradeGood>();
                        var sellerOre = oreTable.First(o => o.Key.EndsWith("_ore") && o.Key != ctx.Self.Id + "_ore" && o.Value.Quantity >= 10);
                        var sellerId = sellerOre.Key.Replace("_ore", "");

                        ctx.ApplyChange(d =>
                        {
                            // Pay for ore
                            var traderGold = d.GetEntry<GoldItem>(ctx.Self.Id + "_gold");
                            d = d.Insert(ctx.Self.Id + "_gold", traderGold with { Amount = traderGold.Amount - 50 });

                            // Seller gets paid
                            var sellerGoldKey = sellerId + "_gold";
                            var goldTable = d.GetTable<GoldItem>();
                            if (goldTable.TryGetValue(sellerGoldKey, out var sellerGold))
                                d = d.Insert(sellerGoldKey, sellerGold with { Amount = sellerGold.Amount + 50 });
                            else
                                d = d.Insert(sellerGoldKey, new GoldItem(50));

                            // Transfer ore
                            var ore = d.GetEntry<TradeGood>(sellerOre.Key);
                            d = d.Insert(sellerOre.Key, ore with { Quantity = ore.Quantity - 10 });

                            var traderOreKey = ctx.Self.Id + "_ore";
                            var traderOreTable = d.GetTable<TradeGood>();
                            if (traderOreTable.TryGetValue(traderOreKey, out var traderOre))
                                return d.Insert(traderOreKey, traderOre with { Quantity = traderOre.Quantity + 10 });
                            else
                                return d.Insert(traderOreKey, new TradeGood("Iron Ore", 10, 10));
                        });
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Miner", minerDomain);
        aiSystem.RegisterDomain("Trader", traderDomain);

        // Initialize game after domain registration
        db = game.Init(db);

        // Simulate a full month
        var nextDb = game.SimulateMonth(db);

        // Assert: Miner produced ore and earned gold
        var minerGold = nextDb.GetEntry<GoldItem>("miner_1_gold");
        Assert.True(minerGold.Amount > 0); // Miner got paid by trader

        // Assert: Trade happened - trader has some ore
        var traderOreTable = nextDb.GetTable<TradeGood>();
        Assert.True(traderOreTable.ContainsKey("trader_1_ore"));
    }

    /// <summary>
    /// Tests enabling/disabling the AI system via the game.
    /// </summary>
    [Fact]
    public void AISystem_CanBeEnabledDisabled_ViaGame()
    {
        var game = new Game();
        var dateSystem = new DateSystem();
        var aiSystem = new AISystem();

        game.AddSystem(dateSystem.Name, dateSystem);
        game.AddSystem(aiSystem.Name, aiSystem);

        var db = new InvictaDatabase();
        db = db.RegisterTable<Character>();
        db = db.RegisterTable<AIAgentData>();
        db = db.RegisterTable<GoldItem>();

        db = db.Insert("worker", new Character("Worker", 25, 5, 10, 5, 5, new DateTime(2175, 1, 1)));
        db = db.Insert("worker", new AIAgentData("Worker", "Idle"));

        var workCount = 0;
        var workerDomain = new DomainBuilder<SimulationContext>("Worker")
            .Select("Work")
                .Action("Work")
                    .Do(ctx =>
                    {
                        workCount++;
                        return FluidHTN.TaskStatus.Success;
                    })
                .End()
            .End()
            .Build();

        aiSystem.RegisterDomain("Worker", workerDomain);

        // Initialize game after domain registration
        db = game.Init(db);

        // Simulate a day with AI enabled
        db = game.SimulateDay(db);
        var workCountEnabled = workCount;
        Assert.True(workCountEnabled > 0);

        // Disable AI system
        game.DisableSystem(aiSystem.Name);

        // Simulate another day - AI should not run
        workCount = 0;
        db = game.SimulateDay(db);
        Assert.Equal(0, workCount);

        // Re-enable AI system
        game.EnableSystem(aiSystem.Name);

        // Simulate another day - AI should run again
        workCount = 0;
        db = game.SimulateDay(db);
        Assert.True(workCount > 0);
    }
}
