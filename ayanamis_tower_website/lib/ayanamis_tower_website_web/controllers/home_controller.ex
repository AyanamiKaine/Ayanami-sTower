defmodule AyanamisTowerWebsiteWeb.HomeController do
  use AyanamisTowerWebsiteWeb, :controller

  def home(conn, _params) do
    render(conn, :home, layout: false)
  end

end
