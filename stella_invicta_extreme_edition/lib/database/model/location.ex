defmodule StellaInvicta.Model.Location do
  @moduledoc """
  Represents a location/province in the game world.
  """

  @enforce_keys [:id, :name]
  defstruct [:id, :name, :description]

  @type t :: %__MODULE__{
          id: integer(),
          name: String.t(),
          description: String.t() | nil
        }

  @doc """
  Creates a Location struct from a JSON-decoded map.
  Returns {:ok, struct} on success or {:error, reason} on failure.

  ## Examples

      iex> Location.from_json(%{"id" => 1, "name" => "Berlin"})
      {:ok, %Location{id: 1, name: "Berlin", description: nil}}

      iex> Location.from_json(%{"id" => 1, "name" => "Berlin", "description" => "A city"})
      {:ok, %Location{id: 1, name: "Berlin", description: "A city"}}

      iex> Location.from_json(%{"name" => "Berlin"})
      {:error, :invalid_location_json}
  """
  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_location_json}
  def from_json(%{"id" => id, "name" => name} = json)
      when is_integer(id) and is_binary(name) do
    description = json["description"]

    if is_nil(description) or is_binary(description) do
      {:ok,
       %__MODULE__{
         id: id,
         name: name,
         description: description
       }}
    else
      {:error, :invalid_location_json}
    end
  end

  def from_json(_), do: {:error, :invalid_location_json}

  @doc """
  Creates a Location struct from a JSON-decoded map.
  Raises ArgumentError on invalid input.

  ## Examples

      iex> Location.from_json!(%{"id" => 1, "name" => "Berlin"})
      %Location{id: 1, name: "Berlin", description: nil}
  """
  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, location} -> location
      {:error, reason} -> raise ArgumentError, "Invalid Location JSON: #{reason}"
    end
  end

  @doc """
  Converts a Location struct to a JSON-encodable map.
  """
  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = location) do
    %{
      "id" => location.id,
      "name" => location.name,
      "description" => location.description
    }
  end

  defimpl String.Chars, for: StellaInvicta.Model.Location do
    def to_string(province) do
      "#{province.name} (ID: #{province.id})"
    end
  end
end
