# HTN Migration Guide - Before & After Examples

This guide shows concrete before/after examples for updating your HTN tasks to the new function arity requirement.

## Preconditions

### Before (❌ Wrong - will crash)

```elixir
Task.primitive(:attack,
  preconditions: [
    fn world -> world.has_weapon end,
    fn world -> world.target_visible end
  ]
)
```

### After (✅ Correct)

```elixir
Task.primitive(:attack,
  preconditions: [
    fn world, _params -> world.has_weapon end,
    fn world, _params -> world.target_visible end
  ]
)
```

---

## Effects

### Before (❌ Wrong)

```elixir
Task.primitive(:heal,
  effects: [
    fn world -> Map.update!(world, :health, &(&1 + 10)) end,
    fn world -> Map.put(world, :action_taken, true) end
  ]
)
```

### After (✅ Correct)

```elixir
Task.primitive(:heal,
  effects: [
    fn world, _params -> Map.update!(world, :health, &(&1 + 10)) end,
    fn world, _params -> Map.put(world, :action_taken, true) end
  ]
)
```

---

## Method Conditions

### Before (❌ Wrong)

```elixir
HTN.compound(:move,
  methods: [
    HTN.method(:fast_travel,
      priority: 10,
      conditions: [fn world -> world.has_mount end],
      subtasks: [{:gallop, %{}}]
    ),
    HTN.method(:walk,
      priority: 5,
      conditions: [fn world -> world.has_legs end],
      subtasks: [{:walk_step, %{}}, {:walk_step, %{}}]
    )
  ]
)
```

### After (✅ Correct)

```elixir
HTN.compound(:move,
  methods: [
    HTN.method(:fast_travel,
      priority: 10,
      conditions: [fn world, _params -> world.has_mount end],
      subtasks: [{:gallop, %{}}]
    ),
    HTN.method(:walk,
      priority: 5,
      conditions: [fn world, _params -> world.has_legs end],
      subtasks: [{:walk_step, %{}}, {:walk_step, %{}}]
    )
  ]
)
```

---

## When You Need the `params` Argument

### Use params when creating subtasks with dynamic data

```elixir
HTN.compound(:gather_resources,
  methods: [
    HTN.method(:gather_in_forest,
      conditions: [
        fn world, params ->
          location = Map.get(params, :location)
          Map.get(world, :location) == location && String.contains?(location, "forest")
        end
      ],
      subtasks: [
        {:find_berries, %{}},
        {:collect_firewood, %{}}
      ]
    ),
    HTN.method(:gather_in_cave,
      conditions: [
        fn world, params ->
          location = Map.get(params, :location)
          Map.get(world, :location) == location && String.contains?(location, "cave")
        end
      ],
      subtasks: [
        {:mine_ore, %{}},
        {:collect_geodes, %{}}
      ]
    )
  ]
)
```

When planning:

```elixir
HTN.find_plan(domain, world, :gather_resources, params: %{location: "Black Forest"})
```

---

## Real-World Example: Character Daily Routine

### Before (❌ Wrong)

```elixir
defp study_task do
  HTN.primitive(:study,
    preconditions: [
      fn world ->
        # ❌ This will crash - needs 2 args
        traits = Map.get(world, :_character_traits, [])
        :scholar in traits
      end
    ],
    effects: [
      fn world ->
        character_id = Map.get(world, :_character_id)
        if character_id do
          update_character_stat(world, character_id, :stewardship, 1)
        else
          world
        end
      end
    ],
    operator: fn world, params ->  # Note: operator already has 2 args
      character_id = Map.get(world, :_character_id)
      if character_id do
        {:ok, update_character_stat(world, character_id, :stewardship, 1)}
      else
        {:ok, world}
      end
    end
  )
end
```

### After (✅ Correct)

```elixir
defp study_task do
  HTN.primitive(:study,
    preconditions: [
      fn world, _params ->
        # ✅ Now accepts 2 arguments
        traits = Map.get(world, :_character_traits, [])
        :scholar in traits
      end
    ],
    effects: [
      fn world, _params ->
        character_id = Map.get(world, :_character_id)
        if character_id do
          update_character_stat(world, character_id, :stewardship, 1)
        else
          world
        end
      end
    ],
    operator: fn world, _params ->
      character_id = Map.get(world, :_character_id)
      if character_id do
        {:ok, update_character_stat(world, character_id, :stewardship, 1)}
      else
        {:ok, world}
      end
    end
  )
end
```

---

## Long-Running Operations (New Feature)

