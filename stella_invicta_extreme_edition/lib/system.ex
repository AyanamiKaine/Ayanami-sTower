defmodule StellaInvicta.System do
  @moduledoc """
  Behaviour for game systems that can participate in the message queue.

  Systems implementing this behaviour can:
  - Run their main logic via `run/1`
  - Process incoming messages via `handle_message/3`
  - Declare their subscriptions via `subscriptions/0`

  ## Example

      defmodule MySystem do
        @behaviour StellaInvicta.System

        @impl true
        def run(game_state) do
          # Main system logic
          game_state
        end

        @impl true
        def subscriptions do
          [:character_events, :date_events]
        end

        @impl true
        def handle_message(game_state, topic, message) do
          case {topic, message} do
            {:character_events, {:character_died, id}} ->
              # Handle character death
              game_state
            _ ->
              game_state
          end
        end
      end
  """

  @doc """
  Runs the system's main logic for the current tick.
  """
  @callback run(game_state :: map()) :: map()

  @doc """
  Returns a list of topics this system subscribes to.
  Default implementation returns an empty list.
  """
  @callback subscriptions() :: [atom() | String.t() | tuple()]

  @doc """
  Handles an incoming message from a subscribed topic.
  Returns the updated game state.
  """
  @callback handle_message(
              game_state :: map(),
              topic :: atom() | String.t() | tuple(),
              message :: any()
            ) :: map()

  @optional_callbacks subscriptions: 0, handle_message: 3

  @doc """
  Checks if a module implements the System behaviour.
  """
  def implements_behaviour?(module) do
    behaviours = module.module_info(:attributes)[:behaviour] || []
    __MODULE__ in behaviours
  end

  @doc """
  Gets the subscriptions for a system, returning empty list if not implemented.
  """
  def get_subscriptions(system_module) do
    if function_exported?(system_module, :subscriptions, 0) do
      system_module.subscriptions()
    else
      []
    end
  end

  @doc """
  Calls handle_message if the system implements it, otherwise returns state unchanged.
  """
  def dispatch_message(system_module, game_state, topic, message) do
    if function_exported?(system_module, :handle_message, 3) do
      system_module.handle_message(game_state, topic, message)
    else
      game_state
    end
  end
end
