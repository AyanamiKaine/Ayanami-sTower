defmodule Stella.Galaxy do
  alias Stella.StarSystem
  use Ecto.Schema

  schema "galaxies" do
    field :name, :string
    has_many  :star_systems, StarSystem
  end
end
