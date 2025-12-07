defmodule StellaInvictaUi.Application do
  # See https://hexdocs.pm/elixir/Application.html
  # for more information on OTP Applications
  @moduledoc false

  use Application

  @impl true
  def start(_type, _args) do
    children = [
      StellaInvictaUiWeb.Telemetry,
      StellaInvictaUi.Repo,
      {DNSCluster, query: Application.get_env(:stella_invicta_ui, :dns_cluster_query) || :ignore},
      {Phoenix.PubSub, name: StellaInvictaUi.PubSub},
      # Start the game library's supervisor (for its internal PubSub)
      StellaInvicta.Supervisor,
      # Game state server
      StellaInvictaUi.GameServer,
      # Start to serve requests, typically the last entry
      StellaInvictaUiWeb.Endpoint
    ]

    # See https://hexdocs.pm/elixir/Supervisor.html
    # for other strategies and supported options
    opts = [strategy: :one_for_one, name: StellaInvictaUi.Supervisor]
    Supervisor.start_link(children, opts)
  end

  # Tell Phoenix to update the endpoint configuration
  # whenever the application is updated.
  @impl true
  def config_change(changed, _new, removed) do
    StellaInvictaUiWeb.Endpoint.config_change(changed, removed)
    :ok
  end
end
