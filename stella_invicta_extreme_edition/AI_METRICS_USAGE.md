# HTN AI Metrics - UI Integration Guide

Your HTN AI metrics are now easily accessible through the main `StellaInvicta.Metrics` module. The UI can consume AI decision data with minimal effort.

## Critical: Function Arity Requirements

**All precondition, effect, and method condition functions MUST accept exactly 2 arguments: `(world, params)`**

This is a breaking change that enables significant performance optimizations by removing runtime function introspection.

### Correct Examples

```elixir
# ✅ CORRECT: 2 arguments
preconditions: [
  fn world, _params -> Map.get(world, :_character_traits, []) end
]

effects: [
  fn world, _params -> Map.put(world, :flag, true) end
]

conditions: [
  fn world, _params -> world.energy > 50 end
]

# ❌ WRONG: Only 1 argument
preconditions: [
  fn world -> Map.get(world, :_character_traits, []) end  # Will crash!
]
```

The second argument (`params`) is useful when you need to access task parameters passed during planning.

## Quick Start

### 1. Get Overall AI Performance Summary

```elixir
game_state = StellaInvicta.Game.get_state()

# Get all AI metrics in one call
summary = StellaInvicta.Metrics.get_summary(game_state)

# Extract the AI portion
ai_summary = summary.ai
# => %{
#   tracked_entities: 5,
#   total_planning_attempts: 42,
#   total_successful_plans: 38,
#   total_failed_plans: 4,
#   total_backtracks: 8,
#   overall_success_rate: 90.48
# }
```

### 2. Get Formatted Decision Log for a Character

```elixir
# Get human-readable decision log (newest first by default)
decisions = StellaInvicta.Metrics.get_ai_decisions(game_state, character_id, limit: 20)

# => [
#   "★ Execution complete (3 steps)",
#   "✓ Executed :rest",
#   "→ Method :rest_routine selected for :daily_routine",
#   "▶ Planning started for task :daily_routine",
#   ...
# ]
```

### 3. Get Detailed Planning Statistics for a Character

```elixir
stats = StellaInvicta.Metrics.get_ai_summary(game_state, character_id)

# => %{
#   planning_attempts: 5,
#   successful_plans: 4,
#   failed_plans: 1,
#   success_rate: 80.0,
#   total_backtracks: 2,
#   total_iterations: 47,
#   avg_planning_time_us: 1523,
#   method_selection_counts: %{scholar_routine: 2, rest_routine: 2, warrior_routine: 1},
#   task_execution_counts: %{study: 2, rest: 1, train: 1},
#   decisions_logged: 18
# }
```

### 4. Get Raw Decision Entries (for custom formatting)

```elixir
# Get raw decision objects for maximum flexibility
raw_entries = StellaInvicta.Metrics.get_ai_decision_log(game_state, character_id)

# Each entry has:
# %{
#   timestamp: 1234567890,
#   type: :method_selected,
#   task: :daily_routine,
#   method: :scholar_routine,
#   reason: nil,
#   params: %{character_id: 1},
#   world_snapshot: nil,  # Only if capture_world_snapshots: true
#   details: %{...}
# }
```

### 5. Explore World as Domain Concept

The HTN planner now works directly with the world as its domain. When planning for a character:

```elixir
# Instead of passing abstract parameters, the world is enriched with character context
context = CharacterAI.prepare_planning_context(world, character_id)

# The context now includes:
# - All world state (environmental reasoning)
# - Character identity: `_character_id`
# - Character object: `_character`
# - Character traits: `_character_traits`
# - Character stats: `_character_stats`

# Task conditions naturally read from this enriched context:
# fn world, _params ->
#   traits = Map.get(world, :_character_traits, [])
#   :scholar in traits
# end

# The world IS the domain of knowledge - no intermediate data structures needed
```

## Key Concepts

### Planning Context = Knowledge Domain

Instead of thinking of HTN conditions as querying abstract parameters, think of them as reasoning about the world:

```elixir
# OLD (parameter-based):
preconditions: [
  fn world, params ->
    character_id = params[:character_id]
    traits = Map.get(world.character_traits, character_id, [])
    :scholar in traits
  end
]

# NEW (world as domain):
preconditions: [
  fn world, _params ->
    traits = Map.get(world, :_character_traits, [])
    :scholar in traits
  end
]
```

This is much more natural - the planner sees the world with all the information needed already prepared.

## Integration Points

### In CharacterAI System

