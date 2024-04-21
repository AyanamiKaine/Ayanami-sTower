defmodule Stella.Data.Factory do
  alias Stella.Data.CelestialBody
  use Ecto.Schema

  schema "factories" do
    belongs_to :celestial_body, CelestialBody
  end
end
