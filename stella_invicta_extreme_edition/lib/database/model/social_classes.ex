defmodule StellaInvicta.Model.SocialClasses do
  @moduledoc """
  Represents a social class with its needs and preferences.
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

  @spec from_json(map()) :: {:ok, t()} | {:error, :invalid_social_classes_json}
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

  def from_json(_), do: {:error, :invalid_social_classes_json}

  @spec from_json!(map()) :: t()
  def from_json!(json) do
    case from_json(json) do
      {:ok, social_class} -> social_class
      {:error, reason} -> raise ArgumentError, "Invalid SocialClasses JSON: #{reason}"
    end
  end

  @spec to_json(t()) :: map()
  def to_json(%__MODULE__{} = social_class) do
    %{
      "id" => social_class.id,
      "name" => social_class.name,
      "life_needs" => social_class.life_needs,
      "everyday_needs" => social_class.everyday_needs,
      "luxury_needs" => social_class.luxury_needs
    }
  end
end
