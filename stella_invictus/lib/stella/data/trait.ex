defmodule Stella.Trait do
  alias Stella.Character
  use Ecto.Schema

  schema "traits" do
    field :name, :string
    many_to_many :characters, Character, join_through: "character_traits"
  end
end
