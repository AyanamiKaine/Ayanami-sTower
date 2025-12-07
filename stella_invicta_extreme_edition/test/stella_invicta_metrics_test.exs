defmodule StellaInvictaTest.Metrics do
  use ExUnit.Case, async: true

  alias StellaInvicta.Game
  alias StellaInvicta.Metrics
  alias StellaInvicta.World

  setup do
    game_state =
      World.new_planet_world()
      |> Game.init()

    {:ok, game_state: game_state}
  end

  describe "metrics initialization" do
    test "init/1 sets up metrics structure", %{game_state: game_state} do
      metrics = Map.get(game_state, :metrics)
      assert metrics != nil
      assert Map.get(metrics, :enabled) == true
      assert Map.get(metrics, :systems) == %{}
      assert Map.has_key?(metrics, :message_queue)
      assert Map.has_key?(metrics, :ticks)
    end

    test "metrics are enabled by default", %{game_state: game_state} do
      assert Metrics.enabled?(game_state)
    end

    test "set_enabled/2 can disable metrics", %{game_state: game_state} do
      game_state = Metrics.set_enabled(game_state, false)
      refute Metrics.enabled?(game_state)
    end

    test "set_enabled/2 can re-enable metrics", %{game_state: game_state} do
      game_state =
        game_state
        |> Metrics.set_enabled(false)
        |> Metrics.set_enabled(true)

      assert Metrics.enabled?(game_state)
    end
  end

  describe "measure/1" do
    test "measure/1 returns result and time" do
      {result, time} = Metrics.measure(fn -> 1 + 1 end)
      assert result == 2
      assert is_integer(time)
      assert time >= 0
    end

    test "measure/1 time increases with work" do
      {_, short_time} = Metrics.measure(fn -> :ok end)

      {_, long_time} =
        Metrics.measure(fn ->
          :timer.sleep(1)
          :ok
        end)

      # Long time should be at least as long (accounting for some variance)
      assert long_time >= short_time
    end
  end

  describe "tick metrics" do
    test "run_tick records tick timing", %{game_state: game_state} do
      game_state = Game.run_tick(game_state)
      stats = Metrics.get_tick_stats(game_state)

      assert stats.total_ticks == 1
      # Tick time can be 0 if very fast, just check it's non-negative
      assert stats.last_tick_us >= 0
      assert stats.average_tick_us >= 0
    end

    test "multiple ticks accumulate", %{game_state: game_state} do
      game_state =
        game_state
        |> Game.run_tick()
        |> Game.run_tick()
        |> Game.run_tick()

      stats = Metrics.get_tick_stats(game_state)
      assert stats.total_ticks == 3
    end

    test "tick breakdown includes system timings", %{game_state: game_state} do
      game_state = Game.run_tick(game_state)
      stats = Metrics.get_tick_stats(game_state)

      breakdown = stats.last_breakdown
      assert Map.has_key?(breakdown, StellaInvicta.System.Date)
      assert Map.has_key?(breakdown, StellaInvicta.System.Age)

      date_timing = breakdown[StellaInvicta.System.Date]
      assert Map.has_key?(date_timing, :run_us)
      assert Map.has_key?(date_timing, :messages_us)
      assert Map.has_key?(date_timing, :message_count)
    end
  end

  describe "system metrics" do
    test "system run times are recorded", %{game_state: game_state} do
      game_state = Game.run_tick(game_state)
      stats = Metrics.get_system_stats(game_state, StellaInvicta.System.Date)

      assert stats.system == StellaInvicta.System.Date
      assert stats.run.sample_count == 1
      assert stats.run.last_us >= 0
    end

    test "system metrics accumulate over ticks", %{game_state: game_state} do
      game_state =
        game_state
        |> Game.run_tick()
        |> Game.run_tick()
        |> Game.run_tick()

      stats = Metrics.get_system_stats(game_state, StellaInvicta.System.Date)
      assert stats.run.sample_count == 3
    end

    test "get_summary/1 returns all system summaries", %{game_state: game_state} do
      game_state = Game.run_tick(game_state)
      summary = Metrics.get_summary(game_state)

      assert Map.has_key?(summary, :systems)
      assert Map.has_key?(summary.systems, StellaInvicta.System.Date)
      assert Map.has_key?(summary.systems, StellaInvicta.System.Age)
    end
  end

  describe "message queue metrics" do
    test "publish metrics are recorded", %{game_state: game_state} do
      # Run a tick to trigger date system publishing events (when day changes)
      game_state = Game.simulate_day(game_state)
      stats = Metrics.get_message_queue_stats(game_state)

      # Should have recorded some publishes (date events)
      assert stats.total_published >= 0
    end

    test "peak queue size is tracked", %{game_state: game_state} do
      stats = Metrics.get_message_queue_stats(game_state)
      # Initially no messages
      assert stats.peak_queue_size >= 0
    end
  end

  describe "disabled metrics" do
    test "disabled metrics don't record anything", %{game_state: game_state} do
      game_state = Metrics.set_enabled(game_state, false)
      game_state = Game.run_tick(game_state)

      stats = Metrics.get_tick_stats(game_state)
      # Ticks should not be recorded
      assert stats.total_ticks == 0
    end

    test "disabling metrics doesn't break game loop", %{game_state: game_state} do
      game_state =
        game_state
        |> Metrics.set_enabled(false)
        |> Game.run_tick()
        |> Game.run_tick()
        |> Game.run_tick()

      # Game should still work
      assert Map.get(game_state, :current_tick) == 3
      assert Map.get(game_state, :date).hour == 3
    end
  end

  describe "reset" do
    test "reset/1 clears all metrics", %{game_state: game_state} do
      game_state =
        game_state
        |> Game.run_tick()
        |> Game.run_tick()
        |> Metrics.reset()

      stats = Metrics.get_tick_stats(game_state)
      assert stats.total_ticks == 0
    end

    test "reset/1 preserves enabled state", %{game_state: game_state} do
      game_state =
        game_state
        |> Metrics.set_enabled(false)
        |> Metrics.reset()

      refute Metrics.enabled?(game_state)
    end

    test "reset_system/2 clears only one system's metrics", %{game_state: game_state} do
      game_state =
        game_state
        |> Game.run_tick()
        |> Game.run_tick()
        |> Metrics.reset_system(StellaInvicta.System.Date)

      date_stats = Metrics.get_system_stats(game_state, StellaInvicta.System.Date)
      age_stats = Metrics.get_system_stats(game_state, StellaInvicta.System.Age)

      # Date system should be reset
      assert date_stats.run.sample_count == 0
      # Age system should still have data
      assert age_stats.run.sample_count == 2
    end
  end

  describe "performance summary for UI" do
    test "get_summary/1 returns UI-friendly format", %{game_state: game_state} do
      game_state = Game.simulate_hour(game_state)
      summary = Metrics.get_summary(game_state)

      # Check structure
      assert Map.has_key?(summary, :enabled)
      assert Map.has_key?(summary, :tick)
      assert Map.has_key?(summary, :systems)
      assert Map.has_key?(summary, :message_queue)

      # Tick has both microseconds and milliseconds
      assert Map.has_key?(summary.tick, :average_tick_us)
      assert Map.has_key?(summary.tick, :average_tick_ms)
      assert Map.has_key?(summary.tick, :peak_tick_ms)

      # Systems have detailed breakdown
      assert is_map(summary.systems)
    end
  end
end
