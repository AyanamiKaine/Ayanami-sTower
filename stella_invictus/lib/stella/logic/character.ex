defmodule Stella.Logic.Character do

  def add_trait(character = %Stella.Data.Character{}) do
    character
  end

  @spec set_name(%{:name => any(), optional(any()) => any()}, any()) :: %{
          :name => any(),
          optional(any()) => any()
        }
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
end
