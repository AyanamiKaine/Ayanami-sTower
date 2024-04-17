defmodule Stella.Repo.Migrations.CreateHouses do
  use Ecto.Migration

  def change do
    create table(:Houses) do
      add :name, :string
    end
  end
end
