# HTN AI Metrics - UI Integration Guide

Your HTN AI metrics are now easily accessible through the main `StellaInvicta.Metrics` module. The UI can consume AI decision data with minimal effort.

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

## Next Steps

1. Display the decision log in your UI
2. Show planning statistics per character
3. Add alerts for high backtrack counts (indicates difficult planning problems)
4. Track success rates over time to tune task/method priorities
5. Use world snapshots (when enabled) to debug "why did the planner choose this?"
