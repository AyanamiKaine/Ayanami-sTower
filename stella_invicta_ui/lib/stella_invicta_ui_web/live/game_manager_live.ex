defmodule StellaInvictaUiWeb.GameManagerLive do
  use StellaInvictaUiWeb, :live_view

  @impl true
  def mount(_params, _session, socket) do
    if connected?(socket) do
      Phoenix.PubSub.subscribe(StellaInvictaUi.PubSub, "games:list")
    end

    games = enrich_games(StellaInvictaUi.GameManager.list_games())

    {:ok, assign(socket, games: games, page_title: "Game Manager")}
  end

  @impl true
  def render(assigns) do
    ~H"""
    <Layouts.flash_group flash={@flash} />
    <div class="min-h-screen bg-gradient-to-br from-base-100 to-base-200 p-4 sm:p-8">
      <%!-- Header --%>
      <div class="mb-12">
        <div class="flex items-center gap-3 mb-2">
          <.link navigate={~p"/"} class="btn btn-ghost btn-sm gap-2">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M10 19l-7-7m0 0l7-7m-7 7h18"
              />
            </svg>
            Back to Home
          </.link>
        </div>

        <h1 class="text-4xl font-bold text-primary mb-2">Game Manager</h1>

        <p class="text-base-content/70">Create, manage, and play your civilization simulations</p>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-8 max-w-7xl">
        <%!-- Create Game Section --%>
        <div class="lg:col-span-1">
          <div class="card bg-base-100 shadow-xl sticky top-8">
            <div class="card-body">
              <h2 class="card-title text-xl mb-4 flex items-center gap-2">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M12 4v16m8-8H4"
                  />
                </svg>
                New Game
              </h2>

              <form phx-submit="create_game" id="create-game-form" class="space-y-4">
                <div class="form-control">
                  <label class="label"><span class="label-text font-semibold">Game Name</span></label>
                  <input
                    type="text"
                    name="name"
                    placeholder="e.g., Ancient Rome"
                    class="input input-bordered focus:input-primary w-full"
                    required
                    minlength="1"
                    maxlength="50"
                  />
                  <label class="label">
                    <span class="label-text-alt text-xs text-base-content/60">
                      Give your civilization a name
                    </span>
                  </label>
                </div>

                <button type="submit" class="btn btn-primary w-full gap-2">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M12 4v16m8-8H4"
                    />
                  </svg>
                  Create Game
                </button>
              </form>

              <div class="divider my-4">or</div>

              <.link navigate={~p"/login"} class="btn btn-outline w-full gap-2">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z"
                  />
                </svg>
                Join Existing Game
              </.link>
            </div>
          </div>
        </div>
        <%!-- Games List Section --%>
        <div class="lg:col-span-2">
          <div class="space-y-4">
            <%= if Enum.empty?(@games) do %>
              <div class="card bg-base-100 shadow-lg border-2 border-dashed border-base-300">
                <div class="card-body text-center py-12">
                  <svg
                    class="w-16 h-16 mx-auto mb-4 text-base-content/30"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M7 12a5 5 0 1110 0A5 5 0 017 12z"
                    />
                  </svg>
                  <h3 class="text-lg font-semibold text-base-content/70 mb-2">No Games Yet</h3>

                  <p class="text-base-content/60 mb-6">
                    Create your first game to get started with your civilization simulation.
                  </p>
                </div>
              </div>
            <% else %>
              <div class="flex items-center gap-2 mb-4">
                <h2 class="text-2xl font-bold text-base-content">Your Games</h2>
                <span class="badge badge-primary badge-lg">{length(@games)}</span>
              </div>

              <div class="grid grid-cols-1 gap-4">
                <%= for game <- @games do %>
                  <div class="card bg-base-100 shadow-md hover:shadow-xl transition-shadow duration-200 border border-base-200 hover:border-primary/50">
                    <div class="card-body p-6">
                      <div class="flex items-start justify-between gap-4">
                        <div class="flex-1">
                          <h3 class="card-title text-xl text-base-content mb-2">{game.name}</h3>

                          <div class="space-y-2">
                            <div class="flex items-center gap-2 text-sm text-base-content/70">
                              <span class="font-semibold">ID:</span>
                              <span class="font-mono bg-base-200 px-3 py-1 rounded-md text-base-content select-all hover:bg-base-300 cursor-pointer">
                                {game.id}
                              </span>
                            </div>

                            <%= if Map.get(game, :world_date) do %>
                              <div class="flex items-center gap-2 text-sm text-base-content/70">
                                <span class="font-semibold">Date:</span>
                                <span class="text-sm">{format_date(Map.get(game, :world_date))}</span>
                              </div>
                            <% end %>

                            <div class="flex items-center gap-2 text-sm text-base-content/70">
                              <span class="font-semibold">Status:</span>
                              <%= if game.playing do %>
                                <span class="badge badge-success gap-1">
                                  <span class="w-2 h-2 bg-success rounded-full animate-pulse"></span>
                                  Playing
                                </span>
                              <% else %>
                                <span class="badge badge-ghost">Paused</span>
                              <% end %>
                            </div>

                            <div class="text-xs text-base-content/50">
                              Created {DateTime.to_iso8601(game.created_at) |> String.slice(0..9)}
                            </div>
                          </div>
                        </div>

                        <div class="flex gap-2 flex-col">
                          <.link navigate={~p"/game/#{game.id}"} class="btn btn-primary btn-sm gap-2">
                            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                              <path d="M8 5v14l11-7z" />
                            </svg>
                            Play
                          </.link>
                          <button
                            phx-click="delete_game"
                            phx-value-game_id={game.id}
                            data-confirm="Are you sure you want to delete '{game.name}'? This cannot be undone."
                            class="btn btn-outline btn-error btn-sm gap-2"
                          >
                            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path
                                stroke-linecap="round"
                                stroke-linejoin="round"
                                stroke-width="2"
                                d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                              />
                            </svg>
                            Delete
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                <% end %>
              </div>
            <% end %>
          </div>
        </div>
      </div>
    </div>
    """
  end

  @impl true
  def handle_event("create_game", %{"name" => name}, socket) do
    case StellaInvictaUi.GameManager.create_game(name) do
      {:ok, _game_id} ->
        Phoenix.PubSub.broadcast(StellaInvictaUi.PubSub, "games:list", :games_updated)
        {:noreply, socket}

      {:error, _reason} ->
        {:noreply, put_flash(socket, :error, "Failed to create game")}
    end
  end

  @impl true
  def handle_event("delete_game", %{"game_id" => game_id}, socket) do
    game_id = String.to_integer(game_id)

    case StellaInvictaUi.GameManager.delete_game(game_id) do
      :ok ->
        Phoenix.PubSub.broadcast(StellaInvictaUi.PubSub, "games:list", :games_updated)
        {:noreply, socket}

      {:error, _reason} ->
        {:noreply, put_flash(socket, :error, "Failed to delete game")}
    end
  end

  @impl true
  def handle_info(:games_updated, socket) do
    games = StellaInvictaUi.GameManager.list_games() |> enrich_games()
    {:noreply, assign(socket, games: games)}
  end

  # Helper function to enrich games with real-time status and date information
  defp enrich_games(games) do
    games
    |> Enum.map(fn game ->
      case StellaInvictaUi.GameManager.get_game(game.id) do
        {:ok, game_info} ->
          sim_state = StellaInvictaUi.GameServer.get_simulation_state(game_info.game_server)
          game_state = StellaInvictaUi.GameServer.get_state(game_info.game_server)

          # Extract world date from game state if available
          world_date =
            case game_state do
              %{world: %{date: date}} -> date
              _ -> nil
            end

          game
          |> Map.put(:playing, sim_state.playing)
          |> Map.put(:speed, sim_state.speed)
          |> Map.put(:world_date, world_date)

        {:error, _} ->
          game
          |> Map.put(:playing, false)
          |> Map.put(:world_date, nil)
      end
    end)
  end

  # Helper function to format world date
  defp format_date(date_map) when is_map(date_map) do
    %{"day" => day, "month" => month, "year" => year} = date_map
    "#{year}-#{String.pad_leading("#{month}", 2, "0")}-#{String.pad_leading("#{day}", 2, "0")}"
  end

  defp format_date(nil), do: ""

  defp format_date(date) when is_atom(date), do: ""
end
