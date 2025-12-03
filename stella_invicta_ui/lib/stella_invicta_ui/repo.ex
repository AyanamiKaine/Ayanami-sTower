defmodule StellaInvictaUi.Repo do
  use Ecto.Repo,
    otp_app: :stella_invicta_ui,
    adapter: Ecto.Adapters.SQLite3
end
