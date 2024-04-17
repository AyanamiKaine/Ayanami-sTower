defmodule Stella.StarSystem do
  alias Stella.StarSystem
  alias Stella.Galaxy
  alias Stella.CelestialBody
  use Ecto.Schema

  schema "star_systems" do
    field :name, :string
    has_many  :celestial_bodys, CelestialBody
    belongs_to :galaxy, Galaxy
    many_to_many :connected_systems, StarSystem, join_through: "star_system_connections", on_replace: :delete
  end
end
