defmodule Pw.Application do
  # See https://hexdocs.pm/elixir/Application.html
  # for more information on OTP Applications
  @moduledoc false

  use Application

  @impl true
  def start(_type, _args) do
    children = [
      PwWeb.Telemetry,
      {DNSCluster, query: Application.get_env(:pw, :dns_cluster_query) || :ignore},
      {Phoenix.PubSub, name: Pw.PubSub},
      # Start the Finch HTTP client for sending emails
      {Finch, name: Pw.Finch},
      # Start a worker by calling: Pw.Worker.start_link(arg)
      # {Pw.Worker, arg},
      # Start to serve requests, typically the last entry
      PwWeb.Endpoint
    ]

    # See https://hexdocs.pm/elixir/Supervisor.html
    # for other strategies and supported options
    opts = [strategy: :one_for_one, name: Pw.Supervisor]
    Supervisor.start_link(children, opts)
  end

  # Tell Phoenix to update the endpoint configuration
  # whenever the application is updated.
  @impl true
  def config_change(changed, _new, removed) do
    PwWeb.Endpoint.config_change(changed, removed)
    :ok
  end
end
