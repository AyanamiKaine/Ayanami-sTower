defmodule StellaInvicta.Model.Building do
  @moduledoc """
  Represents a building that can produce goods via production chains.
  """

  @enforce_keys [:id, :name]
  defstruct [:id, :name, :production_chain_id, :inventory]

  @type t :: %__MODULE__{
          id: integer() | atom(),
          name: String.t(),
          production_chain_id: integer() | atom() | nil,
          inventory: map() | nil
        }

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_building_json}
  def from_json(%{"id" => id, "name" => name} = json)
      when (is_integer(id) or is_atom(id)) and is_binary(name) do
    production_chain_id = json["production_chain_id"]
    inventory = json["inventory"]

    if valid_optional_id?(production_chain_id) and valid_optional_map?(inventory) do
      {:ok,
       %__MODULE__{
         id: id,
         name: name,
         production_chain_id: production_chain_id,
         inventory: inventory
       }}
    else
      {:error, :invalid_building_json}
    end
  end

  def from_json(_), do: {:error, :invalid_building_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, building} -> building
      {:error, reason} -> raise ArgumentError, "Invalid Building JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = building) do
    %{
      "id" => building.id,
      "name" => building.name,
      "production_chain_id" => building.production_chain_id,
      "inventory" => building.inventory
    }
  end

  defp valid_optional_id?(nil), do: true
  defp valid_optional_id?(id) when is_integer(id) or is_atom(id), do: true
  defp valid_optional_id?(_), do: false

  defp valid_optional_map?(nil), do: true
  defp valid_optional_map?(m) when is_map(m), do: true
  defp valid_optional_map?(_), do: false
end
