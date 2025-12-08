defmodule SubscriptionTest do
  use ExUnit.Case

  test "age system subscriptions are registered" do
    world = StellaInvicta.World.new_planet_world()
    game_state = StellaInvicta.Game.init(world)

    # Get subscriptions for Age system
    subs = StellaInvicta.MessageQueue.get_subscriptions(game_state, StellaInvicta.System.Age)

    IO.puts("\nAge system subscriptions: #{inspect(subs)}")
    assert subs == [:date_events], "Age system should be subscribed to :date_events"
  end

  test "date system publishes new_day events" do
    world = StellaInvicta.World.new_planet_world()
    game_state = StellaInvicta.Game.init(world)

    # Run 24 ticks (one full day)
    game_state =
      Enum.reduce(1..24, game_state, fn _i, acc ->
        result = StellaInvicta.Game.run_tick(acc)
        StellaInvicta.Game.unwrap_tick_result(result)
      end)

    # Check the date
    IO.puts("\nAfter 24 ticks:")
    IO.puts("  Current date: #{inspect(game_state.date)}")

    # Check if messages were published
    messages = StellaInvicta.MessageQueue.get_messages(game_state, StellaInvicta.System.Age)
    IO.puts("  Age system messages: #{inspect(messages)}")

    # Check Date system messages
    date_messages = StellaInvicta.MessageQueue.get_messages(game_state, StellaInvicta.System.Date)
    IO.puts("  Date system messages: #{inspect(date_messages)}")

    # Check CharacterAI messages
    char_messages =
      StellaInvicta.MessageQueue.get_messages(game_state, StellaInvicta.System.CharacterAI)

    IO.puts("  CharacterAI messages: #{inspect(char_messages)}")

    # Check all message topics
    all_topics = game_state.message_queue |> Map.keys()
    IO.puts("  All message queue topics: #{inspect(all_topics)}")

    # Print contents of each topic
    Enum.each(game_state.message_queue, fn {topic, msgs} ->
      IO.puts("    #{inspect(topic)}: #{inspect(msgs)}")
    end)
  end
end
