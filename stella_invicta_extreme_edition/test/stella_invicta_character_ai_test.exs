defmodule StellaInvicta.System.CharacterAITest do
  use ExUnit.Case, async: true

  alias StellaInvicta.System.CharacterAI
  alias StellaInvicta.AI.HierarchicalTaskNetwork, as: HTN
  alias StellaInvicta.AI.HierarchicalTaskNetwork.Plan

  describe "plan management" do
    setup do
      world = StellaInvicta.World.new_planet_world()
      {:ok, world: world}
    end

    test "stores and retrieves character plans", %{world: world} do
      plan = Plan.new([{:idle, %{}}])

      world = CharacterAI.store_character_plan(world, 1, plan)

      retrieved = CharacterAI.get_character_plan(world, 1)
      assert retrieved == plan
    end

    test "returns nil for character without plan", %{world: world} do
      assert CharacterAI.get_character_plan(world, 999) == nil
    end

    test "clears character plan", %{world: world} do
      plan = Plan.new([{:idle, %{}}])

      world =
        world
        |> CharacterAI.store_character_plan(1, plan)
        |> CharacterAI.clear_character_plan(1)

      assert CharacterAI.get_character_plan(world, 1) == nil
    end

    test "gets all character plans", %{world: world} do
      plan1 = Plan.new([{:idle, %{}}])
      plan2 = Plan.new([{:rest, %{}}])

      world =
        world
        |> CharacterAI.store_character_plan(1, plan1)
        |> CharacterAI.store_character_plan(2, plan2)

      all_plans = CharacterAI.get_all_character_plans(world)

      assert map_size(all_plans) == 2
      assert Map.has_key?(all_plans, 1)
      assert Map.has_key?(all_plans, 2)
    end
  end

  describe "domain management" do
    setup do
      world = StellaInvicta.World.new_planet_world()
      {:ok, world: world}
    end

    test "returns default domain for character without custom domain", %{world: world} do
      {:ok, domain} = CharacterAI.get_character_domain(world, 1)

      assert domain.name == "default_character"
      assert :idle in HTN.Domain.list_tasks(domain)
      assert :rest in HTN.Domain.list_tasks(domain)
      assert :daily_routine in HTN.Domain.list_tasks(domain)
    end

    test "sets and retrieves custom domain", %{world: world} do
      custom_domain =
        HTN.new_domain("custom")
        |> HTN.add_task(HTN.primitive(:custom_task))

      world = CharacterAI.set_character_domain(world, 1, custom_domain)

      {:ok, retrieved} = CharacterAI.get_character_domain(world, 1)
      assert retrieved.name == "custom"
      assert :custom_task in HTN.Domain.list_tasks(retrieved)
    end
  end

  describe "default domain tasks" do
    setup do
      world = StellaInvicta.World.new_planet_world()
      {:ok, domain} = CharacterAI.get_character_domain(world, 1)
      {:ok, world: world, domain: domain}
    end

    test "idle task works", %{world: world, domain: domain} do
      {:ok, plan} = HTN.find_plan(domain, world, :idle)

      assert Plan.length(plan) == 1
      assert [{:idle, _}] = plan.steps
    end

    test "rest task works", %{world: world, domain: domain} do
      {:ok, plan} = HTN.find_plan(domain, world, :rest)

      assert Plan.length(plan) == 1
      assert [{:rest, _}] = plan.steps
    end

    test "study task requires scholar trait", %{world: world, domain: domain} do
      # Character 1 has scholar trait
      context = CharacterAI.prepare_planning_context(world, 1)
      {:ok, plan} = HTN.find_plan(domain, context, :study)
      assert Plan.length(plan) == 1

      # Character 2 does not have scholar trait
      context2 = CharacterAI.prepare_planning_context(world, 2)
      {:error, :no_plan_found} = HTN.find_plan(domain, context2, :study)
    end

    test "train task requires brave trait", %{world: world, domain: domain} do
      # Character 1 has brave trait
      context = CharacterAI.prepare_planning_context(world, 1)
      {:ok, plan} = HTN.find_plan(domain, context, :train)
      assert Plan.length(plan) == 1

      # Character 2 does not have brave trait
      context2 = CharacterAI.prepare_planning_context(world, 2)
      {:error, :no_plan_found} = HTN.find_plan(domain, context2, :train)
    end

    test "daily_routine selects based on traits", %{world: world, domain: domain} do
      # Charlemagne (1) has both scholar and brave - scholar has same priority but comes first
      context = CharacterAI.prepare_planning_context(world, 1)
      {:ok, plan} = HTN.find_plan(domain, context, :daily_routine)
      [{task_name, _}] = plan.steps
      assert task_name == :study

      # Character without traits falls back to rest
      world_no_traits = %{world | character_traits: %{}}
      context_no_traits = CharacterAI.prepare_planning_context(world_no_traits, 1)

      {:ok, plan} =
        HTN.find_plan(domain, context_no_traits, :daily_routine)

      [{task_name, _}] = plan.steps
      assert task_name == :rest
    end
  end

  describe "plan execution integration" do
    setup do
      world = StellaInvicta.World.new_planet_world()
      {:ok, domain} = CharacterAI.get_character_domain(world, 1)
      {:ok, world: world, domain: domain}
    end

    test "rest task restores health", %{world: world, domain: domain} do
      # Set initial health
      world = put_in(world.characters[1].health, 50)

      context = CharacterAI.prepare_planning_context(world, 1)
      {:ok, plan} = HTN.find_plan(domain, context, :rest)
      {:ok, new_context} = HTN.execute_plan(plan, domain, context)

      # Extract the updated character from the context
      assert new_context.characters[1].health == 55
    end

    test "study task improves stewardship", %{world: world, domain: domain} do
      # Set initial stewardship
      world = put_in(world.characters[1].stewardship, 10)

      context = CharacterAI.prepare_planning_context(world, 1)
      {:ok, plan} = HTN.find_plan(domain, context, :study)
      {:ok, new_context} = HTN.execute_plan(plan, domain, context)

      assert new_context.characters[1].stewardship == 11
    end

    test "train task improves martial", %{world: world, domain: domain} do
      # Set initial martial
      world = put_in(world.characters[1].martial, 10)

      context = CharacterAI.prepare_planning_context(world, 1)
      {:ok, plan} = HTN.find_plan(domain, context, :train)
      {:ok, new_context} = HTN.execute_plan(plan, domain, context)

      assert new_context.characters[1].martial == 11
    end
  end

  describe "system run/1" do
    setup do
      world = StellaInvicta.World.new_planet_world()
      {:ok, world: world}
    end

    test "executes pending character plans", %{world: world} do
      {:ok, _domain} = CharacterAI.get_character_domain(world, 1)

      # Create a simple plan
      plan = Plan.new([{:idle, %{character_id: 1}}])

      # Store the plan and domain
      world =
        world
        |> CharacterAI.store_character_plan(1, plan)

      # Run the system
      new_world = CharacterAI.run(world)

      # Plan should have advanced
      updated_plan = CharacterAI.get_character_plan(new_world, 1)

      # After running idle (single step plan), it should complete and be cleared
      # or be marked as completed
      assert updated_plan == nil || updated_plan.status == :completed
    end

    test "returns world unchanged when no plans", %{world: world} do
      result = CharacterAI.run(world)
      assert result == world
    end
  end

  describe "message handling" do
    setup do
      world = StellaInvicta.World.new_planet_world()
      {:ok, world: world}
    end

    test "handles character_needs_plan message", %{world: world} do
      new_world =
        CharacterAI.handle_message(world, :character_events, {:character_needs_plan, 1})

      # Should have a plan stored for character 1
      plan = CharacterAI.get_character_plan(new_world, 1)
      assert plan != nil
    end

    test "handles new_day message", %{world: world} do
      # Currently just returns world unchanged
      result = CharacterAI.handle_message(world, :date_events, {:new_day, 1})
      assert result == world
    end

    test "ignores unknown messages", %{world: world} do
      result = CharacterAI.handle_message(world, :unknown_topic, :unknown_message)
      assert result == world
    end
  end

  describe "subscriptions" do
    test "subscribes to character and date events" do
      subs = CharacterAI.subscriptions()

      assert :character_events in subs
      assert :date_events in subs
    end
  end

  describe "multi-tick plan execution" do
    setup do
      world = StellaInvicta.World.new_planet_world()
      {:ok, world: world}
    end

    test "multi-step plan executes one step per tick", %{world: world} do
      # Create a domain with a simple task that tracks execution
      domain =
        HTN.new_domain("multi_step_test")
        |> HTN.add_task(
          HTN.primitive(:step,
            operator: fn world, params ->
              step_num = params.step
              steps_executed = Map.get(world, :steps_executed, [])
              {:ok, Map.put(world, :steps_executed, steps_executed ++ [step_num])}
            end
          )
        )

      # Create a plan with 3 steps
      plan =
        Plan.new([
          {:step, %{step: 1}},
          {:step, %{step: 2}},
          {:step, %{step: 3}}
        ])

      world =
        world
        |> CharacterAI.set_character_domain(1, domain)
        |> CharacterAI.store_character_plan(1, plan)

      # Tick 1: Execute first step
      world = CharacterAI.run(world)
      assert Map.get(world, :steps_executed) == [1]
      plan_after_tick1 = CharacterAI.get_character_plan(world, 1)
      assert plan_after_tick1.current_step == 1

      # Tick 2: Execute second step
      world = CharacterAI.run(world)
      assert Map.get(world, :steps_executed) == [1, 2]
      plan_after_tick2 = CharacterAI.get_character_plan(world, 1)
      assert plan_after_tick2.current_step == 2

      # Tick 3: Execute third step (plan should complete)
      world = CharacterAI.run(world)
      assert Map.get(world, :steps_executed) == [1, 2, 3]
      # Plan should be cleared after completion
      assert CharacterAI.get_character_plan(world, 1) == nil
    end

    test "running task stays on same step until complete", %{world: world} do
      # Create a task that takes 3 ticks to complete
      domain =
        HTN.new_domain("running_test")
        |> HTN.add_task(
          HTN.primitive(:long_task,
            operator: fn world, _params ->
              ticks = Map.get(world, :task_ticks, 0) + 1

              if ticks >= 3 do
                # Complete after 3 ticks
                {:ok, Map.put(world, :task_ticks, ticks)}
              else
                # Still running
                {:running, Map.put(world, :task_ticks, ticks)}
              end
            end
          )
        )
        |> HTN.add_task(
          HTN.primitive(:next_task,
            operator: fn world, _params ->
              {:ok, Map.put(world, :next_task_executed, true)}
            end
          )
        )

      # Plan with running task followed by another task
      plan =
        Plan.new([
          {:long_task, %{}},
          {:next_task, %{}}
        ])

      world =
        world
        |> CharacterAI.set_character_domain(1, domain)
        |> CharacterAI.store_character_plan(1, plan)

      # Tick 1: Task still running
      world = CharacterAI.run(world)
      assert Map.get(world, :task_ticks) == 1
      plan_after_tick1 = CharacterAI.get_character_plan(world, 1)
      # Still on first step
      assert plan_after_tick1.current_step == 0

      # Tick 2: Task still running
      world = CharacterAI.run(world)
      assert Map.get(world, :task_ticks) == 2
      plan_after_tick2 = CharacterAI.get_character_plan(world, 1)
      # Still on first step
      assert plan_after_tick2.current_step == 0

      # Tick 3: Task completes, moves to next step
      world = CharacterAI.run(world)
      assert Map.get(world, :task_ticks) == 3
      plan_after_tick3 = CharacterAI.get_character_plan(world, 1)
      # Now on second step
      assert plan_after_tick3.current_step == 1

      # Tick 4: Execute next_task
      world = CharacterAI.run(world)
      assert Map.get(world, :next_task_executed) == true
      # Plan should be cleared
      assert CharacterAI.get_character_plan(world, 1) == nil
    end

    test "movement takes multiple ticks (2 moves = 2 ticks)", %{world: world} do
      # Simulate character movement where each move takes one tick
      domain =
        HTN.new_domain("movement_test")
        |> HTN.add_task(
          HTN.primitive(:move,
            operator: fn world, params ->
              destination = params.destination
              move_history = Map.get(world, :move_history, [])
              new_history = move_history ++ [destination]
              {:ok, Map.put(world, :move_history, new_history) |> Map.put(:location, destination)}
            end
          )
        )

      # Plan with 2 moves: Berlin -> Neumark -> Black Forest
      plan =
        Plan.new([
          {:move, %{destination: :neumark}},
          {:move, %{destination: :black_forest}}
        ])

      world =
        world
        |> Map.put(:location, :berlin)
        |> CharacterAI.set_character_domain(1, domain)
        |> CharacterAI.store_character_plan(1, plan)

      # Tick 1: First move
      world = CharacterAI.run(world)
      assert world.location == :neumark
      assert Map.get(world, :move_history) == [:neumark]
      assert CharacterAI.get_character_plan(world, 1) != nil

      # Tick 2: Second move (plan completes)
      world = CharacterAI.run(world)
      assert world.location == :black_forest
      assert Map.get(world, :move_history) == [:neumark, :black_forest]
      assert CharacterAI.get_character_plan(world, 1) == nil
    end

    test "multiple characters execute plans independently", %{world: world} do
      domain =
        HTN.new_domain("multi_char_test")
        |> HTN.add_task(
          HTN.primitive(:action,
            operator: fn world, params ->
              char_id = params.character_id
              actions = Map.get(world, :character_actions, %{})
              char_actions = Map.get(actions, char_id, [])
              new_actions = Map.put(actions, char_id, char_actions ++ [params.action])
              {:ok, Map.put(world, :character_actions, new_actions)}
            end
          )
        )

      # Character 1 has 2-step plan
      plan1 =
        Plan.new([
          {:action, %{character_id: 1, action: :first}},
          {:action, %{character_id: 1, action: :second}}
        ])

      # Character 2 has 3-step plan
      plan2 =
        Plan.new([
          {:action, %{character_id: 2, action: :a}},
          {:action, %{character_id: 2, action: :b}},
          {:action, %{character_id: 2, action: :c}}
        ])

      world =
        world
        |> CharacterAI.set_character_domain(1, domain)
        |> CharacterAI.set_character_domain(2, domain)
        |> CharacterAI.store_character_plan(1, plan1)
        |> CharacterAI.store_character_plan(2, plan2)

      # Tick 1: Both execute first step
      world = CharacterAI.run(world)
      actions = Map.get(world, :character_actions, %{})
      assert Map.get(actions, 1) == [:first]
      assert Map.get(actions, 2) == [:a]

      # Tick 2: Both execute second step (char 1 completes)
      world = CharacterAI.run(world)
      actions = Map.get(world, :character_actions, %{})
      assert Map.get(actions, 1) == [:first, :second]
      assert Map.get(actions, 2) == [:a, :b]
      # Char 1 done
      assert CharacterAI.get_character_plan(world, 1) == nil
      # Char 2 still going
      assert CharacterAI.get_character_plan(world, 2) != nil

      # Tick 3: Only char 2 executes
      world = CharacterAI.run(world)
      actions = Map.get(world, :character_actions, %{})
      # Unchanged
      assert Map.get(actions, 1) == [:first, :second]
      assert Map.get(actions, 2) == [:a, :b, :c]
      # Char 2 done
      assert CharacterAI.get_character_plan(world, 2) == nil
    end
  end

  describe "replanning when conditions change" do
    setup do
      world = StellaInvicta.World.new_planet_world()
      {:ok, world: world}
    end

    test "plan fails when destination is destroyed mid-execution", %{world: world} do
      # Create a domain where movement requires destination to exist
      domain =
        HTN.new_domain("movement_with_validation")
        |> HTN.add_task(
          HTN.primitive(:move_to,
            preconditions: [
              fn world, params ->
                # Destination must exist in locations
                dest = params.destination
                Map.has_key?(world.locations, dest)
              end
            ],
            operator: fn world, params ->
              dest = params.destination
              # Double-check destination still exists
              if Map.has_key?(world.locations, dest) do
                history = Map.get(world, :move_history, [])

                {:ok,
                 world |> Map.put(:location, dest) |> Map.put(:move_history, history ++ [dest])}
              else
                {:error, {:destination_destroyed, dest}}
              end
            end
          )
        )

      # Plan: move through 3 locations (1 -> 2 -> 3)
      plan =
        Plan.new([
          # Berlin to Neumark
          {:move_to, %{destination: 2}},
          # Neumark to Black Forest
          {:move_to, %{destination: 3}},
          # Stay at Black Forest (for testing)
          {:move_to, %{destination: 3}}
        ])

      world =
        world
        |> Map.put(:location, 1)
        |> CharacterAI.set_character_domain(1, domain)
        |> CharacterAI.store_character_plan(1, plan)

      # Tick 1: Successfully move to Neumark
      world = CharacterAI.run(world)
      assert world.location == 2
      assert Map.get(world, :move_history) == [2]

      # Now destroy the Black Forest (location 3) before tick 2
      world = %{world | locations: Map.delete(world.locations, 3)}

      # Tick 2: Should fail because destination 3 no longer exists
      world = CharacterAI.run(world)

      # Plan should be cleared due to failure (replanning disabled by default)
      assert CharacterAI.get_character_plan(world, 1) == nil
      # Location should still be Neumark
      assert world.location == 2
    end

    test "character replans when destination is destroyed (replanning enabled)", %{world: world} do
      # Create domain with move and idle tasks
      domain =
        HTN.new_domain("movement_with_fallback")
        |> HTN.add_task(
          HTN.primitive(:move_to,
            preconditions: [
              fn world, params ->
                dest = params.destination
                Map.has_key?(world.locations, dest)
              end
            ],
            operator: fn world, params ->
              dest = params.destination

              if Map.has_key?(world.locations, dest) do
                history = Map.get(world, :move_history, [])

                {:ok,
                 world |> Map.put(:location, dest) |> Map.put(:move_history, history ++ [dest])}
              else
                {:error, {:destination_destroyed, dest}}
              end
            end
          )
        )
        |> HTN.add_task(
          HTN.primitive(:idle,
            operator: fn world, _params ->
              idle_count = Map.get(world, :idle_count, 0)
              {:ok, Map.put(world, :idle_count, idle_count + 1)}
            end
          )
        )
        |> HTN.add_task(
          HTN.compound(:travel_to_forest,
            methods: [
              HTN.method(:direct_path,
                conditions: [
                  fn world, _params -> Map.has_key?(world.locations, 3) end
                ],
                subtasks: [
                  {:move_to, %{destination: 2}},
                  {:move_to, %{destination: 3}}
                ]
              ),
              HTN.method(:stay_put,
                # Fallback when forest doesn't exist
                subtasks: [
                  {:idle, %{}}
                ]
              )
            ]
          )
        )

      # Create plan to travel to forest
      plan =
        Plan.new([
          {:move_to, %{destination: 2}},
          {:move_to, %{destination: 3}}
        ])

      world =
        world
        |> Map.put(:location, 1)
        |> CharacterAI.set_character_domain(1, domain)
        |> CharacterAI.store_character_plan(1, plan)
        |> CharacterAI.store_plan_goal(1, :travel_to_forest)
        |> CharacterAI.set_replan_settings(1, %{
          enabled: true,
          max_attempts: 3,
          fallback_goal: :idle
        })

      # Tick 1: Move to Neumark
      world = CharacterAI.run(world)
      assert world.location == 2

      # Destroy Black Forest
      world = %{world | locations: Map.delete(world.locations, 3)}

      # Tick 2: Should fail and replan
      # Since forest is gone, travel_to_forest will now use :stay_put method
      world = CharacterAI.run(world)

      # Should have a new plan (the replanned one)
      new_plan = CharacterAI.get_character_plan(world, 1)
      assert new_plan != nil

      # Check that replan event was published
      # The character should now have an idle-based plan
    end

    test "character falls back to idle when replanning fails completely", %{world: world} do
      # Create domain where the only travel option requires location 3
      domain =
        HTN.new_domain("strict_movement")
        |> HTN.add_task(
          HTN.primitive(:move_to,
            preconditions: [
              fn world, params ->
                dest = params.destination
                Map.has_key?(world.locations, dest)
              end
            ],
            operator: fn world, params ->
              dest = params.destination

              if Map.has_key?(world.locations, dest) do
                {:ok, Map.put(world, :location, dest)}
              else
                {:error, {:destination_destroyed, dest}}
              end
            end
          )
        )
        |> HTN.add_task(
          HTN.primitive(:idle,
            operator: fn world, _params ->
              {:ok, Map.put(world, :idled, true)}
            end
          )
        )
        |> HTN.add_task(
          HTN.compound(:go_to_forest,
            methods: [
              HTN.method(:must_have_forest,
                conditions: [
                  fn world, _params -> Map.has_key?(world.locations, 3) end
                ],
                subtasks: [{:move_to, %{destination: 3}}]
              )
            ]
          )
        )

      plan = Plan.new([{:move_to, %{destination: 3}}])

      world =
        world
        |> Map.put(:location, 1)
        |> CharacterAI.set_character_domain(1, domain)
        |> CharacterAI.store_character_plan(1, plan)
        |> CharacterAI.store_plan_goal(1, :go_to_forest)
        |> CharacterAI.set_replan_settings(1, %{
          enabled: true,
          max_attempts: 3,
          fallback_goal: :idle
        })

      # Destroy forest before first tick
      world = %{world | locations: Map.delete(world.locations, 3)}

      # Tick 1: Move fails, replanning for :go_to_forest fails (no forest),
      # falls back to :idle
      world = CharacterAI.run(world)

      # Should have idle plan as fallback
      new_plan = CharacterAI.get_character_plan(world, 1)
      assert new_plan != nil
      assert [{:idle, _}] = new_plan.steps
    end

    test "replanning respects max attempts", %{world: world} do
      # Create a domain where movement always fails
      domain =
        HTN.new_domain("always_fail")
        |> HTN.add_task(
          HTN.primitive(:always_fails,
            operator: fn _world, _params ->
              {:error, :intentional_failure}
            end
          )
        )
        |> HTN.add_task(
          HTN.compound(:doomed_task,
            methods: [
              HTN.method(:doomed,
                subtasks: [{:always_fails, %{}}]
              )
            ]
          )
        )

      plan = Plan.new([{:always_fails, %{}}])

      world =
        world
        |> CharacterAI.set_character_domain(1, domain)
        |> CharacterAI.store_character_plan(1, plan)
        |> CharacterAI.store_plan_goal(1, :doomed_task)
        |> CharacterAI.set_replan_settings(1, %{
          enabled: true,
          max_attempts: 2,
          fallback_goal: nil
        })

      # Tick 1: Fails, replan attempt 1
      world = CharacterAI.run(world)
      plan1 = CharacterAI.get_character_plan(world, 1)
      # Replanned
      assert plan1 != nil

      # Tick 2: Fails again, replan attempt 2
      world = CharacterAI.run(world)
      plan2 = CharacterAI.get_character_plan(world, 1)
      # Replanned again
      assert plan2 != nil

      # Tick 3: Fails, but max attempts (2) reached - gives up
      world = CharacterAI.run(world)
      assert CharacterAI.get_character_plan(world, 1) == nil
    end

    test "replanning is disabled by default", %{world: world} do
      domain =
        HTN.new_domain("simple")
        |> HTN.add_task(
          HTN.primitive(:fails,
            operator: fn _world, _params -> {:error, :oops} end
          )
        )

      plan = Plan.new([{:fails, %{}}])

      world =
        world
        |> CharacterAI.set_character_domain(1, domain)
        |> CharacterAI.store_character_plan(1, plan)

      # Default settings should have replanning disabled
      settings = CharacterAI.get_replan_settings(world, 1)
      assert settings.enabled == false

      # Tick 1: Fails, no replanning, plan cleared
      world = CharacterAI.run(world)
      assert CharacterAI.get_character_plan(world, 1) == nil
    end

    test "world state changes detected via preconditions", %{world: world} do
      # This test demonstrates how preconditions catch changed world state
      domain =
        HTN.new_domain("precondition_check")
        |> HTN.add_task(
          HTN.primitive(:use_gold,
            preconditions: [
              fn world, _params ->
                resources = Map.get(world, :resources, %{})
                Map.get(resources, :gold, 0) > 0
              end
            ],
            operator: fn world, _params ->
              resources = Map.get(world, :resources, %{})
              current = Map.get(resources, :gold, 0)

              if current > 0 do
                updated = Map.put(resources, :gold, current - 1)
                {:ok, Map.put(world, :resources, updated)}
              else
                {:error, :no_gold}
              end
            end
          )
        )
        |> HTN.add_task(
          HTN.primitive(:gather_gold,
            effects: [
              fn world, _params ->
                resources = Map.get(world, :resources, %{})
                current = Map.get(resources, :gold, 0)
                updated = Map.put(resources, :gold, current + 1)
                Map.put(world, :resources, updated)
              end
            ],
            operator: fn world, _params ->
              resources = Map.get(world, :resources, %{})
              current = Map.get(resources, :gold, 0)
              updated = Map.put(resources, :gold, current + 1)
              {:ok, Map.put(world, :resources, updated)}
            end
          )
        )
        |> HTN.add_task(
          HTN.primitive(:idle,
            operator: fn world, _params ->
              {:ok, Map.put(world, :idled, true)}
            end
          )
        )
        |> HTN.add_task(
          HTN.compound(:acquire_and_use_gold,
            methods: [
              HTN.method(:have_gold,
                priority: 10,
                conditions: [
                  fn world, _params ->
                    resources = Map.get(world, :resources, %{})
                    Map.get(resources, :gold, 0) > 0
                  end
                ],
                subtasks: [{:use_gold, %{}}]
              ),
              HTN.method(:gather_first,
                priority: 5,
                subtasks: [
                  {:gather_gold, %{}},
                  {:use_gold, %{}}
                ]
              )
            ]
          )
        )

      # Start with some gold
      world =
        world
        |> Map.put(:resources, %{gold: 1})
        |> CharacterAI.set_character_domain(1, domain)

      # Create plan that uses gold twice (second will fail since we only have 1)
      plan =
        Plan.new([
          {:use_gold, %{}},
          # This will fail - no gold left
          {:use_gold, %{}}
        ])

      world =
        world
        |> CharacterAI.store_character_plan(1, plan)
        |> CharacterAI.store_plan_goal(1, :acquire_and_use_gold)
        |> CharacterAI.set_replan_settings(1, %{
          enabled: true,
          max_attempts: 3,
          fallback_goal: :idle
        })

      # Tick 1: Use gold (gold: 1 -> 0)
      world = CharacterAI.run(world)
      assert world.resources[:gold] == 0
      # Plan should still exist, on step 2
      plan_after_tick1 = CharacterAI.get_character_plan(world, 1)
      assert plan_after_tick1 != nil
      assert plan_after_tick1.current_step == 1

      # Tick 2: Try to use gold again, fails (no gold), triggers replan
      # Replan should succeed with :gather_first method (gather then use)
      world = CharacterAI.run(world)

      # Should have replanned - the new plan from :acquire_and_use_gold
      # Since gold is 0, :have_gold condition fails, so :gather_first is chosen
      new_plan = CharacterAI.get_character_plan(world, 1)
      assert new_plan != nil, "Expected a new plan after replanning"

      # The replanned plan should have 2 steps: gather_gold then use_gold
      # (from the :gather_first method)
      assert length(new_plan.steps) == 2,
             "Expected 2 steps (gather_gold, use_gold) but got #{length(new_plan.steps)} steps: #{inspect(new_plan.steps)}"
    end
  end
end
