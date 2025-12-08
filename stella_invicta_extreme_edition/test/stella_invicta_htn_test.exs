defmodule StellaInvicta.AI.HierarchicalTaskNetworkTest do
  use ExUnit.Case, async: true

  alias StellaInvicta.AI.HierarchicalTaskNetwork, as: HTN
  alias StellaInvicta.AI.HierarchicalTaskNetwork.{Task, Method, Domain, Plan, Planner}

  # =============================================================================
  # Task Tests
  # =============================================================================

  describe "Task.primitive/2" do
    test "creates a primitive task with defaults" do
      task = Task.primitive(:move)

      assert task.name == :move
      assert task.type == :primitive
      assert task.preconditions == []
      assert task.effects == []
      assert task.operator == nil
      assert task.params == %{}
    end

    test "creates a primitive task with options" do
      operator_fn = fn world, _params -> {:ok, world} end
      precond_fn = fn _world, _params -> true end
      effect_fn = fn world, _params -> Map.put(world, :moved, true) end

      task =
        Task.primitive(:move,
          preconditions: [precond_fn],
          effects: [effect_fn],
          operator: operator_fn,
          params: %{speed: 1}
        )

      assert task.name == :move
      assert task.type == :primitive
      assert length(task.preconditions) == 1
      assert length(task.effects) == 1
      assert task.operator != nil
      assert task.params == %{speed: 1}
    end
  end

  describe "Task.compound/2" do
    test "creates a compound task with defaults" do
      task = Task.compound(:travel)

      assert task.name == :travel
      assert task.type == :compound
      assert task.methods == []
      assert task.params == %{}
    end

    test "creates a compound task with methods" do
      method = Method.new(:walk, subtasks: [{:move, %{}}])
      task = Task.compound(:travel, methods: [method])

      assert task.name == :travel
      assert task.type == :compound
      assert length(task.methods) == 1
    end
  end

  describe "Task.preconditions_met?/3" do
    test "returns true when no preconditions" do
      task = Task.primitive(:idle)
      assert Task.preconditions_met?(task, %{}, %{})
    end

    test "returns true when all preconditions pass" do
      task =
        Task.primitive(:attack,
          preconditions: [
            fn world, _params -> world.has_weapon end,
            fn world, _params -> world.target_in_range end
          ]
        )

      world = %{has_weapon: true, target_in_range: true}
      assert Task.preconditions_met?(task, world, %{})
    end

    test "returns false when any precondition fails" do
      task =
        Task.primitive(:attack,
          preconditions: [
            fn world, _params -> world.has_weapon end,
            fn world, _params -> world.target_in_range end
          ]
        )

      world = %{has_weapon: true, target_in_range: false}
      refute Task.preconditions_met?(task, world, %{})
    end

    test "supports preconditions with params" do
      task =
        Task.primitive(:move_to,
          preconditions: [
            fn world, params -> world.location != params.destination end
          ]
        )

      world = %{location: :berlin}
      assert Task.preconditions_met?(task, world, %{destination: :neumark})
      refute Task.preconditions_met?(task, world, %{destination: :berlin})
    end
  end

  describe "Task.apply_effects/3" do
    test "returns world unchanged when no effects" do
      task = Task.primitive(:idle)
      world = %{value: 1}
      assert Task.apply_effects(task, world, %{}) == world
    end

    test "applies all effects to world" do
      task =
        Task.primitive(:heal,
          effects: [
            fn world, _params -> Map.update!(world, :health, &(&1 + 10)) end,
            fn world, _params -> Map.put(world, :healed, true) end
          ]
        )

      world = %{health: 50, healed: false}
      result = Task.apply_effects(task, world, %{})

      assert result.health == 60
      assert result.healed == true
    end

    test "supports effects with params" do
      task =
        Task.primitive(:move_to,
          effects: [
            fn world, params -> Map.put(world, :location, params.destination) end
          ]
        )

      world = %{location: :berlin}
      result = Task.apply_effects(task, world, %{destination: :neumark})

      assert result.location == :neumark
    end
  end

  # =============================================================================
  # Method Tests
  # =============================================================================

  describe "Method.new/2" do
    test "creates a method with defaults" do
      method = Method.new(:default_method)

      assert method.name == :default_method
      assert method.priority == 0
      assert method.conditions == []
      assert method.subtasks == []
    end

    test "creates a method with options" do
      method =
        Method.new(:fast_travel,
          priority: 10,
          conditions: [fn world, _params -> world.has_mount end],
          subtasks: [{:mount_horse, %{}}, {:ride, %{}}]
        )

      assert method.name == :fast_travel
      assert method.priority == 10
      assert length(method.conditions) == 1
      assert length(method.subtasks) == 2
    end
  end

  describe "Method.applicable?/3" do
    test "returns true when no conditions" do
      method = Method.new(:always_applicable)
      assert Method.applicable?(method, %{}, %{})
    end

    test "returns true when all conditions pass" do
      method =
        Method.new(:conditional,
          conditions: [
            fn world, _params -> world.a end,
            fn world, _params -> world.b end
          ]
        )

      assert Method.applicable?(method, %{a: true, b: true}, %{})
    end

    test "returns false when any condition fails" do
      method =
        Method.new(:conditional,
          conditions: [
            fn world, _params -> world.a end,
            fn world, _params -> world.b end
          ]
        )

      refute Method.applicable?(method, %{a: true, b: false}, %{})
    end

    test "supports conditions with params" do
      method =
        Method.new(:parameterized,
          conditions: [
            fn _world, params -> params.allowed end
          ]
        )

      assert Method.applicable?(method, %{}, %{allowed: true})
      refute Method.applicable?(method, %{}, %{allowed: false})
    end
  end

  # =============================================================================
  # Domain Tests
  # =============================================================================

  describe "Domain" do
    test "creates an empty domain" do
      domain = Domain.new("test_domain")

      assert domain.name == "test_domain"
      assert domain.tasks == %{}
    end

    test "adds tasks to domain" do
      domain =
        Domain.new("test")
        |> Domain.add_task(Task.primitive(:task_a))
        |> Domain.add_task(Task.primitive(:task_b))

      assert length(Domain.list_tasks(domain)) == 2
      assert :task_a in Domain.list_tasks(domain)
      assert :task_b in Domain.list_tasks(domain)
    end

    test "gets task by name" do
      domain =
        Domain.new("test")
        |> Domain.add_task(Task.primitive(:my_task))

      assert {:ok, task} = Domain.get_task(domain, :my_task)
      assert task.name == :my_task
    end

    test "returns error for unknown task" do
      domain = Domain.new("test")
      assert {:error, :task_not_found} = Domain.get_task(domain, :unknown)
    end
  end

  # =============================================================================
  # Plan Tests
  # =============================================================================

  describe "Plan" do
    test "creates a new plan" do
      steps = [{:step1, %{}}, {:step2, %{}}]
      plan = Plan.new(steps)

      assert plan.steps == steps
      assert plan.status == :pending
      assert plan.current_step == 0
    end

    test "reports length correctly" do
      plan = Plan.new([{:a, %{}}, {:b, %{}}, {:c, %{}}])
      assert Plan.length(plan) == 3
    end

    test "detects empty plan" do
      assert Plan.empty?(Plan.new([]))
      refute Plan.empty?(Plan.new([{:a, %{}}]))
    end

    test "gets current step" do
      plan = Plan.new([{:first, %{x: 1}}, {:second, %{}}])
      assert Plan.current(plan) == {:first, %{x: 1}}
    end

    test "advances to next step" do
      plan = Plan.new([{:a, %{}}, {:b, %{}}, {:c, %{}}])

      plan = Plan.advance(plan)
      assert plan.current_step == 1
      assert plan.status == :executing
      assert Plan.current(plan) == {:b, %{}}

      plan = Plan.advance(plan)
      assert plan.current_step == 2
      assert plan.status == :executing

      plan = Plan.advance(plan)
      assert plan.status == :completed
    end

    test "marks plan as failed" do
      plan = Plan.new([{:a, %{}}])
      plan = Plan.fail(plan)
      assert plan.status == :failed
    end
  end

  # =============================================================================
  # Planner Tests - Basic Planning
  # =============================================================================

  describe "Planner.find_plan/4 - primitive tasks" do
    test "finds plan for single primitive task" do
      domain =
        Domain.new("test")
        |> Domain.add_task(Task.primitive(:do_thing))

      {:ok, plan} = Planner.find_plan(domain, %{}, :do_thing)

      assert Plan.length(plan) == 1
      assert [{:do_thing, %{}}] = plan.steps
    end

    test "finds plan with preconditions" do
      domain =
        Domain.new("test")
        |> Domain.add_task(
          Task.primitive(:conditional_task,
            preconditions: [fn world, _params -> world.ready end]
          )
        )

      # Precondition met
      {:ok, plan} = Planner.find_plan(domain, %{ready: true}, :conditional_task)
      assert Plan.length(plan) == 1

      # Precondition not met
      {:error, :no_plan_found} = Planner.find_plan(domain, %{ready: false}, :conditional_task)
    end

    test "passes params to task" do
      domain =
        Domain.new("test")
        |> Domain.add_task(
          Task.primitive(:parameterized,
            preconditions: [fn _world, params -> params.value > 0 end]
          )
        )

      {:ok, plan} = Planner.find_plan(domain, %{}, :parameterized, params: %{value: 5})
      assert [{:parameterized, %{value: 5}}] = plan.steps
    end
  end

  describe "Planner.find_plan/4 - compound tasks" do
    test "decomposes compound task into subtasks" do
      domain =
        Domain.new("test")
        |> Domain.add_task(Task.primitive(:step_a))
        |> Domain.add_task(Task.primitive(:step_b))
        |> Domain.add_task(
          Task.compound(:do_both,
            methods: [
              Method.new(:sequential,
                subtasks: [{:step_a, %{}}, {:step_b, %{}}]
              )
            ]
          )
        )

      {:ok, plan} = Planner.find_plan(domain, %{}, :do_both)

      assert Plan.length(plan) == 2
      assert [{:step_a, %{}}, {:step_b, %{}}] = plan.steps
    end

    test "selects method based on conditions" do
      domain =
        Domain.new("test")
        |> Domain.add_task(Task.primitive(:walk))
        |> Domain.add_task(Task.primitive(:run))
        |> Domain.add_task(
          Task.compound(:move,
            methods: [
              Method.new(:method_run,
                conditions: [fn world, _params -> world.energy > 50 end],
                subtasks: [{:run, %{}}]
              ),
              Method.new(:method_walk,
                subtasks: [{:walk, %{}}]
              )
            ]
          )
        )

      # High energy - should run
      {:ok, plan} = Planner.find_plan(domain, %{energy: 80}, :move)
      assert [{:run, %{}}] = plan.steps

      # Low energy - should walk
      {:ok, plan} = Planner.find_plan(domain, %{energy: 30}, :move)
      assert [{:walk, %{}}] = plan.steps
    end

    test "respects method priority" do
      domain =
        Domain.new("test")
        |> Domain.add_task(Task.primitive(:option_a))
        |> Domain.add_task(Task.primitive(:option_b))
        |> Domain.add_task(
          Task.compound(:choose,
            methods: [
              Method.new(:low_priority,
                priority: 1,
                subtasks: [{:option_a, %{}}]
              ),
              Method.new(:high_priority,
                priority: 10,
                subtasks: [{:option_b, %{}}]
              )
            ]
          )
        )

      {:ok, plan} = Planner.find_plan(domain, %{}, :choose)
      # High priority method should be chosen
      assert [{:option_b, %{}}] = plan.steps
    end

    test "handles nested compound tasks" do
      domain =
        Domain.new("test")
        |> Domain.add_task(Task.primitive(:action_1))
        |> Domain.add_task(Task.primitive(:action_2))
        |> Domain.add_task(Task.primitive(:action_3))
        |> Domain.add_task(
          Task.compound(:inner_task,
            methods: [
              Method.new(:inner_method,
                subtasks: [{:action_2, %{}}, {:action_3, %{}}]
              )
            ]
          )
        )
        |> Domain.add_task(
          Task.compound(:outer_task,
            methods: [
              Method.new(:outer_method,
                subtasks: [{:action_1, %{}}, {:inner_task, %{}}]
              )
            ]
          )
        )

      {:ok, plan} = Planner.find_plan(domain, %{}, :outer_task)

      assert Plan.length(plan) == 3
      assert [{:action_1, %{}}, {:action_2, %{}}, {:action_3, %{}}] = plan.steps
    end
  end

  describe "Planner.find_plan/4 - backtracking" do
    test "backtracks when method leads to failure" do
      domain =
        Domain.new("test")
        |> Domain.add_task(
          Task.primitive(:requires_key,
            preconditions: [fn world, _params -> world.has_key end]
          )
        )
        |> Domain.add_task(Task.primitive(:pick_lock))
        |> Domain.add_task(
          Task.compound(:open_door,
            methods: [
              # First try to use key (will fail)
              Method.new(:use_key,
                priority: 10,
                subtasks: [{:requires_key, %{}}]
              ),
              # Fallback to picking lock
              Method.new(:lockpick,
                priority: 1,
                subtasks: [{:pick_lock, %{}}]
              )
            ]
          )
        )

      # No key - should backtrack and pick lock
      {:ok, plan} = Planner.find_plan(domain, %{has_key: false}, :open_door)
      assert [{:pick_lock, %{}}] = plan.steps
    end

    test "fails when no method works" do
      domain =
        Domain.new("test")
        |> Domain.add_task(
          Task.primitive(:requires_a,
            preconditions: [fn world, _params -> world.has_a end]
          )
        )
        |> Domain.add_task(
          Task.primitive(:requires_b,
            preconditions: [fn world, _params -> world.has_b end]
          )
        )
        |> Domain.add_task(
          Task.compound(:need_something,
            methods: [
              Method.new(:try_a, subtasks: [{:requires_a, %{}}]),
              Method.new(:try_b, subtasks: [{:requires_b, %{}}])
            ]
          )
        )

      # Has neither a nor b
      {:error, :no_plan_found} =
        Planner.find_plan(domain, %{has_a: false, has_b: false}, :need_something)
    end
  end

  describe "Planner.find_plan/4 - effects during planning" do
    test "applies effects for simulated world state" do
      domain =
        Domain.new("test")
        |> Domain.add_task(
          Task.primitive(:get_key,
            effects: [fn world, _params -> Map.put(world, :has_key, true) end]
          )
        )
        |> Domain.add_task(
          Task.primitive(:unlock_door,
            preconditions: [fn world, _params -> world.has_key end]
          )
        )
        |> Domain.add_task(
          Task.compound(:open_locked_door,
            methods: [
              Method.new(:get_key_then_unlock,
                subtasks: [{:get_key, %{}}, {:unlock_door, %{}}]
              )
            ]
          )
        )

      # Initially no key, but get_key effect provides it
      {:ok, plan} = Planner.find_plan(domain, %{has_key: false}, :open_locked_door)
      assert [{:get_key, %{}}, {:unlock_door, %{}}] = plan.steps
    end
  end

  describe "Planner.find_plan/4 - error cases" do
    test "returns error for unknown task" do
      domain = Domain.new("test")
      {:error, {:task_not_found, :nonexistent}} = Planner.find_plan(domain, %{}, :nonexistent)
    end

    test "respects max iterations" do
      # Create a domain that would loop forever
      domain =
        Domain.new("test")
        |> Domain.add_task(
          Task.compound(:loop,
            methods: [
              Method.new(:recurse, subtasks: [{:loop, %{}}])
            ]
          )
        )

      {:error, :max_depth_exceeded} =
        Planner.find_plan(domain, %{}, :loop, max_iterations: 100)
    end
  end

  # =============================================================================
  # Planner Tests - Execution
  # =============================================================================

  describe "Planner.execute_plan/3" do
    test "executes all steps in order" do
      domain =
        Domain.new("test")
        |> Domain.add_task(
          Task.primitive(:increment,
            operator: fn world, _params ->
              {:ok, Map.update!(world, :counter, &(&1 + 1))}
            end
          )
        )

      plan = Plan.new([{:increment, %{}}, {:increment, %{}}, {:increment, %{}}])

      {:ok, result} = Planner.execute_plan(plan, domain, %{counter: 0})
      assert result.counter == 3
    end

    test "applies effects when no operator" do
      domain =
        Domain.new("test")
        |> Domain.add_task(
          Task.primitive(:set_flag,
            effects: [fn world, _params -> Map.put(world, :flag, true) end]
          )
        )

      plan = Plan.new([{:set_flag, %{}}])

      {:ok, result} = Planner.execute_plan(plan, domain, %{flag: false})
      assert result.flag == true
    end

    test "stops on operator failure" do
      domain =
        Domain.new("test")
        |> Domain.add_task(
          Task.primitive(:succeed,
            operator: fn world, _params -> {:ok, Map.put(world, :step1, true)} end
          )
        )
        |> Domain.add_task(
          Task.primitive(:fail,
            operator: fn _world, _params -> {:error, :intentional_failure} end
          )
        )
        |> Domain.add_task(
          Task.primitive(:never_reached,
            operator: fn world, _params -> {:ok, Map.put(world, :step3, true)} end
          )
        )

      plan = Plan.new([{:succeed, %{}}, {:fail, %{}}, {:never_reached, %{}}])

      {:error, :intentional_failure, world} = Planner.execute_plan(plan, domain, %{})
      assert world.step1 == true
      refute Map.has_key?(world, :step3)
    end
  end

  describe "Planner.execute_step/3" do
    test "executes one step at a time" do
      domain =
        Domain.new("test")
        |> Domain.add_task(
          Task.primitive(:step,
            operator: fn world, params ->
              {:ok, Map.put(world, params.key, true)}
            end
          )
        )

      plan = Plan.new([{:step, %{key: :a}}, {:step, %{key: :b}}])
      world = %{}

      {:ok, plan, world} = Planner.execute_step(plan, domain, world)
      assert world.a == true
      refute Map.has_key?(world, :b)
      assert plan.current_step == 1

      {:ok, plan, world} = Planner.execute_step(plan, domain, world)
      assert world.a == true
      assert world.b == true
      assert plan.status == :completed

      {:complete, ^world} = Planner.execute_step(plan, domain, world)
    end

    test "returns error on step failure" do
      domain =
        Domain.new("test")
        |> Domain.add_task(
          Task.primitive(:failing_step,
            operator: fn _world, _params -> {:error, :step_failed} end
          )
        )

      plan = Plan.new([{:failing_step, %{}}])

      {:error, :step_failed, failed_plan, _world} = Planner.execute_step(plan, domain, %{})
      assert failed_plan.status == :failed
    end
  end

  # =============================================================================
  # Convenience Function Tests
  # =============================================================================

  describe "HTN convenience functions" do
    test "primitive/2 delegates to Task" do
      task = HTN.primitive(:test_task)
      assert task.name == :test_task
      assert task.type == :primitive
    end

    test "compound/2 delegates to Task" do
      task = HTN.compound(:test_compound)
      assert task.name == :test_compound
      assert task.type == :compound
    end

    test "method/2 delegates to Method" do
      method = HTN.method(:test_method, priority: 5)
      assert method.name == :test_method
      assert method.priority == 5
    end

    test "new_domain/1 delegates to Domain" do
      domain = HTN.new_domain("test")
      assert domain.name == "test"
    end

    test "add_task/2 delegates to Domain" do
      domain =
        HTN.new_domain("test")
        |> HTN.add_task(HTN.primitive(:task))

      assert :task in Domain.list_tasks(domain)
    end

    test "find_plan/4 delegates to Planner" do
      domain =
        HTN.new_domain("test")
        |> HTN.add_task(HTN.primitive(:action))

      {:ok, plan} = HTN.find_plan(domain, %{}, :action)
      assert Plan.length(plan) == 1
    end

    test "execute_plan/3 delegates to Planner" do
      domain =
        HTN.new_domain("test")
        |> HTN.add_task(
          HTN.primitive(:set_value,
            effects: [fn world, _params -> Map.put(world, :done, true) end]
          )
        )

      plan = Plan.new([{:set_value, %{}}])
      {:ok, result} = HTN.execute_plan(plan, domain, %{})
      assert result.done == true
    end
  end

  # =============================================================================
  # Integration Tests with Game World
  # =============================================================================

  describe "integration with StellaInvicta.World" do
    setup do
      world = StellaInvicta.World.new_planet_world()
      {:ok, world: world}
    end

    test "can plan with game world state", %{world: world} do
      # Define a domain for character actions
      domain =
        HTN.new_domain("character_actions")
        |> HTN.add_task(
          HTN.primitive(:check_location,
            preconditions: [
              fn world, _params -> Map.has_key?(world.locations, 1) end
            ]
          )
        )

      {:ok, plan} = HTN.find_plan(domain, world, :check_location)
      assert Plan.length(plan) == 1
    end

    test "can modify world state through execution", %{world: world} do
      domain =
        HTN.new_domain("world_modification")
        |> HTN.add_task(
          HTN.primitive(:advance_tick,
            operator: fn world, _params ->
              {:ok, %{world | current_tick: world.current_tick + 1}}
            end
          )
        )

      {:ok, plan} = HTN.find_plan(domain, world, :advance_tick)
      {:ok, new_world} = HTN.execute_plan(plan, domain, world)

      assert new_world.current_tick == world.current_tick + 1
    end

    test "complex character movement planning", %{world: world} do
      # Domain for character movement
      domain =
        HTN.new_domain("character_movement")
        # Primitive: Move to adjacent location
        |> HTN.add_task(
          HTN.primitive(:move_to_adjacent,
            preconditions: [
              fn world, params ->
                # Check if destination is adjacent to current location
                current = Map.get(world, :character_location)
                dest = params.destination
                connections = Map.get(world.connections, current, [])
                dest in connections
              end
            ],
            effects: [
              fn world, params ->
                # Update character location in effects (for planning simulation)
                Map.put(world, :character_location, params.destination)
              end
            ],
            operator: fn world, params ->
              # Actually update the character's location
              {:ok, Map.put(world, :character_location, params.destination)}
            end
          )
        )
        # Compound: Travel (handles pathfinding)
        |> HTN.add_task(
          HTN.compound(:travel_one_hop,
            methods: [
              HTN.method(:already_there,
                priority: 100,
                conditions: [
                  fn world, params ->
                    Map.get(world, :character_location) == params.destination
                  end
                ],
                subtasks: []
              ),
              HTN.method(:move_adjacent,
                priority: 50,
                conditions: [
                  fn world, params ->
                    current = Map.get(world, :character_location)
                    dest = params.destination
                    connections = Map.get(world.connections, current, [])
                    dest in connections
                  end
                ],
                subtasks: [
                  {:move_to_adjacent, %{destination: 2}}
                ]
              )
            ]
          )
        )

      # Character at Berlin (1), wants to go to Neumark (2)
      test_world = Map.put(world, :character_location, 1)

      {:ok, plan} =
        HTN.find_plan(domain, test_world, :travel_one_hop, params: %{destination: 2})

      # Should have one move action
      assert Plan.length(plan) == 1

      # Execute and verify
      {:ok, result} =
        HTN.execute_plan(plan, domain, %{
          test_world
          | character_location: 1
        })

      assert result.character_location == 2
    end

    test "character decision making based on traits", %{world: world} do
      # Domain for character behavior based on traits
      domain =
        HTN.new_domain("character_behavior")
        |> HTN.add_task(HTN.primitive(:study))
        |> HTN.add_task(HTN.primitive(:train))
        |> HTN.add_task(HTN.primitive(:rest))
        |> HTN.add_task(
          HTN.compound(:daily_activity,
            methods: [
              HTN.method(:scholar_studies,
                priority: 10,
                conditions: [
                  fn world, params ->
                    char_id = params.character_id
                    traits = Map.get(world.character_traits, char_id, [])
                    :scholar in traits
                  end
                ],
                subtasks: [{:study, %{}}]
              ),
              HTN.method(:brave_trains,
                priority: 10,
                conditions: [
                  fn world, params ->
                    char_id = params.character_id
                    traits = Map.get(world.character_traits, char_id, [])
                    :brave in traits
                  end
                ],
                subtasks: [{:train, %{}}]
              ),
              HTN.method(:default_rest,
                priority: 0,
                subtasks: [{:rest, %{}}]
              )
            ]
          )
        )

      # Charlemagne (id: 1) has traits [:brave, :scholar]
      # Scholar method should be selected due to same priority but listed first
      {:ok, plan} = HTN.find_plan(domain, world, :daily_activity, params: %{character_id: 1})

      # Should select study (scholar trait)
      [{task_name, _}] = plan.steps
      assert task_name == :study

      # Character without scholar trait but with brave
      world_brave_only = %{world | character_traits: %{1 => [:brave]}}

      {:ok, plan} =
        HTN.find_plan(domain, world_brave_only, :daily_activity, params: %{character_id: 1})

      [{task_name, _}] = plan.steps
      assert task_name == :train

      # Character with no special traits - falls back to rest
      world_no_traits = %{world | character_traits: %{}}

      {:ok, plan} =
        HTN.find_plan(domain, world_no_traits, :daily_activity, params: %{character_id: 1})

      [{task_name, _}] = plan.steps
      assert task_name == :rest
    end
  end
end
