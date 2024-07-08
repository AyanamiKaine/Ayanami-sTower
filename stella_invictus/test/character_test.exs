defmodule CharacterTest do
  use ExUnit.Case
  doctest Stella.Data.Character

  test "Change Name of character" do
    character =  %Stella.Data.Character{}
      |> Stella.Logic.Character.set_name("Tom")

      assert "Tom" == character.name
  end

  test "substract prestige from character" do
    character =  %Stella.Data.Character{}
      |> Stella.Logic.Character.substract_prestige(100)

    assert -100.0 == character.prestige
  end

  test "add prestige to character" do
    character =  %Stella.Data.Character{}
      |> Stella.Logic.Character.add_prestige(100)

  assert 100.0 == character.prestige
  end

  test "turn character into female" do
    character = %Stella.Data.Character{}
      |> Stella.Logic.Character.set_is_female(true)

      assert true == character.is_female
  end

  test "Increase the character age by one" do
    character = %Stella.Data.Character{}
    |> Stella.Logic.Character.age_character_by_one()

    assert 1 == character.age
  end
end
