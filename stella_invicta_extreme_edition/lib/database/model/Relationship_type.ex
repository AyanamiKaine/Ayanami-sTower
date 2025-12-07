defmodule StellaInvicta.Model.RelationshipType do
  @moduledoc """
  Represents a type of relationship between characters.
  """

  @enforce_keys [:id, :name]
  defstruct [:id, :name, :is_symmetric]

  @type t :: %__MODULE__{
          id: integer() | atom(),
          name: String.t(),
          is_symmetric: boolean() | nil
        }

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_relationship_type_json}
  def from_json(%{"id" => id, "name" => name} = json)
      when (is_integer(id) or is_atom(id)) and is_binary(name) do
    is_symmetric = json["is_symmetric"]

    if is_nil(is_symmetric) or is_boolean(is_symmetric) do
      {:ok, %__MODULE__{id: id, name: name, is_symmetric: is_symmetric}}
    else
      {:error, :invalid_relationship_type_json}
    end
  end

  def from_json(_), do: {:error, :invalid_relationship_type_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, type} -> type
      {:error, reason} -> raise ArgumentError, "Invalid RelationshipType JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = type) do
    %{
      "id" => type.id,
      "name" => type.name,
      "is_symmetric" => type.is_symmetric
    }
  end
end
