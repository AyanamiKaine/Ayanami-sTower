# All AI interactions are done by characters. Characters push the game world.
defmodule StellaInvicta.System.CharacterAI do
  @moduledoc """
  AI System that manages character decision-making using HTN planning.

  Each character can have their own HTN plan that gets executed over time.
  The system runs each tick, executing one step of each character's plan,
  and re-planning when plans complete or fail.
  """

  @behaviour StellaInvicta.System

  alias StellaInvicta.AI.HierarchicalTaskNetwork, as: HTN
  alias StellaInvicta.MessageQueue

  @impl true
  def subscriptions do
    [:character_events, :date_events]
  end

  @impl true
  def handle_message(world, :date_events, {:new_day, _day}) do
    # On new day, evaluate character goals
    world
  end

  def handle_message(world, :character_events, {:character_needs_plan, character_id}) do
    # Generate a new plan for a character with metrics tracking
    # Check cooldown - don't plan if still on cooldown
    case get_planning_cooldown_remaining(world, character_id) do
      0 ->
        generate_new_plan_for_character(world, character_id)

      _ ->
        # Still on cooldown, defer planning
        world
    end
  end

  def handle_message(world, :character_events, {:plan_completed, character_id}) do
    # Plan completed - don't re-plan immediately, respect cooldown
    # Reset cooldown to configured default value so it ticks down before next plan
    reset_planning_cooldown(world, character_id)
  end

  def handle_message(world, :character_events, {:plan_failed, character_id, _reason}) do
    # Plan failed - don't re-plan immediately, respect cooldown
    # Reset cooldown to configured default value so it ticks down before next plan
    reset_planning_cooldown(world, character_id)
  end

  def handle_message(world, _topic, _message), do: world

  # =============================================================================
  # Plan Generation
  # =============================================================================

  # Generates a new plan for a character with metrics tracking.
  # Used both for initial planning and re-planning after plan completion/failure.
  defp generate_new_plan_for_character(world, character_id) do
    {:ok, domain} = get_character_domain(world, character_id)
    planning_context = prepare_planning_context(world, character_id)

    metrics =
      StellaInvicta.Metrics.get_ai_metrics(world, character_id) ||
        StellaInvicta.Metrics.new_ai_metrics()

    case HTN.find_plan_with_metrics(domain, planning_context, :daily_routine, metrics,
           params: %{character_id: character_id}
         ) do
      {:ok, plan, updated_metrics} ->
        world
        |> store_character_plan(character_id, plan)
        |> reset_planning_cooldown(character_id)
        |> StellaInvicta.Metrics.store_ai_metrics(character_id, updated_metrics)

      {:error, _reason, updated_metrics} ->
        # Even on planning failure, reset cooldown so we don't spam planning attempts
        world
        |> reset_planning_cooldown(character_id)
        |> StellaInvicta.Metrics.store_ai_metrics(character_id, updated_metrics)
    end
  end

  @impl true
  def run(world) do
    # Decrement planning cooldowns for all characters each tick
    world = decrement_all_planning_cooldowns(world)

    # Generate new plans for characters whose cooldown reached 0 and have no plan
    world = request_plans_for_ready_characters(world)

    # Execute one step of each character's plan
    world
    |> get_all_character_plans()
    |> Enum.reduce(world, fn {character_id, plan}, acc_world ->
      execute_character_plan_step(acc_world, character_id, plan)
    end)
  end

  # Requests plans for all characters whose cooldown is 0 and don't have an active plan
  defp request_plans_for_ready_characters(world) do
    all_characters = Map.get(world, :characters, %{})
    cooldowns = Map.get(world, :character_planning_cooldowns, %{})

    Enum.reduce(all_characters, world, fn {character_id, _character}, acc_world ->
      # Only auto-request if cooldown has been explicitly set for this character
      # (i.e., the character is being managed by the cooldown system)
      if Map.has_key?(cooldowns, character_id) do
        cooldown_remaining = get_planning_cooldown_remaining(acc_world, character_id)
        current_plan = get_character_plan(acc_world, character_id)

        if cooldown_remaining == 0 and current_plan == nil do
          # Request a new plan
          generate_new_plan_for_character(acc_world, character_id)
        else
          acc_world
        end
      else
        acc_world
      end
    end)
  end

  # =============================================================================
  # Plan Management
  # =============================================================================

  @doc """
  Stores a plan for a character in the world state.
  """
  def store_character_plan(world, character_id, plan) do
    plans = Map.get(world, :character_plans, %{})
    updated_plans = Map.put(plans, character_id, plan)
    Map.put(world, :character_plans, updated_plans)
  end

  @doc """
  Retrieves a character's current plan.
  """
  def get_character_plan(world, character_id) do
    plans = Map.get(world, :character_plans, %{})
    Map.get(plans, character_id)
  end

  @doc """
  Gets all character plans.
  """
  def get_all_character_plans(world) do
    Map.get(world, :character_plans, %{})
  end

  @doc """
  Removes a character's plan (e.g., when completed or failed).
  """
  def clear_character_plan(world, character_id) do
    plans = Map.get(world, :character_plans, %{})
    updated_plans = Map.delete(plans, character_id)
    Map.put(world, :character_plans, updated_plans)
  end

  # =============================================================================
  # Plan Tracking (History and Statistics)
  # =============================================================================

  @doc """
  Records that a plan was completed for a character.
  Moves current plan to history and increments completed count.
  """
  def record_plan_completed(world, character_id, completed_plan) do
    # Store as last completed plan
    world = store_last_plan(world, character_id, completed_plan)

    # Increment plan execution counter
    increment_plan_count(world, character_id)
  end

  @doc """
  Stores a plan as the last completed/active plan for a character.
  """
  def store_last_plan(world, character_id, plan) do
    plans_history = Map.get(world, :character_plan_history, %{})
    character_history = Map.get(plans_history, character_id, %{})

    updated_history = Map.put(character_history, :last_plan, plan)
    updated_plans_history = Map.put(plans_history, character_id, updated_history)

    Map.put(world, :character_plan_history, updated_plans_history)
  end

  @doc """
  Gets the last completed plan for a character.
  """
  def get_last_plan(world, character_id) do
    plans_history = Map.get(world, :character_plan_history, %{})
    character_history = Map.get(plans_history, character_id, %{})
    Map.get(character_history, :last_plan)
  end

  @doc """
  Increments the plan execution counter for a character.
  """
  def increment_plan_count(world, character_id) do
    plans_history = Map.get(world, :character_plan_history, %{})
    character_history = Map.get(plans_history, character_id, %{})

    current_count = Map.get(character_history, :plans_executed, 0)
    updated_history = Map.put(character_history, :plans_executed, current_count + 1)
    updated_plans_history = Map.put(plans_history, character_id, updated_history)

    Map.put(world, :character_plan_history, updated_plans_history)
  end

  @doc """
  Gets the total number of plans executed by a character.
  """
  def get_plans_executed_count(world, character_id) do
    plans_history = Map.get(world, :character_plan_history, %{})
    character_history = Map.get(plans_history, character_id, %{})
    Map.get(character_history, :plans_executed, 0)
  end

  @doc """
  Gets plan execution statistics for a character.
  Returns a map with:
  - `:current_plan` - The plan currently being executed (if any)
  - `:last_plan` - The last completed plan
  - `:plans_executed` - Total number of completed plans
  """
  def get_plan_stats(world, character_id) do
    current_plan = get_character_plan(world, character_id)
    last_plan = get_last_plan(world, character_id)
    plans_executed = get_plans_executed_count(world, character_id)

    %{
      current_plan: current_plan,
      last_plan: last_plan,
      plans_executed: plans_executed
    }
  end

  # =============================================================================
  # Plan Execution
  # =============================================================================

  defp execute_character_plan_step(world, character_id, plan) do
    {:ok, domain} = get_character_domain(world, character_id)

    # Get current metrics for this character
    metrics =
      StellaInvicta.Metrics.get_ai_metrics(world, character_id) ||
        StellaInvicta.Metrics.new_ai_metrics()

    case HTN.execute_step_with_metrics(plan, domain, world, metrics) do
      {:ok, new_plan, new_world, updated_metrics} ->
        # Step completed - check if plan is now done
        new_world =
          StellaInvicta.Metrics.store_ai_metrics(new_world, character_id, updated_metrics)

        cond do
          new_plan.status == :completed ->
            # Plan finished after this step
            new_world
            |> record_plan_completed(character_id, new_plan)
            |> clear_character_plan(character_id)
            |> MessageQueue.publish(:character_events, {:plan_completed, character_id})

          true ->
            # More steps remaining, store updated plan for next tick
            store_character_plan(new_world, character_id, new_plan)
        end

      {:running, plan, new_world, updated_metrics} ->
        # Task is still running (multi-tick task), keep the same plan position
        # The same step will continue execution on the next tick
        new_world =
          StellaInvicta.Metrics.store_ai_metrics(new_world, character_id, updated_metrics)

        store_character_plan(new_world, character_id, plan)

      {:complete, new_world, updated_metrics} ->
        # Plan completed - clear it and potentially request new plan
        new_world
        |> StellaInvicta.Metrics.store_ai_metrics(character_id, updated_metrics)
        |> record_plan_completed(character_id, plan)
        |> clear_character_plan(character_id)
        |> MessageQueue.publish(:character_events, {:plan_completed, character_id})

      {:error, reason, _failed_plan, new_world, updated_metrics} ->
        # Plan failed - attempt to replan if replanning is enabled
        new_world =
          StellaInvicta.Metrics.store_ai_metrics(new_world, character_id, updated_metrics)

        handle_plan_failure(new_world, character_id, plan, reason)
    end
  end

  @doc """
  Handles a plan failure by attempting to replan or giving up.

  The behavior depends on the character's replanning settings:
  - If replanning is enabled, attempts to find a new plan with the same goal
  - If replanning fails or is disabled, clears the plan and publishes failure event
  """
  def handle_plan_failure(world, character_id, failed_plan, reason) do
    replan_settings = get_replan_settings(world, character_id)

    if replan_settings.enabled do
      attempt_replan(world, character_id, failed_plan, reason, replan_settings)
    else
      # Replanning disabled - just fail
      world
      |> clear_character_plan(character_id)
      |> MessageQueue.publish(:character_events, {:plan_failed, character_id, reason})
    end
  end

  defp attempt_replan(world, character_id, _failed_plan, original_reason, settings) do
    # Check replan attempt count
    attempts = get_replan_attempts(world, character_id)

    if attempts >= settings.max_attempts do
      # Exceeded max attempts - give up
      world
      |> clear_character_plan(character_id)
      |> clear_replan_attempts(character_id)
      |> MessageQueue.publish(
        :character_events,
        {:plan_failed, character_id, {:max_replan_attempts, original_reason}}
      )
    else
      # Try to replan with metrics tracking
      {:ok, domain} = get_character_domain(world, character_id)
      goal = get_plan_goal(world, character_id) || settings.fallback_goal

      planning_context = prepare_planning_context(world, character_id)

      # Get or create metrics tracker for this character
      metrics =
        StellaInvicta.Metrics.get_ai_metrics(world, character_id) ||
          StellaInvicta.Metrics.new_ai_metrics()

      case HTN.find_plan_with_metrics(domain, planning_context, goal, metrics,
             params: %{character_id: character_id}
           ) do
        {:ok, new_plan, updated_metrics} ->
          # Replanning succeeded
          world
          |> increment_replan_attempts(character_id)
          |> store_character_plan(character_id, new_plan)
          |> store_plan_goal(character_id, goal)
          |> StellaInvicta.Metrics.store_ai_metrics(character_id, updated_metrics)
          |> MessageQueue.publish(
            :character_events,
            {:plan_replanned, character_id, original_reason}
          )

        {:error, replan_reason, updated_metrics} ->
          # Replanning failed - try fallback or give up, but store metrics
          world = StellaInvicta.Metrics.store_ai_metrics(world, character_id, updated_metrics)

          if goal != settings.fallback_goal and settings.fallback_goal != nil do
            # Try fallback goal
            fallback_metrics =
              StellaInvicta.Metrics.get_ai_metrics(world, character_id) ||
                StellaInvicta.Metrics.new_ai_metrics()

            case HTN.find_plan_with_metrics(
                   domain,
                   planning_context,
                   settings.fallback_goal,
                   fallback_metrics,
                   params: %{character_id: character_id}
                 ) do
              {:ok, fallback_plan, fallback_updated_metrics} ->
                world
                |> clear_replan_attempts(character_id)
                |> store_character_plan(character_id, fallback_plan)
                |> store_plan_goal(character_id, settings.fallback_goal)
                |> StellaInvicta.Metrics.store_ai_metrics(character_id, fallback_updated_metrics)
                |> MessageQueue.publish(
                  :character_events,
                  {:plan_fallback, character_id, settings.fallback_goal}
                )

              {:error, _} ->
                world
                |> clear_character_plan(character_id)
                |> clear_replan_attempts(character_id)
                |> MessageQueue.publish(
                  :character_events,
                  {:plan_failed, character_id, {:replan_failed, replan_reason}}
                )
            end
          else
            world
            |> clear_character_plan(character_id)
            |> clear_replan_attempts(character_id)
            |> MessageQueue.publish(
              :character_events,
              {:plan_failed, character_id, {:replan_failed, replan_reason}}
            )
          end
      end
    end
  end

  # =============================================================================
  # Replan Settings Management
  # =============================================================================

  @doc """
  Gets the replanning settings for a character.
  Returns defaults if not set.
  """
  def get_replan_settings(world, character_id) do
    settings = Map.get(world, :character_replan_settings, %{})
    Map.get(settings, character_id, default_replan_settings())
  end

  @doc """
  Sets replanning settings for a character.
  """
  def set_replan_settings(world, character_id, settings) do
    all_settings = Map.get(world, :character_replan_settings, %{})
    merged = Map.merge(default_replan_settings(), settings)
    updated = Map.put(all_settings, character_id, merged)
    Map.put(world, :character_replan_settings, updated)
  end

  @doc """
  Returns default replanning settings.
  """
  def default_replan_settings do
    %{
      enabled: false,
      max_attempts: 3,
      fallback_goal: :idle
    }
  end

  # Replan attempt tracking
  defp get_replan_attempts(world, character_id) do
    attempts = Map.get(world, :character_replan_attempts, %{})
    Map.get(attempts, character_id, 0)
  end

  defp increment_replan_attempts(world, character_id) do
    attempts = Map.get(world, :character_replan_attempts, %{})
    current = Map.get(attempts, character_id, 0)
    updated = Map.put(attempts, character_id, current + 1)
    Map.put(world, :character_replan_attempts, updated)
  end

  defp clear_replan_attempts(world, character_id) do
    attempts = Map.get(world, :character_replan_attempts, %{})
    updated = Map.delete(attempts, character_id)
    Map.put(world, :character_replan_attempts, updated)
  end

  # Plan goal tracking (so we know what to replan for)
  defp get_plan_goal(world, character_id) do
    goals = Map.get(world, :character_plan_goals, %{})
    Map.get(goals, character_id)
  end

  @doc """
  Stores the goal associated with a character's current plan.
  Used for replanning when the plan fails.
  """
  def store_plan_goal(world, character_id, goal) do
    goals = Map.get(world, :character_plan_goals, %{})
    updated = Map.put(goals, character_id, goal)
    Map.put(world, :character_plan_goals, updated)
  end

  @doc """
  Clears the stored goal for a character.
  """
  def clear_plan_goal(world, character_id) do
    goals = Map.get(world, :character_plan_goals, %{})
    updated = Map.delete(goals, character_id)
    Map.put(world, :character_plan_goals, updated)
  end

  # =============================================================================
  # Domain Management
  # =============================================================================

  @doc """
  Gets or creates the HTN domain for a character.
  Characters can have different domains based on their role, traits, etc.
  """
  def get_character_domain(world, character_id) do
    # Check if character has a custom domain stored
    domains = Map.get(world, :character_domains, %{})

    case Map.get(domains, character_id) do
      nil ->
        # Use default domain
        {:ok, default_character_domain(world)}

      domain ->
        {:ok, domain}
    end
  end

  @doc """
  Sets a custom domain for a character.
  """
  def set_character_domain(world, character_id, domain) do
    domains = Map.get(world, :character_domains, %{})
    updated_domains = Map.put(domains, character_id, domain)
    Map.put(world, :character_domains, updated_domains)
  end

  @doc """
  Creates the default character domain with common tasks.
  This can be extended based on game needs.
  """
  def default_character_domain(_world) do
    HTN.new_domain("default_character")
    |> HTN.add_task(idle_task())
    |> HTN.add_task(rest_task())
    |> HTN.add_task(study_task())
    |> HTN.add_task(train_task())
    |> HTN.add_task(daily_routine_task())
  end

  # =============================================================================
  # Common Tasks
  # =============================================================================

  defp idle_task do
    HTN.primitive(:idle,
      operator: fn world, _params ->
        # Do nothing, just pass time
        {:ok, world}
      end
    )
  end

  defp rest_task do
    HTN.primitive(:rest,
      effects: [
        fn world, _params ->
          # Use the character ID from the planning context
          character_id = Map.get(world, :_character_id)

          if character_id do
            update_character_stat(world, character_id, :health, 5)
          else
            world
          end
        end
      ],
      operator: fn world, _params ->
        character_id = Map.get(world, :_character_id)

        if character_id do
          {:ok, update_character_stat(world, character_id, :health, 5)}
        else
          {:ok, world}
        end
      end
    )
  end

  defp study_task do
    HTN.primitive(:study,
      preconditions: [
        fn world, _params ->
          # Check the character's traits from the planning context
          traits = Map.get(world, :_character_traits, [])
          :scholar in traits
        end
      ],
      effects: [
        fn world, _params ->
          character_id = Map.get(world, :_character_id)

          if character_id do
            # Studying improves stewardship
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

  defp train_task do
    HTN.primitive(:train,
      preconditions: [
        fn world, _params ->
          # Check the character's traits from the planning context
          traits = Map.get(world, :_character_traits, [])
          :brave in traits
        end
      ],
      effects: [
        fn world, _params ->
          character_id = Map.get(world, :_character_id)

          if character_id do
            # Training improves martial
            update_character_stat(world, character_id, :martial, 1)
          else
            world
          end
        end
      ],
      operator: fn world, _params ->
        character_id = Map.get(world, :_character_id)

        if character_id do
          {:ok, update_character_stat(world, character_id, :martial, 1)}
        else
          {:ok, world}
        end
      end
    )
  end

  defp daily_routine_task do
    HTN.compound(:daily_routine,
      methods: [
        # Scholars prefer to study
        HTN.method(:scholar_routine,
          priority: 10,
          conditions: [
            fn world, _params ->
              traits = Map.get(world, :_character_traits, [])
              :scholar in traits
            end
          ],
          subtasks: [{:study, %{}}]
        ),
        # Brave characters prefer to train
        HTN.method(:warrior_routine,
          priority: 10,
          conditions: [
            fn world, _params ->
              traits = Map.get(world, :_character_traits, [])
              :brave in traits
            end
          ],
          subtasks: [{:train, %{}}]
        ),
        # Default: rest
        HTN.method(:rest_routine,
          priority: 0,
          subtasks: [{:rest, %{}}]
        )
      ]
    )
  end

  # =============================================================================
  # Helpers
  # =============================================================================

  @doc """
  Prepares a rich planning context that includes both world state and character-specific data.
  This becomes the "domain" that the HTN planner reasons about.

  The HTN planner sees this as its world - it contains all the information needed
  to make decisions for that character.
  """
  def prepare_planning_context(world, character_id) do
    character = get_character(world, character_id)
    character_traits = Map.get(world, :character_traits, %{}) |> Map.get(character_id, [])

    # Build a context map that includes:
    # - Global world state (for environmental reasoning)
    # - Character-specific state (stats, traits, inventory)
    # - Character identity (so operators don't need to pass it around)
    world
    |> Map.put(:_character_id, character_id)
    |> Map.put(:_character, character)
    |> Map.put(:_character_traits, character_traits)
    |> Map.put(:_character_stats, Map.get(character || %{}, :stats, %{}))
  end

  # =============================================================================
  # Planning Cooldown Management
  # =============================================================================

  @doc """
  Gets the planning cooldown configuration for a character.
  Returns the default cooldown if not configured.

  Default: 1000 ticks between replanning
  """
  def get_planning_cooldown_config(world, character_id) do
    config = Map.get(world, :character_planning_config, %{})
    # Default: 1000 ticks
    Map.get(config, character_id, 1000)
  end

  @doc """
  Sets the planning cooldown configuration for a character.
  """
  def set_planning_cooldown_config(world, character_id, ticks) do
    config = Map.get(world, :character_planning_config, %{})
    updated_config = Map.put(config, character_id, ticks)
    Map.put(world, :character_planning_config, updated_config)
  end

  @doc """
  Gets the current planning cooldown remaining for a character.
  Returns 0 if cooldown has expired (ready to plan).
  """
  def get_planning_cooldown_remaining(world, character_id) do
    cooldowns = Map.get(world, :character_planning_cooldowns, %{})
    Map.get(cooldowns, character_id, 0)
  end

  @doc """
  Sets the planning cooldown for a character to a specific number of ticks.
  """
  def set_planning_cooldown(world, character_id, ticks) do
    cooldowns = Map.get(world, :character_planning_cooldowns, %{})
    updated_cooldowns = Map.put(cooldowns, character_id, ticks)
    Map.put(world, :character_planning_cooldowns, updated_cooldowns)
  end

  @doc """
  Resets the planning cooldown for a character to the configured default.
  Called after a plan is generated.
  """
  def reset_planning_cooldown(world, character_id) do
    cooldown_ticks = get_planning_cooldown_config(world, character_id)
    set_planning_cooldown(world, character_id, cooldown_ticks)
  end

  @doc """
  Decrements all character planning cooldowns by 1 tick.
  Called once per game tick to progress cooldowns.
  """
  def decrement_all_planning_cooldowns(world) do
    cooldowns = Map.get(world, :character_planning_cooldowns, %{})

    # Only process if there are any cooldowns to decrement
    if map_size(cooldowns) == 0 do
      world
    else
      updated_cooldowns =
        Enum.reduce(cooldowns, %{}, fn {character_id, remaining}, acc ->
          # Decrement but don't go below 0
          new_remaining = max(0, remaining - 1)
          Map.put(acc, character_id, new_remaining)
        end)

      Map.put(world, :character_planning_cooldowns, updated_cooldowns)
    end
  end

  @doc """
  Retrieves a character from the world by ID.
  """
  def get_character(world, character_id) do
    characters = Map.get(world, :characters, %{})
    Map.get(characters, character_id)
  end

  defp update_character_stat(world, character_id, stat, delta) do
    characters = Map.get(world, :characters, %{})

    case Map.get(characters, character_id) do
      nil ->
        world

      character ->
        current_value = Map.get(character, stat) || 0
        updated_character = Map.put(character, stat, current_value + delta)
        updated_characters = Map.put(characters, character_id, updated_character)
        Map.put(world, :characters, updated_characters)
    end
  end
end
