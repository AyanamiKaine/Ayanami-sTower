defmodule Stella.StarSystemConnections do
  alias Stella.StarSystem
  use Ecto.Schema

  schema "star_system_connections" do
    belongs_to :starsystem_1, StarSystem
    belongs_to :starsystem_2, StarSystem
    field :distance, :float
  end
end
