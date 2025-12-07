defmodule StellaInvicta.System.Date do
  @behaviour StellaInvicta.System

  alias StellaInvicta.MessageQueue

  @impl true
  def run(world) do
    # Get current date or default
    current_date = Map.get(world, :date, %{hour: 0, day: 1, month: 1, year: 1})

    # Calculate new date using the helper function
    {new_date, events} = advance_time(current_date)

    # Publish any date events
    world =
      Enum.reduce(events, world, fn event, acc ->
        MessageQueue.publish(acc, :date_events, event)
      end)

    # Update the world
    Map.put(world, :date, new_date)
  end

  @impl true
  def subscriptions, do: []

  @impl true
  def handle_message(world, _topic, _message), do: world

  # 1. End of Year: Month >= 12, Day >= 30, Hour >= 23
  defp advance_time(%{month: month, day: day, hour: hour} = date)
       when month >= 12 and day >= 30 and hour >= 23 do
    new_date = %{date | hour: 0, day: 1, month: 1, year: date.year + 1}
    events = [:new_hour, :new_day, :new_month, {:new_year, new_date.year}]
    {new_date, events}
  end

  # 2. End of Month: Any Month, Day >= 30, Hour >= 23
  defp advance_time(%{day: day, hour: hour} = date)
       when day >= 30 and hour >= 23 do
    new_date = %{date | hour: 0, day: 1, month: date.month + 1}
    events = [:new_hour, :new_day, {:new_month, new_date.month}]
    {new_date, events}
  end

  # 3. End of Day: Any Day, Hour >= 23
  defp advance_time(%{hour: hour} = date)
       when hour >= 23 do
    new_date = %{date | hour: 0, day: date.day + 1}
    events = [:new_hour, {:new_day, new_date.day}]
    {new_date, events}
  end

  # 4. Normal Hour: Any other time
  # Result: Just Hour + 1
  defp advance_time(date) do
    new_date = %{date | hour: date.hour + 1}
    {new_date, [:new_hour]}
  end
end
