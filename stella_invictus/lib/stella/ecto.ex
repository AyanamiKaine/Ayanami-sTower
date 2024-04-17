defmodule Stella.Repo do
  use Ecto.Repo,
    otp_app: :stella,
    adapter: Ecto.Adapters.SQLite3
end
