using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluidHTN;
using FluidHTN.Contexts;
using FluidHTN.Factory;
using InvictaDB;

namespace StellaInvicta.AI;

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
        // READ-ONLY access to the world state at the start of the tick
        public InvictaDatabase Db { get; }
        
        // The Entity currently "thinking"
        public Ref<Character> Self { get; }
        
        // QUEUE of changes to apply after the planner finishes thinking
        private List<Func<InvictaDatabase, InvictaDatabase>> _pendingMutations 
            = new List<Func<InvictaDatabase, InvictaDatabase>>();

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

        // --- Standard Fluid-HTN Logging Overrides ---
        public override void Log(string name, string description)
        {
            // Optional: Route this to your own logging system
            // Console.WriteLine($"[AI-{Self.Id}] {name}: {description}");
        }
    }
