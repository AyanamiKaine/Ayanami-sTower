defmodule Stella.Data.Character do
  alias Stella.Data.Trait
  use Ecto.Schema

  schema "characters" do
    field         :name,          :string,  default: "NO_NAME"
    field         :age,           :integer, default: 0
    many_to_many  :traits, Trait, join_through: "character_traits"
    field         :is_female,     :boolean, default: nil
    field         :prestige,      :float,   default: 0.0
    field         :wealth,        :float,   default: 0.0
  end
end
