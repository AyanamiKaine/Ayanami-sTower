defmodule PwWeb.ListHeader do
  use Phoenix.Component

  attr :title, :string, required: true
  attr :condition, :boolean, required: true

  def listHeader(assigns) do
    ~H"""
    <div class="list_header">
      <%= if @condition do %>
        <div class="mt-8">
          <h1 class="text-xl font-bold"><%= @title %></h1>
          <%= render_slot(@inner_block) %>
        </div>
      <% end %>
    </div>
    """
  end
end
