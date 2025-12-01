defmodule StellaInvicta.Model.Religion do
  defstruct [:id, :name, :life_needs, :everyday_needs, :luxury_needs]

  defimpl String.Chars, for: StellaInvicta.Model.Religion do
    def to_string(r), do: r.name
  end
end
