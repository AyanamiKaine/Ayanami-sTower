defmodule StellaInvictaTest.MessageQueue do
  use ExUnit.Case, async: true

  alias StellaInvicta.MessageQueue
  alias StellaInvicta.Game
  alias StellaInvicta.World
  alias StellaInvicta.System, as: SystemBehaviour

  # Test module that implements the System behaviour
  defmodule TestListenerSystem do
    @behaviour StellaInvicta.System

    @impl true
    def run(game_state), do: game_state

    @impl true
    def subscriptions, do: [:test_topic, :another_topic]

    @impl true
    def handle_message(game_state, :test_topic, {:test_event, data}) do
      # Track that we received the message by storing it in state
      received = Map.get(game_state, :received_messages, [])
      Map.put(game_state, :received_messages, received ++ [{:test_event, data}])
    end

    def handle_message(game_state, _topic, _message), do: game_state
  end

  defmodule TestPublisherSystem do
    @behaviour StellaInvicta.System

    @impl true
    def run(game_state) do
      # Publish a message when running
      MessageQueue.publish(game_state, :test_topic, {:test_event, "from_publisher"})
    end

    @impl true
    def subscriptions, do: []

    @impl true
    def handle_message(game_state, _topic, _message), do: game_state
  end

  describe "MessageQueue initialization" do
    test "init/1 sets up empty message queue and subscriptions" do
      game_state = %{} |> MessageQueue.init()

      assert game_state.message_queue == %{}
      assert game_state.system_subscriptions == %{}
    end
  end

  describe "system subscriptions" do
    test "subscribe_system/3 adds a topic subscription for a system" do
      game_state =
        %{}
        |> MessageQueue.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :events)

      subscriptions = MessageQueue.get_subscriptions(game_state, TestListenerSystem)
      assert :events in subscriptions
    end

    test "subscribe_system/3 can subscribe to multiple topics" do
      game_state =
        %{}
        |> MessageQueue.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :events)
        |> MessageQueue.subscribe_system(TestListenerSystem, :alerts)

      subscriptions = MessageQueue.get_subscriptions(game_state, TestListenerSystem)
      assert :events in subscriptions
      assert :alerts in subscriptions
    end

    test "unsubscribe_system/3 removes a topic subscription" do
      game_state =
        %{}
        |> MessageQueue.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :events)
        |> MessageQueue.subscribe_system(TestListenerSystem, :alerts)
        |> MessageQueue.unsubscribe_system(TestListenerSystem, :events)

      subscriptions = MessageQueue.get_subscriptions(game_state, TestListenerSystem)
      refute :events in subscriptions
      assert :alerts in subscriptions
    end

    test "get_subscribers/2 returns all systems subscribed to a topic" do
      game_state =
        %{}
        |> MessageQueue.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :shared_topic)
        |> MessageQueue.subscribe_system(TestPublisherSystem, :shared_topic)

      subscribers = MessageQueue.get_subscribers(game_state, :shared_topic)
      assert TestListenerSystem in subscribers
      assert TestPublisherSystem in subscribers
    end
  end

  describe "message publishing and retrieval" do
    test "publish/3 queues messages for subscribed systems" do
      game_state =
        %{}
        |> MessageQueue.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :events)
        |> MessageQueue.publish(:events, {:something_happened, 42})

      messages = MessageQueue.get_messages(game_state, TestListenerSystem)
      assert [{:events, {:something_happened, 42}}] == messages
    end

    test "publish/3 does not queue messages for unsubscribed systems" do
      game_state =
        %{}
        |> MessageQueue.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :events)
        |> MessageQueue.publish(:other_topic, {:something_happened, 42})

      messages = MessageQueue.get_messages(game_state, TestListenerSystem)
      assert [] == messages
    end

    test "publish/3 queues messages for multiple subscribers" do
      game_state =
        %{}
        |> MessageQueue.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :shared)
        |> MessageQueue.subscribe_system(TestPublisherSystem, :shared)
        |> MessageQueue.publish(:shared, {:shared_event, "data"})

      listener_messages = MessageQueue.get_messages(game_state, TestListenerSystem)
      publisher_messages = MessageQueue.get_messages(game_state, TestPublisherSystem)

      assert [{:shared, {:shared_event, "data"}}] == listener_messages
      assert [{:shared, {:shared_event, "data"}}] == publisher_messages
    end

    test "pop_messages/2 returns and clears messages" do
      game_state =
        %{}
        |> MessageQueue.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :events)
        |> MessageQueue.publish(:events, {:event1, 1})
        |> MessageQueue.publish(:events, {:event2, 2})

      {messages, game_state} = MessageQueue.pop_messages(game_state, TestListenerSystem)

      assert [{:events, {:event1, 1}}, {:events, {:event2, 2}}] == messages
      assert [] == MessageQueue.get_messages(game_state, TestListenerSystem)
    end

    test "clear_messages/2 removes all messages for a system" do
      game_state =
        %{}
        |> MessageQueue.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :events)
        |> MessageQueue.publish(:events, {:event1, 1})
        |> MessageQueue.clear_messages(TestListenerSystem)

      assert [] == MessageQueue.get_messages(game_state, TestListenerSystem)
    end

    test "clear_all/1 removes all messages from the queue" do
      game_state =
        %{}
        |> MessageQueue.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :events)
        |> MessageQueue.subscribe_system(TestPublisherSystem, :events)
        |> MessageQueue.publish(:events, {:event, 1})
        |> MessageQueue.clear_all()

      assert [] == MessageQueue.get_messages(game_state, TestListenerSystem)
      assert [] == MessageQueue.get_messages(game_state, TestPublisherSystem)
    end

    test "queue size is limited to max_queue_size" do
      game_state =
        %{}
        |> MessageQueue.init()
        |> MessageQueue.set_max_queue_size(3)
        |> MessageQueue.subscribe_system(TestListenerSystem, :events)

      # Publish 5 messages
      game_state =
        Enum.reduce(1..5, game_state, fn i, acc ->
          MessageQueue.publish(acc, :events, {:event, i})
        end)

      messages = MessageQueue.get_messages(game_state, TestListenerSystem)

      # Should only have the last 3 messages (newest)
      assert length(messages) == 3
      assert [{:events, {:event, 3}}, {:events, {:event, 4}}, {:events, {:event, 5}}] == messages
    end

    test "get_max_queue_size returns default when not set" do
      game_state = %{} |> MessageQueue.init()
      assert MessageQueue.get_max_queue_size(game_state) == 100
    end

    test "queue_length returns number of pending messages" do
      game_state =
        %{}
        |> MessageQueue.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :events)
        |> MessageQueue.publish(:events, {:event, 1})
        |> MessageQueue.publish(:events, {:event, 2})

      assert MessageQueue.queue_length(game_state, TestListenerSystem) == 2
    end
  end

  describe "Game integration with message queue" do
    test "Game.init/1 initializes the message queue" do
      game_state = World.new_planet_world() |> Game.init()

      assert Map.has_key?(game_state, :message_queue)
      assert Map.has_key?(game_state, :system_subscriptions)
    end

    test "Game.init/1 sets up system subscriptions from behaviour" do
      # Ensure the Age system module is loaded
      Code.ensure_loaded(StellaInvicta.System.Age)

      # Verify the Age system declares subscriptions
      assert SystemBehaviour.get_subscriptions(StellaInvicta.System.Age) == [:date_events]

      game_state =
        World.new_planet_world()
        |> Game.init()

      # Age system should be subscribed to date_events
      subscriptions = MessageQueue.get_subscriptions(game_state, StellaInvicta.System.Age)
      assert :date_events in subscriptions
    end

    test "Game.register_system/3 sets up subscriptions for new systems" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.register_system(TestListenerSystem, true)

      subscriptions = MessageQueue.get_subscriptions(game_state, TestListenerSystem)
      assert :test_topic in subscriptions
      assert :another_topic in subscriptions
    end

    test "Game.publish/3 is a convenience wrapper for MessageQueue.publish/3" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.register_system(TestListenerSystem, true)
        |> Game.publish(:test_topic, {:test_event, "hello"})

      messages = MessageQueue.get_messages(game_state, TestListenerSystem)
      assert [{:test_topic, {:test_event, "hello"}}] == messages
    end

    test "Game.subscribe/3 is a convenience wrapper for MessageQueue.subscribe_system/3" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.subscribe(TestListenerSystem, :custom_topic)

      subscriptions = MessageQueue.get_subscriptions(game_state, TestListenerSystem)
      assert :custom_topic in subscriptions
    end
  end

  describe "message handling during game ticks" do
    test "systems receive and process messages during run_tick" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.register_system(TestListenerSystem, true)
        |> Game.publish(:test_topic, {:test_event, "tick_message"})

      # Run a tick - the TestListenerSystem should process the message
      game_state = Game.run_tick(game_state) |> Game.unwrap_tick_result()

      # The message should have been processed (stored in :received_messages)
      received = Map.get(game_state, :received_messages, [])
      assert {:test_event, "tick_message"} in received

      # The message queue should be cleared after processing
      messages = MessageQueue.get_messages(game_state, TestListenerSystem)
      assert [] == messages
    end

    test "system can publish messages that other systems receive" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.register_system(TestPublisherSystem, true)
        |> Game.register_system(TestListenerSystem, true)

      # Run a tick - TestPublisherSystem publishes, TestListenerSystem should receive
      game_state = Game.run_tick(game_state) |> Game.unwrap_tick_result()

      # On the next tick, TestListenerSystem will process the message
      game_state = Game.run_tick(game_state) |> Game.unwrap_tick_result()

      received = Map.get(game_state, :received_messages, [])
      assert {:test_event, "from_publisher"} in received
    end

    test "disabled systems do not receive messages" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        # disabled
        |> Game.register_system(TestListenerSystem, false)
        |> Game.publish(:test_topic, {:test_event, "should_not_receive"})

      game_state = Game.run_tick(game_state) |> Game.unwrap_tick_result()

      # The message should NOT have been processed
      received = Map.get(game_state, :received_messages, [])
      assert received == [] or received == nil
    end

    test "disabled systems have their message queues cleared during tick" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.register_system(TestListenerSystem, false)
        |> Game.publish(:test_topic, {:test_event, "will_be_cleared"})

      # Before tick, messages should be queued
      messages_before = MessageQueue.get_messages(game_state, TestListenerSystem)
      assert length(messages_before) > 0

      # After tick, messages should be cleared for disabled systems
      game_state = Game.run_tick(game_state) |> Game.unwrap_tick_result()
      messages_after = MessageQueue.get_messages(game_state, TestListenerSystem)
      assert messages_after == []
    end

    test "unregistering a system clears its subscriptions and messages" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.register_system(TestListenerSystem, true)
        |> Game.publish(:test_topic, {:test_event, "will_be_cleared"})

      # Before unregister, system should have subscriptions and messages
      subs_before = MessageQueue.get_subscriptions(game_state, TestListenerSystem)
      messages_before = MessageQueue.get_messages(game_state, TestListenerSystem)
      assert length(subs_before) > 0
      assert length(messages_before) > 0

      # After unregister, subscriptions and messages should be cleared
      game_state = Game.unregister_system(game_state, TestListenerSystem)
      subs_after = MessageQueue.get_subscriptions(game_state, TestListenerSystem)
      messages_after = MessageQueue.get_messages(game_state, TestListenerSystem)
      assert subs_after == []
      assert messages_after == []
    end
  end

  describe "Date system publishes events" do
    test "Date system does NOT publish events on normal hour tick" do
      # Register but DISABLE TestListenerSystem so it doesn't consume messages during tick
      game_state =
        World.new_planet_world()
        |> Game.init()
        # disabled
        |> Game.register_system(TestListenerSystem, false)
        |> MessageQueue.subscribe_system(TestListenerSystem, :date_events)

      # Run one tick (normal hour advance, not end of day)
      game_state = Game.run_tick(game_state) |> Game.unwrap_tick_result()

      # Since it's just a normal hour tick, no events should be published
      messages = MessageQueue.get_messages(game_state, TestListenerSystem)
      date_events = Enum.filter(messages, fn {topic, _} -> topic == :date_events end)
      assert length(date_events) == 0
    end

    test "Date system publishes :new_day event at end of day" do
      # Set up state with subscriptions but don't use run_tick which clears disabled system messages
      game_state =
        %{
          date: %{hour: 23, day: 1, month: 1, year: 1},
          current_tick: 0,
          characters: %{}
        }
        |> Game.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :date_events)

      # Run the Date system directly to publish events
      game_state = StellaInvicta.System.Date.run(game_state)

      messages = MessageQueue.get_messages(game_state, TestListenerSystem)
      event_types = Enum.map(messages, fn {_, msg} -> msg end)

      # Should have new_day event
      assert Enum.any?(event_types, fn
               {:new_day, _} -> true
               _ -> false
             end)
    end

    test "Date system publishes :new_month event at end of month" do
      game_state =
        %{
          date: %{hour: 23, day: 30, month: 1, year: 1},
          current_tick: 0,
          characters: %{}
        }
        |> Game.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :date_events)

      # Run the Date system directly to publish events
      game_state = StellaInvicta.System.Date.run(game_state)

      messages = MessageQueue.get_messages(game_state, TestListenerSystem)
      event_types = Enum.map(messages, fn {_, msg} -> msg end)

      # Should have new_month event
      assert Enum.any?(event_types, fn
               {:new_month, _} -> true
               _ -> false
             end)
    end

    test "Date system publishes :new_year event at end of year" do
      game_state =
        %{
          date: %{hour: 23, day: 30, month: 12, year: 1},
          current_tick: 0,
          characters: %{}
        }
        |> Game.init()
        |> MessageQueue.subscribe_system(TestListenerSystem, :date_events)

      # Run the Date system directly to publish events
      game_state = StellaInvicta.System.Date.run(game_state)

      messages = MessageQueue.get_messages(game_state, TestListenerSystem)
      event_types = Enum.map(messages, fn {_, msg} -> msg end)

      # Should have new_year event
      assert Enum.any?(event_types, fn
               {:new_year, _} -> true
               _ -> false
             end)
    end
  end

  describe "Age system responds to date events" do
    test "Age system is subscribed to date_events" do
      Code.ensure_loaded(StellaInvicta.System.Age)
      subscriptions = StellaInvicta.System.get_subscriptions(StellaInvicta.System.Age)
      assert :date_events in subscriptions
    end
  end
end
