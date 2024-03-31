defmodule PwWeb.BlogController do
  use PwWeb, :controller

  alias Pw.Blog

  @spec index(Plug.Conn.t(), any()) :: Plug.Conn.t()
  def index(conn, _params) do
    render(conn, "index.html", posts: Blog.all_posts())
  end

  @spec show(Plug.Conn.t(), map()) :: Plug.Conn.t()
  def show(conn, %{"id" => id}) do
    render(conn, "show.html", post: Blog.get_post_by_id!(id))
  end
end
