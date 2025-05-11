using FluidHTN;
using FluidHTN.Contexts;
using FluidHTN.Debug;
using FluidHTN.Factory;
using TaskStatus = FluidHTN.TaskStatus;

public enum CoffeeAIWorldState
{
    HasCoffee,        // Does the agent have coffee? (0 = no, 1 = yes)
    AtCoffeeMachine,  // Is the agent at the coffee machine? (0 = no, 1 = yes)
    HasCup,           // Does the agent have a cup? (0 = no, 1 = yes)
    CoffeeMachine_Ready // Is the coffee machine ready to brew? (0 = no, 1 = yes)
    // Add other states if needed, e.g., ingredients available
}

// Assuming CoffeeAIWorldState is defined as above
public class CoffeeAIContext : BaseContext
{
    private byte[] _worldState = new byte[Enum.GetValues<CoffeeAIWorldState>().Length];
    public override IFactory Factory { get; protected set; } = new DefaultFactory();
    public override List<string> MTRDebug { get; set; } = [];
    public override List<string> LastMTRDebug { get; set; } = [];
    public override bool DebugMTR { get; } = false; // Set to true for MTR debugging
    public override Queue<IBaseDecompositionLogEntry> DecompositionLog { get; set; } = new();
    public override bool LogDecomposition { get; } = true; // Enable decomposition logging
    public override byte[] WorldState => _worldState;

    public Agent Agent { get; } // Reference to our coffee agent

    // You might have a simple World object for this example
    // public CoffeeWorld World { get; }

    public override IPlannerState PlannerState { get; protected set; }

    public CoffeeAIContext(Agent agent /*, CoffeeWorld world*/)
    {
        Agent = agent;
        // World = world;
        PlannerState = new DefaultPlannerState(); // Using the provided DefaultPlannerState
        Init();
    }

    public override void Init()
    {
        base.Init();
        // Initialize world states if needed, e.g.:
        // SetState(CoffeeAIWorldState.CoffeeMachine_Ready, 1, EffectType.Permanent); // Coffee machine is always ready
    }

    public bool HasState(CoffeeAIWorldState state, bool value)
    {
        return HasState((int)state, (byte)(value ? 1 : 0));
    }

    public bool HasState(CoffeeAIWorldState state)
    {
        return HasState((int)state, 1);
    }

    public void SetState(CoffeeAIWorldState state, byte value, EffectType type)
    {
        SetState((int)state, value, true, type);
    }

    public void SetState(CoffeeAIWorldState state, bool value, EffectType type)
    {
        SetState((int)state, (byte)(value ? 1 : 0), true, type);
    }

    public byte GetState(CoffeeAIWorldState state)
    {
        return GetState((int)state);
    }
}

// Using the DefaultPlannerState from the provided example
public class DefaultPlannerState : IPlannerState
{
    public ITask? CurrentTask { get; set; }
    public Queue<ITask> Plan { get; set; } = new Queue<ITask>();
    public FluidHTN.TaskStatus LastStatus { get; set; } = FluidHTN.TaskStatus.Success;
    public Action<Queue<ITask>>? OnNewPlan { get; set; }
    public Action<Queue<ITask>, ITask, Queue<ITask>>? OnReplacePlan { get; set; }
    public Action<ITask>? OnNewTask { get; set; }
    public Action<ITask, FluidHTN.Conditions.ICondition>? OnNewTaskConditionFailed { get; set; }
    public Action<FluidHTN.PrimitiveTasks.IPrimitiveTask>? OnStopCurrentTask { get; set; }
    public Action<FluidHTN.PrimitiveTasks.IPrimitiveTask>? OnCurrentTaskCompletedSuccessfully { get; set; }
    public Action<IEffect>? OnApplyEffect { get; set; }
    public Action<FluidHTN.PrimitiveTasks.IPrimitiveTask>? OnCurrentTaskFailed { get; set; }
    public Action<FluidHTN.PrimitiveTasks.IPrimitiveTask>? OnCurrentTaskContinues { get; set; }
    public Action<FluidHTN.PrimitiveTasks.IPrimitiveTask, FluidHTN.Conditions.ICondition>? OnCurrentTaskExecutingConditionFailed { get; set; }
}

