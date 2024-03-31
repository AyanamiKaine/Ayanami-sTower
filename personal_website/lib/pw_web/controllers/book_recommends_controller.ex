defmodule PwWeb.BookRecommendsController do
  use PwWeb, :controller

  def book_recommends(conn, _params) do
    render(conn, :book_recommends, books: Pw.BookRecommends.get_all_books)
  end
end
