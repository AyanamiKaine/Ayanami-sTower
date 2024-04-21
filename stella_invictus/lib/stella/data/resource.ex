defmodule Stella.Data.Resource do
  alias Stella.Data.CelestialBody
  use Ecto.Schema

  schema "resources" do
    field       :name,            :string
    field       :type,            :string
    belongs_to  :celestial_body,  CelestialBody
  end
end
