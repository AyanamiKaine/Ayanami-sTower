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

  # Maximum number of messages per system in the queue
  # Prevents unbounded memory growth for disabled systems
  @default_max_queue_size 100

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
    # Reverse index: topic -> set of systems (for O(1) subscriber lookup)
    |> Map.put(:topic_subscribers, %{})
  end

  @doc """
  Subscribes a system module to a topic.
  The system will receive messages published to this topic.
  """
  def subscribe_system(game_state, system_module, topic) do
    subscriptions = Map.get(game_state, :system_subscriptions, %{})
    topic_subscribers = Map.get(game_state, :topic_subscribers, %{})

    # Update system -> topics mapping
    system_topics = Map.get(subscriptions, system_module, MapSet.new())
    updated_topics = MapSet.put(system_topics, topic)
    updated_subscriptions = Map.put(subscriptions, system_module, updated_topics)

    # Update topic -> systems reverse index
    topic_systems = Map.get(topic_subscribers, topic, MapSet.new())
    updated_topic_systems = MapSet.put(topic_systems, system_module)
    updated_topic_subscribers = Map.put(topic_subscribers, topic, updated_topic_systems)

    game_state
    |> Map.put(:system_subscriptions, updated_subscriptions)
    |> Map.put(:topic_subscribers, updated_topic_subscribers)
  end

  @doc """
  Unsubscribes a system module from a topic.
  """
  def unsubscribe_system(game_state, system_module, topic) do
    subscriptions = Map.get(game_state, :system_subscriptions, %{})
    topic_subscribers = Map.get(game_state, :topic_subscribers, %{})

    # Update system -> topics mapping
    system_topics = Map.get(subscriptions, system_module, MapSet.new())
    updated_topics = MapSet.delete(system_topics, topic)
    updated_subscriptions = Map.put(subscriptions, system_module, updated_topics)

    # Update topic -> systems reverse index
    topic_systems = Map.get(topic_subscribers, topic, MapSet.new())
    updated_topic_systems = MapSet.delete(topic_systems, system_module)
    updated_topic_subscribers = Map.put(topic_subscribers, topic, updated_topic_systems)

    game_state
    |> Map.put(:system_subscriptions, updated_subscriptions)
    |> Map.put(:topic_subscribers, updated_topic_subscribers)
  end

  @doc """
  Publishes a message to a topic.
  The message will be queued for all systems subscribed to this topic.
  Also broadcasts to external PubSub subscribers if PubSub is running.
  """
  def publish(game_state, topic, message) do
    # Broadcast to external PubSub subscribers (if running)
    # Use pattern matching instead of try/rescue for better performance
    case Process.whereis(StellaInvicta.PubSub) do
      nil -> :ok
      _pid -> broadcast(topic, message)
    end

    # Queue message for system subscribers using reverse index (O(1) lookup)
    queue = Map.get(game_state, :message_queue, %{})
    topic_subscribers = Map.get(game_state, :topic_subscribers, %{})
    subscribed_systems = Map.get(topic_subscribers, topic, MapSet.new())

    # Skip if no subscribers
    if MapSet.size(subscribed_systems) == 0 do
      game_state
    else
      max_size = Map.get(game_state, :max_queue_size, @default_max_queue_size)
      message_tuple = {topic, message}

      # Add message to each system's queue (with size limiting)
      updated_queue =
        Enum.reduce(subscribed_systems, queue, fn system, acc ->
          system_messages = Map.get(acc, system, [])
          # Prepend for O(1) insertion, we'll reverse when reading if order matters
          # But for now, append and trim efficiently
          new_messages = [message_tuple | system_messages]
          trimmed_messages = trim_queue_fast(new_messages, max_size)
          Map.put(acc, system, trimmed_messages)
        end)

      Map.put(game_state, :message_queue, updated_queue)
    end
  end

  # Fast queue trimming - prepends so newest is at head, keeps max_size newest
  defp trim_queue_fast(messages, max_size) do
    len = length(messages)

    if len > max_size do
      Enum.take(messages, max_size)
    else
      messages
    end
  end

  @doc """
  Gets all pending messages for a system.
  Returns a list of {topic, message} tuples in chronological order.
  """
  def get_messages(game_state, system_module) do
    queue = Map.get(game_state, :message_queue, %{})
    Map.get(queue, system_module, []) |> Enum.reverse()
  end

  @doc """
  Gets and clears all pending messages for a system.
  Returns {messages, updated_game_state}.
  Messages are returned in chronological order (oldest first).
  """
  def pop_messages(game_state, system_module) do
    queue = Map.get(game_state, :message_queue, %{})
    # Reverse to get chronological order since we prepend
    messages = Map.get(queue, system_module, []) |> Enum.reverse()
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
  Uses reverse index for O(1) lookup.
  """
  def get_subscribers(game_state, topic) do
    topic_subscribers = Map.get(game_state, :topic_subscribers, %{})
    Map.get(topic_subscribers, topic, MapSet.new()) |> MapSet.to_list()
  end

  @doc """
  Removes all subscriptions for a system.
  Use this when unregistering a system to stop it from receiving messages.
  """
  def unsubscribe_all(game_state, system_module) do
    subscriptions = Map.get(game_state, :system_subscriptions, %{})
    topic_subscribers = Map.get(game_state, :topic_subscribers, %{})

    # Get all topics this system is subscribed to
    system_topics = Map.get(subscriptions, system_module, MapSet.new())

    # Remove system from reverse index for each topic
    updated_topic_subscribers =
      Enum.reduce(system_topics, topic_subscribers, fn topic, acc ->
        topic_systems = Map.get(acc, topic, MapSet.new())
        updated_topic_systems = MapSet.delete(topic_systems, system_module)
        Map.put(acc, topic, updated_topic_systems)
      end)

    # Remove system from subscriptions
    updated_subscriptions = Map.delete(subscriptions, system_module)

    game_state
    |> Map.put(:system_subscriptions, updated_subscriptions)
    |> Map.put(:topic_subscribers, updated_topic_subscribers)
  end

  @doc """
  Completely removes a system from the message queue.
  Clears all subscriptions and pending messages for the system.
  Use this when a system is being unregistered.
  """
  def cleanup_system(game_state, system_module) do
    game_state
    |> unsubscribe_all(system_module)
    |> clear_messages(system_module)
  end

  @doc """
  Sets the maximum queue size per system.
  Messages beyond this limit are dropped (oldest first).
  """
  def set_max_queue_size(game_state, max_size) when is_integer(max_size) and max_size > 0 do
    Map.put(game_state, :max_queue_size, max_size)
  end

  @doc """
  Gets the current maximum queue size setting.
  """
  def get_max_queue_size(game_state) do
    Map.get(game_state, :max_queue_size, @default_max_queue_size)
  end

  @doc """
  Returns the number of pending messages for a system.
  """
  def queue_length(game_state, system_module) do
    queue = Map.get(game_state, :message_queue, %{})
    Map.get(queue, system_module, []) |> length()
  end

  @doc """
  Clears messages for all disabled systems.
  Pass in the systems map with {module, enabled?} entries.
  """
  def clear_disabled_system_messages(game_state, systems) do
    disabled_systems =
      systems
      |> Enum.filter(fn {_module, enabled} -> not enabled end)
      |> Enum.map(fn {module, _} -> module end)

    Enum.reduce(disabled_systems, game_state, fn system, acc ->
      clear_messages(acc, system)
    end)
  end
end