```elixir
def run(world) do
  world
  |> get_all_character_plans()
  |> Enum.reduce(world, fn {character_id, plan}, acc_world ->
    # Plans now operate on the enriched planning context
    execute_character_plan_step(acc_world, character_id, plan)
  end)
end
```

### In Game Loop

```elixir
# After each game tick
def tick(game_state) do
  # ... game logic ...

  # Update metrics (already integrated in systems)
  updated_state = StellaInvicta.Game.run_systems(game_state)

  # UI can now query:
  summary = StellaInvicta.Metrics.get_summary(updated_state)

  # Return for UI rendering
  {:ok, updated_state, summary}
end
```

## Advanced: Creating New AI Entities with Metrics

```elixir
# Create a new metrics tracker for a new character
metrics = StellaInvicta.Metrics.new_ai_metrics(
  capture_world_snapshots: false,  # Set to true for debugging (expensive!)
  max_decisions: 1000
)

# Store it
game_state = StellaInvicta.Metrics.store_ai_metrics(game_state, character_id, metrics)

# Clear when needed
game_state = StellaInvicta.Metrics.clear_ai_metrics(game_state, character_id)
```

## UI Display Examples

### Simple Dashboard

```elixir
summary = Metrics.get_summary(game_state)

dashboard = %{
  "AI Performance" => %{
    "Success Rate" => "#{summary.ai.overall_success_rate |> Float.round(1)}%",
    "Planning Attempts" => summary.ai.total_planning_attempts,
    "Tracked Entities" => summary.ai.tracked_entities,
    "Backtracks" => summary.ai.total_backtracks
  }
}
```

### Character Decision History

```elixir
decisions = Metrics.get_ai_decisions(game_state, character_id, limit: 50)
stats = Metrics.get_ai_summary(game_state, character_id)

history = %{
  "Character" => "Charlemagne",
  "Planning Stats" => %{
    "Success Rate" => "#{stats.success_rate |> Float.round(1)}%",
    "Avg Planning Time" => "#{div(stats.avg_planning_time_us, 1000)}ms",
    "Decisions Logged" => stats.decisions_logged
  },
  "Recent Decisions" => decisions
}
```

## Design Philosophy

**The world is the domain.** All the information the AI needs to make decisions exists in the game world. By enriching the planning context with character-specific data, the HTN planner has access to everything it needs without:

-   Abstract parameter passing
-   External configuration lookups
-   Indirect data access patterns

This makes the AI logic clearer, easier to debug, and more intuitive for both developers and UI designers.

## Performance Optimizations

The latest version includes critical performance improvements:

### 1. **Arity Standardization**

Removed all runtime `Function.info/2` checks. All functions now have fixed arity (2 arguments), eliminating introspection overhead. This provides a significant performance boost for heavily used code paths.

### 2. **Memory Optimization in Metrics**

The Metrics module now uses probabilistic trimming - the decision log size is only checked every 100 insertions instead of after every insertion. This eliminates O(N) list operations on the hot path while maintaining size bounds.

### 3. **Depth Limiting**

Added maximum decomposition depth limiting (default: 50 levels). The planner now returns `{:error, :max_depth_exceeded}` if nesting gets too deep, preventing infinite recursion that could hang the game engine.

### 4. **Domain Validation**

New `Domain.validate/1` function catches configuration errors at startup. Use this to validate your domain before deploying:

```elixir
domain = HTN.new_domain("my_domain")
  |> HTN.add_task(idle_task())
  |> HTN.add_task(work_task())

# Catch typos in subtask names at startup
case HTN.Domain.validate(domain) do
  :ok -> {:ok, domain}
  {:error, issues} ->
    IO.inspect(issues)
    {:error, "Invalid domain"}
end
```

### 5. **Long-Running Actions Support**

The `execute_step/3` function now properly handles long-running primitive tasks:

```elixir
operator: fn world, _params ->
  # Tasks can return:
  # - {:ok, new_world} → Task completes, advance to next step
  # - {:running, new_world} → Task still running, stay on same step next tick
  # - {:error, reason} → Task failed

  {:running, Map.put(world, :progress, 50)}  # Task will re-execute next tick
end
```

This is perfect for multi-tick animations, gradual state transitions, or any action that takes multiple game ticks to complete.

## Next Steps

1. Display the decision log in your UI
2. Show planning statistics per character
3. Add alerts for high backtrack counts (indicates difficult planning problems)
4. Track success rates over time to tune task/method priorities
5. Use world snapshots (when enabled) to debug "why did the planner choose this?"
6. Call `Domain.validate/1` at startup to catch configuration errors early
