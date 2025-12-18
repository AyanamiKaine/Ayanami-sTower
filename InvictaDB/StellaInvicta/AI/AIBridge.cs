using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluidHTN;
using FluidHTN.Contexts;
using FluidHTN.Debug;
using FluidHTN.Factory;
using InvictaDB;
using StellaInvicta.Data;

namespace StellaInvicta.AI;

/// <summary>
/// World state flags used by the HTN planner for simulation.
/// </summary>
public enum WorldState
{
    /// <inheritdoc/>
    HasCredits,
    /// <inheritdoc/>
    HasCargo,
    /// <inheritdoc/>
    HasTradeRoute,
    /// <inheritdoc/>
    AtOrigin,
    /// <inheritdoc/>
    AtDestination
}

/// <summary>
/// Component data for an entity controlled by HTN.
/// Stores high-level state that needs to be persisted in the DB.
/// </summary>
public record AIAgentData(
    string DomainName,       // Which behavior logic to use (e.g., "Trader", "Fighter")
    string CurrentStateLabel // Debug label for what they are doing
);

/// <summary>
/// The Bridge Context.
/// It wraps the Immutable Database for 'Read' access,
/// and buffers 'Write' access into a queue of mutations.
/// </summary>
public class SimulationContext : BaseContext
{
    /// <summary>
    /// READ-ONLY access to the world state at the start of the tick
    /// </summary>
    public InvictaDatabase Db { get; }

    /// <summary>
    /// The Entity currently "thinking"
    /// </summary>
    public Ref<Character> Self { get; }

    /// <summary>
    /// QUEUE of changes to apply after the planner finishes thinking
    /// </summary>
    private List<Func<InvictaDatabase, InvictaDatabase>> _pendingMutations
        = [];

    /// <inheritdoc/>
    public SimulationContext(InvictaDatabase db, Ref<Character> self)
    {
        Db = db;
        Self = self;
        Init(); // fluid-htn init
    }

    /// <summary>
    /// Call this from within HTN Actions to schedule a DB change.
    /// </summary>
    public void ApplyChange(Func<InvictaDatabase, InvictaDatabase> mutation)
    {
        _pendingMutations.Add(mutation);
    }

    /// <summary>
    /// Executes all buffered changes on the database and returns the new state.
    /// </summary>
    public InvictaDatabase Commit(InvictaDatabase originalDb)
    {
        var result = originalDb;
        foreach (var mutation in _pendingMutations)
        {
            result = mutation(result);
        }
        return result;
    }

    /// <summary>
    /// Gets the number of pending mutations queued.
    /// </summary>
    public int PendingMutationCount => _pendingMutations.Count;

    /// <inheritdoc/>
    public override IFactory Factory { get; protected set; } = new DefaultFactory();
    /// <inheritdoc/>
    public override IPlannerState PlannerState { get; protected set; } = new DefaultPlannerState();
    /// <inheritdoc/>
    public override List<string> MTRDebug { get; set; } = [];
    /// <inheritdoc/>
    public override List<string> LastMTRDebug { get; set; } = [];

    /// <inheritdoc/>
    public override bool DebugMTR => true;

    /// <inheritdoc/>
    public override Queue<IBaseDecompositionLogEntry> DecompositionLog { get; set; } = new Queue<IBaseDecompositionLogEntry>();

    /// <inheritdoc/>
    public override bool LogDecomposition => true;
    /// <inheritdoc/>
    public override byte[] WorldState { get; } = new byte[Enum.GetValues<WorldState>().Length];

    // --- World State Helpers for HTN Effects ---

    /// <summary>
    /// Sets a world state flag (used by HTN effects during planning).
    /// </summary>
    public void SetWorldState(WorldState state, bool value)
    {
        if (ContextState == ContextState.Planning)
        {
            // During planning, we use the base SetState which handles the state stack
            SetState((int)state, (byte)(value ? 1 : 0), true, EffectType.PlanAndExecute);
        }
        else
        {
            // During execution or initialization, we set the state directly
            // But we should still use SetState to be safe if the library expects it
            SetState((int)state, (byte)(value ? 1 : 0), true, EffectType.Permanent);
        }
    }

    /// <summary>
    /// Gets a world state flag.
    /// </summary>
    public bool GetWorldState(WorldState state)
    {
        return GetState((int)state) == 1;
    }
}
