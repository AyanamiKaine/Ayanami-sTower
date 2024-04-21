defmodule Stella.Data.PopulationType do
  use Ecto.Schema

  schema "population_types" do
    field :name, :string
  end
end
