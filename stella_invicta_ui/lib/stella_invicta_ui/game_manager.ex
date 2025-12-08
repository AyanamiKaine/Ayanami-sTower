defmodule StellaInvictaUi.GameManager do
  @moduledoc """
  Manages multiple game instances, allowing creation, deletion, and switching between games.

  Each game runs in its own GenServer process and has a unique identifier.
  """

  use GenServer
  require Logger

  # Client API

  def start_link(opts \\ []) do
    name = Keyword.get(opts, :name, __MODULE__)
    GenServer.start_link(__MODULE__, opts, name: name)
  end

  @doc """
  Creates a new game with the given name.
  Returns {:ok, game_id} or {:error, reason}
  """
  def create_game(name, manager \\ __MODULE__) do
    GenServer.call(manager, {:create_game, name})
  end

  @doc """
  Deletes a game by ID.
  Returns :ok or {:error, reason}
  """
  def delete_game(game_id, manager \\ __MODULE__) do
    GenServer.call(manager, {:delete_game, game_id})
  end

  @doc """
  Lists all active games with their metadata.
  Returns list of game info maps.
  """
  def list_games(manager \\ __MODULE__) do
    GenServer.call(manager, :list_games)
  end

  @doc """
  Gets info about a specific game.
  Returns {:ok, game_info} or {:error, :not_found}
  """
  def get_game(game_id, manager \\ __MODULE__) do
    GenServer.call(manager, {:get_game, game_id})
  end

  @doc """
  Gets the current active game ID.
  """
  def get_current_game(manager \\ __MODULE__) do
    GenServer.call(manager, :get_current_game)
  end

  @doc """
  Sets the current active game.
  """
  def set_current_game(game_id, manager \\ __MODULE__) do
    GenServer.call(manager, {:set_current_game, game_id})
  end

  # Server Callbacks

  @impl true
  def init(_opts) do
    {:ok,
     %{
       games: %{},
       current_game: nil,
       next_id: 1
     }}
  end

  @impl true
  def handle_call(:list_games, _from, state) do
    games_list =
      state.games
      |> Enum.map(fn {game_id, game_info} ->
        Map.put(game_info, :id, game_id)
      end)
      |> Enum.sort_by(& &1.created_at)

    {:reply, games_list, state}
  end

  @impl true
  def handle_call({:get_game, game_id}, _from, state) do
    case Map.get(state.games, game_id) do
      nil -> {:reply, {:error, :not_found}, state}
      game_info -> {:reply, {:ok, Map.put(game_info, :id, game_id)}, state}
    end
  end

  @impl true
  def handle_call(:get_current_game, _from, state) do
    {:reply, state.current_game, state}
  end

  @impl true
  def handle_call({:set_current_game, game_id}, _from, state) do
    if Map.has_key?(state.games, game_id) do
      new_state = %{state | current_game: game_id}
      {:reply, :ok, new_state}
    else
      {:reply, {:error, :not_found}, state}
    end
  end

  @impl true
  def handle_call({:create_game, name}, _from, state) do
    game_id = state.next_id
    game_name = :"StellaInvictaUi.GameServer_#{game_id}"

    case StellaInvictaUi.GameServer.start_link(name: game_name, game_id: game_id) do
      {:ok, _pid} ->
        game_info = %{
          name: name,
          game_server: game_name,
          created_at: DateTime.utc_now(),
          playing: false,
          speed: :hour
        }

        new_games = Map.put(state.games, game_id, game_info)

        new_state = %{
          state
          | games: new_games,
            next_id: game_id + 1,
            current_game: state.current_game || game_id
        }

        Logger.info("Created new game #{name} with ID #{game_id}")
        {:reply, {:ok, game_id}, new_state}

      {:error, reason} ->
        Logger.error("Failed to create game #{name}: #{inspect(reason)}")
        {:reply, {:error, reason}, state}
    end
  end

  @impl true
  def handle_call({:delete_game, game_id}, _from, state) do
    case Map.get(state.games, game_id) do
      nil ->
        {:reply, {:error, :not_found}, state}

      game_info ->
        # Stop the game server
        case GenServer.stop(game_info.game_server) do
          :ok ->
            new_games = Map.delete(state.games, game_id)

            # If we deleted the current game, switch to another one
            new_current =
              if state.current_game == game_id do
                case Enum.at(Map.keys(new_games), 0) do
                  nil -> nil
                  other_id -> other_id
                end
              else
                state.current_game
              end

            new_state = %{state | games: new_games, current_game: new_current}
            Logger.info("Deleted game #{game_id}")
            {:reply, :ok, new_state}

          {:error, reason} ->
            Logger.error("Failed to stop game server for game #{game_id}: #{inspect(reason)}")
            {:reply, {:error, reason}, state}
        end
    end
  end
end
