defmodule Stella.Data.BaseStats do
  use Ecto.Schema

  schema "BaseStats" do
    field :diplomancy,  :integer, default: 1
    field :martial,     :integer, default: 1
    field :stewardship, :integer, default: 1
    field :intrigue,    :integer, default: 1
    field :learning,    :integer, default: 1
    field :health,      :integer, default: 1
  end
end
