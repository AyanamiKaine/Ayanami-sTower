defmodule PwWeb.AboutHTML do
  use PwWeb, :html
  import PwWeb.Quoteblock

  embed_templates "about_html/*"
end
