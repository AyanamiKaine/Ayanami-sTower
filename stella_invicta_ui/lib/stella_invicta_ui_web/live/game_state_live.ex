defmodule StellaInvictaUiWeb.GameStateLive do
  @moduledoc """
  LiveView for displaying and controlling the game state.
  Shows the game world data as tables, organized by data type.
  """
  use StellaInvictaUiWeb, :live_view

  @impl true
  def mount(_params, _session, socket) do
    if connected?(socket) do
      Phoenix.PubSub.subscribe(StellaInvictaUi.PubSub, "game:state")
    end

    game_state = StellaInvictaUi.GameServer.get_state()
    message_history = StellaInvictaUi.GameServer.get_message_history()
    sim_state = StellaInvictaUi.GameServer.get_simulation_state()
    world_keys = get_world_keys(game_state)

    socket =
      socket
      |> assign(:game_state, game_state)
      |> assign(:message_history, message_history)
      |> assign(:playing, sim_state.playing)
      |> assign(:speed, sim_state.speed)
      |> assign(:world_keys, world_keys)
      |> assign(:selected_table, Enum.at(world_keys, 0))
      |> assign(:page_title, "Game State Viewer")

    {:ok, socket}
  end

  @impl true
  def handle_event("simulate_hour", _params, socket) do
    StellaInvictaUi.GameServer.simulate_hour()
    {:noreply, socket}
  end

  @impl true
  def handle_event("simulate_day", _params, socket) do
    StellaInvictaUi.GameServer.simulate_day()
    {:noreply, socket}
  end

  @impl true
  def handle_event("simulate_week", _params, socket) do
    StellaInvictaUi.GameServer.simulate_week()
    {:noreply, socket}
  end

  @impl true
  def handle_event("simulate_month", _params, socket) do
    StellaInvictaUi.GameServer.simulate_month()
    {:noreply, socket}
  end

  @impl true
  def handle_event("simulate_year", _params, socket) do
    StellaInvictaUi.GameServer.simulate_year()
    {:noreply, socket}
  end

  @impl true
  def handle_event("reset", _params, socket) do
    StellaInvictaUi.GameServer.reset()
    {:noreply, socket}
  end

  @impl true
  def handle_event("select_table", %{"table" => table}, socket) do
    {:noreply, assign(socket, :selected_table, String.to_existing_atom(table))}
  end

  @impl true
  def handle_event("toggle_system", %{"system" => system_name}, socket) do
    system_module = String.to_existing_atom(system_name)
    StellaInvictaUi.GameServer.toggle_system(system_module)
    {:noreply, socket}
  end

  @impl true
  def handle_event("clear_message_history", _params, socket) do
    StellaInvictaUi.GameServer.clear_message_history()
    {:noreply, socket}
  end

  @impl true
  def handle_event("toggle_metrics", _params, socket) do
    StellaInvictaUi.GameServer.toggle_metrics()
    {:noreply, socket}
  end

  @impl true
  def handle_event("reset_metrics", _params, socket) do
    StellaInvictaUi.GameServer.reset_metrics()
    {:noreply, socket}
  end

  @impl true
  def handle_event("play", _params, socket) do
    StellaInvictaUi.GameServer.play()
    {:noreply, socket}
  end

  @impl true
  def handle_event("pause", _params, socket) do
    StellaInvictaUi.GameServer.pause()
    {:noreply, socket}
  end

  @impl true
  def handle_event("set_speed", %{"speed" => speed}, socket) do
    speed = String.to_integer(speed)
    StellaInvictaUi.GameServer.set_speed(speed)
    {:noreply, socket}
  end

  @impl true
  def handle_info({:game_state_updated, new_state, message_history, playing, speed}, socket) do
    socket =
      socket
      |> assign(:game_state, new_state)
      |> assign(:message_history, message_history)
      |> assign(:playing, playing)
      |> assign(:speed, speed)
      |> assign(:world_keys, get_world_keys(new_state))

    {:noreply, socket}
  end

  defp get_world_keys(game_state) do
    game_state
    |> Map.from_struct()
    |> Map.keys()
    |> Enum.sort()
  end

  defp format_date(%{day: day, month: month, year: year, hour: hour}) do
    "Year #{year}, Month #{month}, Day #{day}, Hour #{hour}"
  end

  defp format_date(date), do: inspect(date)

  defp get_table_data(game_state, key) do
    Map.get(game_state, key)
  end

  defp get_systems(game_state) do
    Map.get(game_state, :systems, %{})
    |> Enum.sort_by(fn {mod, _} -> Atom.to_string(mod) end)
  end

  defp format_system_name(module) do
    module
    |> Atom.to_string()
    |> String.replace("Elixir.StellaInvicta.System.", "")
  end

  defp get_system_subscriptions(game_state, system_module) do
    subscriptions = Map.get(game_state, :system_subscriptions, %{})

    case Map.get(subscriptions, system_module) do
      nil -> "none"
      topics when is_struct(topics, MapSet) -> topics |> MapSet.to_list() |> Enum.join(", ")
      topics when is_list(topics) -> Enum.join(topics, ", ")
      _ -> "none"
    end
  end

  defp get_pending_messages(game_state) do
    queue = Map.get(game_state, :message_queue, %{})

    queue
    |> Enum.flat_map(fn {system, messages} ->
      Enum.map(messages, fn {topic, msg} ->
        %{system: system, topic: topic, message: msg}
      end)
    end)
  end

  defp get_metrics_summary(game_state) do
    StellaInvicta.Metrics.get_summary(game_state)
  end

  defp format_time_us(time_us) when is_float(time_us),
    do: :erlang.float_to_binary(time_us, decimals: 1)

  defp format_time_us(time_us) when is_integer(time_us), do: Integer.to_string(time_us)
  defp format_time_us(_), do: "0"

  defp format_time_ms(time_ms) when is_float(time_ms),
    do: :erlang.float_to_binary(time_ms, decimals: 3)

  defp format_time_ms(time_ms) when is_integer(time_ms),
    do: :erlang.float_to_binary(time_ms / 1, decimals: 3)

  defp format_time_ms(_), do: "0.000"

  @impl true
  def render(assigns) do
    ~H"""
    <Layouts.app flash={@flash}>
      <div class="space-y-6">
        <%!-- Header with date and tick info --%>
        <div class="card bg-base-200 p-4">
          <div class="flex flex-wrap items-center justify-between gap-4">
            <div>
              <h1 class="text-2xl font-bold">Game State Viewer</h1>
              
              <p class="text-base-content/70">
                <span class="font-semibold">Date:</span> {format_date(@game_state.date)}
              </p>
              
              <p class="text-base-content/70">
                <span class="font-semibold">Tick:</span> {@game_state.current_tick}
              </p>
            </div>
             <%!-- Real-time Simulation Controls --%>
            <div class="flex items-center gap-3">
              <%!-- Play/Pause Button --%>
              <%= if @playing do %>
                <button
                  id="btn-pause"
                  phx-click="pause"
                  class="btn btn-circle btn-primary"
                  title="Pause"
                >
                  <.icon name="hero-pause-solid" class="size-5" />
                </button>
              <% else %>
                <button
                  id="btn-play"
                  phx-click="play"
                  class="btn btn-circle btn-primary"
                  title="Play"
                >
                  <.icon name="hero-play-solid" class="size-5" />
                </button>
              <% end %>
               <%!-- Speed Controls --%>
              <div class="join">
                <%= for speed <- [1, 2, 5, 10, 20, 50, 100] do %>
                  <button
                    id={"btn-speed-#{speed}"}
                    phx-click="set_speed"
                    phx-value-speed={speed}
                    class={[
                      "join-item btn btn-sm",
                      if(@speed == speed, do: "btn-primary", else: "btn-ghost")
                    ]}
                    title={"Speed #{speed}x"}
                  >
                    {speed}x
                  </button>
                <% end %>
              </div>
               <%!-- Status indicator --%>
              <div class={[
                "badge gap-1",
                if(@playing, do: "badge-success", else: "badge-neutral")
              ]}>
                <span class={[
                  "size-2 rounded-full",
                  if(@playing, do: "bg-success-content animate-pulse", else: "bg-neutral-content")
                ]}>
                </span> {if @playing, do: "Running", else: "Paused"}
              </div>
            </div>
            
            <div class="flex flex-wrap gap-2">
              <button
                id="btn-simulate-hour"
                phx-click="simulate_hour"
                class="btn btn-sm btn-primary btn-soft"
              >
                +1 Hour
              </button>
              <button
                id="btn-simulate-day"
                phx-click="simulate_day"
                class="btn btn-sm btn-primary btn-soft"
              >
                +1 Day
              </button>
              <button
                id="btn-simulate-week"
                phx-click="simulate_week"
                class="btn btn-sm btn-primary btn-soft"
              >
                +1 Week
              </button>
              <button
                id="btn-simulate-month"
                phx-click="simulate_month"
                class="btn btn-sm btn-primary btn-soft"
              >
                +1 Month
              </button>
              <button
                id="btn-simulate-year"
                phx-click="simulate_year"
                class="btn btn-sm btn-primary btn-soft"
              >
                +1 Year
              </button>
              <button id="btn-reset" phx-click="reset" class="btn btn-sm btn-error btn-soft">
                Reset
              </button>
            </div>
          </div>
        </div>
         <%!-- Systems Panel --%>
        <div class="card bg-base-200 p-4">
          <h2 class="text-lg font-semibold mb-3">Systems</h2>
          
          <div class="flex flex-wrap gap-4">
            <%= for {system_module, enabled} <- get_systems(@game_state) do %>
              <label class="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  id={"system-#{format_system_name(system_module)}"}
                  checked={enabled}
                  phx-click="toggle_system"
                  phx-value-system={Atom.to_string(system_module)}
                  class="checkbox checkbox-sm checkbox-primary"
                /> <span class="text-sm">{format_system_name(system_module)}</span>
              </label>
            <% end %>
          </div>
        </div>
         <%!-- Performance Metrics Panel --%> <.render_metrics_panel game_state={@game_state} />
        <%!-- Message Queue Debug Panel --%>
        <div class="card bg-base-200 p-4">
          <div class="flex items-center justify-between mb-3">
            <h2 class="text-lg font-semibold">Message Queue</h2>
            
            <button
              id="btn-clear-messages"
              phx-click="clear_message_history"
              class="btn btn-xs btn-ghost"
            >
              Clear History
            </button>
          </div>
           <%!-- Subscriptions --%>
          <div class="mb-4">
            <h3 class="text-sm font-semibold mb-2 text-base-content/70">System Subscriptions</h3>
            
            <div class="flex flex-wrap gap-2">
              <%= for {system_module, _enabled} <- get_systems(@game_state) do %>
                <div class="badge badge-outline badge-sm">
                  <span class="font-semibold mr-1">{format_system_name(system_module)}:</span>
                  <span class="text-base-content/70">
                    {get_system_subscriptions(@game_state, system_module)}
                  </span>
                </div>
              <% end %>
            </div>
          </div>
           <%!-- Pending Messages --%>
          <div class="mb-4">
            <h3 class="text-sm font-semibold mb-2 text-base-content/70">Pending Messages</h3>
             <.render_pending_messages game_state={@game_state} />
          </div>
           <%!-- Message History --%>
          <div>
            <h3 class="text-sm font-semibold mb-2 text-base-content/70">
              Message History ({length(@message_history)} messages)
            </h3>
            
            <div class="max-h-48 overflow-y-auto bg-base-100 rounded-box p-2">
              <%= if @message_history == [] do %>
                <p class="text-base-content/50 italic text-sm">No messages yet</p>
              <% else %>
                <ul class="space-y-1">
                  <li
                    :for={entry <- @message_history}
                    class="text-xs font-mono border-b border-base-200 pb-1"
                  >
                    <span class="text-base-content/50">[Tick {entry.tick}]</span>
                    <span class="text-primary ml-1">{inspect(entry.message)}</span>
                  </li>
                </ul>
              <% end %>
            </div>
          </div>
        </div>
         <%!-- Table selector tabs --%>
        <div class="flex flex-wrap gap-2">
          <%= for key <- @world_keys do %>
            <button
              id={"tab-#{key}"}
              phx-click="select_table"
              phx-value-table={key}
              class={[
                "btn btn-sm",
                if(@selected_table == key, do: "btn-primary", else: "btn-ghost")
              ]}
            >
              {key |> Atom.to_string() |> String.replace("_", " ") |> String.capitalize()}
            </button>
          <% end %>
        </div>
         <%!-- Data display --%>
        <div class="card bg-base-200 p-4">
          <h2 class="text-xl font-semibold mb-4">
            {@selected_table |> Atom.to_string() |> String.replace("_", " ") |> String.capitalize()}
          </h2>
          
          <.render_table_data
            data={get_table_data(@game_state, @selected_table)}
            key={@selected_table}
          />
        </div>
      </div>
    </Layouts.app>
    """
  end

  attr :data, :any, required: true
  attr :key, :atom, required: true

  defp render_table_data(%{} = assigns) when is_struct(assigns.data) do
    ~H"""
    <.render_struct_data data={@data} />
    """
  end

  defp render_table_data(%{data: data} = assigns) when is_map(data) and map_size(data) == 0 do
    ~H"""
    <p class="text-base-content/50 italic">No data</p>
    """
  end

  defp render_table_data(%{data: data, key: key} = assigns) when is_map(data) do
    entries = Map.to_list(data)
    first_value = elem(Enum.at(entries, 0), 1)

    assigns =
      assigns
      |> assign(:entries, entries)
      |> assign(:first_value, first_value)
      |> assign(:is_struct_map, is_struct(first_value))
      |> assign(:is_list_map, is_list(first_value))
      |> assign(:key, key)

    ~H"""
    <%= cond do %>
      <% @is_struct_map -> %>
        <.render_struct_map entries={@entries} />
      <% @is_list_map -> %>
        <.render_list_map entries={@entries} />
      <% true -> %>
        <.render_simple_map entries={@entries} />
    <% end %>
    """
  end

  defp render_table_data(%{data: data} = assigns) when is_tuple(data) do
    assigns = assign(assigns, :list_data, Tuple.to_list(data))

    ~H"""
    <div class="overflow-x-auto">
      <%= if @list_data == [] do %>
        <p class="text-base-content/50 italic">No data</p>
      <% else %>
        <ul class="list bg-base-100 rounded-box">
          <li :for={{item, idx} <- Enum.with_index(@list_data)} class="list-row">
            <div class="list-col-grow">
              <span class="font-mono text-sm">{idx}:</span> <span class="ml-2">{inspect(item)}</span>
            </div>
          </li>
        </ul>
      <% end %>
    </div>
    """
  end

  defp render_table_data(assigns) do
    ~H"""
    <div class="p-4 bg-base-100 rounded-box">
      <pre class="text-sm font-mono whitespace-pre-wrap">{inspect(@data, pretty: true)}</pre>
    </div>
    """
  end

  attr :data, :any, required: true

  defp render_struct_data(assigns) do
    fields =
      assigns.data
      |> Map.from_struct()
      |> Map.to_list()

    assigns = assign(assigns, :fields, fields)

    ~H"""
    <div class="overflow-x-auto">
      <table class="table table-zebra">
        <thead>
          <tr>
            <th>Field</th>
            
            <th>Value</th>
          </tr>
        </thead>
        
        <tbody>
          <tr :for={{field, value} <- @fields}>
            <td class="font-semibold">{field}</td>
            
            <td><code class="text-sm">{inspect(value)}</code></td>
          </tr>
        </tbody>
      </table>
    </div>
    """
  end

  attr :entries, :list, required: true

  defp render_struct_map(assigns) do
    # Get all field names from the first struct
    {_id, first_struct} = Enum.at(assigns.entries, 0)

    columns =
      first_struct
      |> Map.from_struct()
      |> Map.keys()

    assigns =
      assigns
      |> assign(:columns, columns)

    ~H"""
    <div class="overflow-x-auto">
      <table class="table table-zebra table-sm">
        <thead>
          <tr>
            <th>ID</th>
            
            <th :for={col <- @columns}>{col |> Atom.to_string() |> String.capitalize()}</th>
          </tr>
        </thead>
        
        <tbody>
          <tr :for={{id, struct} <- @entries}>
            <td class="font-mono">{inspect(id)}</td>
            
            <td :for={col <- @columns}><.render_cell_value value={Map.get(struct, col)} /></td>
          </tr>
        </tbody>
      </table>
    </div>
    """
  end

  attr :entries, :list, required: true

  defp render_list_map(assigns) do
    ~H"""
    <div class="overflow-x-auto">
      <table class="table table-zebra table-sm">
        <thead>
          <tr>
            <th>ID</th>
            
            <th>Values</th>
          </tr>
        </thead>
        
        <tbody>
          <tr :for={{id, list} <- @entries}>
            <td class="font-mono">{inspect(id)}</td>
            
            <td>
              <%= if list == [] do %>
                <span class="text-base-content/50 italic">Empty</span>
              <% else %>
                <ul class="space-y-1">
                  <li :for={item <- list} class="text-sm"><.render_cell_value value={item} /></li>
                </ul>
              <% end %>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    """
  end

  attr :entries, :list, required: true

  defp render_simple_map(assigns) do
    ~H"""
    <div class="overflow-x-auto">
      <table class="table table-zebra table-sm">
        <thead>
          <tr>
            <th>Key</th>
            
            <th>Value</th>
          </tr>
        </thead>
        
        <tbody>
          <tr :for={{key, value} <- @entries}>
            <td class="font-mono">{inspect(key)}</td>
            
            <td><.render_cell_value value={value} /></td>
          </tr>
        </tbody>
      </table>
    </div>
    """
  end

  attr :value, :any, required: true

  defp render_cell_value(%{value: value} = assigns) when is_struct(value) do
    fields = Map.from_struct(value) |> Map.to_list()
    assigns = assign(assigns, :fields, fields)

    ~H"""
    <div class="flex flex-wrap gap-1">
      <span :for={{k, v} <- @fields} class="badge badge-sm badge-ghost">{k}: {inspect(v)}</span>
    </div>
    """
  end

  defp render_cell_value(%{value: value} = assigns) when is_list(value) do
    ~H"""
    <div class="flex flex-wrap gap-1">
      <span :for={item <- @value} class="badge badge-sm badge-primary badge-soft">
        {inspect(item)}
      </span>
    </div>
    """
  end

  defp render_cell_value(assigns) do
    ~H"""
    <code class="text-sm">{inspect(@value)}</code>
    """
  end

  attr :game_state, :map, required: true

  defp render_pending_messages(assigns) do
    pending = get_pending_messages(assigns.game_state)
    assigns = assign(assigns, :pending, pending)

    ~H"""
    <%= if @pending == [] do %>
      <p class="text-base-content/50 italic text-sm">No pending messages</p>
    <% else %>
      <div class="overflow-x-auto">
        <table class="table table-zebra table-xs">
          <thead>
            <tr>
              <th>System</th>
              
              <th>Topic</th>
              
              <th>Message</th>
            </tr>
          </thead>
          
          <tbody>
            <tr :for={entry <- @pending}>
              <td class="font-mono text-xs">{format_system_name(entry.system)}</td>
              
              <td class="text-xs">{inspect(entry.topic)}</td>
              
              <td class="text-xs">{inspect(entry.message)}</td>
            </tr>
          </tbody>
        </table>
      </div>
    <% end %>
    """
  end

  attr :game_state, :map, required: true

  defp render_metrics_panel(assigns) do
    metrics = get_metrics_summary(assigns.game_state)
    assigns = assign(assigns, :metrics, metrics)

    ~H"""
    <div class="card bg-base-200 p-4">
      <div class="flex items-center justify-between mb-3">
        <h2 class="text-lg font-semibold">Performance Metrics</h2>
        
        <div class="flex gap-2">
          <label class="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              id="metrics-enabled"
              checked={@metrics.enabled}
              phx-click="toggle_metrics"
              class="checkbox checkbox-sm checkbox-primary"
            /> <span class="text-sm">Enabled</span>
          </label>
          <button
            id="btn-reset-metrics"
            phx-click="reset_metrics"
            class="btn btn-xs btn-ghost"
          >
            Reset
          </button>
        </div>
      </div>
      
      <%= if @metrics.enabled do %>
        <%!-- Tick Performance --%>
        <div class="mb-4">
          <h3 class="text-sm font-semibold mb-2 text-base-content/70">Tick Performance</h3>
          
          <div class="stats stats-horizontal bg-base-100 shadow-sm w-full">
            <div class="stat py-2 px-3">
              <div class="stat-title text-xs">Total Ticks</div>
              
              <div class="stat-value text-lg">{@metrics.tick.total_ticks}</div>
            </div>
            
            <div class="stat py-2 px-3">
              <div class="stat-title text-xs">Avg Tick</div>
              
              <div class="stat-value text-lg">{format_time_ms(@metrics.tick.average_tick_ms)} ms</div>
            </div>
            
            <div class="stat py-2 px-3">
              <div class="stat-title text-xs">Peak Tick</div>
              
              <div class="stat-value text-lg">{format_time_ms(@metrics.tick.peak_tick_ms)} ms</div>
            </div>
            
            <div class="stat py-2 px-3">
              <div class="stat-title text-xs">Last Tick</div>
              
              <div class="stat-value text-lg">{format_time_ms(@metrics.tick.last_tick_ms)} ms</div>
            </div>
          </div>
        </div>
         <%!-- System Performance --%>
        <div class="mb-4">
          <h3 class="text-sm font-semibold mb-2 text-base-content/70">System Performance</h3>
          
          <%= if map_size(@metrics.systems) == 0 do %>
            <p class="text-base-content/50 italic text-sm">No system metrics yet</p>
          <% else %>
            <div class="overflow-x-auto">
              <table class="table table-zebra table-xs">
                <thead>
                  <tr>
                    <th>System</th>
                    
                    <th>Run Avg (µs)</th>
                    
                    <th>Run Peak (µs)</th>
                    
                    <th>Msg Avg (µs)</th>
                    
                    <th>Msgs Processed</th>
                    
                    <th>Total (µs)</th>
                  </tr>
                </thead>
                
                <tbody>
                  <%= for {system_module, stats} <- @metrics.systems do %>
                    <tr>
                      <td class="font-mono text-xs">{format_system_name(system_module)}</td>
                      
                      <td class="text-xs">{format_time_us(stats.run_average_us)}</td>
                      
                      <td class="text-xs">{format_time_us(stats.run_peak_us)}</td>
                      
                      <td class="text-xs">{format_time_us(stats.message_average_us)}</td>
                      
                      <td class="text-xs">{stats.messages_processed}</td>
                      
                      <td class="text-xs font-semibold">{format_time_us(stats.total_average_us)}</td>
                    </tr>
                  <% end %>
                </tbody>
              </table>
            </div>
          <% end %>
        </div>
         <%!-- Last Tick Breakdown --%>
        <%= if map_size(@metrics.tick.last_breakdown) > 0 do %>
          <div class="mb-4">
            <h3 class="text-sm font-semibold mb-2 text-base-content/70">Last Tick Breakdown</h3>
            
            <div class="flex flex-wrap gap-2">
              <%= for {system_module, timing} <- @metrics.tick.last_breakdown do %>
                <div class="badge badge-lg badge-ghost gap-1">
                  <span class="font-semibold">{format_system_name(system_module)}:</span>
                  <span>run {timing.run_us}µs</span> <span class="text-base-content/50">|</span>
                  <span>msg {timing.messages_us}µs ({timing.message_count})</span>
                </div>
              <% end %>
            </div>
          </div>
        <% end %>
         <%!-- Message Queue Stats --%>
        <div>
          <h3 class="text-sm font-semibold mb-2 text-base-content/70">Message Queue Stats</h3>
          
          <div class="flex flex-wrap gap-4 text-sm">
            <div>
              <span class="text-base-content/70">Published:</span>
              <span class="font-semibold ml-1">{@metrics.message_queue.total_published}</span>
            </div>
            
            <div>
              <span class="text-base-content/70">Avg Publish:</span>
              <span class="font-semibold ml-1">
                {format_time_us(@metrics.message_queue.average_publish_us)} µs
              </span>
            </div>
            
            <div>
              <span class="text-base-content/70">Peak Queue:</span>
              <span class="font-semibold ml-1">{@metrics.message_queue.peak_queue_size}</span>
            </div>
          </div>
        </div>
      <% else %>
        <p class="text-base-content/50 italic text-sm">
          Metrics collection is disabled. Enable to see performance data.
        </p>
      <% end %>
    </div>
    """
  end
end
