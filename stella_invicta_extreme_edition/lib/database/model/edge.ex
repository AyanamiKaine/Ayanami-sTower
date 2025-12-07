defmodule StellaInvicta.Model.Edge do
  @moduledoc """
  Represents a connection/edge between locations with a distance.
  """

  @enforce_keys [:to_id]
  defstruct [:to_id, :distance]

  @type t :: %__MODULE__{
          to_id: integer(),
          distance: number() | nil
        }

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_edge_json}
  def from_json(%{"to_id" => to_id} = json) when is_integer(to_id) do
    distance = json["distance"]

    if is_nil(distance) or is_number(distance) do
      {:ok, %__MODULE__{to_id: to_id, distance: distance}}
    else
      {:error, :invalid_edge_json}
    end
  end

  def from_json(_), do: {:error, :invalid_edge_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, edge} -> edge
      {:error, reason} -> raise ArgumentError, "Invalid Edge JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = edge) do
    %{
      "to_id" => edge.to_id,
      "distance" => edge.distance
    }
  end
end
