defmodule StellaInvicta.Model.Culture do
  @moduledoc """
  Represents a culture with its needs and preferences.
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

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_culture_json}
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

  def from_json(_), do: {:error, :invalid_culture_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, culture} -> culture
      {:error, reason} -> raise ArgumentError, "Invalid Culture JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = culture) do
    %{
      "id" => culture.id,
      "name" => culture.name,
      "life_needs" => culture.life_needs,
      "everyday_needs" => culture.everyday_needs,
      "luxury_needs" => culture.luxury_needs
    }
  end
end
