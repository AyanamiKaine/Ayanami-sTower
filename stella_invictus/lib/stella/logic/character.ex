defmodule Stella.Logic.Character do

  def set_name(%{name: _name} = character, new_name) do
    %{character | name: new_name}
  end

  def substract_prestige(%{prestige: _prestige} = character, value_to_subtract) do
    %{character | prestige: character.prestige - value_to_subtract}
  end

  def add_prestige(%{prestige: _prestige} = character, value_to_add) do
    %{character | prestige: character.prestige + value_to_add}
  end

  def set_is_female(%{is_female: _is_female} = character, is_female) do
    %{character | is_female: is_female}
  end

  def age_character_by_one(%{age: _age} = character) do
    %{character | age: character.age + 1}
  end
end