public enum AgentProcessingMode
{
    NeedsNewPlan,    // Agent needs to run a full decomposition to find a new plan.
    ExecutingPlan    // Agent has a plan and is focused on executing its next step.
}

public class Agent
{
    private Planner<CoffeeAIContext> _planner;
    public CoffeeAIContext Context { get; private set; }
    private Domain<CoffeeAIContext> _domain;
    private AgentProcessingMode _currentMode = AgentProcessingMode.NeedsNewPlan;

    public Agent(/* CoffeeWorld world */)
    {
        _planner = new Planner<CoffeeAIContext>();
        Context = new CoffeeAIContext(this /*, world */);
        // Context.Init() is called in CoffeeAIContext constructor

        _domain = BuildDomain(); // Your existing BuildDomain method
    }

    // This method will be called by your main game loop once per agent's "turn" or "tick"
    public void UpdateAI()
    {
        Console.WriteLine($"\n--- Agent UpdateAI (Mode: {_currentMode}) ---");
        PrintAgentStatus(); // Helper for logging

        // Check if we need to switch from ExecutingPlan to NeedsNewPlan
        if (_currentMode == AgentProcessingMode.ExecutingPlan)
        {
            bool isPlanEmpty = Context.PlannerState.Plan == null ||
                               (Context.PlannerState.Plan.Count == 0 && Context.PlannerState.CurrentTask == null);
            bool lastTaskFailed = Context.PlannerState.LastStatus == TaskStatus.Failure;

            if (isPlanEmpty)
            {
                Console.WriteLine("Agent: Current plan completed or is empty. Switching to NeedsNewPlan.");
                _currentMode = AgentProcessingMode.NeedsNewPlan;
            }
            else if (lastTaskFailed)
            {
                Console.WriteLine("Agent: Last task failed. Switching to NeedsNewPlan to find an alternative.");
                _currentMode = AgentProcessingMode.NeedsNewPlan;
            }
        }

        // Process based on current mode
        if (_currentMode == AgentProcessingMode.NeedsNewPlan)
        {
            Console.WriteLine("Agent: Mode is NeedsNewPlan. Performing planning (and potentially first step execution)...");
            _planner.Tick(_domain, Context); // This will decompose and find a plan.
                                             // It will also execute the first task if it's instantaneous.

            // After a planning tick, if a plan is now active, switch to execution mode for the next AI update.
            if (Context.PlannerState.CurrentTask != null || (Context.PlannerState.Plan != null && Context.PlannerState.Plan.Count > 0))
            {
                Console.WriteLine("Agent: New plan established. Switching to ExecutingPlan for subsequent updates.");
                _currentMode = AgentProcessingMode.ExecutingPlan;
            }
            else
            {
                // Stay in NeedsNewPlan if no viable plan was found (e.g., agent might just idle).
                // The planner.Tick would have found the Idle task if nothing else.
                Console.WriteLine("Agent: Planning did not result in a new multi-step plan, or an idle task was chosen. Will re-evaluate next update if necessary.");
                // If idling, it effectively completed its "plan" (the idle task), so it might go back to NeedsNewPlan.
                if (Context.PlannerState.CurrentTask?.Name == "Idle" && Context.PlannerState.LastStatus == TaskStatus.Success)
                {
                    // This ensures that if Idle is the only thing to do, it doesn't get stuck in ExecutingPlan with an empty plan.
                    _currentMode = AgentProcessingMode.NeedsNewPlan;
                }
            }
        }
        else // AgentProcessingMode.ExecutingPlan
        {
            Console.WriteLine("Agent: Mode is ExecutingPlan. Executing next step of the current plan...");
            // We expect Planner.Tick to execute the CurrentTask (if it was 'Continue')
            // or dequeue the next task from the Plan and execute it.
            // It should *not* perform a full, expensive replan unless the current task fails
            // or the plan structure itself dictates a re-evaluation due to a sensed change.
            _planner.Tick(_domain, Context);
        }

        LogDecomposition(); // Your existing decomposition logging
    }

