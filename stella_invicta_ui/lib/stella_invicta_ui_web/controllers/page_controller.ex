defmodule StellaInvictaUiWeb.PageController do
  use StellaInvictaUiWeb, :controller

  def home(conn, _params) do
    render(conn, :home)
  end
end
