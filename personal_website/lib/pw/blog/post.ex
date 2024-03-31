defmodule Pw.Blog.Post do
  @enforce_keys [:id,:published, :title, :body]
  defstruct [:id,:published, :title, :category, :body]

  def build(filename, attrs, body) do
    id = Path.basename(filename)
    struct!(__MODULE__, [id: id, body: body] ++ Map.to_list(attrs))
  end
end
