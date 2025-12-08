defmodule StellaInvicta.Metrics do
  @moduledoc """
  Performance metrics tracking for game systems, message queues, and game ticks.

  Collects timing data that can be used by a UI to display performance stats,
  identify bottlenecks, and monitor system health.

  ## Tracked Metrics

  - **System Execution**: Time taken by each system's `run/1` call
  - **Message Processing**: Time spent processing messages per system
  - **Message Queue**: Message counts, queue sizes, publish rates
  - **Game Tick**: Total tick time, breakdown by phase
  - **AI Decisions**: HTN planning decisions, method selections, backtracking
  - **Historical Data**: Rolling averages and peak values

  ## Usage

      # Initialize metrics in game state
      game_state = Metrics.init(game_state)

      # Get performance summary for UI
      summary = Metrics.get_summary(game_state)

      # Get detailed system stats
      system_stats = Metrics.get_system_stats(game_state, MySystem)

      # Get AI decision log for a character
      ai_log = Metrics.get_ai_decisions(game_state, character_id)
  """

  alias StellaInvicta.AI.HierarchicalTaskNetwork, as: HTN

  # Number of samples to keep for rolling averages
  @history_size 100

  @doc """
  Initializes metrics tracking in the game state.
  """
  def init(game_state) do
    game_state
    |> Map.put(:metrics, %{
      # Per-system metrics
      systems: %{},
      # Message queue metrics
      message_queue: %{
        total_published: 0,
        total_processed: 0,
        publish_times: [],
        peak_queue_size: 0
      },
      # Game tick metrics
      ticks: %{
        total_ticks: 0,
        tick_times: [],
        peak_tick_time: 0,
        last_tick_time: 0,
        last_tick_breakdown: %{}
      },
      # Tracking enabled flag
      enabled: true
    })
  end

  @doc """
  Enables or disables metrics collection.
  Disabling can improve performance slightly if metrics aren't needed.
  """
  def set_enabled(game_state, enabled) when is_boolean(enabled) do
    metrics = Map.get(game_state, :metrics, %{})
    Map.put(game_state, :metrics, Map.put(metrics, :enabled, enabled))
  end

  @doc """
  Checks if metrics collection is enabled.
  """
  def enabled?(game_state) do
    case Map.get(game_state, :metrics) do
      nil -> false
      metrics -> Map.get(metrics, :enabled, false)
    end
  end

  # --- Timing Helpers ---

  @doc """
  Measures the execution time of a function in microseconds.
  Returns {result, time_microseconds}.
  """
  def measure(fun) when is_function(fun, 0) do
    start = System.monotonic_time(:microsecond)
    result = fun.()
    finish = System.monotonic_time(:microsecond)
    {result, finish - start}
  end

  @doc """
  Records execution time for a system's run phase.
  """
  def record_system_run(game_state, system_module, time_us) do
    if enabled?(game_state) do
      update_system_metric(game_state, system_module, :run_times, time_us)
    else
      game_state
    end
  end

  @doc """
  Records execution time for a system's message processing phase.
  """
  def record_system_messages(game_state, system_module, time_us, message_count) do
    if enabled?(game_state) do
      game_state
      |> update_system_metric(system_module, :message_times, time_us)
      |> update_system_counter(system_module, :messages_processed, message_count)
    else
      game_state
    end
  end

  @doc """
  Records a message publish event.
  """
  def record_publish(game_state, time_us, subscriber_count) do
    if enabled?(game_state) do
      metrics = Map.get(game_state, :metrics, %{})
      mq_metrics = Map.get(metrics, :message_queue, %{})

      updated_mq =
        mq_metrics
        |> Map.update(:total_published, 1, &(&1 + subscriber_count))
        |> Map.update(:publish_times, [time_us], &add_to_history(&1, time_us, @history_size))

      updated_metrics = Map.put(metrics, :message_queue, updated_mq)
      Map.put(game_state, :metrics, updated_metrics)
    else
      game_state
    end
  end

  @doc """
  Records a complete game tick with timing breakdown.
  """
  def record_tick(game_state, total_time_us, breakdown) do
    if enabled?(game_state) do
      metrics = Map.get(game_state, :metrics, %{})
      tick_metrics = Map.get(metrics, :ticks, %{})

      updated_ticks =
        tick_metrics
        |> Map.update(:total_ticks, 1, &(&1 + 1))
        |> Map.update(
          :tick_times,
          [total_time_us],
          &add_to_history(&1, total_time_us, @history_size)
        )
        |> Map.update(:peak_tick_time, total_time_us, &max(&1, total_time_us))
        |> Map.put(:last_tick_time, total_time_us)
        |> Map.put(:last_tick_breakdown, breakdown)

      updated_metrics = Map.put(metrics, :ticks, updated_ticks)
      Map.put(game_state, :metrics, updated_metrics)
    else
      game_state
    end
  end

  @doc """
  Records the current queue size for tracking peaks.
  """
  def record_queue_size(game_state, size) do
    if enabled?(game_state) do
      metrics = Map.get(game_state, :metrics, %{})
      mq_metrics = Map.get(metrics, :message_queue, %{})
      current = Map.get(mq_metrics, :peak_queue_size, 0)
      updated_mq = Map.put(mq_metrics, :peak_queue_size, max(current, size))
      updated_metrics = Map.put(metrics, :message_queue, updated_mq)
      Map.put(game_state, :metrics, updated_metrics)
    else
      game_state
    end
  end

  # --- Retrieval API ---

  @doc """
  Gets a summary of all performance metrics for UI display.
  """
  def get_summary(game_state) do
    metrics = Map.get(game_state, :metrics, %{})

    %{
      enabled: Map.get(metrics, :enabled, false),
      tick: get_tick_summary(metrics),
      systems: get_all_system_summaries(metrics),
      message_queue: get_message_queue_summary(metrics),
      ai: get_ai_metrics_summary(metrics)
    }
  end

  @doc """
  Gets detailed statistics for a specific system.
  """
  def get_system_stats(game_state, system_module) do
    metrics = Map.get(game_state, :metrics, %{})
    systems = Map.get(metrics, :systems, %{})
    system_metrics = Map.get(systems, system_module, %{})

    run_times = Map.get(system_metrics, :run_times, [])
    message_times = Map.get(system_metrics, :message_times, [])

    %{
      system: system_module,
      run: %{
        average_us: safe_average(run_times),
        peak_us: safe_max(run_times),
        last_us: List.first(run_times, 0),
        sample_count: length(run_times)
      },
      messages: %{
        average_processing_us: safe_average(message_times),
        peak_processing_us: safe_max(message_times),
        total_processed: Map.get(system_metrics, :messages_processed, 0)
      }
    }
  end

  @doc """
  Gets tick performance statistics.
  """
  def get_tick_stats(game_state) do
    metrics = Map.get(game_state, :metrics, %{})
    get_tick_summary(metrics)
  end

  @doc """
  Gets message queue statistics.
  """
  def get_message_queue_stats(game_state) do
    metrics = Map.get(game_state, :metrics, %{})
    get_message_queue_summary(metrics)
  end

  @doc """
  Resets all metrics to initial state.
  """
  def reset(game_state) do
    metrics = Map.get(game_state, :metrics, %{})
    enabled = Map.get(metrics, :enabled, true)
    game_state |> init() |> set_enabled(enabled)
  end

  @doc """
  Resets metrics for a specific system.
  """
  def reset_system(game_state, system_module) do
    if enabled?(game_state) do
      metrics = Map.get(game_state, :metrics, %{})
      systems = Map.get(metrics, :systems, %{})
      updated_systems = Map.put(systems, system_module, %{})
      updated_metrics = Map.put(metrics, :systems, updated_systems)
      Map.put(game_state, :metrics, updated_metrics)
    else
      game_state
    end
  end

  # --- Private Helpers ---

  defp update_system_metric(game_state, system_module, metric_key, value) do
    metrics = Map.get(game_state, :metrics, %{})
    systems = Map.get(metrics, :systems, %{})
    system_metrics = Map.get(systems, system_module, %{})

    updated_system =
      Map.update(system_metrics, metric_key, [value], &add_to_history(&1, value, @history_size))

    updated_systems = Map.put(systems, system_module, updated_system)
    updated_metrics = Map.put(metrics, :systems, updated_systems)
    Map.put(game_state, :metrics, updated_metrics)
  end

  defp update_system_counter(game_state, system_module, counter_key, increment) do
    metrics = Map.get(game_state, :metrics, %{})
    systems = Map.get(metrics, :systems, %{})
    system_metrics = Map.get(systems, system_module, %{})

    updated_system = Map.update(system_metrics, counter_key, increment, &(&1 + increment))
    updated_systems = Map.put(systems, system_module, updated_system)
    updated_metrics = Map.put(metrics, :systems, updated_systems)
    Map.put(game_state, :metrics, updated_metrics)
  end

  defp add_to_history(list, value, max_size) do
    [value | list] |> Enum.take(max_size)
  end

  defp get_tick_summary(metrics) do
    tick_metrics = Map.get(metrics, :ticks, %{})
    tick_times = Map.get(tick_metrics, :tick_times, [])

    %{
      total_ticks: Map.get(tick_metrics, :total_ticks, 0),
      average_tick_us: safe_average(tick_times),
      peak_tick_us: Map.get(tick_metrics, :peak_tick_time, 0),
      last_tick_us: Map.get(tick_metrics, :last_tick_time, 0),
      last_breakdown: Map.get(tick_metrics, :last_tick_breakdown, %{}),
      # Convert to ms for easier reading
      average_tick_ms: safe_average(tick_times) / 1000,
      peak_tick_ms: Map.get(tick_metrics, :peak_tick_time, 0) / 1000,
      last_tick_ms: Map.get(tick_metrics, :last_tick_time, 0) / 1000
    }
  end

  defp get_all_system_summaries(metrics) do
    systems = Map.get(metrics, :systems, %{})

    Map.new(systems, fn {system_module, system_metrics} ->
      run_times = Map.get(system_metrics, :run_times, [])
      message_times = Map.get(system_metrics, :message_times, [])

      summary = %{
        run_average_us: safe_average(run_times),
        run_peak_us: safe_max(run_times),
        run_last_us: List.first(run_times, 0),
        message_average_us: safe_average(message_times),
        message_peak_us: safe_max(message_times),
        messages_processed: Map.get(system_metrics, :messages_processed, 0),
        # Percentage of total tick time (if available)
        total_average_us: safe_average(run_times) + safe_average(message_times)
      }

      {system_module, summary}
    end)
  end

  defp get_message_queue_summary(metrics) do
    mq_metrics = Map.get(metrics, :message_queue, %{})
    publish_times = Map.get(mq_metrics, :publish_times, [])

    %{
      total_published: Map.get(mq_metrics, :total_published, 0),
      total_processed: Map.get(mq_metrics, :total_processed, 0),
      average_publish_us: safe_average(publish_times),
      peak_publish_us: safe_max(publish_times),
      peak_queue_size: Map.get(mq_metrics, :peak_queue_size, 0)
    }
  end

  defp get_ai_metrics_summary(metrics) do
    ai_metrics = Map.get(metrics, :ai_decisions, %{})
    entity_count = map_size(ai_metrics)

    if entity_count == 0 do
      %{
        tracked_entities: 0,
        total_planning_attempts: 0,
        total_successful_plans: 0,
        total_failed_plans: 0,
        total_backtracks: 0
      }
    else
      # Aggregate stats across all entities
      totals =
        Enum.reduce(ai_metrics, %{attempts: 0, success: 0, failed: 0, backtracks: 0}, fn {_id,
                                                                                          htn_metrics},
                                                                                         acc ->
          summary = HTN.get_planning_summary(htn_metrics)

          %{
            attempts: acc.attempts + summary.planning_attempts,
            success: acc.success + summary.successful_plans,
            failed: acc.failed + summary.failed_plans,
            backtracks: acc.backtracks + summary.total_backtracks
          }
        end)

      %{
        tracked_entities: entity_count,
        total_planning_attempts: totals.attempts,
        total_successful_plans: totals.success,
        total_failed_plans: totals.failed,
        total_backtracks: totals.backtracks,
        overall_success_rate:
          if(totals.attempts > 0,
            do: totals.success / totals.attempts * 100,
            else: 0.0
          )
      }
    end
  end

  defp safe_average([]), do: 0.0
  defp safe_average(list), do: Enum.sum(list) / length(list)

  defp safe_max([]), do: 0
  defp safe_max(list), do: Enum.max(list)

  # --- AI Decision Metrics ---

  @doc """
  Stores HTN metrics for an entity (e.g., character) in the game state.

  ## Example

      {:ok, plan, htn_metrics} = HTN.find_plan_with_metrics(domain, world, :goal, metrics)
      game_state = Metrics.store_ai_metrics(game_state, character_id, htn_metrics)
  """
  def store_ai_metrics(game_state, entity_id, %HTN.Metrics{} = htn_metrics) do
    if enabled?(game_state) do
      metrics = Map.get(game_state, :metrics, %{})
      ai_metrics = Map.get(metrics, :ai_decisions, %{})
      updated_ai = Map.put(ai_metrics, entity_id, htn_metrics)
      updated_metrics = Map.put(metrics, :ai_decisions, updated_ai)
      Map.put(game_state, :metrics, updated_metrics)
    else
      game_state
    end
  end

  @doc """
  Gets HTN metrics for a specific entity.
  Returns nil if no metrics exist for that entity.
  """
  def get_ai_metrics(game_state, entity_id) do
    metrics = Map.get(game_state, :metrics, %{})
    ai_metrics = Map.get(metrics, :ai_decisions, %{})
    Map.get(ai_metrics, entity_id)
  end

  @doc """
  Gets AI decision log for an entity, formatted for UI display.

  ## Options

  - `:limit` - Maximum decisions to return (default: 50)
  - `:chronological` - If true, oldest first (default: true)
  - `:types` - Filter by decision types (default: all)

  ## Example

      decisions = Metrics.get_ai_decisions(game_state, character_id, limit: 20)
      # => ["▶ Planning started for task :daily_routine", "→ Method :morning selected", ...]
  """
  def get_ai_decisions(game_state, entity_id, opts \\ []) do
    case get_ai_metrics(game_state, entity_id) do
      nil -> []
      htn_metrics -> HTN.get_formatted_log(htn_metrics, opts)
    end
  end

  @doc """
  Gets AI decision log as raw entries (for custom formatting by UI).
  """
  def get_ai_decision_log(game_state, entity_id) do
    case get_ai_metrics(game_state, entity_id) do
      nil -> []
      htn_metrics -> HTN.get_decision_log(htn_metrics)
    end
  end

  @doc """
  Gets AI planning summary statistics for an entity.

  Returns a map with:
  - `:planning_attempts` - Total planning attempts
  - `:successful_plans` - Number of successful plans
  - `:failed_plans` - Number of failed plans
  - `:success_rate` - Percentage of successful plans
  - `:total_backtracks` - Total backtrack events
  - `:avg_planning_time_us` - Average planning time in microseconds
  - `:method_selection_counts` - How often each method was selected
  - `:task_execution_counts` - How often each task was executed
  """
  def get_ai_summary(game_state, entity_id) do
    case get_ai_metrics(game_state, entity_id) do
      nil -> nil
      htn_metrics -> HTN.get_planning_summary(htn_metrics)
    end
  end

  @doc """
  Gets a combined AI summary across all tracked entities.
  Useful for overall AI performance monitoring.
  """
  def get_all_ai_summaries(game_state) do
    metrics = Map.get(game_state, :metrics, %{})
    ai_metrics = Map.get(metrics, :ai_decisions, %{})

    Map.new(ai_metrics, fn {entity_id, htn_metrics} ->
      {entity_id, HTN.get_planning_summary(htn_metrics)}
    end)
  end

  @doc """
  Clears AI metrics for a specific entity.
  """
  def clear_ai_metrics(game_state, entity_id) do
    if enabled?(game_state) do
      metrics = Map.get(game_state, :metrics, %{})
      ai_metrics = Map.get(metrics, :ai_decisions, %{})

      case Map.get(ai_metrics, entity_id) do
        nil ->
          game_state

        htn_metrics ->
          cleared = HTN.clear_metrics(htn_metrics)
          updated_ai = Map.put(ai_metrics, entity_id, cleared)
          updated_metrics = Map.put(metrics, :ai_decisions, updated_ai)
          Map.put(game_state, :metrics, updated_metrics)
      end
    else
      game_state
    end
  end

  @doc """
  Clears all AI metrics.
  """
  def clear_all_ai_metrics(game_state) do
    if enabled?(game_state) do
      metrics = Map.get(game_state, :metrics, %{})
      updated_metrics = Map.put(metrics, :ai_decisions, %{})
      Map.put(game_state, :metrics, updated_metrics)
    else
      game_state
    end
  end

  @doc """
  Creates a new HTN metrics tracker.
  Convenience function to create metrics for a new AI entity.

  ## Options

  - `:capture_world_snapshots` - Include world state in decisions (expensive, default: false)
  - `:max_decisions` - Maximum decisions to keep (default: 1000)
  """
  def new_ai_metrics(opts \\ []) do
    HTN.new_metrics(opts)
  end
end
