defmodule StellaInvicta.Model.Religion do
  @moduledoc """
  Represents a religion with its needs and preferences.
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

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_religion_json}
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

  def from_json(_), do: {:error, :invalid_religion_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, religion} -> religion
      {:error, reason} -> raise ArgumentError, "Invalid Religion JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = religion) do
    %{
      "id" => religion.id,
      "name" => religion.name,
      "life_needs" => religion.life_needs,
      "everyday_needs" => religion.everyday_needs,
      "luxury_needs" => religion.luxury_needs
    }
  end

  defimpl String.Chars, for: StellaInvicta.Model.Religion do
    def to_string(r), do: r.name
  end
end
