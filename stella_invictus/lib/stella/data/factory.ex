defmodule Stella.Factory do
  alias Stella.CelestialBody
  use Ecto.Schema

  schema "factories" do
    belongs_to :celestial_body, CelestialBody
  end
end
