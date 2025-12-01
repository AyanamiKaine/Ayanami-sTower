defmodule StellaInvicta.Model.Location do
  defstruct [:id, :name, :description]

  defimpl String.Chars, for: StellaInvicta.Model.Location do
    def to_string(province) do
      "#{province.name} (ID: #{province.id})"
    end
  end
end
