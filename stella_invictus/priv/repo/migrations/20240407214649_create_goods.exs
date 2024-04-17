defmodule Stella.Repo.Migrations.CreateGoods do
  use Ecto.Migration

  def change do
    create table(:Goods) do
      add :name, :string
      add :type, :string
    end
  end
end
