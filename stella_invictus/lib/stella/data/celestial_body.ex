defmodule Stella.CelestialBody do
  alias Stella.Factory
  alias Stella.Population
  alias Stella.Resource
  alias Stella.StarSystem
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
