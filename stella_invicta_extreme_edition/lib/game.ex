defmodule StellaInvicta.Game do
  def run_tick(game_state) do
    game_state
    |> Map.update!(:current_tick, &(&1 + 1))
    |> StellaInvicta.System.Date.run()
    |> StellaInvicta.System.Age.run()
  end

  def simulate_hour(game_state) do
    run_tick(game_state)
  end

  def simulate_day(game_state) do
    1..24
    |> Enum.reduce(game_state, fn _, acc -> simulate_hour(acc) end)
  end

  def simulate_week(game_state) do
    1..7
    |> Enum.reduce(game_state, fn _, acc -> simulate_day(acc) end)
  end

  def simulate_month(game_state) do
    1..30
    |> Enum.reduce(game_state, fn _, acc -> simulate_day(acc) end)
  end

  def simulate_year(game_state) do
    1..12
    |> Enum.reduce(game_state, fn _, acc -> simulate_month(acc) end)
  end
end
