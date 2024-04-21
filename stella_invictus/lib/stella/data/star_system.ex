defmodule Stella.Data.StarSystem do
  alias Stella.Data.StarSystem
  alias Stella.Data.Galaxy
  alias Stella.Data.CelestialBody
  use Ecto.Schema

  schema "star_systems" do
    field         :name,              :string, default: "NO_STAR_SYSTEM_NAME"
    has_many      :celestial_bodys,   CelestialBody
    belongs_to    :galaxy,            Galaxy
    many_to_many  :connected_systems, StarSystem, join_through: "star_system_connections", on_replace: :delete
  end
end
