defmodule Stella.Specie do
  use Ecto.Schema

  schema "specien" do
    field :name, :string
    field :base_rate_of_increase, :float
  end
end
