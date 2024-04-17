defmodule Stella.Repo.Migrations.CreatePopulations do
  use Ecto.Migration

  def change do
    create table(:populations) do
      add :celestial_body_id, references(:celestial_bodys)
    end
  end
end
