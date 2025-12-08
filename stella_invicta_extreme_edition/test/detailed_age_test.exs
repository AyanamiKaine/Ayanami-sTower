defmodule DetailedAgeTest do
  use ExUnit.Case

  test "detailed trace of maybe_age_character" do
    # Create a character
    char = %StellaInvicta.Model.Character{
      id: 1,
      name: "Charlemagne",
      age: 29,
      birth_date: %{day: 15, month: 4, year: -29},
      health: 10,
      martial: 0,
      stewardship: 0,
      wealth: 0,
      diplomacy: 0,
      intrigue: 0
    }

    # Test date at month 4, day 15
    current_date = %{day: 15, month: 4, year: 1, hour: 0}
    world = StellaInvicta.World.new_planet_world() |> Map.put(:date, current_date)

    IO.puts("\n=== Detailed Trace ===")
    IO.puts("Character age: #{char.age}")
    IO.puts("Birth date: #{inspect(char.birth_date)}")
    IO.puts("Current date: #{inspect(current_date)}")

    # Calculate expected age
    expected_age = StellaInvicta.System.Age.calculate_age(char.birth_date, current_date)
    IO.puts("Expected age: #{expected_age}")
    IO.puts("Should age? #{expected_age > char.age}")

    # Call maybe_age_character
    {updated_char, _updated_world} =
      StellaInvicta.System.Age.maybe_age_character(char, current_date, world)

    IO.puts("\nAfter maybe_age_character:")
    IO.puts("  Updated character age: #{updated_char.age}")
    IO.puts("  Age changed? #{updated_char.age != char.age}")

    # Test via handle_message (public API)
    IO.puts("\n=== Testing via handle_message ===")
    world_with_char = %{world | characters: %{1 => char}}

    updated_world2 =
      StellaInvicta.System.Age.handle_message(world_with_char, :date_events, {:new_day, 15})

    IO.puts("After handle_message:")
    IO.puts("  Character age: #{updated_world2.characters[1].age}")
    IO.puts("  Age changed? #{updated_world2.characters[1].age != char.age}")

    assert updated_world2.characters[1].age == expected_age
  end
end
