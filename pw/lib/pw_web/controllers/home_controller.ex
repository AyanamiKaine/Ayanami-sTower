defmodule PwWeb.HomeController do
  use PwWeb, :controller

  alias Pw.Blog

  def home(conn, _params) do
    render(conn, :home, posts: Blog.published_posts())
  end
end
