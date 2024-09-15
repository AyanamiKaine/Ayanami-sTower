defmodule AyanamisTowerWebsiteWeb.HomeHTML do
  @moduledoc """
  This module contains home rendered by HomeController.

  See the `home_html` directory for all templates available.
  """
  use AyanamisTowerWebsiteWeb, :html

  embed_templates "home_html/*"
end
