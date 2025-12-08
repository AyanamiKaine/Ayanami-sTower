defmodule StellaInvictaTest.Game do
  use ExUnit.Case, async: true

  alias StellaInvicta.Game
  alias StellaInvicta.World

  describe "system management" do
    test "init/1 sets up default systems" do
      game_state = World.new_planet_world() |> Game.init()

      systems = Game.list_systems(game_state)
      assert Map.get(systems, StellaInvicta.System.Date) == true
      assert Map.get(systems, StellaInvicta.System.Age) == true
    end

    test "disable_system/2 disables a system" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.disable_system(StellaInvicta.System.Age)

      assert Game.system_enabled?(game_state, StellaInvicta.System.Age) == false
      assert Game.system_enabled?(game_state, StellaInvicta.System.Date) == true
    end

    test "enable_system/2 enables a previously disabled system" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.disable_system(StellaInvicta.System.Age)
        |> Game.enable_system(StellaInvicta.System.Age)

      assert Game.system_enabled?(game_state, StellaInvicta.System.Age) == true
    end

    test "toggle_system/2 toggles system state" do
      game_state = World.new_planet_world() |> Game.init()

      # Initially enabled
      assert Game.system_enabled?(game_state, StellaInvicta.System.Age) == true

      # Toggle off
      game_state = Game.toggle_system(game_state, StellaInvicta.System.Age)
      assert Game.system_enabled?(game_state, StellaInvicta.System.Age) == false

      # Toggle back on
      game_state = Game.toggle_system(game_state, StellaInvicta.System.Age)
      assert Game.system_enabled?(game_state, StellaInvicta.System.Age) == true
    end

    test "register_system/3 adds a new system" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.register_system(SomeNewSystem, true)

      assert Game.system_enabled?(game_state, SomeNewSystem) == true
    end

    test "unregister_system/2 removes a system" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.unregister_system(StellaInvicta.System.Age)

      systems = Game.list_systems(game_state)
      assert Map.has_key?(systems, StellaInvicta.System.Age) == false
    end
  end

  describe "disabled systems do not run" do
    test "disabled Date system does not advance time" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.disable_system(StellaInvicta.System.Date)

      initial_date = game_state.date

      game_state = Game.simulate_hour(game_state)

      # Date should not have changed
      assert game_state.date.hour == initial_date.hour
      assert game_state.date.day == initial_date.day
      # But tick should still increment
      assert game_state.current_tick == 1
    end

    test "disabled Age system does not update character ages" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.disable_system(StellaInvicta.System.Age)

      initial_ages =
        game_state.characters
        |> Enum.map(fn {id, char} -> {id, char.age} end)
        |> Map.new()

      # Simulate a full year
      game_state = Game.simulate_year(game_state)

      # Ages should not have changed
      Enum.each(game_state.characters, fn {id, char} ->
        assert char.age == Map.get(initial_ages, id)
      end)
    end

    test "all systems disabled - only tick increments" do
      game_state =
        World.new_planet_world()
        |> Game.init()
        |> Game.disable_system(StellaInvicta.System.Date)
        |> Game.disable_system(StellaInvicta.System.Age)

      initial_date = game_state.date

      initial_ages =
        game_state.characters
        |> Enum.map(fn {id, char} -> {id, char.age} end)
        |> Map.new()

      game_state = Game.simulate_day(game_state)

      # Nothing should change except tick
      assert game_state.date == initial_date
      assert game_state.current_tick == 24

      Enum.each(game_state.characters, fn {id, char} ->
        assert char.age == Map.get(initial_ages, id)
      end)
    end
  end

  describe "systems run without init" do
    test "systems default to enabled when not initialized" do
      # Without calling init, systems should still run (default to enabled)
      game_state = World.new_planet_world()

      initial_hour = game_state.date.hour

      game_state = Game.simulate_hour(game_state)

      # Date system should have run
      assert game_state.date.hour == initial_hour + 1
    end
  end
end
