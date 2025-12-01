defmodule StellaInvicta.System.Date do
  def run(world) do
    # Get current date or default
    current_date = Map.get(world, :date, %{hour: 0, day: 1, month: 1, year: 1})

    # Calculate new date using the helper function
    new_date = advance_time(current_date)

    # Update the world
    Map.put(world, :date, new_date)
  end

  # 1. End of Year: Month >= 12, Day >= 30, Hour >= 23
  defp advance_time(%{month: month, day: day, hour: hour} = date)
       when month >= 12 and day >= 30 and hour >= 23 do
    %{date | hour: 0, day: 1, month: 1, year: date.year + 1}
  end

  # 2. End of Month: Any Month, Day >= 30, Hour >= 23
  defp advance_time(%{day: day, hour: hour} = date)
       when day >= 30 and hour >= 23 do
    %{date | hour: 0, day: 1, month: date.month + 1}
  end

  # 3. End of Day: Any Day, Hour >= 23
  defp advance_time(%{hour: hour} = date)
       when hour >= 23 do
    %{date | hour: 0, day: date.day + 1}
  end

  # 4. Normal Hour: Any other time
  # Result: Just Hour + 1
  defp advance_time(date) do
    %{date | hour: date.hour + 1}
  end
end