    private void PrintAgentStatus()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"AI Status: HasCoffee={Context.GetState(CoffeeAIWorldState.HasCoffee)}, AtMachine={Context.GetState(CoffeeAIWorldState.AtCoffeeMachine)}, HasCup={Context.GetState(CoffeeAIWorldState.HasCup)}, MachineReady={Context.GetState(CoffeeAIWorldState.CoffeeMachine_Ready)}");
        if (Context.PlannerState.CurrentTask != null)
        {
            Console.WriteLine($"Current Task: {Context.PlannerState.CurrentTask.Name} (Status: {Context.PlannerState.LastStatus})");
        }
        else
        {
            Console.WriteLine("Current Task: None");
        }
        if (Context.PlannerState.Plan != null && Context.PlannerState.Plan.Count > 0)
        {
            Console.WriteLine($"Tasks in Plan Queue: {Context.PlannerState.Plan.Count} (Next: {Context.PlannerState.Plan.Peek().Name})");
        }
        else
        {
            Console.WriteLine("Tasks in Plan Queue: Empty");
        }
        Console.ResetColor();
    }


    private void LogDecomposition()
    {
        if (Context.LogDecomposition && Context.DecompositionLog != null && Context.DecompositionLog.Count > 0)
        {
            //Console.WriteLine("---------------------- DECOMP LOG --------------------------");
            while (Context.DecompositionLog.Count > 0) // Check here too
            {
                var entry = Context.DecompositionLog.Dequeue();
                var depth = FluidHTN.Debug.Debug.DepthToString(entry.Depth);
                //    Console.ForegroundColor = entry.Color;
                //    Console.WriteLine($"{depth}{entry.Name}: {entry.Description}");
            }
            //Console.ResetColor();
            //Console.WriteLine("-------------------------------------------------------------");
        }
    }

    // --- Primitive Task Implementations (GoToCoffeeMachine, TakeCup, BrewCoffee, Idle) ---
    // These remain the same as in the previous example. Ensure they return TaskStatus.Success
    // if they are meant to be instantaneous from the game's perspective for this step.
    // If a task should take multiple game ticks to complete (e.g., actual movement),
    // it would return TaskStatus.Continue until done.

    private Domain<CoffeeAIContext> BuildDomain()
    {

        /*
        You might wonder: "How can I make it so task take a certain amount of time?"

        Its quite easy. When a task gets executed its execution time is the time for the task.

        Wdim: Imagine a animation playing instead of printing to the console. The next task gets
        executed as soon as the animation is finished. We could also add a new world state like 
        can move or "Its his turn". When thinking about a turn based game. In many games that 
        are realtime most actions are animations that get executed and when finished executed the 
        next step in their plan gets done. 
        Imagine Walking Animation -> Picking Up Animation -> Drinking Animation;
        */

        var builder = new DomainBuilder<CoffeeAIContext>("GetCoffeeDomain");

        builder.Select("Get Coffee")
            .Condition("Doesn't have coffee", ctx => ctx.GetState(CoffeeAIWorldState.HasCoffee) == 0)
            .Action("Go To Coffee Machine")
                .Condition("Not at coffee machine", ctx => ctx.GetState(CoffeeAIWorldState.AtCoffeeMachine) == 0)
                .Do(GoToCoffeeMachine)
                .Effect("At coffee machine", EffectType.PlanAndExecute, (ctx, type) => ctx.SetState(CoffeeAIWorldState.AtCoffeeMachine, true, type))
            .End()
            .Action("Take Cup")
                .Condition("At coffee machine", ctx => ctx.GetState(CoffeeAIWorldState.AtCoffeeMachine) == 1)
                .Condition("Doesn't have cup", ctx => ctx.GetState(CoffeeAIWorldState.HasCup) == 0)
                .Do(TakeCup)
                .Effect("Has cup", EffectType.PlanAndExecute, (ctx, type) => ctx.SetState(CoffeeAIWorldState.HasCup, true, type))
            .End()
            .Action("Brew Coffee")
                .Condition("At coffee machine", ctx => ctx.GetState(CoffeeAIWorldState.AtCoffeeMachine) == 1)
                .Condition("Has cup", ctx => ctx.GetState(CoffeeAIWorldState.HasCup) == 1)
                .Condition("Coffee machine ready", ctx => ctx.GetState(CoffeeAIWorldState.CoffeeMachine_Ready) == 1)
                .Do(BrewCoffee)
                .Effect("Has coffee", EffectType.PlanAndExecute, (ctx, type) => ctx.SetState(CoffeeAIWorldState.HasCoffee, true, type))
                // Optional: Make machine not ready after brewing to show state change
                .Effect("Coffee machine not ready", EffectType.PlanAndExecute, (ctx, type) => ctx.SetState(CoffeeAIWorldState.CoffeeMachine_Ready, false, type))
            .End()
        .End();

        builder.Action("Idle")
            .Do(Idle)
        .End();

        return builder.Build();
    }

    private TaskStatus GoToCoffeeMachine(CoffeeAIContext context)
    {
        Console.WriteLine("Agent Action: Walking to the coffee machine...");
        Console.WriteLine("Agent Action: Arrived at the coffee machine.");
        return TaskStatus.Success;
    }

    private TaskStatus TakeCup(CoffeeAIContext context)
    {
        Console.WriteLine("Agent Action: Taking a cup...");
        Console.WriteLine("Agent Action: Got a cup.");
        return TaskStatus.Success;
    }

    private TaskStatus BrewCoffee(CoffeeAIContext context)
    {
        Console.WriteLine("Agent Action: Brewing coffee...");
        // Simulate time/effort for brewing if it's not instant
        // For this example, let's say it's quick. If it took time:
        // if (context.GetState("BrewingTimer") == 0) {
        //    context.SetState("BrewingTimer", 3, EffectType.PlanAndExecute); // needs 3 ticks
        //    return TaskStatus.Continue;
        // }
        // int timer = context.GetState("BrewingTimer");
        // timer--;
        // context.SetState("BrewingTimer", (byte)timer, EffectType.PlanAndExecute);
        // if (timer > 0) return TaskStatus.Continue;

        Console.WriteLine("Agent Action: Coffee is ready!");
        return TaskStatus.Success;
    }

    private TaskStatus Idle(CoffeeAIContext context)
    {
        if (context.GetState(CoffeeAIWorldState.HasCoffee) == 1)
        {
            Console.WriteLine("Agent Action: Enjoying my coffee. Ahh...");
        }
        else
        {
            Console.WriteLine("Agent Action: Idling...");
        }
        return TaskStatus.Success;
    }
}

