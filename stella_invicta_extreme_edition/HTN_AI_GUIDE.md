# HTN AI System Guide

This guide explains how to use the Hierarchical Task Network (HTN) planning system in Stella Invicta to create intelligent AI behaviors for characters.

## Table of Contents

1. [Overview](#overview)
2. [Core Concepts](#core-concepts)
3. [Creating Tasks](#creating-tasks)
4. [Building Domains](#building-domains)
5. [Planning and Execution](#planning-and-execution)
6. [Multi-Tick Execution](#multi-tick-execution)
7. [Replanning on Failure](#replanning-on-failure)
8. [Best Practices](#best-practices)
9. [Complete Example](#complete-example)

---

## Overview

HTN (Hierarchical Task Network) planning is an AI technique that decomposes high-level goals into executable actions. Unlike reactive AI systems (like behavior trees), HTN planners generate a complete plan before execution, allowing characters to reason about sequences of actions.

### Key Benefits

-   **Goal-oriented**: Characters work towards objectives, not just react to stimuli
-   **Hierarchical**: Complex behaviors decompose into simple actions
-   **Adaptable**: Plans can be regenerated when the world changes
-   **Debuggable**: You can inspect the plan to understand AI decisions

---

## Core Concepts

### Primitive Tasks

Primitive tasks are the atomic actions your AI can perform. They have:

-   **Preconditions**: Conditions that must be true for the task to execute
-   **Effects**: How the task changes the world (used during planning simulation)
-   **Operator**: The actual function that executes the task

```elixir
alias StellaInvicta.AI.HierarchicalTaskNetwork, as: HTN

# A simple primitive task
move_task = HTN.primitive(:move_to,
  preconditions: [
    fn world, params ->
      # Can only move if destination exists
      Map.has_key?(world.locations, params.destination)
    end
  ],
  effects: [
    fn world, params ->
      # During planning, simulate the move
      Map.put(world, :current_location, params.destination)
    end
  ],
  operator: fn world, params ->
    # Actually perform the move during execution
    {:ok, Map.put(world, :current_location, params.destination)}
  end
)
```

### Compound Tasks

Compound tasks represent high-level goals that decompose into subtasks via **methods**.

```elixir
travel_task = HTN.compound(:travel_to_city,
  methods: [
    # Method 1: If we have a horse, ride
    HTN.method(:ride_horse,
      priority: 10,
      conditions: [
        fn world, _params -> world.has_horse end
      ],
      subtasks: [
        {:mount_horse, %{}},
        {:ride_to, %{}}  # params inherited from parent
      ]
    ),
    # Method 2: Otherwise, walk
    HTN.method(:walk,
      priority: 0,
      subtasks: [
        {:walk_to, %{}}
      ]
    )
  ]
)
```

### Methods

Methods are alternative ways to accomplish a compound task. The planner tries methods in priority order (highest first) and selects the first one whose conditions are met.

```elixir
HTN.method(:method_name,
  priority: 10,           # Higher = tried first
  conditions: [...],      # When is this method applicable?
  subtasks: [...]         # What tasks does this decompose into?
)
```

### Domains

A domain is a collection of all tasks available to the planner.

```elixir
domain = HTN.new_domain("character_ai")
|> HTN.add_task(move_task)
|> HTN.add_task(attack_task)
|> HTN.add_task(travel_task)
```

---

## Creating Tasks

### Primitive Task Structure

```elixir
HTN.primitive(:task_name,
  # Preconditions: All must return true for task to be valid
  # Signature: (world, params) -> boolean
  preconditions: [
    fn world, params -> ... end
  ],

  # Effects: Applied during planning to simulate world changes
  # Signature: (world, params) -> updated_world
  effects: [
    fn world, params -> ... end
  ],

  # Operator: Executed during plan execution
  # Signature: (world, params) -> {:ok, world} | {:running, world} | {:error, reason}
  operator: fn world, params ->
    {:ok, updated_world}
  end
)
```

### Operator Return Values

| Return Value        | Meaning                                           |
| ------------------- | ------------------------------------------------- |
| `{:ok, world}`      | Task completed successfully, advance to next step |
| `{:running, world}` | Task still in progress, execute again next tick   |
| `{:error, reason}`  | Task failed, trigger replanning or failure        |

### Compound Task Structure

```elixir
HTN.compound(:task_name,
  methods: [
    HTN.method(:method_name,
      priority: 10,
      conditions: [
        fn world, params -> ... end
      ],
      subtasks: [
        {:subtask_name, %{param: value}},
        {:another_subtask, %{}}
      ]
    )
  ]
)
```

---

## Building Domains

### Basic Domain

```elixir
defmodule MyGame.AI.WarriorDomain do
  alias StellaInvicta.AI.HierarchicalTaskNetwork, as: HTN

  def create do
    HTN.new_domain("warrior")
    |> HTN.add_task(idle_task())
    |> HTN.add_task(attack_task())
    |> HTN.add_task(defend_task())
    |> HTN.add_task(combat_task())  # Compound
  end

  defp idle_task do
    HTN.primitive(:idle,
      operator: fn world, _params -> {:ok, world} end
    )
  end

  defp attack_task do
    HTN.primitive(:attack,
      preconditions: [
        fn world, _params -> world.has_weapon end,
        fn world, _params -> world.enemy_in_range end
      ],
      effects: [
        fn world, _params -> Map.update!(world, :enemy_health, &(&1 - 10)) end
      ],
      operator: fn world, _params ->
        # Perform attack logic
        {:ok, deal_damage(world)}
      end
    )
  end

  defp combat_task do
    HTN.compound(:engage_enemy,
      methods: [
        HTN.method(:aggressive,
          priority: 10,
          conditions: [fn world, _params -> world.health > 50 end],
          subtasks: [{:attack, %{}}, {:attack, %{}}]
        ),
        HTN.method(:defensive,
          priority: 5,
          subtasks: [{:defend, %{}}, {:attack, %{}}]
        )
      ]
    )
  end
end
```

### Domain Validation

Validate your domain at startup to catch typos in subtask references:

```elixir
domain = MyGame.AI.WarriorDomain.create()

case HTN.validate_domain(domain) do
  :ok ->
    IO.puts("Domain is valid!")
  {:error, errors} ->
    Enum.each(errors, &IO.puts/1)
    raise "Invalid domain!"
end
```

---

## Planning and Execution

### Finding a Plan

```elixir
# Create domain and world state
domain = MyGame.AI.WarriorDomain.create()
world = %{health: 100, has_weapon: true, enemy_in_range: true}

# Find a plan for the goal
case HTN.find_plan(domain, world, :engage_enemy) do
  {:ok, plan} ->
    IO.inspect(plan.steps)  # [{:attack, %{}}, {:attack, %{}}]

  {:error, reason} ->
    IO.puts("No plan found: #{inspect(reason)}")
end
```

### Planning with Parameters

```elixir
{:ok, plan} = HTN.find_plan(domain, world, :travel_to_city,
  params: %{destination: :berlin, character_id: 1}
)
```

### Execute Entire Plan at Once

```elixir
{:ok, plan} = HTN.find_plan(domain, world, :goal)
{:ok, final_world} = HTN.execute_plan(plan, domain, world)
```

### Execute Step by Step

```elixir
# Execute one step
case HTN.execute_step(plan, domain, world) do
  {:ok, updated_plan, new_world} ->
    # Step completed, plan advanced

  {:running, same_plan, new_world} ->
    # Task still running, will continue next tick

  {:complete, final_world} ->
    # Plan finished!

  {:error, reason, failed_plan, world} ->
    # Step failed
end
```

---

## Multi-Tick Execution

The HTN system supports plans that execute over multiple game ticks. This is essential for game AI where actions take time.

### How It Works

1. Each tick, the `CharacterAI` system calls `run/1`
2. For each character with a plan, it executes one step
3. The plan advances only when a step returns `{:ok, world}`
4. Steps returning `{:running, world}` continue next tick

### Example: Multi-Step Movement

```elixir
# Plan with 3 movement steps
plan = Plan.new([
  {:move_to, %{destination: :town_a}},
  {:move_to, %{destination: :town_b}},
  {:move_to, %{destination: :town_c}}
])

# Tick 1: Execute move to town_a
# Tick 2: Execute move to town_b
# Tick 3: Execute move to town_c, plan completes
```

### Example: Long-Running Task

```elixir
# A task that takes 3 ticks to complete
HTN.primitive(:gather_resources,
  operator: fn world, _params ->
    progress = Map.get(world, :gather_progress, 0) + 1

    if progress >= 3 do
      {:ok, world |> Map.put(:resources, world.resources + 10) |> Map.delete(:gather_progress)}
    else
      {:running, Map.put(world, :gather_progress, progress)}
    end
  end
)
```

---

## Replanning on Failure

The system can automatically replan when execution fails due to world state changes.

### Enable Replanning

```elixir
alias StellaInvicta.System.CharacterAI

world = world
|> CharacterAI.set_replan_settings(character_id, %{
  enabled: true,        # Enable automatic replanning
  max_attempts: 3,      # Give up after 3 failed replans
  fallback_goal: :idle  # Use this goal if main goal fails
})
|> CharacterAI.store_plan_goal(character_id, :travel_to_forest)
```

### How Replanning Works

1. A plan step fails (precondition not met, operator returns error)
2. If replanning is enabled, try to find a new plan for the same goal
3. The new plan may choose a different method based on current world state
4. If replanning fails, try the fallback goal
5. After max_attempts, give up and publish failure event

### Example: Adapting to Destroyed Location

```elixir
# Original plan: travel through forest
# World changes: forest gets destroyed
# Replan: finds alternate route or falls back to idle

domain = HTN.new_domain("travel")
|> HTN.add_task(HTN.primitive(:move_to,
  preconditions: [
    fn world, params -> Map.has_key?(world.locations, params.destination) end
  ],
  operator: fn world, params ->
    if Map.has_key?(world.locations, params.destination) do
      {:ok, Map.put(world, :location, params.destination)}
    else
      {:error, {:destination_destroyed, params.destination}}
    end
  end
))
|> HTN.add_task(HTN.compound(:travel_to_city,
  methods: [
    HTN.method(:via_forest,
      priority: 10,
      conditions: [fn world, _params -> Map.has_key?(world.locations, :forest) end],
      subtasks: [{:move_to, %{destination: :forest}}, {:move_to, %{destination: :city}}]
    ),
    HTN.method(:via_plains,
      priority: 5,
      subtasks: [{:move_to, %{destination: :plains}}, {:move_to, %{destination: :city}}]
    )
  ]
))
```

---

## Best Practices

### 1. Always Define Both Effects and Operators

Effects are used during **planning** to simulate state changes. Operators are used during **execution**. If you only define an operator, the planner can't simulate the task's impact.

```elixir
# ❌ Bad: No effects, planner can't simulate
HTN.primitive(:heal,
  operator: fn world, _params ->
    {:ok, Map.update!(world, :health, &(&1 + 20))}
  end
)

# ✅ Good: Effects mirror operator logic
HTN.primitive(:heal,
  effects: [
    fn world, _params -> Map.update!(world, :health, &(&1 + 20)) end
  ],
  operator: fn world, _params ->
    {:ok, Map.update!(world, :health, &(&1 + 20))}
  end
)
```

### 2. Use Preconditions to Guide Planning

Preconditions help the planner find valid plans and cause early failure during execution if the world changed.

```elixir
HTN.primitive(:open_door,
  preconditions: [
    fn world, params -> world.doors[params.door_id].locked == false end
  ],
  # ...
)
```

### 3. Keep Primitive Tasks Simple

Each primitive should do ONE thing. Complex behaviors emerge from compound task decomposition.

```elixir
# ❌ Bad: Too complex
HTN.primitive(:fight_enemy, ...)

# ✅ Good: Simple primitives, complex compound
HTN.primitive(:draw_weapon, ...)
HTN.primitive(:attack, ...)
HTN.primitive(:block, ...)
HTN.compound(:fight_enemy,
  methods: [
    HTN.method(:aggressive, subtasks: [{:draw_weapon, %{}}, {:attack, %{}}, {:attack, %{}}]),
    HTN.method(:defensive, subtasks: [{:draw_weapon, %{}}, {:block, %{}}, {:attack, %{}}])
  ]
)
```

### 4. Use Method Priority Wisely

Higher priority methods are tried first. Use this for preferred strategies:

```elixir
HTN.method(:optimal_strategy, priority: 100, ...)
HTN.method(:fallback_strategy, priority: 10, ...)
HTN.method(:last_resort, priority: 0, ...)
```

### 5. Validate Domains at Startup

Catch typos and missing tasks early:

```elixir
def start(_type, _args) do
  domain = create_ai_domain()
  :ok = HTN.validate_domain(domain)
  # ... rest of startup
end
```

---

## Complete Example

Here's a complete example of an AI character that can gather resources, craft items, and trade:

```elixir
defmodule MyGame.AI.MerchantDomain do
  alias StellaInvicta.AI.HierarchicalTaskNetwork, as: HTN

  def create do
    HTN.new_domain("merchant")
    |> HTN.add_task(idle_task())
    |> HTN.add_task(gather_wood_task())
    |> HTN.add_task(gather_ore_task())
    |> HTN.add_task(craft_sword_task())
    |> HTN.add_task(sell_item_task())
    |> HTN.add_task(make_money_task())  # High-level goal
  end

  # === Primitive Tasks ===

  defp idle_task do
    HTN.primitive(:idle,
      operator: fn world, _params -> {:ok, world} end
    )
  end

  defp gather_wood_task do
    HTN.primitive(:gather_wood,
      preconditions: [
        fn world, _params -> Map.has_key?(world.locations, :forest) end
      ],
      effects: [
        fn world, _params ->
          resources = Map.get(world, :resources, %{})
          Map.put(world, :resources, Map.update(resources, :wood, 1, &(&1 + 1)))
        end
      ],
      operator: fn world, _params ->
        resources = Map.get(world, :resources, %{})
        {:ok, Map.put(world, :resources, Map.update(resources, :wood, 1, &(&1 + 1)))}
      end
    )
  end

  defp gather_ore_task do
    HTN.primitive(:gather_ore,
      preconditions: [
        fn world, _params -> Map.has_key?(world.locations, :mine) end
      ],
      effects: [
        fn world, _params ->
          resources = Map.get(world, :resources, %{})
          Map.put(world, :resources, Map.update(resources, :ore, 1, &(&1 + 1)))
        end
      ],
      operator: fn world, _params ->
        resources = Map.get(world, :resources, %{})
        {:ok, Map.put(world, :resources, Map.update(resources, :ore, 1, &(&1 + 1)))}
      end
    )
  end

  defp craft_sword_task do
    HTN.primitive(:craft_sword,
      preconditions: [
        fn world, _params ->
          resources = Map.get(world, :resources, %{})
          Map.get(resources, :wood, 0) >= 1 and Map.get(resources, :ore, 0) >= 2
        end
      ],
      effects: [
        fn world, _params ->
          resources = Map.get(world, :resources, %{})
          inventory = Map.get(world, :inventory, [])

          world
          |> Map.put(:resources, resources |> Map.update!(:wood, &(&1 - 1)) |> Map.update!(:ore, &(&1 - 2)))
          |> Map.put(:inventory, [:sword | inventory])
        end
      ],
      operator: fn world, _params ->
        resources = Map.get(world, :resources, %{})
        inventory = Map.get(world, :inventory, [])

        new_world = world
        |> Map.put(:resources, resources |> Map.update!(:wood, &(&1 - 1)) |> Map.update!(:ore, &(&1 - 2)))
        |> Map.put(:inventory, [:sword | inventory])

        {:ok, new_world}
      end
    )
  end

  defp sell_item_task do
    HTN.primitive(:sell_sword,
      preconditions: [
        fn world, _params ->
          inventory = Map.get(world, :inventory, [])
          :sword in inventory
        end
      ],
      effects: [
        fn world, _params ->
          inventory = Map.get(world, :inventory, [])
          gold = Map.get(world, :gold, 0)

          world
          |> Map.put(:inventory, List.delete(inventory, :sword))
          |> Map.put(:gold, gold + 50)
        end
      ],
      operator: fn world, _params ->
        inventory = Map.get(world, :inventory, [])
        gold = Map.get(world, :gold, 0)

        new_world = world
        |> Map.put(:inventory, List.delete(inventory, :sword))
        |> Map.put(:gold, gold + 50)

        {:ok, new_world}
      end
    )
  end

  # === Compound Task (High-Level Goal) ===

  defp make_money_task do
    HTN.compound(:make_money,
      methods: [
        # If we have a sword, sell it
        HTN.method(:sell_existing,
          priority: 100,
          conditions: [
            fn world, _params ->
              inventory = Map.get(world, :inventory, [])
              :sword in inventory
            end
          ],
          subtasks: [{:sell_sword, %{}}]
        ),

        # If we have resources, craft and sell
        HTN.method(:craft_and_sell,
          priority: 50,
          conditions: [
            fn world, _params ->
              resources = Map.get(world, :resources, %{})
              Map.get(resources, :wood, 0) >= 1 and Map.get(resources, :ore, 0) >= 2
            end
          ],
          subtasks: [{:craft_sword, %{}}, {:sell_sword, %{}}]
        ),

        # Otherwise, gather resources first
        HTN.method(:gather_and_craft,
          priority: 10,
          subtasks: [
            {:gather_wood, %{}},
            {:gather_ore, %{}},
            {:gather_ore, %{}},
            {:craft_sword, %{}},
            {:sell_sword, %{}}
          ]
        )
      ]
    )
  end
end

# === Usage ===

# Create domain
domain = MyGame.AI.MerchantDomain.create()

# Initial world state
world = %{
  locations: %{forest: true, mine: true, market: true},
  resources: %{wood: 0, ore: 0},
  inventory: [],
  gold: 0
}

# Find a plan to make money
{:ok, plan} = HTN.find_plan(domain, world, :make_money)

IO.inspect(plan.steps)
# Output: [
#   {:gather_wood, %{}},
#   {:gather_ore, %{}},
#   {:gather_ore, %{}},
#   {:craft_sword, %{}},
#   {:sell_sword, %{}}
# ]

# Execute the plan (one step per tick in real game)
{:ok, final_world} = HTN.execute_plan(plan, domain, world)

IO.inspect(final_world.gold)  # 50
```

---

## Debugging Tips

### Enable Metrics

```elixir
metrics = HTN.new_metrics(capture_world_snapshots: true)

{:ok, plan, metrics} = HTN.find_plan_with_metrics(domain, world, :goal, metrics)

# View decision log
metrics
|> HTN.get_formatted_log(limit: 20)
|> Enum.each(&IO.puts/1)

# Get planning summary
HTN.get_planning_summary(metrics)
```

### Common Issues

| Issue                                | Cause                               | Solution                               |
| ------------------------------------ | ----------------------------------- | -------------------------------------- |
| `{:error, :no_plan_found}`           | No method conditions are satisfied  | Check your conditions and world state  |
| `{:error, :max_iterations_exceeded}` | Infinite loop in task decomposition | Check for circular task references     |
| Plan doesn't change world            | Missing effects on primitive tasks  | Add effects that mirror your operators |
| Replanning loops forever             | Same plan keeps failing             | Set `max_attempts` in replan settings  |

---

## Further Reading

-   `StellaInvicta.AI.HierarchicalTaskNetwork` - Main module documentation
-   `StellaInvicta.System.CharacterAI` - Game integration
-   `test/stella_invicta_htn_test.exs` - Comprehensive test examples
-   `test/stella_invicta_character_ai_test.exs` - Integration test examples
