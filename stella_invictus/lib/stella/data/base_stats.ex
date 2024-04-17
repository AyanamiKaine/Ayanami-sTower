defmodule Stella.BaseStats do
  use Ecto.Schema

  schema "BaseStats" do
    field :diplomancy,  :integer
    field :martial,     :integer
    field :stewardship, :integer
    field :intrigue,    :integer
    field :learning,    :integer
    field :health,      :integer
  end
end
