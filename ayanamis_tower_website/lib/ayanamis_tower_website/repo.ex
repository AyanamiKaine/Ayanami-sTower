defmodule AyanamisTowerWebsite.Repo do
  use Ecto.Repo,
    otp_app: :ayanamis_tower_website,
    adapter: Ecto.Adapters.SQLite3
end
