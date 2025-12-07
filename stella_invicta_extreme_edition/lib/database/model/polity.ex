defmodule StellaInvicta.Model.Polity do
  @moduledoc """
  Represents a political entity (nation, kingdom, etc.).
  """

  @enforce_keys [:id, :name]
  defstruct [:id, :name, :wealth, :leader_title, :civics, :parent_entity]

  @type t :: %__MODULE__{
          id: integer() | atom(),
          name: String.t(),
          wealth: number() | nil,
          leader_title: String.t() | nil,
          civics: list() | nil,
          parent_entity: integer() | atom() | nil
        }

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_polity_json}
  def from_json(%{"id" => id, "name" => name} = json)
      when (is_integer(id) or is_atom(id)) and is_binary(name) do
    {:ok,
     %__MODULE__{
       id: id,
       name: name,
       wealth: json["wealth"],
       leader_title: json["leader_title"],
       civics: json["civics"],
       parent_entity: json["parent_entity"]
     }}
  end

  def from_json(_), do: {:error, :invalid_polity_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, polity} -> polity
      {:error, reason} -> raise ArgumentError, "Invalid Polity JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = polity) do
    %{
      "id" => polity.id,
      "name" => polity.name,
      "wealth" => polity.wealth,
      "leader_title" => polity.leader_title,
      "civics" => polity.civics,
      "parent_entity" => polity.parent_entity
    }
  end
end
