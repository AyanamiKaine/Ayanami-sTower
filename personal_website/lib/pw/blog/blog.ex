defmodule Pw.Blog do
  alias Pw.Blog.Post

  use NimblePublisher,
    build: Post,
    from: Application.app_dir(:pw, "priv/posts/*.md"),
    as: :posts,
    highlighters: [:makeup_elixir, :makeup_erlang]


  # And finally export them
  def all_posts, do: @posts
  def published_posts, do: Enum.filter(all_posts(), &(&1.published == true))
  def recent_posts(num \\ 5), do: Enum.take(published_posts(), num)

  defmodule NotFoundError, do: defexception [:message, plug_status: 404]

  def get_post_by_id!(id) do
    Enum.find(all_posts(), &(&1.id == id)) ||
      raise NotFoundError, "post with id=#{id} not found"
  end

  def get_posts_by_tag!(tag) do
    case Enum.filter(all_posts(), &(tag in &1.tags)) do
      [] -> raise NotFoundError, "posts with tag=#{tag} not found"
      posts -> posts
    end
  end

end
