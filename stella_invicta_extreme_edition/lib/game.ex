defmodule StellaInvicta.Game do
  @moduledoc """
  Main game module with configurable systems.
  Systems can be enabled/disabled at runtime.
  Systems can communicate via the message queue.
  """

  alias StellaInvicta.MessageQueue
  alias StellaInvicta.System, as: SystemBehaviour

  # Default systems in execution order
  @default_systems [
    {StellaInvicta.System.Date, true},
    {StellaInvicta.System.Age, true}
  ]

  @doc """
  Initializes the game state with system configuration and message queue.
  Call this when creating a new game to set up the systems map.
  """
  def init(game_state) do
    systems = Map.new(@default_systems)

    game_state
    |> Map.put(:systems, systems)
    |> MessageQueue.init()
    |> setup_system_subscriptions()
  end

  # Sets up subscriptions for all systems that declare them
  defp setup_system_subscriptions(game_state) do
    systems = Map.get(game_state, :systems, %{})

    Enum.reduce(systems, game_state, fn {system_module, _enabled}, acc ->
      subscriptions = SystemBehaviour.get_subscriptions(system_module)

      Enum.reduce(subscriptions, acc, fn topic, inner_acc ->
        MessageQueue.subscribe_system(inner_acc, system_module, topic)
      end)
    end)
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
  Also clears any pending messages for the system to prevent queue buildup.
  """
  def disable_system(game_state, system_module) do
    systems = Map.get(game_state, :systems, %{})
    updated_systems = Map.put(systems, system_module, false)

    game_state
    |> Map.put(:systems, updated_systems)
    |> MessageQueue.clear_messages(system_module)
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
  Also sets up the system's message subscriptions.
  """
  def register_system(game_state, system_module, enabled \\ false) do
    systems = Map.get(game_state, :systems, %{})
    updated_systems = Map.put(systems, system_module, enabled)

    # Set up subscriptions for the new system
    subscriptions = SystemBehaviour.get_subscriptions(system_module)

    game_state
    |> Map.put(:systems, updated_systems)
    |> invalidate_system_order_cache()
    |> then(fn state ->
      Enum.reduce(subscriptions, state, fn topic, acc ->
        MessageQueue.subscribe_system(acc, system_module, topic)
      end)
    end)
  end

  @doc """
  Unregisters a system.
  Also removes all subscriptions and clears pending messages for the system.
  """
  def unregister_system(game_state, system_module) do
    systems = Map.get(game_state, :systems, %{})
    updated_systems = Map.delete(systems, system_module)

    game_state
    |> Map.put(:systems, updated_systems)
    |> invalidate_system_order_cache()
    |> MessageQueue.cleanup_system(system_module)
  end

  @doc """
  Runs a single game tick, executing all enabled systems in order.
  Also processes pending messages for each system.
  Disabled systems have their message queues cleared to prevent buildup.
  """
  def run_tick(game_state) do
    game_state = Map.update!(game_state, :current_tick, &(&1 + 1))

    # Use cached system order if available, otherwise compute and cache
    {ordered_systems, game_state} = get_or_compute_system_order(game_state)

    # Run each enabled system in order, processing messages first
    # Clear messages for disabled systems to prevent queue buildup
    systems = Map.get(game_state, :systems, %{})

    Enum.reduce(ordered_systems, game_state, fn system_module, acc ->
      if Map.get(systems, system_module, true) do
        acc
        |> process_system_messages(system_module)
        |> system_module.run()
      else
        # Clear messages for disabled systems
        MessageQueue.clear_messages(acc, system_module)
      end
    end)
  end

  # Caches the computed system order to avoid recalculating every tick
  defp get_or_compute_system_order(game_state) do
    case Map.get(game_state, :cached_system_order) do
      nil ->
        ordered = compute_system_order(game_state)
        {ordered, Map.put(game_state, :cached_system_order, ordered)}

      cached ->
        {cached, game_state}
    end
  end

  defp compute_system_order(game_state) do
    # Get system order from @default_systems to maintain execution order
    system_order = Enum.map(@default_systems, fn {mod, _} -> mod end)

    # Also include any dynamically registered systems
    all_systems = Map.get(game_state, :systems, %{}) |> Map.keys()
    additional_systems = all_systems -- system_order
    system_order ++ additional_systems
  end

  # Invalidate cached system order when systems change
  defp invalidate_system_order_cache(game_state) do
    Map.delete(game_state, :cached_system_order)
  end

  # Processes all pending messages for a system
  defp process_system_messages(game_state, system_module) do
    {messages, game_state} = MessageQueue.pop_messages(game_state, system_module)

    Enum.reduce(messages, game_state, fn {topic, message}, acc ->
      SystemBehaviour.dispatch_message(system_module, acc, topic, message)
    end)
  end

  @doc """
  Publishes a message to a topic. Convenience wrapper around MessageQueue.publish/3.
  """
  def publish(game_state, topic, message) do
    MessageQueue.publish(game_state, topic, message)
  end

  @doc """
  Subscribes a system to a topic. Convenience wrapper around MessageQueue.subscribe_system/3.
  """
  def subscribe(game_state, system_module, topic) do
    MessageQueue.subscribe_system(game_state, system_module, topic)
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
