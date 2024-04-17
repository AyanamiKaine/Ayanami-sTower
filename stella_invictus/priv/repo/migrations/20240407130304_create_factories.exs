defmodule Stella.Repo.Migrations.CreateFactories do
  use Ecto.Migration

  def change do
    create table(:factories) do
      add :celestial_body_id, references(:celestial_bodys)
    end
  end
end
