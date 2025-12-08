defmodule ManualHandleMessageTest do
  use ExUnit.Case

  test "handle_message pipes correctly" do
    world = StellaInvicta.World.new_planet_world()
    world = %{world | date: %{day: 15, month: 4, year: 1, hour: 0}}

    char_before = world.characters[1]
    IO.puts("\nBefore handle_message:")
    IO.puts("  Character age: #{char_before.age}")
    IO.puts("  World has #{map_size(world.characters)} characters")

    # Call handle_message
    world_after = StellaInvicta.System.Age.handle_message(world, :date_events, {:new_day, 15})

    char_after = world_after.characters[1]
    IO.puts("\nAfter handle_message:")
    IO.puts("  Character age: #{char_after.age}")
    IO.puts("  World has #{map_size(world_after.characters)} characters")
    IO.puts("  Age changed? #{char_after.age != char_before.age}")

    assert char_after.age == 30, "Age should be 30, got #{char_after.age}"
  end
end
