defmodule Stella.Population do
  alias Stella.CelestialBody
  use Ecto.Schema

  schema "populations" do
    belongs_to :celestial_body, CelestialBody
  end
end
