defmodule StellaInvicta.System.Age do
  @moduledoc """
  System responsible for updating character ages.
  Listens to date events and updates ages on new days.
  """

  @behaviour StellaInvicta.System

  alias StellaInvicta.Model.Character
  alias StellaInvicta.MessageQueue

  @impl true
  def subscriptions do
    [:date_events]
  end

  @impl true
  def handle_message(world, :date_events, {:new_day, _day}) do
    # Age characters when a new day starts (birthday check)
    update_ages(world)
  end

  def handle_message(world, :date_events, {:new_year, _year}) do
    # Also check on new year
    update_ages(world)
  end

  def handle_message(world, _topic, _message), do: world

  @doc """
  Runs the age system, updating all characters whose birthday has passed.
  """
  @impl true
  def run(world) do
    # Main tick logic - can be used for periodic checks
    # Most age updates happen via message handling
    world
  end

  defp update_ages(world) do
    current_date = Map.get(world, :date, %{day: 1, month: 1, year: 1})
    characters = Map.get(world, :characters, %{})

    {updated_characters, world} =
      Enum.reduce(characters, {%{}, world}, fn {id, character}, {chars_acc, world_acc} ->
        {updated_char, world_acc} = maybe_age_character(character, current_date, world_acc)
        {Map.put(chars_acc, id, updated_char), world_acc}
      end)

    %{world | characters: updated_characters}
  end

  @doc """
  Checks if a character should age up and returns the updated character.
  A character ages when the current date reaches or passes their birth date anniversary.
  Also publishes events when a character ages.
  """
  def maybe_age_character(%Character{birth_date: nil} = character, _current_date, world) do
    # No birth date set, cannot calculate age
    {character, world}
  end

  def maybe_age_character(%Character{birth_date: birth_date} = character, current_date, world) do
    expected_age = calculate_age(birth_date, current_date)

    if expected_age > character.age do
      updated_char = %{character | age: expected_age}
      # Publish birthday event
      world =
        MessageQueue.publish(world, :character_events, {:birthday, character.id, expected_age})

      {updated_char, world}
    else
      {character, world}
    end
  end

  @doc """
  Calculates age based on birth date and current date.
  Returns the number of complete years lived.
  """
  def calculate_age(birth_date, current_date) do
    years_diff = current_date.year - birth_date.year

    # Check if birthday has occurred this year
    birthday_passed? =
      current_date.month > birth_date.month or
        (current_date.month == birth_date.month and current_date.day >= birth_date.day)

    if birthday_passed? do
      years_diff
    else
      years_diff - 1
    end
  end

  @doc """
  Creates a birth date for a character based on their current age and the current date.
  Useful for initializing characters with an age but no birth date.
  """
  def create_birth_date(age, current_date, opts \\ []) do
    birth_day = Keyword.get(opts, :day, 1)
    birth_month = Keyword.get(opts, :month, 1)

    %{
      day: birth_day,
      month: birth_month,
      year: current_date.year - age
    }
  end

  @doc """
  Initializes birth dates for all characters that have an age but no birth date.
  """
  def initialize_birth_dates(world) do
    current_date = Map.get(world, :date, %{day: 1, month: 1, year: 1})
    characters = Map.get(world, :characters, %{})

    updated_characters =
      characters
      |> Enum.map(fn {id, character} ->
        {id, maybe_set_birth_date(character, current_date)}
      end)
      |> Map.new()

    %{world | characters: updated_characters}
  end

  defp maybe_set_birth_date(%Character{birth_date: nil, age: age} = character, current_date)
       when not is_nil(age) do
    birth_date = create_birth_date(age, current_date)
    %{character | birth_date: birth_date}
  end

  defp maybe_set_birth_date(character, _current_date) do
    character
  end
end
