using System;
using FluidHTN;
using InvictaDB;
using StellaInvicta.AI;
using StellaInvicta.Data;
using Xunit;

namespace StellaInvicta.Tests;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


public class AISystemTest
{
    public record GoldItem(int Amount);

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
}
