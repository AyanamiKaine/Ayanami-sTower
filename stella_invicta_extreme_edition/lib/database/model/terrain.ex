defmodule StellaInvicta.Model.Terrain do
  @moduledoc """
  Represents a terrain type.
  """

  @enforce_keys [:id, :name]
  defstruct [:id, :name]

  @type t :: %__MODULE__{
          id: integer() | atom(),
          name: String.t()
        }

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_terrain_json}
  def from_json(%{"id" => id, "name" => name})
      when (is_integer(id) or is_atom(id)) and is_binary(name) do
    {:ok, %__MODULE__{id: id, name: name}}
  end

  def from_json(_), do: {:error, :invalid_terrain_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, terrain} -> terrain
      {:error, reason} -> raise ArgumentError, "Invalid Terrain JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = terrain) do
    %{
      "id" => terrain.id,
      "name" => terrain.name
    }
  end

  defimpl String.Chars, for: StellaInvicta.Model.Terrain do
    def to_string(t), do: t.name
  end
end