### Multi-Tick Animation

```elixir
HTN.primitive(:animate_character,
  operator: fn world, params ->
    duration = Map.get(params, :duration_ms, 1000)
    elapsed = Map.get(world, :_animation_elapsed, 0)

    if elapsed >= duration do
      # Animation complete
      world = Map.delete(world, :_animation_elapsed)
      {:ok, world}
    else
      # Animation still running - will be called again next tick
      new_elapsed = elapsed + 16  # Assume 16ms per game tick
      {:running, Map.put(world, :_animation_elapsed, new_elapsed)}
    end
  end
)
```

### Gradual Resource Gathering

```elixir
HTN.primitive(:gather_wood,
  operator: fn world, params ->
    total_needed = Map.get(params, :amount, 100)
    gathered = Map.get(world, :_wood_gathered, 0)
    rate = Map.get(params, :rate_per_tick, 10)

    new_gathered = min(gathered + rate, total_needed)
    world = Map.put(world, :_wood_gathered, new_gathered)

    if new_gathered >= total_needed do
      # Done gathering
      world = world
        |> Map.delete(:_wood_gathered)
        |> update_inventory(:wood, total_needed)
      {:ok, world}
    else
      # Keep gathering next tick
      {:running, world}
    end
  end
)
```

---

## Common Mistakes & Fixes

### ❌ Mistake 1: Using only 1 argument

```elixir
preconditions: [fn world -> world.flag end]
# CompileWarning: Expected function with arity 2
# Runtime Error: BadArityError when invoked
```

**Fix:**

```elixir
preconditions: [fn world, _params -> world.flag end]
```

---

### ❌ Mistake 2: Wrong order of arguments

```elixir
preconditions: [fn params, world -> world.flag end]
# Function will receive world as first arg, params as second
# Logic may fail silently or crash
```

**Fix:**

```elixir
preconditions: [fn world, params -> world.flag end]
```

---

### ❌ Mistake 3: Forgetting params in nested conditions

```elixir
HTN.method(:my_method,
  conditions: [fn world -> check_something(world) end],  # ❌ Missing params arg
  subtasks: [...]
)
```

**Fix:**

```elixir
HTN.method(:my_method,
  conditions: [fn world, _params -> check_something(world) end],  # ✅ Has params
  subtasks: [...]
)
```

---

### ❌ Mistake 4: Forgetting to handle {:running, ...} return

```elixir
operator: fn world, params ->
  progress = Map.get(world, :progress, 0) + 1
  new_world = Map.put(world, :progress, progress)

  if progress >= 100 do
    {:ok, new_world}
  else
    new_world  # ❌ Missing the {:running, ...} wrapper!
  end
end
```

**Fix:**

```elixir
operator: fn world, params ->
  progress = Map.get(world, :progress, 0) + 1
  new_world = Map.put(world, :progress, progress)

  if progress >= 100 do
    {:ok, new_world}  # ✅ Complete
  else
    {:running, new_world}  # ✅ Still running - try again next tick
  end
end
```

---

## Testing Your Updates

After updating your tasks, test them with the new signatures:

```elixir
defmodule MyTasksTest do
  use ExUnit.Case

  test "study task requires scholar trait" do
    domain = HTN.new_domain("test")
      |> HTN.add_task(study_task())
      |> HTN.add_task(idle_task())

    # With scholar trait
    context = %{_character_traits: [:scholar]}
    {:ok, plan} = HTN.find_plan(domain, context, :study)
    assert Plan.length(plan) == 1

    # Without scholar trait
    context = %{_character_traits: []}
    {:error, :no_plan_found} = HTN.find_plan(domain, context, :study)
  end
end
```

---

## Summary

| What              | Before                           | After                          | Why                             |
| ----------------- | -------------------------------- | ------------------------------ | ------------------------------- |
| Preconditions     | `fn world -> ... end`            | `fn world, _params -> ... end` | Eliminate runtime introspection |
| Effects           | `fn world -> ... end`            | `fn world, _params -> ... end` | Eliminate runtime introspection |
| Method Conditions | `fn world -> ... end`            | `fn world, _params -> ... end` | Eliminate runtime introspection |
| Operator Return   | Only `{:ok, w}` or `{:error, e}` | Also supports `{:running, w}`  | Support multi-tick actions      |
| Error             | `:max_iterations_exceeded`       | `:max_depth_exceeded`          | Prevent infinite recursion      |

All these changes work together to make your HTN planner faster, safer, and more flexible!
