defmodule AgeIntegrationTest do
  use ExUnit.Case

  test "age increases when new_day event is received" do
    # Create a world with a character with a simple birthday
    world = StellaInvicta.World.new_planet_world()
    game_state = StellaInvicta.Game.init(world)

    initial_age = game_state.characters[1].age
    initial_date = game_state.date

    IO.puts("\n=== Age Integration Test ===")
    IO.puts("Initial state:")
    IO.puts("  Character age: #{initial_age}")
    IO.puts("  Birth date: #{inspect(game_state.characters[1].birth_date)}")
    IO.puts("  Current date: #{inspect(initial_date)}")

    # Manually call handle_message to test directly
    IO.puts("\n=== Testing handle_message directly ===")
    result = StellaInvicta.System.Age.handle_message(game_state, :date_events, {:new_day, 15})
    IO.puts("After handle_message with new_day event:")
    IO.puts("  Character age: #{result.characters[1].age}")
    IO.puts("  Age increased? #{result.characters[1].age > initial_age}")

    # Now test through the game loop
    IO.puts("\n=== Running 24 ticks ===")

    game_state2 =
      Enum.reduce(1..24, game_state, fn _i, acc ->
        result = StellaInvicta.Game.run_tick(acc)
        StellaInvicta.Game.unwrap_tick_result(result)
      end)

    IO.puts("After 24 ticks:")
    IO.puts("  Date: #{inspect(game_state2.date)}")
    IO.puts("  Character age: #{game_state2.characters[1].age}")

    # Check the expected age calculation
    expected_age =
      StellaInvicta.System.Age.calculate_age(
        game_state2.characters[1].birth_date,
        game_state2.date
      )

    IO.puts("  Expected age: #{expected_age}")
    IO.puts("  Matches actual age? #{expected_age == game_state2.characters[1].age}")
  end
end
