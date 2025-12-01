defmodule StellaInvicta.Model.Terrain do
  defstruct [:id, :name]

  defimpl String.Chars, for: StellaInvicta.Model.Terrain do
    def to_string(t), do: t.name
  end
end
