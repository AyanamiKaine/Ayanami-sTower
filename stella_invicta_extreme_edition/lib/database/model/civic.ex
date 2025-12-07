defmodule StellaInvicta.Model.Civic do
  @moduledoc """
  Represents a civic/policy that a polity can adopt.
  """

  @enforce_keys [:id, :name]
  defstruct [:id, :name, :description, :opposite]

  @type t :: %__MODULE__{
          id: integer() | atom(),
          name: String.t(),
          description: String.t() | nil,
          opposite: integer() | atom() | nil
        }

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_civic_json}
  def from_json(%{"id" => id, "name" => name} = json)
      when (is_integer(id) or is_atom(id)) and is_binary(name) do
    {:ok,
     %__MODULE__{
       id: id,
       name: name,
       description: json["description"],
       opposite: json["opposite"]
     }}
  end

  def from_json(_), do: {:error, :invalid_civic_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, civic} -> civic
      {:error, reason} -> raise ArgumentError, "Invalid Civic JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = civic) do
    %{
      "id" => civic.id,
      "name" => civic.name,
      "description" => civic.description,
      "opposite" => civic.opposite
    }
  end
end
