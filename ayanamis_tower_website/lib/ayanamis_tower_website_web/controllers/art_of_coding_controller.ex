defmodule AyanamisTowerWebsiteWeb.ArtOfCodingController do
  use AyanamisTowerWebsiteWeb, :controller

  def index(conn, _params) do
    render(conn, :index, layout: false)
  end
end