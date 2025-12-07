defmodule StellaInvicta.System.AgeTest do
  use ExUnit.Case, async: true

  alias StellaInvicta.System.Age
  alias StellaInvicta.Model.Character

  describe "calculate_age/2" do
    test "calculates correct age when birthday has passed this year" do
      birth_date = %{day: 1, month: 1, year: 1}
      current_date = %{day: 15, month: 6, year: 31}

      assert Age.calculate_age(birth_date, current_date) == 30
    end

    test "calculates correct age when birthday has not passed this year" do
      birth_date = %{day: 15, month: 6, year: 1}
      current_date = %{day: 1, month: 1, year: 31}

      assert Age.calculate_age(birth_date, current_date) == 29
    end

    test "calculates correct age on birthday" do
      birth_date = %{day: 15, month: 6, year: 1}
      current_date = %{day: 15, month: 6, year: 31}

      assert Age.calculate_age(birth_date, current_date) == 30
    end

    test "calculates age of 0 for newborn" do
      birth_date = %{day: 1, month: 1, year: 1}
      current_date = %{day: 1, month: 1, year: 1}

      assert Age.calculate_age(birth_date, current_date) == 0
    end
  end

  describe "maybe_age_character/3" do
    test "ages character when expected age is greater than current age" do
      character = %Character{
        id: 1,
        name: "Test",
        age: 29,
        birth_date: %{day: 1, month: 1, year: 1}
      }

      current_date = %{day: 15, month: 6, year: 31}
      world = %{}

      {result, _world} = Age.maybe_age_character(character, current_date, world)
      assert result.age == 30
    end

    test "does not change age when character age matches expected" do
      character = %Character{
        id: 1,
        name: "Test",
        age: 30,
        birth_date: %{day: 1, month: 1, year: 1}
      }

      current_date = %{day: 15, month: 6, year: 31}
      world = %{}

      {result, _world} = Age.maybe_age_character(character, current_date, world)
      assert result.age == 30
    end

    test "returns character unchanged when no birth_date" do
      character = %Character{
        id: 1,
        name: "Test",
        age: 30,
        birth_date: nil
      }

      current_date = %{day: 15, month: 6, year: 31}
      world = %{}

      {result, _world} = Age.maybe_age_character(character, current_date, world)
      assert result.age == 30
    end
  end

  describe "create_birth_date/3" do
    test "creates birth date based on age and current date" do
      current_date = %{day: 15, month: 6, year: 31}
      birth_date = Age.create_birth_date(30, current_date)

      assert birth_date.year == 1
      assert birth_date.month == 1
      assert birth_date.day == 1
    end

    test "creates birth date with custom day and month" do
      current_date = %{day: 15, month: 6, year: 31}
      birth_date = Age.create_birth_date(30, current_date, day: 15, month: 6)

      assert birth_date.year == 1
      assert birth_date.month == 6
      assert birth_date.day == 15
    end
  end

  describe "run/1" do
    test "run/1 returns world unchanged (aging happens via message handling)" do
      world = %{
        date: %{day: 15, month: 6, year: 31, hour: 0},
        characters: %{
          1 => %Character{
            id: 1,
            name: "Charlemagne",
            age: 29,
            birth_date: %{day: 1, month: 1, year: 1}
          },
          2 => %Character{
            id: 2,
            name: "Carloman",
            age: 24,
            birth_date: %{day: 1, month: 1, year: 6}
          }
        }
      }

      # run/1 no longer updates ages - that happens via handle_message
      result = Age.run(world)

      # Ages should be unchanged since run/1 just returns world
      assert result.characters[1].age == 29
      assert result.characters[2].age == 24
    end

    test "handle_message updates ages on :new_day event" do
      world = %{
        date: %{day: 15, month: 6, year: 31, hour: 0},
        message_queue: %{},
        system_subscriptions: %{},
        characters: %{
          1 => %Character{
            id: 1,
            name: "Charlemagne",
            age: 29,
            birth_date: %{day: 1, month: 1, year: 1}
          },
          2 => %Character{
            id: 2,
            name: "Carloman",
            age: 24,
            birth_date: %{day: 1, month: 1, year: 6}
          }
        }
      }

      # Simulate receiving a :new_day event
      result = Age.handle_message(world, :date_events, {:new_day, 15})

      assert result.characters[1].age == 30
      assert result.characters[2].age == 25
    end

    test "handle_message correctly ages characters with negative birth years (BCE dates)" do
      world = %{
        date: %{day: 15, month: 6, year: 31, hour: 0},
        message_queue: %{},
        system_subscriptions: %{},
        characters: %{
          1 => %Character{
            id: 1,
            name: "Charlemagne",
            age: 29,
            birth_date: %{day: 1, month: 1, year: -29}
          },
          2 => %Character{
            id: 2,
            name: "Carloman",
            age: 24,
            birth_date: %{day: 1, month: 1, year: -24}
          }
        }
      }

      # Simulate receiving a :new_day event
      result = Age.handle_message(world, :date_events, {:new_day, 15})

      # Negative birth years are valid (BCE dates)
      # Character 1: birth year -29, current year 31
      # years_diff = 31 - (-29) = 60, birthday passed (month 1 < month 6), so age = 60
      assert result.characters[1].age == 60
      # Character 2: birth year -24, current year 31
      # years_diff = 31 - (-24) = 55, birthday passed, so age = 55
      assert result.characters[2].age == 55

      # Birth dates should remain unchanged (negative years are valid)
      assert result.characters[1].birth_date.year == -29
      assert result.characters[2].birth_date.year == -24
    end

    test "does not modify characters without birth dates" do
      world = %{
        date: %{day: 15, month: 6, year: 31, hour: 0},
        characters: %{
          1 => %Character{
            id: 1,
            name: "Charlemagne",
            age: 30,
            birth_date: nil
          }
        }
      }

      result = Age.run(world)

      assert result.characters[1].age == 30
    end
  end

  describe "initialize_birth_dates/1" do
    test "sets birth dates for characters with age but no birth date" do
      world = %{
        date: %{day: 15, month: 6, year: 31, hour: 0},
        characters: %{
          1 => %Character{
            id: 1,
            name: "Charlemagne",
            age: 30,
            birth_date: nil
          }
        }
      }

      result = Age.initialize_birth_dates(world)

      assert result.characters[1].birth_date != nil
      assert result.characters[1].birth_date.year == 1
    end

    test "does not overwrite existing birth dates" do
      existing_birth_date = %{day: 15, month: 6, year: 1}

      world = %{
        date: %{day: 15, month: 6, year: 31, hour: 0},
        characters: %{
          1 => %Character{
            id: 1,
            name: "Charlemagne",
            age: 30,
            birth_date: existing_birth_date
          }
        }
      }

      result = Age.initialize_birth_dates(world)

      assert result.characters[1].birth_date == existing_birth_date
    end
  end

  describe "ensure_valid_birth_dates/1" do
    test "assigns default birth date for character with nil birth_date and valid age" do
      world = %{
        date: %{day: 15, month: 6, year: 31, hour: 0},
        characters: %{
          1 => %Character{
            id: 1,
            name: "Test",
            age: 25,
            birth_date: nil
          }
        }
      }

      result = Age.ensure_valid_birth_dates(world)

      assert result.characters[1].birth_date != nil
      # Birth year should be current_year - age = 31 - 25 = 6
      assert result.characters[1].birth_date.year == 6
    end

    test "assigns default birth date and age 0 for character with nil birth_date and nil age" do
      world = %{
        date: %{day: 15, month: 6, year: 31, hour: 0},
        characters: %{
          1 => %Character{
            id: 1,
            name: "Test",
            age: nil,
            birth_date: nil
          }
        }
      }

      result = Age.ensure_valid_birth_dates(world)

      assert result.characters[1].age == 0
      assert result.characters[1].birth_date == %{day: 15, month: 6, year: 31}
    end

    test "does not modify characters with negative birth years (BCE dates are valid)" do
      world = %{
        date: %{day: 15, month: 6, year: 31, hour: 0},
        characters: %{
          1 => %Character{
            id: 1,
            name: "Test",
            age: 20,
            birth_date: %{day: 1, month: 1, year: -5}
          }
        }
      }

      result = Age.ensure_valid_birth_dates(world)

      # Negative birth years are valid (BCE dates), should remain unchanged
      assert result.characters[1].birth_date.year == -5
    end

    test "does not modify characters with valid positive birth years" do
      world = %{
        date: %{day: 15, month: 6, year: 31, hour: 0},
        characters: %{
          1 => %Character{
            id: 1,
            name: "Test",
            age: 25,
            birth_date: %{day: 10, month: 3, year: 6}
          }
        }
      }

      result = Age.ensure_valid_birth_dates(world)

      # Birth date should remain unchanged
      assert result.characters[1].birth_date == %{day: 10, month: 3, year: 6}
    end
  end
end
