defmodule StellaInvictaUi.GameServer do
  @moduledoc """
  A GenServer that manages a running game instance.
  """
  use GenServer

  @max_message_history 100

  # Time scale for each speed setting
  # Each tick simulates this amount of in-game time
  @speed_scales %{
    :hour => :hour,
    :day => :day,
    :week => :week,
    :month => :month,
    :year => :year
  }

  # Fixed interval (in ms) between ticks for all speeds
  @tick_interval 500

  # Client API

  def start_link(opts \\ []) do
    name = Keyword.get(opts, :name, __MODULE__)
    game_id = Keyword.get(opts, :game_id)
    GenServer.start_link(__MODULE__, {opts, game_id}, name: name)
  end

  def get_state(server \\ __MODULE__) do
    GenServer.call(server, :get_state)
  end

  def get_simulation_state(server \\ __MODULE__) do
    GenServer.call(server, :get_simulation_state)
  end

  def get_message_history(server \\ __MODULE__) do
    GenServer.call(server, :get_message_history)
  end

  def clear_message_history(server \\ __MODULE__) do
    GenServer.call(server, :clear_message_history)
  end

  def play(server \\ __MODULE__) do
    GenServer.call(server, :play)
  end

  def pause(server \\ __MODULE__) do
    GenServer.call(server, :pause)
  end

  def set_speed(server \\ __MODULE__, speed) when speed in [:hour, :day, :week, :month, :year] do
    GenServer.call(server, {:set_speed, speed})
  end

  def simulate_hour(server \\ __MODULE__) do
    GenServer.call(server, :simulate_hour)
  end

  def simulate_day(server \\ __MODULE__) do
    GenServer.call(server, :simulate_day)
  end

  def simulate_week(server \\ __MODULE__) do
    GenServer.call(server, :simulate_week)
  end

  def simulate_month(server \\ __MODULE__) do
    GenServer.call(server, :simulate_month)
  end

  def simulate_year(server \\ __MODULE__) do
    GenServer.call(server, :simulate_year)
  end

  def reset(server \\ __MODULE__) do
    GenServer.call(server, :reset)
  end

  def toggle_system(server \\ __MODULE__, system_module) do
    GenServer.call(server, {:toggle_system, system_module})
  end

  def list_systems(server \\ __MODULE__) do
    GenServer.call(server, :list_systems)
  end

  def toggle_metrics(server \\ __MODULE__) do
    GenServer.call(server, :toggle_metrics)
  end

  def reset_metrics(server \\ __MODULE__) do
    GenServer.call(server, :reset_metrics)
  end

  # Server Callbacks

  @impl true
  def init({_opts, game_id}) do
    # Subscribe to the library's PubSub for all message queue events
    subscribe_to_message_topics()

    game_state =
      StellaInvicta.World.new_planet_world()
      |> StellaInvicta.Game.init()

    # Store game state, message history, and simulation state
    {:ok,
     %{
       game_id: game_id,
       game_state: game_state,
       message_history: [],
       playing: false,
       speed: :hour,
       timer_ref: nil
     }}
  end

  defp subscribe_to_message_topics do
    # Subscribe to common topics to capture messages
    topics = [:date_events, :character_events, :economy_events, :system_events]

    Enum.each(topics, fn topic ->
      StellaInvicta.MessageQueue.subscribe_process(topic)
    end)
  end

  @impl true
  def handle_call(:get_state, _from, %{game_state: game_state} = state) do
    {:reply, game_state, state}
  end

  @impl true
  def handle_call(:get_simulation_state, _from, state) do
    sim_state = %{playing: state.playing, speed: state.speed}
    {:reply, sim_state, state}
  end

  @impl true
  def handle_call(:get_message_history, _from, %{message_history: history} = state) do
    {:reply, history, state}
  end

  @impl true
  def handle_call(:clear_message_history, _from, state) do
    new_state = %{state | message_history: []}
    broadcast_state_update(state.game_id, new_state.game_state, [], state.playing, state.speed)
    {:reply, :ok, new_state}
  end

  @impl true
  def handle_call(:simulate_hour, _from, %{game_state: game_state} = state) do
    new_game_state = StellaInvicta.Game.simulate_hour(game_state)
    new_state = %{state | game_state: new_game_state}

    broadcast_state_update(
      state.game_id,
      new_game_state,
      state.message_history,
      state.playing,
      state.speed
    )

    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call(:simulate_day, _from, %{game_state: game_state} = state) do
    new_game_state = StellaInvicta.Game.simulate_day(game_state)
    new_state = %{state | game_state: new_game_state}

    broadcast_state_update(
      state.game_id,
      new_game_state,
      state.message_history,
      state.playing,
      state.speed
    )

    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call(:simulate_week, _from, %{game_state: game_state} = state) do
    new_game_state = StellaInvicta.Game.simulate_week(game_state)
    new_state = %{state | game_state: new_game_state}

    broadcast_state_update(
      state.game_id,
      new_game_state,
      state.message_history,
      state.playing,
      state.speed
    )

    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call(:simulate_month, _from, %{game_state: game_state} = state) do
    new_game_state = StellaInvicta.Game.simulate_month(game_state)
    new_state = %{state | game_state: new_game_state}

    broadcast_state_update(
      state.game_id,
      new_game_state,
      state.message_history,
      state.playing,
      state.speed
    )

    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call(:simulate_year, _from, %{game_state: game_state} = state) do
    new_game_state = StellaInvicta.Game.simulate_year(game_state)
    new_state = %{state | game_state: new_game_state}

    broadcast_state_update(
      state.game_id,
      new_game_state,
      state.message_history,
      state.playing,
      state.speed
    )

    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call(:reset, _from, state) do
    # Cancel any existing timer
    if state.timer_ref, do: Process.cancel_timer(state.timer_ref)

    new_game_state =
      StellaInvicta.World.new_planet_world()
      |> StellaInvicta.Game.init()

    new_state = %{
      game_state: new_game_state,
      message_history: [],
      playing: false,
      speed: :hour,
      timer_ref: nil
    }

    broadcast_state_update(state.game_id, new_game_state, [], new_state.playing, new_state.speed)
    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call(:play, _from, %{playing: true} = state) do
    # Already playing
    {:reply, :ok, state}
  end

  def handle_call(:play, _from, state) do
    timer_ref = Process.send_after(self(), :tick, @tick_interval)
    new_state = %{state | playing: true, timer_ref: timer_ref}

    broadcast_state_update(
      state.game_id,
      state.game_state,
      state.message_history,
      true,
      state.speed
    )

    {:reply, :ok, new_state}
  end

  @impl true
  def handle_call(:pause, _from, %{playing: false} = state) do
    # Already paused
    {:reply, :ok, state}
  end

  def handle_call(:pause, _from, state) do
    if state.timer_ref, do: Process.cancel_timer(state.timer_ref)
    new_state = %{state | playing: false, timer_ref: nil}

    broadcast_state_update(
      state.game_id,
      state.game_state,
      state.message_history,
      false,
      state.speed
    )

    {:reply, :ok, new_state}
  end

  @impl true
  def handle_call({:set_speed, speed}, _from, state) do
    # Cancel existing timer if playing
    if state.timer_ref, do: Process.cancel_timer(state.timer_ref)

    # Start new timer with fixed interval if playing
    timer_ref =
      if state.playing do
        Process.send_after(self(), :tick, @tick_interval)
      else
        nil
      end

    new_state = %{state | speed: speed, timer_ref: timer_ref}

    broadcast_state_update(
      state.game_id,
      state.game_state,
      state.message_history,
      state.playing,
      speed
    )

    {:reply, :ok, new_state}
  end

  @impl true
  def handle_call({:toggle_system, system_module}, _from, %{game_state: game_state} = state) do
    new_game_state = StellaInvicta.Game.toggle_system(game_state, system_module)
    new_state = %{state | game_state: new_game_state}

    broadcast_state_update(
      state.game_id,
      new_game_state,
      state.message_history,
      state.playing,
      state.speed
    )

    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call(:list_systems, _from, %{game_state: game_state} = state) do
    systems = StellaInvicta.Game.list_systems(game_state)
    {:reply, systems, state}
  end

  @impl true
  def handle_call(:toggle_metrics, _from, %{game_state: game_state} = state) do
    current_enabled = StellaInvicta.Metrics.enabled?(game_state)
    new_game_state = StellaInvicta.Metrics.set_enabled(game_state, !current_enabled)
    new_state = %{state | game_state: new_game_state}

    broadcast_state_update(
      state.game_id,
      new_game_state,
      state.message_history,
      state.playing,
      state.speed
    )

    {:reply, :ok, new_state}
  end

  @impl true
  def handle_call(:reset_metrics, _from, %{game_state: game_state} = state) do
    new_game_state = StellaInvicta.Metrics.reset(game_state)
    new_state = %{state | game_state: new_game_state}

    broadcast_state_update(
      state.game_id,
      new_game_state,
      state.message_history,
      state.playing,
      state.speed
    )

    {:reply, :ok, new_state}
  end

  # Handle the tick timer for real-time simulation
  @impl true
  def handle_info(:tick, %{playing: false} = state) do
    # Paused, don't process
    {:noreply, state}
  end

  def handle_info(:tick, state) do
    # Run one tick based on the speed scale
    new_game_state =
      case state.speed do
        :hour -> StellaInvicta.Game.simulate_hour(state.game_state)
        :day -> StellaInvicta.Game.simulate_day(state.game_state)
        :week -> StellaInvicta.Game.simulate_week(state.game_state)
        :month -> StellaInvicta.Game.simulate_month(state.game_state)
        :year -> StellaInvicta.Game.simulate_year(state.game_state)
        _ -> state.game_state
      end

    # Schedule next tick with fixed interval
    timer_ref = Process.send_after(self(), :tick, @tick_interval)

    new_state = %{state | game_state: new_game_state, timer_ref: timer_ref}

    broadcast_state_update(
      state.game_id,
      new_game_state,
      state.message_history,
      true,
      state.speed
    )

    {:noreply, new_state}
  end

  # Handle messages from the library's PubSub
  def handle_info(message, %{message_history: history, game_state: game_state} = state) do
    # Add message to history with timestamp
    entry = %{
      timestamp: DateTime.utc_now(),
      tick: Map.get(game_state, :current_tick, 0),
      message: message
    }

    # Keep only last N messages
    new_history = Enum.take([entry | history], @max_message_history)
    new_state = %{state | message_history: new_history}

    # Broadcast the update
    broadcast_state_update(state.game_id, game_state, new_history, state.playing, state.speed)

    {:noreply, new_state}
  end

  defp broadcast_state_update(game_id, game_state, message_history, playing, speed) do
    Phoenix.PubSub.broadcast(
      StellaInvictaUi.PubSub,
      "game:#{game_id}:state",
      {:game_state_updated, game_state, message_history, playing, speed}
    )
  end
end
