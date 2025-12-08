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
end
