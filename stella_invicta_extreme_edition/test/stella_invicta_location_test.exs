defmodule StellaInvictaTest.Model.Location do
  use ExUnit.Case, async: true

  alias StellaInvicta.Model.Location

  describe "from_json/1" do
    test "creates location with all fields" do
      json = %{"id" => 1, "name" => "Berlin", "description" => "A bustling grey city."}

      assert {:ok, location} = Location.from_json(json)
      assert location.id == 1
      assert location.name == "Berlin"
      assert location.description == "A bustling grey city."
    end

    test "creates location with required fields only" do
      json = %{"id" => 42, "name" => "The Black Forest"}

      assert {:ok, location} = Location.from_json(json)
      assert location.id == 42
      assert location.name == "The Black Forest"
      assert location.description == nil
    end

    test "creates location with nil description" do
      json = %{"id" => 1, "name" => "Berlin", "description" => nil}

      assert {:ok, location} = Location.from_json(json)
      assert location.description == nil
    end

    test "fails when id is missing" do
      json = %{"name" => "Berlin"}

      assert {:error, :invalid_location_json} = Location.from_json(json)
    end

    test "fails when name is missing" do
      json = %{"id" => 1}

      assert {:error, :invalid_location_json} = Location.from_json(json)
    end

    test "fails when id is not an integer" do
      json = %{"id" => "not_an_int", "name" => "Berlin"}

      assert {:error, :invalid_location_json} = Location.from_json(json)
    end

    test "fails when name is not a string" do
      json = %{"id" => 1, "name" => 123}

      assert {:error, :invalid_location_json} = Location.from_json(json)
    end

    test "fails when description is not a string or nil" do
      json = %{"id" => 1, "name" => "Berlin", "description" => 123}

      assert {:error, :invalid_location_json} = Location.from_json(json)
    end

    test "fails with empty map" do
      assert {:error, :invalid_location_json} = Location.from_json(%{})
    end

    test "fails with non-map input" do
      assert {:error, :invalid_location_json} = Location.from_json("not a map")
      assert {:error, :invalid_location_json} = Location.from_json(nil)
      assert {:error, :invalid_location_json} = Location.from_json([])
    end

    test "ignores extra fields in JSON" do
      json = %{
        "id" => 1,
        "name" => "Berlin",
        "description" => "A city",
        "extra_field" => "ignored",
        "another" => 999
      }

      assert {:ok, location} = Location.from_json(json)
      assert location.id == 1
      assert location.name == "Berlin"
      assert location.description == "A city"
    end
  end

  describe "from_json!/1" do
    test "returns location on valid input" do
      json = %{"id" => 1, "name" => "Berlin"}

      location = Location.from_json!(json)
      assert %Location{} = location
      assert location.id == 1
      assert location.name == "Berlin"
    end

    test "raises ArgumentError on invalid input" do
      json = %{"name" => "Berlin"}

      assert_raise ArgumentError, ~r/Invalid Location JSON/, fn ->
        Location.from_json!(json)
      end
    end
  end

  describe "to_json/1" do
    test "converts location to JSON map" do
      location = %Location{id: 1, name: "Berlin", description: "A city"}

      json = Location.to_json(location)

      assert json == %{
               "id" => 1,
               "name" => "Berlin",
               "description" => "A city"
             }
    end

    test "includes nil description" do
      location = %Location{id: 1, name: "Berlin", description: nil}

      json = Location.to_json(location)

      assert json["description"] == nil
    end

    test "roundtrip from_json -> to_json preserves data" do
      original = %{"id" => 42, "name" => "Neumark", "description" => "Smells of ale."}

      {:ok, location} = Location.from_json(original)
      roundtrip = Location.to_json(location)

      assert roundtrip == original
    end
  end

  describe "String.Chars protocol" do
    test "converts location to string" do
      location = %Location{id: 1, name: "Berlin", description: nil}

      assert to_string(location) == "Berlin (ID: 1)"
    end
  end

  describe "struct enforcement" do
    test "creating struct without id raises error" do
      assert_raise ArgumentError, ~r/the following keys must also be given/, fn ->
        struct!(Location, name: "Berlin")
      end
    end

    test "creating struct without name raises error" do
      assert_raise ArgumentError, ~r/the following keys must also be given/, fn ->
        struct!(Location, id: 1)
      end
    end

    test "creating struct with required keys succeeds" do
      location = struct!(Location, id: 1, name: "Berlin")
      assert location.id == 1
      assert location.name == "Berlin"
    end
  end
end
