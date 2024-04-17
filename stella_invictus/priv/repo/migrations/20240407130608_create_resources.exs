defmodule Stella.Repo.Migrations.CreateResources do
  use Ecto.Migration

  def change do
    create table(:resources) do
      add :name, :string
      add :type, :string
      add :celestial_body_id, references(:celestial_bodys)
    end
  end
end
