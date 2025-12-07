defmodule StellaInvicta.Model.ProductionChains do
  @moduledoc """
  Represents a production chain that transforms inputs to outputs.
  """

  @enforce_keys [:id, :name]
  defstruct [:id, :name, :input, :output, :workforce]

  @type t :: %__MODULE__{
          id: integer() | atom(),
          name: String.t(),
          input: map() | list() | nil,
          output: map() | list() | nil,
          workforce: integer() | nil
        }

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_production_chains_json}
  def from_json(%{"id" => id, "name" => name} = json)
      when (is_integer(id) or is_atom(id)) and is_binary(name) do
    {:ok,
     %__MODULE__{
       id: id,
       name: name,
       input: json["input"],
       output: json["output"],
       workforce: json["workforce"]
     }}
  end

  def from_json(_), do: {:error, :invalid_production_chains_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, chain} -> chain
      {:error, reason} -> raise ArgumentError, "Invalid ProductionChains JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = chain) do
    %{
      "id" => chain.id,
      "name" => chain.name,
      "input" => chain.input,
      "output" => chain.output,
      "workforce" => chain.workforce
    }
  end
end
