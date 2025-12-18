using System;
using System.Collections.Generic;
using FluidHTN;
using InvictaDB;
using StellaInvicta.Data;

namespace StellaInvicta.AI;

/// <summary>
/// AI System that manages HTN Planners for entities.
/// </summary>
public class AISystem : ISystem
{
    /// <inheritdoc/>
    public string Name => "Fluid-HTN AI System";
    /// <inheritdoc/>
    public string Description => "Orchestrates HTN Planners for entities.";
    /// <inheritdoc/>
    public string Author => "System";
    /// <inheritdoc/>
    public bool Enabled { get; set; } = true;
    /// <inheritdoc/>
    public bool IsInitialized { get; set; }

    // Planner Cache:
    // Since Fluid-HTN Planners are stateful objects (holding the current plan stack),
    // we keep them alive in memory. 
    // Key: Entity ID, Value: The Planner Instance
    private readonly Dictionary<string, Planner<SimulationContext>> _planners = [];

    // Domain Cache:
    // We only want to build the Domain definitions once.
    private readonly Dictionary<string, Domain<SimulationContext>> _domains = [];

    /// <summary>
    /// Registers a domain definition for use by agents.
    /// </summary>
    public void RegisterDomain(string name, Domain<SimulationContext> domain)
    {
        _domains[name] = domain;
    }

    /// <inheritdoc/>
    public InvictaDatabase Initialize(InvictaDatabase db)
    {
        // 1. Register the AI Component Table if strictly necessary, 
        // though InvictaDB creates tables lazily on generic calls.

        // NOTE: How can we make this moddable? What are domains? should system define them?

        IsInitialized = true;
        return db;
    }

    /// <inheritdoc/>
    public InvictaDatabase Shutdown(InvictaDatabase db)
    {
        _planners.Clear();
        return db;
    }

    /// <inheritdoc/>
    public InvictaDatabase Run(InvictaDatabase db)
    {
        if (!Enabled) return db;

        // 1. Identify all entities that need AI. 
        // Assuming we attach AIAgentData to Characters for this example.
        // In a real scenario, you might have a specific component for "Spaceship" or "Faction".
        var agentTable = db.GetTable<AIAgentData>();

        // We accumulate changes from all agents here
        var nextDb = db;

        foreach (var (agentId, agentData) in agentTable)
        {
            // 2. Get or Create Planner
            if (!_planners.TryGetValue(agentId, out var planner))
            {
                planner = new Planner<SimulationContext>();
                _planners[agentId] = planner;
            }

            // 3. Resolve Domain
            if (!_domains.TryGetValue(agentData.DomainName, out var domain))
                continue; // Domain not found, skip

            // 4. Create Context
            // This wraps the *current* state of the DB (nextDb)
            // We use 'nextDb' so agents reacting later in the frame see changes from earlier agents?
            // OR use 'db' if we want all agents to act simultaneously based on start-of-frame state.
            // Using 'db' (simultaneous) is usually safer for parallelism, using 'nextDb' is more responsive.
            var context = new SimulationContext(db, new Ref<Character>(agentId));

            // 5. TICK THE PLANNER
            // This runs the logic. If an action executes, it calls context.ApplyChange()
            planner.Tick(domain, context);

            // 6. Commit Changes
            // The context now holds a list of mutations (e.g., "Add Gold", "Move Ship").
            // We apply them to generate the new database state.
            nextDb = context.Commit(nextDb);
        }

        return nextDb;
    }
}
