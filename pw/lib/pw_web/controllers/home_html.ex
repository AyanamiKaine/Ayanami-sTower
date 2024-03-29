defmodule PwWeb.HomeHTML do
  use PwWeb, :html
  import PwWeb.ListHeader
  import PwWeb.Quoteblock

  embed_templates "home_html/*"
end
