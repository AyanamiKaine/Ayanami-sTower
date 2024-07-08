defmodule PwWeb.BecomeProgrammerController do
  use PwWeb, :controller

  def become_programmer(conn, _params) do
    render(conn, :become_programmer)
  end
end
