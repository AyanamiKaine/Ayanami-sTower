defmodule Stella.Predefined.Specien do
  def terraner do
    %{
      name: "Terraner",
      base_rate_of_increase: 0.1,
      base_stats: %{
        diplomancy: 5,
        martial: 3,
        stewardship: 2,
        intrigue: 6,
        learning: 2,
        health: 100
      }
    }
  end

  def raakons do
    %{
      name: "Ra'akons",
      base_rate_of_increase: 0.08,
      base_stats: %{
        diplomancy: 2,
        martial: 2,
        stewardship: 6,
        intrigue: 2,
        learning: 4,
        health: 200
      }
    }
  end
end
