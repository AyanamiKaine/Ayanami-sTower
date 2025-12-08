defmodule StellaInvictaUiWeb.GameLoginLive do
  use StellaInvictaUiWeb, :live_view

  @impl true
  def mount(_params, _session, socket) do
    games = StellaInvictaUi.GameManager.list_games()
    {:ok, assign(socket, page_title: "Join Game", games: games)}
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

        <h1 class="text-4xl font-bold text-primary mb-2">Join a Game</h1>

        <p class="text-base-content/70">Enter a game ID or select from available games</p>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-8 max-w-7xl">
        <%!-- Join Form --%>
        <div class="lg:col-span-1">
          <div class="card bg-base-100 shadow-xl sticky top-8">
            <div class="card-body">
              <h2 class="card-title text-xl mb-4 flex items-center gap-2">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M13 10V3L4 14h7v7l9-11h-7z"
                  />
                </svg>
                Enter Game ID
              </h2>

              <form phx-submit="join_game" id="join-game-form" class="space-y-4">
                <div class="form-control">
                  <label class="label"><span class="label-text font-semibold">Game ID</span></label>
                  <input
                    type="number"
                    name="game_id"
                    placeholder="e.g., 42"
                    class="input input-bordered focus:input-primary w-full"
                    required
                    min="1"
                  />
                  <label class="label">
                    <span class="label-text-alt text-xs text-base-content/60">
                      Enter the unique ID of the game you want to join
                    </span>
                  </label>
                </div>

                <button type="submit" class="btn btn-primary w-full gap-2">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M13 10V3L4 14h7v7l9-11h-7z"
                    />
                  </svg>
                  Join Game
                </button>
              </form>

              <div class="divider my-4">or</div>

              <.link navigate={~p"/manager"} class="btn btn-outline w-full gap-2">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M12 4v16m8-8H4"
                  />
                </svg>
                Create New Game
              </.link>
            </div>
          </div>
        </div>
        <%!-- Available Games List --%>
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
                      d="M12 6v6m0 0v6m0-6h6m-6 0H6"
                    />
                  </svg>
                  <h3 class="text-lg font-semibold text-base-content/70 mb-2">No Games Available</h3>

                  <p class="text-base-content/60 mb-6">
                    There are currently no games available. Create the first one!
                  </p>
                  <.link navigate={~p"/manager"} class="btn btn-primary">Create a Game</.link>
                </div>
              </div>
            <% else %>
              <div class="flex items-center gap-2 mb-4">
                <h2 class="text-2xl font-bold text-base-content">Available Games</h2>
                <span class="badge badge-success badge-lg">{length(@games)}</span>
              </div>

              <div class="grid grid-cols-1 gap-4">
                <%= for game <- @games do %>
                  <button
                    phx-click="join_game"
                    phx-value-game_id={game.id}
                    class="card bg-base-100 shadow-md hover:shadow-xl transition-all duration-200 border border-base-200 hover:border-success/50 hover:bg-base-50 text-left group"
                  >
                    <div class="card-body p-6">
                      <div class="flex items-start justify-between gap-4">
                        <div class="flex-1">
                          <h3 class="card-title text-lg text-base-content group-hover:text-success transition-colors mb-2">
                            {game.name}
                          </h3>

                          <div class="space-y-2">
                            <div class="flex items-center gap-2 text-sm text-base-content/70">
                              <span class="font-semibold">ID:</span>
                              <span class="font-mono bg-base-200 px-3 py-1 rounded-md text-base-content group-hover:bg-success/20 transition-colors">
                                {game.id}
                              </span>
                            </div>

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

                        <div class="flex-shrink-0">
                          <svg
                            class="w-6 h-6 text-base-content/30 group-hover:text-success group-hover:translate-x-1 transition-all duration-200"
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                          >
                            <path
                              stroke-linecap="round"
                              stroke-linejoin="round"
                              stroke-width="2"
                              d="M13 7l5 5m0 0l-5 5m5-5H6"
                            />
                          </svg>
                        </div>
                      </div>
                    </div>
                  </button>
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
  def handle_event("join_game", params, socket) do
    game_id_str =
      params
      |> Map.get("game_id")
      |> to_string()

    game_id_int = String.to_integer(game_id_str)

    case StellaInvictaUi.GameManager.get_game(game_id_int) do
      {:ok, _game} ->
        {:noreply, push_navigate(socket, to: ~p"/game/#{game_id_int}")}

      {:error, _} ->
        {:noreply, put_flash(socket, :error, "Game not found. Check the ID and try again.")}
    end
  end
end
