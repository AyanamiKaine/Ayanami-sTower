defmodule StellaInvicta.Model.Specie do
  @moduledoc """
  Represents a species with its needs and preferences.
  """

  @enforce_keys [:id, :name]
  defstruct [:id, :name, :life_needs, :everyday_needs, :luxury_needs]

  @type t :: %__MODULE__{
          id: integer() | atom(),
          name: String.t(),
          life_needs: list() | nil,
          everyday_needs: list() | nil,
          luxury_needs: list() | nil
        }

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_specie_json}
  def from_json(%{"id" => id, "name" => name} = json)
      when (is_integer(id) or is_atom(id)) and is_binary(name) do
    {:ok,
     %__MODULE__{
       id: id,
       name: name,
       life_needs: json["life_needs"],
       everyday_needs: json["everyday_needs"],
       luxury_needs: json["luxury_needs"]
     }}
  end

  def from_json(_), do: {:error, :invalid_specie_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, specie} -> specie
      {:error, reason} -> raise ArgumentError, "Invalid Specie JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = specie) do
    %{
      "id" => specie.id,
      "name" => specie.name,
      "life_needs" => specie.life_needs,
      "everyday_needs" => specie.everyday_needs,
      "luxury_needs" => specie.luxury_needs
    }
  end
end