/// <summary>
/// This example demonstrates a simple AI agent that wants to get coffee.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("// Game loop integration: Agent gets coffee.");

        var agent = new Agent();
        // Initial world setup
        agent.Context.SetState(CoffeeAIWorldState.CoffeeMachine_Ready, true, EffectType.Permanent);

        int gameTick = 0;
        const int maxGameTicks = 20; // Let it run for a bit

        while (gameTick < maxGameTicks)
        {
            Console.WriteLine($"\n<=============== GAME TICK {gameTick} ===============>");

            // In a real game, you'd update other systems here (physics, input, other AIs)

            agent.UpdateAI(); // Agent processes its logic for this tick

            // Check for goal completion or other conditions
            if (agent.Context.GetState(CoffeeAIWorldState.HasCoffee) == 1)
            {
                Console.WriteLine("\nSUCCESS: Agent has coffee!");
                // You might give the agent a new goal or let it idle.
                // For this example, we can let it continue idling if that's what the plan leads to.
            }

            gameTick++;
            Console.WriteLine($"<=============== END GAME TICK {gameTick - 1} ===============>");
            System.Threading.Thread.Sleep(100); // Simulate delay of a real game tick
        }

        Console.WriteLine("\n--- Simulation Finished ---");
        if (agent.Context.GetState(CoffeeAIWorldState.HasCoffee) == 0)
        {
            Console.WriteLine("Max game ticks reached; agent did NOT get coffee.");
        }
        else
        {
            Console.WriteLine("Agent got coffee and finished its main objective.");
        }
    }
}