defmodule StellaInvicta.Model.Character do
  @moduledoc """
  Represents a character in the game with stats and attributes.
  """

  @enforce_keys [:id, :name]
  defstruct [
    :id,
    :name,
    :health,
    :age,
    :birth_date,
    :wealth,
    :martial,
    :diplomacy,
    :stewardship,
    :intrigue
  ]

  @type t :: %__MODULE__{
          id: integer(),
          name: String.t(),
          health: number() | nil,
          age: integer() | nil,
          birth_date: map() | nil,
          wealth: number() | nil,
          martial: integer() | nil,
          diplomacy: integer() | nil,
          stewardship: integer() | nil,
          intrigue: integer() | nil
        }

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_character_json}
  def from_json(%{"id" => id, "name" => name} = json)
      when is_integer(id) and is_binary(name) do
    {:ok,
     %__MODULE__{
       id: id,
       name: name,
       health: json["health"],
       age: json["age"],
       birth_date: parse_birth_date(json["birth_date"]),
       wealth: json["wealth"],
       martial: json["martial"],
       diplomacy: json["diplomacy"],
       stewardship: json["stewardship"],
       intrigue: json["intrigue"]
     }}
  end

  def from_json(_), do: {:error, :invalid_character_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, character} -> character
      {:error, reason} -> raise ArgumentError, "Invalid Character JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = character) do
    %{
      "id" => character.id,
      "name" => character.name,
      "health" => character.health,
      "age" => character.age,
      "birth_date" => character.birth_date,
      "wealth" => character.wealth,
      "martial" => character.martial,
      "diplomacy" => character.diplomacy,
      "stewardship" => character.stewardship,
      "intrigue" => character.intrigue
    }
  end

  defp parse_birth_date(nil), do: nil
  defp parse_birth_date(%{} = date), do: date

  defp parse_birth_date(%{"year" => y, "month" => m, "day" => d}),
    do: %{year: y, month: m, day: d}

  defp parse_birth_date(_), do: nil
end
