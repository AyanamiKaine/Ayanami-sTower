defmodule PwWeb.Quoteblock do
  use Phoenix.Component

  attr :quote, :string, required: true
  attr :author, :string, required: true

  def quoteblock(assigns) do
    ~H"""
    <div class="quoteblock">
      <blockquote class="p-4 my-4 bg-gray-50 border-l-4 border-gray-300 dark:border-gray-500 dark:bg-gray-800">
      <p class="text-xl italic font-medium leading-relaxed text-gray-900 dark:text-white"><%= @quote %></p>
      <footer class="block text-right mt-4">
          <cite class="font-medium not-italic text-gray-700 dark:text-gray-300">—<%= @author %></cite>
      </footer>
      </blockquote>
    </div>
    """
  end
end