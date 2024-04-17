defmodule Stella.Good do
  use Ecto.Schema

  schema "goods" do
    field       :name,            :string
    field       :type,            :string
  end
end
