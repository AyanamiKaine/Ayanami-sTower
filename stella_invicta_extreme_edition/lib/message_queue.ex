defmodule StellaInvicta.MessageQueue do
  @moduledoc """
  Message queue system for inter-system communication using Phoenix.PubSub.

  Systems can publish messages to topics and subscribe to receive messages.
  Messages are stored in the game state and processed during game ticks.

  ## Topics

  Topics are atoms or strings that identify message channels. Common patterns:
  - `:character_events` - Character-related events (birth, death, age changes)
  - `:date_events` - Date/time progression events
  - `:economy_events` - Economic changes
  - `{:entity, entity_id}` - Entity-specific events

  ## Usage

      # Publishing a message
      game_state = MessageQueue.publish(game_state, :character_events, {:character_died, character_id})

      # Subscribing a system to a topic
      game_state = MessageQueue.subscribe(game_state, MySystem, :character_events)

      # Getting pending messages for a system
      messages = MessageQueue.get_messages(game_state, MySystem)
  """

  alias StellaInvicta.Supervisor, as: LibSup

  # --- PubSub-based API (for real-time external subscribers) ---

  @doc """
  Subscribes the current process to a topic via Phoenix.PubSub.
  Use this for external processes that need real-time notifications.
  """
  def subscribe_process(topic) do
    Phoenix.PubSub.subscribe(LibSup.pubsub_name(), to_string(topic))
  end

  @doc """
  Unsubscribes the current process from a topic.
  """
  def unsubscribe_process(topic) do
    Phoenix.PubSub.unsubscribe(LibSup.pubsub_name(), to_string(topic))
  end

  @doc """
  Broadcasts a message to all external subscribers via Phoenix.PubSub.
  """
  def broadcast(topic, message) do
    Phoenix.PubSub.broadcast(LibSup.pubsub_name(), to_string(topic), message)
  end

  # --- Game State-based API (for system-to-system communication) ---

  @doc """
  Initializes the message queue in the game state.
  Call this when setting up a new game.
  """
  def init(game_state) do
    game_state
    |> Map.put(:message_queue, %{})
    |> Map.put(:system_subscriptions, %{})
  end

  @doc """
  Subscribes a system module to a topic.
  The system will receive messages published to this topic.
  """
  def subscribe_system(game_state, system_module, topic) do
    subscriptions = Map.get(game_state, :system_subscriptions, %{})

    system_topics = Map.get(subscriptions, system_module, MapSet.new())
    updated_topics = MapSet.put(system_topics, topic)
    updated_subscriptions = Map.put(subscriptions, system_module, updated_topics)

    Map.put(game_state, :system_subscriptions, updated_subscriptions)
  end

  @doc """
  Unsubscribes a system module from a topic.
  """
  def unsubscribe_system(game_state, system_module, topic) do
    subscriptions = Map.get(game_state, :system_subscriptions, %{})

    system_topics = Map.get(subscriptions, system_module, MapSet.new())
    updated_topics = MapSet.delete(system_topics, topic)
    updated_subscriptions = Map.put(subscriptions, system_module, updated_topics)

    Map.put(game_state, :system_subscriptions, updated_subscriptions)
  end

  @doc """
  Publishes a message to a topic.
  The message will be queued for all systems subscribed to this topic.
  Also broadcasts to external PubSub subscribers if PubSub is running.
  """
  def publish(game_state, topic, message) do
    # Broadcast to external PubSub subscribers (if running)
    try do
      broadcast(topic, message)
    rescue
      # PubSub not running, skip external broadcast
      ArgumentError -> :ok
    end

    # Queue message for system subscribers
    queue = Map.get(game_state, :message_queue, %{})
    subscriptions = Map.get(game_state, :system_subscriptions, %{})

    # Find all systems subscribed to this topic
    subscribed_systems =
      subscriptions
      |> Enum.filter(fn {_system, topics} -> MapSet.member?(topics, topic) end)
      |> Enum.map(fn {system, _topics} -> system end)

    # Add message to each system's queue
    updated_queue =
      Enum.reduce(subscribed_systems, queue, fn system, acc ->
        system_messages = Map.get(acc, system, [])
        Map.put(acc, system, system_messages ++ [{topic, message}])
      end)

    Map.put(game_state, :message_queue, updated_queue)
  end

  @doc """
  Gets all pending messages for a system.
  Returns a list of {topic, message} tuples.
  """
  def get_messages(game_state, system_module) do
    queue = Map.get(game_state, :message_queue, %{})
    Map.get(queue, system_module, [])
  end

  @doc """
  Gets and clears all pending messages for a system.
  Returns {messages, updated_game_state}.
  """
  def pop_messages(game_state, system_module) do
    queue = Map.get(game_state, :message_queue, %{})
    messages = Map.get(queue, system_module, [])
    updated_queue = Map.put(queue, system_module, [])
    updated_state = Map.put(game_state, :message_queue, updated_queue)

    {messages, updated_state}
  end

  @doc """
  Clears all messages for a system.
  """
  def clear_messages(game_state, system_module) do
    queue = Map.get(game_state, :message_queue, %{})
    updated_queue = Map.put(queue, system_module, [])
    Map.put(game_state, :message_queue, updated_queue)
  end

  @doc """
  Clears all messages in the queue.
  """
  def clear_all(game_state) do
    Map.put(game_state, :message_queue, %{})
  end

  @doc """
  Returns the list of topics a system is subscribed to.
  """
  def get_subscriptions(game_state, system_module) do
    subscriptions = Map.get(game_state, :system_subscriptions, %{})
    Map.get(subscriptions, system_module, MapSet.new()) |> MapSet.to_list()
  end

  @doc """
  Returns all systems subscribed to a specific topic.
  """
  def get_subscribers(game_state, topic) do
    subscriptions = Map.get(game_state, :system_subscriptions, %{})

    subscriptions
    |> Enum.filter(fn {_system, topics} -> MapSet.member?(topics, topic) end)
    |> Enum.map(fn {system, _topics} -> system end)
  end
end
