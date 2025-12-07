defmodule StellaInvicta.Supervisor do
  use Supervisor

  # A unique name for your library's internal PubSub server.
  # We use a module attribute to avoid typo bugs.
  @pubsub_name StellaInvicta.PubSub

  def start_link(init_arg) do
    Supervisor.start_link(__MODULE__, init_arg, name: __MODULE__)
  end

  @impl true
  def init(_init_arg) do
    children = [
      # Start the PubSub server specifically for this library.
      # We give it a specific name so it doesn't clash with the
      # host app's own Phoenix PubSub.
      {Phoenix.PubSub, name: @pubsub_name}
    ]

    Supervisor.init(children, strategy: :one_for_one)
  end

  # Helper function to expose the internal name to other modules in your lib
  def pubsub_name, do: @pubsub_name
end
