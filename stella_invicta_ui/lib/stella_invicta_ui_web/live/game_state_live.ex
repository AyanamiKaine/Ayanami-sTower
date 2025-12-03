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
    world_keys = get_world_keys(game_state)

    socket =
      socket
      |> assign(:game_state, game_state)
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
  def handle_info({:game_state_updated, new_state}, socket) do
    socket =
      socket
      |> assign(:game_state, new_state)
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
            
            <div class="flex flex-wrap gap-2">
              <button
                id="btn-simulate-hour"
                phx-click="simulate_hour"
                class="btn btn-sm btn-primary"
              >
                +1 Hour
              </button>
              <button
                id="btn-simulate-day"
                phx-click="simulate_day"
                class="btn btn-sm btn-primary"
              >
                +1 Day
              </button>
              <button
                id="btn-simulate-week"
                phx-click="simulate_week"
                class="btn btn-sm btn-primary"
              >
                +1 Week
              </button>
              <button
                id="btn-simulate-month"
                phx-click="simulate_month"
                class="btn btn-sm btn-primary"
              >
                +1 Month
              </button>
              <button
                id="btn-simulate-year"
                phx-click="simulate_year"
                class="btn btn-sm btn-primary"
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
end
