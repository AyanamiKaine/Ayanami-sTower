defmodule StellaInvicta.Model.Population do
  @moduledoc """
  Represents a population group with demographics.
  """

  @enforce_keys [:id]
  defstruct [:id, :size, :literacy, :happiness, :wealth, :population_type_id]

  @type t :: %__MODULE__{
          id: integer(),
          size: integer() | nil,
          literacy: number() | nil,
          happiness: number() | nil,
          wealth: number() | nil,
          population_type_id: integer() | atom() | nil
        }

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_population_json}
  def from_json(%{"id" => id} = json) when is_integer(id) do
    {:ok,
     %__MODULE__{
       id: id,
       size: json["size"],
       literacy: json["literacy"],
       happiness: json["happiness"],
       wealth: json["wealth"],
       population_type_id: json["population_type_id"]
     }}
  end

  def from_json(_), do: {:error, :invalid_population_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, population} -> population
      {:error, reason} -> raise ArgumentError, "Invalid Population JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = population) do
    %{
      "id" => population.id,
      "size" => population.size,
      "literacy" => population.literacy,
      "happiness" => population.happiness,
      "wealth" => population.wealth,
      "population_type_id" => population.population_type_id
    }
  end
end
