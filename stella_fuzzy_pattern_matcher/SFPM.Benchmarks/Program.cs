namespace SFPM.Benchmarks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

[SimpleJob(RuntimeMoniker.Net90)]               // JIT
//[SimpleJob(RuntimeMoniker.NativeAot90)]       // AOT
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SFPMBenchmarks
{
    private Query query;
    private List<Rule> rules;
    private List<Rule> tenthousandRules;
    private Rule OperatorBasedRule1Criteria;
    private Rule PredicateBasedRule1Criteria;
    private Rule BigPredicateRule10Criteria;
    private Rule BigOperatorRule10Criteria;

    /// <summary>
    /// Here we try to stress test the number of facts, they are auto generated
    /// simply to show case what facts you could have.
    /// </summary>
    private Dictionary<string, object> Facts;

    [GlobalSetup]
    public void Setup()
    {



        tenthousandRules = [];
        for (int i = 0; i < 3333; i++)
        {
            tenthousandRules.Add(new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("timeOfDay", timeOfDay => { return timeOfDay == "Night"; }),
                    new Criteria<string>("weather", weather => { return weather == "Rainy"; }),
                    new Criteria<int>("numberOfEnemiesNearby", enemies => { return enemies >= 1; }),
                    new Criteria<string>("equippedWeapon_type", weapon => { return weapon == "Sword"; }),
                    new Criteria<bool>("isSprinting", sprinting => { return sprinting == true; }),
                    new Criteria<int>("stamina", stamina => { return stamina <= 5; }),
                    new Criteria<int>("numberOfEnemiesNearby", enemies => { return enemies >= 1; }),
                    new Criteria<string>("equippedWeapon_type", weapon => { return weapon == "Sword"; }),
                ], () => { }));

            tenthousandRules.Add(new Rule([
                    new Criteria<string>("who", "Nick", Operator.Equal),
                    new Criteria<string>("weather", "Rainy", Operator.Equal),
                ], () => { }));

            tenthousandRules.Add(new Rule([
                    new Criteria<string>("who", "Nick", Operator.Equal),
                    new Criteria<string>("weather", "Rainy", Operator.Equal),
                    new Criteria<bool>("isSprinting", true, Operator.Equal),
                ], () => { }));
        }

        rules = [
                new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                ], ()=>{
                }),

                new Rule([
                    new Criteria<string>("attacker", attacker => { return attacker == "Hunter"; }),
                    new Criteria<string>("concept", concept => { return concept == "OnHit"; }),
                    new Criteria<double>("damage", damage => { return damage == 12.4; }),
                ], ()=>{
                }),

                new Rule([
                    new Criteria<string>("who", "Nick", Operator.Equal),
                    new Criteria<string>("concept", "onHit", Operator.Equal),
                    new Criteria<string>("timeOfDay", "Night", Operator.Equal),
                    new Criteria<string>("weather", "Rainy", Operator.Equal),
                    new Criteria<int>("numberOfEnemiesNearby", 1, Operator.GreaterThanOrEqual),
                    new Criteria<string>("equippedWeapon_type", "Sword", Operator.Equal),
                    new Criteria<bool>("isSprinting", true, Operator.Equal),
                    new Criteria<int>("stamina", 5, Operator.LessThanOrEqual),
                    new Criteria<bool>("isSprinting", true, Operator.Equal),
                    new Criteria<int>("stamina", 5, Operator.LessThanOrEqual)
                ], () => { }),

                new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("timeOfDay", timeOfDay => { return timeOfDay == "Night"; }),
                    new Criteria<string>("weather", weather => { return weather == "Rainy"; }),
                    new Criteria<int>("numberOfEnemiesNearby", enemies => { return enemies >= 1; }),
                    new Criteria<string>("equippedWeapon_type", weapon => { return weapon == "Sword"; }),
                    new Criteria<bool>("isSprinting", sprinting => { return sprinting == true; }),
                    new Criteria<int>("stamina", stamina => { return stamina <= 5; }),
                    new Criteria<int>("numberOfEnemiesNearby", enemies => { return enemies >= 1; }),
                    new Criteria<string>("equippedWeapon_type", weapon => { return weapon == "Sword"; }),
                ], () => { }),


                new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("timeOfDay", timeOfDay => { return timeOfDay == "Night"; }),
                    new Criteria<string>("weather", weather => { return weather == "Rainy"; }),
                    new Criteria<int>("numberOfEnemiesNearby", enemies => { return enemies >= 1; }),
                    new Criteria<string>("equippedWeapon_type", weapon => { return weapon == "Sword"; }),
                    new Criteria<int>("stamina", stamina => { return stamina <= 5; }),
                    new Criteria<int>("numberOfEnemiesNearby", enemies => { return enemies >= 1; }),
                ], () => { }),



                new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("timeOfDay", timeOfDay => { return timeOfDay == "Night"; }),
                    new Criteria<string>("weather", weather => { return weather == "Rainy"; }),
                    new Criteria<string>("equippedWeapon_type", weapon => { return weapon == "Sword"; }),
                    new Criteria<int>("stamina", stamina => { return stamina <= 5; }),
                    new Criteria<int>("numberOfEnemiesNearby", enemies => { return enemies >= 1; }),
                    new Criteria<string>("equippedWeapon_type", weapon => { return weapon == "Sword"; }),
                ], () => { }),

                new Rule([
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("weather", weather => { return weather == "Rainy"; }),
                    new Criteria<string>("equippedWeapon_type", weapon => { return weapon == "Sword"; }),
                    new Criteria<int>("numberOfEnemiesNearby", enemies => { return enemies >= 1; }),
                    new Criteria<string>("equippedWeapon_type", weapon => { return weapon == "Sword"; }),
                ], () => { }),

                new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("timeOfDay", timeOfDay => { return timeOfDay == "Night"; }),
                    new Criteria<int>("stamina", stamina => { return stamina <= 5; }),
                    new Criteria<int>("numberOfEnemiesNearby", enemies => { return enemies >= 1; }),
                ], () => { }),

                new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                ], ()=>{
                }),

                new Rule([
                    new Criteria<string>("attacker", attacker => { return attacker == "Hunter"; }),
                    new Criteria<string>("concept", concept => { return concept == "OnHit"; }),
                    new Criteria<double>("damage", damage => { return damage == 12.4; }),
                ], ()=>{
                }),

                new Rule([
                    new Criteria<string>("concept", concept => concept == "OnHit"),
                    new Criteria<double>("damage", damage => damage > 10.0),
                ], () => {
                }),

                new Rule([
                    new Criteria<string>("attacker", attacker => attacker.StartsWith("H")),
                    new Criteria<double>("damage", damage => damage < 20.0),
                ], () => {
                })
            ];
        // Sort rules by criteria count in descending order (highest count first)
        rules.Sort((a, b) => b.CriteriaCount.CompareTo(a.CriteriaCount));
        tenthousandRules.Sort((a, b) => b.CriteriaCount.CompareTo(a.CriteriaCount));

        OperatorBasedRule1Criteria = new Rule([
            new Criteria<string>("who", "Nick", Operator.Equal),
        ], () => { });

        PredicateBasedRule1Criteria = new Rule([
            new Criteria<string>("who", who => { return who == "Nick"; }),
        ], () => { });

        BigOperatorRule10Criteria = new Rule([
            new Criteria<string>("who", "Nick", Operator.Equal),
            new Criteria<string>("concept", "onHit", Operator.Equal),
            new Criteria<string>("timeOfDay", "Night", Operator.Equal),
            new Criteria<string>("weather", "Rainy", Operator.Equal),
            new Criteria<int>("numberOfEnemiesNearby", 1, Operator.GreaterThanOrEqual),
            new Criteria<string>("equippedWeapon_type", "Sword", Operator.Equal),
            new Criteria<bool>("isSprinting", true, Operator.Equal),
            new Criteria<int>("stamina", 5, Operator.LessThanOrEqual),
            new Criteria<bool>("isSprinting", true, Operator.Equal),
            new Criteria<int>("stamina", 5, Operator.LessThanOrEqual)
        ], () => { });

        BigPredicateRule10Criteria = new Rule([
            new Criteria<string>("who", who => { return who == "Nick"; }),
            new Criteria<string>("concept", concept => { return concept == "onHit"; }),
            new Criteria<string>("timeOfDay", timeOfDay => { return timeOfDay == "Night"; }),
            new Criteria<string>("weather", weather => { return weather == "Rainy"; }),
            new Criteria<int>("numberOfEnemiesNearby", enemies => { return enemies >= 1; }),
            new Criteria<string>("equippedWeapon_type", weapon => { return weapon == "Sword"; }),
            new Criteria<bool>("isSprinting", sprinting => { return sprinting == true; }),
            new Criteria<int>("stamina", stamina => { return stamina <= 5; }),
            new Criteria<int>("numberOfEnemiesNearby", enemies => { return enemies >= 1; }),
            new Criteria<string>("equippedWeapon_type", weapon => { return weapon == "Sword"; }),
        ], () => { });

        Facts = new Dictionary<string, object>
        {
            { "concept", "onHit" },
            { "who", "Nick"},
            // Health and Vitality
            { "health", 75.5f },           // float - Current health points
            { "maxHealth", 100.0f },        // float - Maximum health
            { "mana", 50 },               // int   - Current mana points
            { "stamina", 80 },            // int   - Current stamina
            { "isAlive", true },          // bool  - Is the entity alive?
            { "isStunned", false },        // bool  - Is the entity stunned?
            { "isBurning", true },         // bool  - Is the entity on fire?
            { "poisonLevel", 3 },          // int   - Poison severity level

            // Attributes and Skills
            { "strength", 15 },             // int   - Strength attribute
            { " Dexterity", 12 },          // int   - Dexterity attribute
            { "intelligence", 20 },         // int   - Intelligence attribute
            { "level", 8 },                // int   - Character level
            { "experience", 1250 },        // int   - Current experience points
            { "skill_swordsmanship", 65 },  // int   - Skill level in swordsmanship

            // Status Effects
            { "speedModifier", 1.2f },      // float - Speed multiplier (e.g., 1.2 = 20% faster)
            { "damageResistance", 0.1f },   // float - Damage reduction percentage (e.g., 0.1 = 10% reduction)
            { "effect_blessed_duration", 15 }, // int - Remaining duration of "blessed" effect (in seconds/turns)

            // Identity and Type
            { "entityType", "PlayerCharacter" }, // string - Type of entity
            { "characterClass", "Warrior" },    // string - Character class
            { "faction", "Humans" },          // string - Faction or allegiance
            { "name", "Sir Reginald" },       // string - Character name
            { "race", "Human" },             // string - Character race

            // Resource levels
            { "gold", 500 },                // int   - Amount of gold
            { "ironOre", 25 },             // int   - Amount of iron ore collected
            { "timeOfDay", "Night" },        // string - Current time of day (e.g., "Day", "Night", "Dawn", "Dusk")
            { "weather", "Rainy" },          // string - Current weather condition (e.g., "Sunny", "Cloudy", "Rainy", "Snowy")
            { "locationType", "Forest" },     // string - Type of location (e.g., "City", "Dungeon", "Forest", "Plains")
            { "ambientLightLevel", 0.3f },   // float - Ambient light level (0.0 - dark, 1.0 - full light)
            { "isCombatActive", false },     // bool  - Is combat currently active?
            { "numberOfEnemiesNearby", 3 },  // int   - Number of enemies within a certain radius
            { "quest_mainStory_progress", 3 }, // int - Main story quest progress (stage/step number)
            { "globalAlarmLevel", 2 },      // int - Global alarm level (e.g., in a stealth game)
            { "gameDifficulty", "Normal" },  // string - Game difficulty setting
            { "isPlayerInTown", true },       // bool - Is the player currently in a town?
            { "lastAction", "Attack" },        // string - Last player action (e.g., "Move", "Attack", "UseItem", "CastSpell")
            { "input_keyPressed", "Space" },  // string - Last key pressed by the player
            { "input_mouseButton", "LeftClick" }, // string - Last mouse button pressed
            { "direction_moved", "North" },     // string - Direction player last moved
            { "targetEntity_type", "Enemy" },    // string - Type of entity currently targeted by the player
            { "targetEntity_distance", 15.0f },  // float - Distance to the targeted entity
            { "isSprinting", false },          // bool  - Is the player currently sprinting?
            { "isJumping", false },            // bool  - Is the player currently jumping?
            { "playerIntent_attack", true },   // bool - Player's intent to attack (might be derived from input)
            { "timeSinceLastAttack", 0.5f },   // float - Time elapsed since the player last attacked
            { "inventory_hasPotion_healing", true },  // bool - Does the player have a healing potion in inventory?
            { "inventory_potion_healing_count", 3 }, // int   - Number of healing potions
            { "equippedWeapon_type", "Sword" },      // string - Type of currently equipped weapon
            { "equippedArmor_defense", 25 },         // int   - Defense value of equipped armor
            { "hasKey_dungeonLevel2", true },       // bool  - Does the player have the key to dungeon level 2?
            { "item_selected_name", "Iron Sword" },   // string - Name of the currently selected item in inventory
            { "item_equipped_slot", "MainHand" },    // string - Equipment slot of the currently equipped item
            { "item_weight_total", 15.2f },         // float - Total weight of items in inventory
            { "isInventoryFull", false },          // bool  - Is the inventory full?
            { "ammo_arrows", 50 },              // int   - Number of arrows in quiver
            { "relationship_guard_friendly", 0.7f },  // float - Relationship level with guards (0.0 - hostile, 1.0 - friendly)
            { "reputation_merchantsGuild", 25 },     // int   - Reputation with the Merchants Guild
            { "quest_npc_villager_helped", true },    // bool  - Has the player helped the villager NPC?
            { "isEnemyOf_orcs", false },            // bool  - Is the player an enemy of the orcs?
            { "partyMember_count", 2 },             // int   - Number of party members
            { "partyMember_leader", "PlayerCharacter" }, // string - Name/ID of the party leader
            { "faction_standing_elves", "Neutral" }, // string - Player's standing with the Elven faction (e.g., "Friendly", "Neutral", "Hostile")
            { "isTalkingTo_npc", "Blacksmith John" }, // string - Name of NPC currently in conversation with
            { "dialogue_topic_unlocked_magic", true }, // bool - Is the "magic" dialogue topic unlocked?
            { "npc_attitude_blacksmith", "Helpful" },  // string - Blacksmith NPC's current attitude (e.g., "Friendly", "Neutral", "Hostile", "Helpful")
            { "playerPosition_x", 150.2f },      // float - Player's X-coordinate
            { "playerPosition_y", 220.8f },      // float - Player's Y-coordinate
            { "playerZone", "Greenwood Forest" }, // string - Current zone/area the player is in
            { "distanceTo_questObjective", 50.5f }, // float - Distance to the current quest objective
            { "isNear_water", true },             // bool  - Is the player near water?
            { "entityLocation_guard1_zone", "CityGate" }, // string - Zone where guard entity 1 is located
            { "room_type", "Cave" },              // string - Type of room player is currently in (e.g., "Cave", "House", "Shop")
            { "elevation", 15 },                 // int   - Current elevation/altitude
            { "isIndoors", false },              // bool  - Is the player indoors?
            { "region_type", "Temperate" },       // string - Type of geographical region (e.g., "Temperate", "Desert", "Arctic")
        };

        query = new Query(Facts);
    }


    [Benchmark]
    public void OneRuleTwoCriteriaOperatorBasedMatch()
    {
        var (matched, numberMatched) = OperatorBasedRule1Criteria.StrictEvaluate(Facts);
    }

    [Benchmark]
    public void OneRuleTwoCriteriaPredicateBasedMatch()
    {
        var (matched, numberMatched) = PredicateBasedRule1Criteria.StrictEvaluate(Facts);
    }

    [Benchmark]
    public void BigPredicateBasedMatch()
    {
        var (matched, numberMatched) = BigPredicateRule10Criteria.StrictEvaluate(Facts);
    }

    [Benchmark]
    public void QueryMatch()
    {
        query.Match(rules);
    }


    [Benchmark]
    public void QueryMatch10000Rules()
    {
        query.Match(tenthousandRules);
    }

    [Benchmark]
    public void QueryMatch10000Times()
    {
        for (int i = 0; i < 9999; i++)
        {
            query.Match(rules);
        }
    }

    [Benchmark]
    public void ParraleQueryMatch10000Times()
    {
        Parallel.For(0, 9999, i =>
        {
            query.Match(rules);
        });
    }

    [Benchmark]
    public void BigOperatorBasedMatch()
    {
        var (matched, numberMatched) = BigOperatorRule10Criteria.StrictEvaluate(Facts);
    }


    [Benchmark]
    public void ParralelIterateOver100000RulesBigPredicate()
    {
        Parallel.For(0, 9999, i =>
        {
            var (matched, numberMatched) = BigPredicateRule10Criteria.StrictEvaluate(Facts);
        });
    }

    [Benchmark]
    public void ParralelIterateOver100000RulesBigOperator()
    {
        Parallel.For(0, 9999, i =>
        {
            var (matched, numberMatched) = BigOperatorRule10Criteria.StrictEvaluate(Facts);
        });
    }

    [Benchmark]
    public void ParralelIterateOver100000RulesPredicate()
    {
        Parallel.For(0, 9999, i =>
        {
            var (matched, numberMatched) = PredicateBasedRule1Criteria.StrictEvaluate(Facts);
        });
    }

    [Benchmark]
    public void ParralelIterateOver100000RulesOperator()
    {
        Parallel.For(0, 9999, i =>
        {
            var (matched, numberMatched) = OperatorBasedRule1Criteria.StrictEvaluate(Facts);
        });
    }


    [Benchmark]
    public void IterateOver100000RulesOperator()
    {
        for (int i = 0; i < 9999; i++)
        {
            var (matched, numberMatched) = OperatorBasedRule1Criteria.StrictEvaluate(Facts);
        }
    }

    [Benchmark]
    public void IterateOver100000RulesPredicate()
    {
        for (int i = 0; i < 9999; i++)
        {
            var (matched, numberMatched) = PredicateBasedRule1Criteria.StrictEvaluate(Facts);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            //var config = ManualConfig.Create(DefaultConfig.Instance)
            //    .WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(TimeUnit.Nanosecond));

            BenchmarkRunner.Run<SFPMBenchmarks>();
        }
    }
}