using System;
using System.Collections.Generic;
using FluidHTN;
using FluidHTN.Conditions;
using FluidHTN.Contexts;
using FluidHTN.Debug;
using FluidHTN.Factory;
using FluidHTN.PrimitiveTasks;

namespace Fluid
{
    public enum AIWorldState
    {
        Location,
    }

    /// <summary>
    /// A default implementation of the detailed IPlannerState.
    /// Provides no-op implementations for all callbacks.
    /// </summary>
    public class DefaultPlannerState : IPlannerState
    {
        public ITask? CurrentTask { get; set; }
        public Queue<ITask>? Plan { get; set; } = new Queue<ITask>(); // Initialize to avoid null issues
        public FluidHTN.TaskStatus LastStatus { get; set; } = FluidHTN.TaskStatus.Success; // Default to a sensible status

        // Callbacks are initialized to null (no-op by default)
        public Action<Queue<ITask>>? OnNewPlan { get; set; }
        public Action<Queue<ITask>, ITask, Queue<ITask>>? OnReplacePlan { get; set; }
        public Action<ITask>? OnNewTask { get; set; }
        public Action<ITask, ICondition>? OnNewTaskConditionFailed { get; set; }
        public Action<IPrimitiveTask>? OnStopCurrentTask { get; set; }
        public Action<IPrimitiveTask>? OnCurrentTaskCompletedSuccessfully { get; set; }
        public Action<IEffect>? OnApplyEffect { get; set; }
        public Action<IPrimitiveTask>? OnCurrentTaskFailed { get; set; }
        public Action<IPrimitiveTask>? OnCurrentTaskContinues { get; set; }
        public Action<
            IPrimitiveTask,
            ICondition
        >? OnCurrentTaskExecutingConditionFailed { get; set; }

        public DefaultPlannerState()
        {
            // Properties are initialized at declaration or can be further set here.
        }
    }

    public class AIContext : BaseContext
    {
        private byte[] _worldState = new byte[Enum.GetValues<AIWorldState>().Length];
        public override IFactory Factory { get; protected set; } = new DefaultFactory(); // 'AIContext.Factory.set': cannot change access modifiers when overriding 'protected' inherited member 'BaseContext.Factory.set'CS0507
        public override List<string> MTRDebug { get; set; } = [];
        public override List<string> LastMTRDebug { get; set; } = [];
        public override bool DebugMTR { get; } = false;
        public override Queue<IBaseDecompositionLogEntry>? DecompositionLog { get; set; }
        public override bool LogDecomposition { get; } = true;
        public override byte[] WorldState => _worldState;

        public Player Player { get; }
        public World World { get; }

        /// <summary>
        /// Gets or protected sets the planner state.
        /// This property implements the abstract member from BaseContext.
        /// </summary>
        public override IPlannerState PlannerState { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AIContext"/> class.
        /// </summary>
        /// <param name="player">The player instance.</param>
        /// <param name="world">The world instance.</param>
        /// <param name="plannerState">The initial planner state for this context. If null, a default will be created.</param>
        public AIContext(Player player, World world, IPlannerState? plannerState = null)
        {
            Player = player;
            World = world;
            PlannerState = plannerState ?? new DefaultPlannerState();
        }

        public override void Init()
        {
            base.Init();

            // Custom init of state
        }

        public bool HasState(AIWorldState state, bool value)
        {
            return HasState((int)state, (byte)(value ? 1 : 0));
        }

        public bool HasState(AIWorldState state)
        {
            return HasState((int)state, 1);
        }

        public void SetState(AIWorldState state, byte value, EffectType type)
        {
            SetState((int)state, value, true, type);
        }

        public byte GetState(AIWorldState state)
        {
            return GetState((int)state);
        }
    }
}
