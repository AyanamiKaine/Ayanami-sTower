defmodule StellaInvicta do
  def run do
  end

  alias StellaInvicta.Supervisor, as: LibSup

  # --- The Public API ---

  @doc """
  Subscribes the current process to updates from MyEventLib.
  """
  def subscribe do
    # The user calls MyEventLib.subscribe()
    # We internally map that to our private PubSub server and a standard topic.
    Phoenix.PubSub.subscribe(LibSup.pubsub_name(), "main_topic")
  end

  @doc """
  Unsubscribes the current process.
  """
  def unsubscribe do
    Phoenix.PubSub.unsubscribe(LibSup.pubsub_name(), "main_topic")
  end

  # --- Internal Logic ---

  @doc """
  Performs some work and broadcasts the result.
  This represents the "Business Logic" of your library.
  """
  def perform_work(data) do
    # 1. Do the work
    result = String.upcase(data)

    # 2. Broadcast internally
    broadcast({:work_completed, result})

    {:ok, result}
  end

  # Private helper to hide the broadcast mechanics
  defp broadcast(message) do
    Phoenix.PubSub.broadcast(LibSup.pubsub_name(), "main_topic", message)
  end

end
