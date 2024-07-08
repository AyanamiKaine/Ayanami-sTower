defmodule Stella.Repo.Migrations.CreateCharacters do
  use Ecto.Migration

  def change do
    create table(:characters) do
      add :name, :string, default: "NO_NAME"
      add :age, :integer, default: 0
      add :is_female, :boolean, default: nil
      add :prestige, :float, default: 0.0
      add :wealth, :float, default: 0.0

      timestamps() # Adds 'inserted_at' and 'updated_at' fields
    end
  end
end
