defmodule StellaInvicta.Model.CharacterRelation do
  @moduledoc """
  Represents a relationship between characters.
  """

  @enforce_keys [:target_id, :type_id]
  defstruct [:target_id, :type_id]

  @type t :: %__MODULE__{
          target_id: integer(),
          type_id: integer() | atom()
        }

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_character_relation_json}
  def from_json(%{"target_id" => target_id, "type_id" => type_id})
      when is_integer(target_id) and (is_integer(type_id) or is_atom(type_id)) do
    {:ok, %__MODULE__{target_id: target_id, type_id: type_id}}
  end

  def from_json(_), do: {:error, :invalid_character_relation_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, relation} -> relation
      {:error, reason} -> raise ArgumentError, "Invalid CharacterRelation JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = relation) do
    %{
      "target_id" => relation.target_id,
      "type_id" => relation.type_id
    }
  end
end
