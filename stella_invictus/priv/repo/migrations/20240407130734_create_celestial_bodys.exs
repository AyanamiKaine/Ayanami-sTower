defmodule Stella.Repo.Migrations.CreateCelestialBodys do
  use Ecto.Migration

  def change do
    create table(:CelestialBodys) do
      add :name, :string
      add :size, :string
      add :type, :string
    end
  end
end
