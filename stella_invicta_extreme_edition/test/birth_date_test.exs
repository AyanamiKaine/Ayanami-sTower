defmodule BirthDateTest do
  use ExUnit.Case

  test "ensure_valid_birth_dates doesn't modify characters with existing birth dates" do
    world = StellaInvicta.World.new_planet_world()

    char_before = world.characters[1]
    IO.puts("\nCharacter before ensure_valid_birth_dates:")
    IO.puts("  Age: #{char_before.age}")
    IO.puts("  Birth date: #{inspect(char_before.birth_date)}")

    world_after = StellaInvicta.System.Age.ensure_valid_birth_dates(world)
    char_after = world_after.characters[1]

    IO.puts("\nCharacter after ensure_valid_birth_dates:")
    IO.puts("  Age: #{char_after.age}")
    IO.puts("  Birth date: #{inspect(char_after.birth_date)}")

    assert char_after.age == char_before.age
    assert char_after.birth_date == char_before.birth_date
  end

  test "handle_message with new_day should trigger age update" do
    world = StellaInvicta.World.new_planet_world()
    char_before = world.characters[1]

    IO.puts("\nBefore handle_message:")
    IO.puts("  Character age: #{char_before.age}")
    IO.puts("  Current date: #{inspect(world.date)}")

    # Simulate the world at month 4, day 15 (the birthday)
    # The new_day event fires AFTER the date has advanced
    world_at_birthday = %{world | date: %{month: 4, day: 15, year: 1, hour: 0}}

    # Call handle_message with new_day(15)
    world_after =
      StellaInvicta.System.Age.handle_message(world_at_birthday, :date_events, {:new_day, 15})

    char_after = world_after.characters[1]

    # Expected age at month 4, day 15
    expected_age =
      StellaInvicta.System.Age.calculate_age(char_before.birth_date, %{
        month: 4,
        day: 15,
        year: 1,
        hour: 0
      })

    IO.puts("\nAfter handle_message:")
    IO.puts("  Character age: #{char_after.age}")
    IO.puts("  Expected age: #{expected_age}")
    IO.puts("  Age updated? #{char_after.age != char_before.age}")

    assert char_after.age == expected_age,
           "Age should have been updated to #{expected_age}, but is #{char_after.age}"
  end
end
