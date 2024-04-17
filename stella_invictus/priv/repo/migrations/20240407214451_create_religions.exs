defmodule Stella.Repo.Migrations.CreateReligions do
  use Ecto.Migration

  def change do
    create table(:Religions) do
      add :name, :string
    end
  end
end
