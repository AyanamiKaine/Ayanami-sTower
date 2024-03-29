defmodule PwWeb.ContactController do
  use PwWeb, :controller

  def contact(conn, _params) do
    render(conn, :contact)
  end
end
