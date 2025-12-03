defmodule StellaInvictaUi.GameServer do
  @moduledoc """
  A GenServer that manages a running game instance.
  """
  use GenServer

  # Client API

  def start_link(opts \\ []) do
    name = Keyword.get(opts, :name, __MODULE__)
    GenServer.start_link(__MODULE__, opts, name: name)
  end

  def get_state(server \\ __MODULE__) do
    GenServer.call(server, :get_state)
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
    game_state =
      StellaInvicta.World.new_planet_world()
      |> StellaInvicta.Game.init()

    {:ok, game_state}
  end

  @impl true
  def handle_call(:get_state, _from, state) do
    {:reply, state, state}
  end

  @impl true
  def handle_call(:simulate_hour, _from, state) do
    new_state = StellaInvicta.Game.simulate_hour(state)
    broadcast_state_update(new_state)
    {:reply, new_state, new_state}
  end

  @impl true
  def handle_call(:simulate_day, _from, state) do
    new_state = StellaInvicta.Game.simulate_day(state)
    broadcast_state_update(new_state)
    {:reply, new_state, new_state}
  end

  @impl true
  def handle_call(:simulate_week, _from, state) do
    new_state = StellaInvicta.Game.simulate_week(state)
    broadcast_state_update(new_state)
    {:reply, new_state, new_state}
  end

  @impl true
  def handle_call(:simulate_month, _from, state) do
    new_state = StellaInvicta.Game.simulate_month(state)
    broadcast_state_update(new_state)
    {:reply, new_state, new_state}
  end

  @impl true
  def handle_call(:simulate_year, _from, state) do
    new_state = StellaInvicta.Game.simulate_year(state)
    broadcast_state_update(new_state)
    {:reply, new_state, new_state}
  end

  @impl true
  def handle_call(:reset, _from, _state) do
    new_state =
      StellaInvicta.World.new_planet_world()
      |> StellaInvicta.Game.init()

    broadcast_state_update(new_state)
    {:reply, new_state, new_state}
  end

  @impl true
  def handle_call({:toggle_system, system_module}, _from, state) do
    new_state = StellaInvicta.Game.toggle_system(state, system_module)
    broadcast_state_update(new_state)
    {:reply, new_state, new_state}
  end

  @impl true
  def handle_call(:list_systems, _from, state) do
    systems = StellaInvicta.Game.list_systems(state)
    {:reply, systems, state}
  end

  defp broadcast_state_update(state) do
    Phoenix.PubSub.broadcast(
      StellaInvictaUi.PubSub,
      "game:state",
      {:game_state_updated, state}
    )
  end
end
