defmodule Stella.Character do
  alias Stella.Trait
  use Ecto.Schema

  schema "characters" do
    field :name,      :string
    many_to_many :traits, Trait, join_through: "character_traits"
    field :is_female, :boolean
    field :prestige,  :float
    field :wealth,    :float
  end
end
