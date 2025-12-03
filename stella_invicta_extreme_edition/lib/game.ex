defmodule StellaInvicta.Game do
  @moduledoc """
  Main game module with configurable systems.
  Systems can be enabled/disabled at runtime.
  """

  # Default systems in execution order
  @default_systems [
    {StellaInvicta.System.Date, true},
    {StellaInvicta.System.Age, true}
  ]

  @doc """
  Initializes the game state with system configuration.
  Call this when creating a new game to set up the systems map.
  """
  def init(game_state) do
    systems = Map.new(@default_systems)
    Map.put(game_state, :systems, systems)
  end

  @doc """
  Enables a system by module name.
  """
  def enable_system(game_state, system_module) do
    systems = Map.get(game_state, :systems, %{})
    updated_systems = Map.put(systems, system_module, true)
    Map.put(game_state, :systems, updated_systems)
  end

  @doc """
  Disables a system by module name.
  """
  def disable_system(game_state, system_module) do
    systems = Map.get(game_state, :systems, %{})
    updated_systems = Map.put(systems, system_module, false)
    Map.put(game_state, :systems, updated_systems)
  end

  @doc """
  Toggles a system on/off.
  """
  def toggle_system(game_state, system_module) do
    systems = Map.get(game_state, :systems, %{})
    current_state = Map.get(systems, system_module, true)
    updated_systems = Map.put(systems, system_module, !current_state)
    Map.put(game_state, :systems, updated_systems)
  end

  @doc """
  Checks if a system is enabled.
  """
  def system_enabled?(game_state, system_module) do
    systems = Map.get(game_state, :systems, %{})
    Map.get(systems, system_module, true)
  end

  @doc """
  Returns a list of all systems and their enabled status.
  """
  def list_systems(game_state) do
    Map.get(game_state, :systems, %{})
  end

  @doc """
  Registers a new system. Systems are disabled by default when added.
  """
  def register_system(game_state, system_module, enabled \\ false) do
    systems = Map.get(game_state, :systems, %{})
    updated_systems = Map.put(systems, system_module, enabled)
    Map.put(game_state, :systems, updated_systems)
  end

  @doc """
  Unregisters a system.
  """
  def unregister_system(game_state, system_module) do
    systems = Map.get(game_state, :systems, %{})
    updated_systems = Map.delete(systems, system_module)
    Map.put(game_state, :systems, updated_systems)
  end

  @doc """
  Runs a single game tick, executing all enabled systems in order.
  """
  def run_tick(game_state) do
    game_state = Map.update!(game_state, :current_tick, &(&1 + 1))

    # Get system order from @default_systems to maintain execution order
    system_order = Enum.map(@default_systems, fn {mod, _} -> mod end)

    # Run each enabled system in order
    Enum.reduce(system_order, game_state, fn system_module, acc ->
      if system_enabled?(acc, system_module) do
        system_module.run(acc)
      else
        acc
      end
    end)
  end

  def simulate_hour(game_state) do
    run_tick(game_state)
  end

  def simulate_day(game_state) do
    1..24
    |> Enum.reduce(game_state, fn _, acc -> simulate_hour(acc) end)
  end

  def simulate_week(game_state) do
    1..7
    |> Enum.reduce(game_state, fn _, acc -> simulate_day(acc) end)
  end

  def simulate_month(game_state) do
    1..30
    |> Enum.reduce(game_state, fn _, acc -> simulate_day(acc) end)
  end

  def simulate_year(game_state) do
    1..12
    |> Enum.reduce(game_state, fn _, acc -> simulate_month(acc) end)
  end
end
