defmodule Stella.Data.Population do
  alias Stella.Data.CelestialBody
  use Ecto.Schema

  schema "populations" do
    belongs_to :celestial_body, CelestialBody
  end
end
