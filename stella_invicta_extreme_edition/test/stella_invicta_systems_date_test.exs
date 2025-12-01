defmodule StellaInvictaTest.Systems.Date do
  use ExUnit.Case, async: true
  doctest StellaInvicta.System.Date

  test "date system increment by one hour" do
    initial_world_state =
      StellaInvicta.World.new_planet_world()

    final_world_state =
      initial_world_state
      |> StellaInvicta.Game.simulate_hour()

    assert final_world_state.date.hour == 1
    assert final_world_state.current_tick == 1
  end

  test "date system increment by one day" do
    initial_world_state =
      StellaInvicta.World.new_planet_world()

    final_world_state =
      initial_world_state
      |> StellaInvicta.Game.simulate_day()

    assert final_world_state.date.hour == 0
    assert final_world_state.date.day == 2
    assert final_world_state.current_tick == 24
  end

  test "date system increment by one month" do
    initial_world_state =
      StellaInvicta.World.new_planet_world()

    final_world_state =
      initial_world_state
      |> StellaInvicta.Game.simulate_month()

    assert final_world_state.date.hour == 0
    assert final_world_state.date.day == 1
    assert final_world_state.date.month == 2
    assert final_world_state.current_tick == 720
  end

  test "date system increment by one year" do
    initial_world_state =
      StellaInvicta.World.new_planet_world()

    final_world_state =
      initial_world_state
      |> StellaInvicta.Game.simulate_year()

    assert final_world_state.date.hour == 0
    assert final_world_state.date.day == 1
    assert final_world_state.date.month == 1
    assert final_world_state.date.year == 2
    assert final_world_state.current_tick == 8640
  end
end
