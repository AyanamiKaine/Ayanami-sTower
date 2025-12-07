defmodule StellaInvictaUi.GameServer do
  @moduledoc """
  A GenServer that manages a running game instance.
  """
  use GenServer

  @max_message_history 100

  # Client API

  def start_link(opts \\ []) do
    name = Keyword.get(opts, :name, __MODULE__)
    GenServer.start_link(__MODULE__, opts, name: name)
  end

  def get_state(server \\ __MODULE__) do
    GenServer.call(server, :get_state)
  end

  def get_message_history(server \\ __MODULE__) do
    GenServer.call(server, :get_message_history)
  end

  def clear_message_history(server \\ __MODULE__) do
    GenServer.call(server, :clear_message_history)
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

  # Server Callbacks

  @impl true
  def init(_opts) do
    # Subscribe to the library's PubSub for all message queue events
    subscribe_to_message_topics()

    game_state =
      StellaInvicta.World.new_planet_world()
      |> StellaInvicta.Game.init()

    # Store game state and message history separately
    {:ok, %{game_state: game_state, message_history: []}}
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
  def handle_call(:get_message_history, _from, %{message_history: history} = state) do
    {:reply, history, state}
  end

  @impl true
  def handle_call(:clear_message_history, _from, state) do
    new_state = %{state | message_history: []}
    broadcast_state_update(new_state.game_state, [])
    {:reply, :ok, new_state}
  end

  @impl true
  def handle_call(:simulate_hour, _from, %{game_state: game_state} = state) do
    new_game_state = StellaInvicta.Game.simulate_hour(game_state)
    new_state = %{state | game_state: new_game_state}
    broadcast_state_update(new_game_state, state.message_history)
    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call(:simulate_day, _from, %{game_state: game_state} = state) do
    new_game_state = StellaInvicta.Game.simulate_day(game_state)
    new_state = %{state | game_state: new_game_state}
    broadcast_state_update(new_game_state, state.message_history)
    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call(:simulate_week, _from, %{game_state: game_state} = state) do
    new_game_state = StellaInvicta.Game.simulate_week(game_state)
    new_state = %{state | game_state: new_game_state}
    broadcast_state_update(new_game_state, state.message_history)
    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call(:simulate_month, _from, %{game_state: game_state} = state) do
    new_game_state = StellaInvicta.Game.simulate_month(game_state)
    new_state = %{state | game_state: new_game_state}
    broadcast_state_update(new_game_state, state.message_history)
    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call(:simulate_year, _from, %{game_state: game_state} = state) do
    new_game_state = StellaInvicta.Game.simulate_year(game_state)
    new_state = %{state | game_state: new_game_state}
    broadcast_state_update(new_game_state, state.message_history)
    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call(:reset, _from, _state) do
    new_game_state =
      StellaInvicta.World.new_planet_world()
      |> StellaInvicta.Game.init()

    new_state = %{game_state: new_game_state, message_history: []}
    broadcast_state_update(new_game_state, [])
    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call({:toggle_system, system_module}, _from, %{game_state: game_state} = state) do
    new_game_state = StellaInvicta.Game.toggle_system(game_state, system_module)
    new_state = %{state | game_state: new_game_state}
    broadcast_state_update(new_game_state, state.message_history)
    {:reply, new_game_state, new_state}
  end

  @impl true
  def handle_call(:list_systems, _from, %{game_state: game_state} = state) do
    systems = StellaInvicta.Game.list_systems(game_state)
    {:reply, systems, state}
  end

  # Handle messages from the library's PubSub
  @impl true
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
    broadcast_state_update(game_state, new_history)

    {:noreply, new_state}
  end

  defp broadcast_state_update(game_state, message_history) do
    Phoenix.PubSub.broadcast(
      StellaInvictaUi.PubSub,
      "game:state",
      {:game_state_updated, game_state, message_history}
    )
  end
end
