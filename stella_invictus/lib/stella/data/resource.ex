defmodule Stella.Resource do
  alias Stella.CelestialBody
  use Ecto.Schema

  schema "resources" do
    field       :name,            :string
    field       :type,            :string
    belongs_to  :celestial_body,  CelestialBody
  end
end
