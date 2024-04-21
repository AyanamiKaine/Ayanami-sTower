defmodule Stella.Data.CelestialBody do
  alias Stella.Data.Factory
  alias Stella.Data.Population
  alias Stella.Data.Resource
  alias Stella.Data.StarSystem
  use Ecto.Schema

  schema "celestial_bodys" do
    field     :name,           :string
    has_many  :factories,      Factory
    has_many  :populations,    Population
    has_many  :resources,      Resource
    field     :size,           :string
    field     :type,           :integer
    belongs_to :star_system, StarSystem
  end
end
