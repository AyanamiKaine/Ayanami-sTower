defmodule Stella.Data.Trait do
  alias Stella.Data.Character
  use Ecto.Schema

  schema "traits" do
    field :name,         :string
    field :description,  :string
    field :icon_path,    :string
    field :type,         :string
    many_to_many :characters, Character, join_through: "character_traits"
  end
end
