defmodule StellaInvictaTest.World do
  use ExUnit.Case, async: true
  doctest StellaInvicta.World

  test "adding a new table to the world" do
    initial_empty_world_state = %StellaInvicta.World{}

    updated_world_state = Map.put_new(initial_empty_world_state, :new_table, %{})

    assert Map.has_key?(updated_world_state, :new_table) == true
    assert updated_world_state.new_table == %{}
  end

  test "adding a new table to the world, inverse" do
    initial_empty_world_state = %StellaInvicta.World{}

    assert Map.has_key?(initial_empty_world_state, :new_table) == false
  end
end
