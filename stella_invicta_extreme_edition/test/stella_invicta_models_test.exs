defmodule StellaInvictaTest.Model.AllModels do
  use ExUnit.Case, async: true

  alias StellaInvicta.Model.{
    Building,
    Character,
    CharacterRelation,
    Civic,
    Culture,
    Edge,
    Location,
    Polity,
    Population,
    ProductionChains,
    RelationshipType,
    Religion,
    SocialClasses,
    Specie,
    Terrain,
    Trait
  }

  describe "Building" do
    test "from_json with all fields" do
      json = %{
        "id" => 1,
        "name" => "Farm",
        "production_chain_id" => 5,
        "inventory" => %{"wheat" => 100}
      }

      assert {:ok, building} = Building.from_json(json)
      assert building.id == 1
      assert building.name == "Farm"
      assert building.production_chain_id == 5
      assert building.inventory == %{"wheat" => 100}
    end

    test "from_json with required fields only" do
      json = %{"id" => 1, "name" => "Farm"}
      assert {:ok, building} = Building.from_json(json)
      assert building.production_chain_id == nil
    end

    test "from_json fails without required fields" do
      assert {:error, :invalid_building_json} = Building.from_json(%{"name" => "Farm"})
      assert {:error, :invalid_building_json} = Building.from_json(%{"id" => 1})
    end

    test "roundtrip" do
      original = %{"id" => 1, "name" => "Farm", "production_chain_id" => nil, "inventory" => nil}
      {:ok, building} = Building.from_json(original)
      assert Building.to_json(building) == original
    end
  end

  describe "Character" do
    test "from_json with all fields" do
      json = %{
        "id" => 1,
        "name" => "Charlemagne",
        "health" => 100,
        "age" => 30,
        "birth_date" => %{"year" => 742, "month" => 4, "day" => 2},
        "wealth" => 1000,
        "martial" => 15,
        "diplomacy" => 12,
        "stewardship" => 10,
        "intrigue" => 8
      }

      assert {:ok, character} = Character.from_json(json)
      assert character.name == "Charlemagne"
      assert character.martial == 15
    end

    test "from_json with required fields only" do
      json = %{"id" => 1, "name" => "John"}
      assert {:ok, character} = Character.from_json(json)
      assert character.age == nil
    end

    test "from_json fails without required fields" do
      assert {:error, :invalid_character_json} = Character.from_json(%{"name" => "John"})
    end

    test "from_json! raises on invalid" do
      assert_raise ArgumentError, fn -> Character.from_json!(%{}) end
    end
  end

  describe "CharacterRelation" do
    test "from_json valid" do
      json = %{"target_id" => 2, "type_id" => 1}
      assert {:ok, relation} = CharacterRelation.from_json(json)
      assert relation.target_id == 2
      assert relation.type_id == 1
    end

    test "from_json fails without required" do
      assert {:error, :invalid_character_relation_json} =
               CharacterRelation.from_json(%{"target_id" => 1})
    end

    test "roundtrip" do
      original = %{"target_id" => 2, "type_id" => 1}
      {:ok, relation} = CharacterRelation.from_json(original)
      assert CharacterRelation.to_json(relation) == original
    end
  end

  describe "Civic" do
    test "from_json with all fields" do
      json = %{
        "id" => 1,
        "name" => "Democracy",
        "description" => "Rule by the people",
        "opposite" => 2
      }

      assert {:ok, civic} = Civic.from_json(json)
      assert civic.name == "Democracy"
      assert civic.opposite == 2
    end

    test "from_json with required only" do
      json = %{"id" => 1, "name" => "Democracy"}
      assert {:ok, civic} = Civic.from_json(json)
      assert civic.description == nil
    end

    test "from_json fails without required" do
      assert {:error, :invalid_civic_json} = Civic.from_json(%{"id" => 1})
    end
  end

  describe "Culture" do
    test "from_json with all fields" do
      json = %{
        "id" => 1,
        "name" => "Roman",
        "life_needs" => ["food", "water"],
        "everyday_needs" => ["clothing"],
        "luxury_needs" => ["wine"]
      }

      assert {:ok, culture} = Culture.from_json(json)
      assert culture.name == "Roman"
      assert culture.life_needs == ["food", "water"]
    end

    test "from_json with required only" do
      json = %{"id" => 1, "name" => "Roman"}
      assert {:ok, culture} = Culture.from_json(json)
      assert culture.life_needs == nil
    end
  end

  describe "Edge" do
    test "from_json with distance" do
      json = %{"to_id" => 5, "distance" => 10.5}
      assert {:ok, edge} = Edge.from_json(json)
      assert edge.to_id == 5
      assert edge.distance == 10.5
    end

    test "from_json without distance" do
      json = %{"to_id" => 5}
      assert {:ok, edge} = Edge.from_json(json)
      assert edge.distance == nil
    end

    test "from_json fails without to_id" do
      assert {:error, :invalid_edge_json} = Edge.from_json(%{"distance" => 10})
    end

    test "from_json fails with invalid distance" do
      assert {:error, :invalid_edge_json} = Edge.from_json(%{"to_id" => 5, "distance" => "far"})
    end
  end

  describe "Polity" do
    test "from_json with all fields" do
      json = %{
        "id" => 1,
        "name" => "Roman Empire",
        "wealth" => 10000,
        "leader_title" => "Emperor",
        "civics" => [1, 2, 3],
        "parent_entity" => nil
      }

      assert {:ok, polity} = Polity.from_json(json)
      assert polity.name == "Roman Empire"
      assert polity.leader_title == "Emperor"
    end

    test "from_json with required only" do
      json = %{"id" => 1, "name" => "Rome"}
      assert {:ok, polity} = Polity.from_json(json)
      assert polity.wealth == nil
    end
  end

  describe "Population" do
    test "from_json with all fields" do
      json = %{
        "id" => 1,
        "size" => 1000,
        "literacy" => 0.5,
        "happiness" => 0.7,
        "wealth" => 500,
        "population_type_id" => 1
      }

      assert {:ok, pop} = Population.from_json(json)
      assert pop.size == 1000
      assert pop.literacy == 0.5
    end

    test "from_json with required only" do
      json = %{"id" => 1}
      assert {:ok, pop} = Population.from_json(json)
      assert pop.size == nil
    end
  end

  describe "ProductionChains" do
    test "from_json with all fields" do
      json = %{
        "id" => 1,
        "name" => "Wheat Farm",
        "input" => %{"seeds" => 10},
        "output" => %{"wheat" => 100},
        "workforce" => 5
      }

      assert {:ok, chain} = ProductionChains.from_json(json)
      assert chain.name == "Wheat Farm"
      assert chain.workforce == 5
    end

    test "from_json with required only" do
      json = %{"id" => 1, "name" => "Farm"}
      assert {:ok, chain} = ProductionChains.from_json(json)
      assert chain.input == nil
    end
  end

  describe "RelationshipType" do
    test "from_json with all fields" do
      json = %{"id" => 1, "name" => "Friend", "is_symmetric" => true}
      assert {:ok, type} = RelationshipType.from_json(json)
      assert type.name == "Friend"
      assert type.is_symmetric == true
    end

    test "from_json with required only" do
      json = %{"id" => 1, "name" => "Parent"}
      assert {:ok, type} = RelationshipType.from_json(json)
      assert type.is_symmetric == nil
    end

    test "from_json fails with invalid is_symmetric" do
      json = %{"id" => 1, "name" => "Friend", "is_symmetric" => "yes"}
      assert {:error, :invalid_relationship_type_json} = RelationshipType.from_json(json)
    end
  end

  describe "Religion" do
    test "from_json with all fields" do
      json = %{
        "id" => 1,
        "name" => "Solarism",
        "life_needs" => ["prayer"],
        "everyday_needs" => [],
        "luxury_needs" => ["temples"]
      }

      assert {:ok, religion} = Religion.from_json(json)
      assert religion.name == "Solarism"
    end

    test "from_json with required only" do
      json = %{"id" => :catholic, "name" => "Catholic"}
      assert {:ok, religion} = Religion.from_json(json)
      assert religion.id == :catholic
    end

    test "String.Chars protocol" do
      {:ok, religion} = Religion.from_json(%{"id" => 1, "name" => "Solarism"})
      assert to_string(religion) == "Solarism"
    end
  end

  describe "SocialClasses" do
    test "from_json with all fields" do
      json = %{
        "id" => 1,
        "name" => "Nobility",
        "life_needs" => ["food"],
        "everyday_needs" => ["servants"],
        "luxury_needs" => ["jewelry"]
      }

      assert {:ok, social_class} = SocialClasses.from_json(json)
      assert social_class.name == "Nobility"
    end

    test "from_json with required only" do
      json = %{"id" => 1, "name" => "Peasant"}
      assert {:ok, social_class} = SocialClasses.from_json(json)
      assert social_class.life_needs == nil
    end
  end

  describe "Specie" do
    test "from_json with all fields" do
      json = %{
        "id" => 1,
        "name" => "Human",
        "life_needs" => ["oxygen", "water"],
        "everyday_needs" => ["food"],
        "luxury_needs" => []
      }

      assert {:ok, specie} = Specie.from_json(json)
      assert specie.name == "Human"
    end

    test "from_json with required only" do
      json = %{"id" => :human, "name" => "Human"}
      assert {:ok, specie} = Specie.from_json(json)
      assert specie.id == :human
    end
  end

  describe "Terrain" do
    test "from_json valid" do
      json = %{"id" => 1, "name" => "Forest"}
      assert {:ok, terrain} = Terrain.from_json(json)
      assert terrain.name == "Forest"
    end

    test "from_json with atom id" do
      json = %{"id" => :forest, "name" => "Forest"}
      assert {:ok, terrain} = Terrain.from_json(json)
      assert terrain.id == :forest
    end

    test "from_json fails without required" do
      assert {:error, :invalid_terrain_json} = Terrain.from_json(%{"id" => 1})
      assert {:error, :invalid_terrain_json} = Terrain.from_json(%{"name" => "Forest"})
    end

    test "String.Chars protocol" do
      {:ok, terrain} = Terrain.from_json(%{"id" => 1, "name" => "Forest"})
      assert to_string(terrain) == "Forest"
    end

    test "roundtrip" do
      original = %{"id" => 1, "name" => "Forest"}
      {:ok, terrain} = Terrain.from_json(original)
      assert Terrain.to_json(terrain) == original
    end
  end

  describe "Trait" do
    test "from_json with all fields" do
      json = %{
        "id" => 1,
        "name" => "Brave",
        "description" => "Fearless in battle",
        "opposite" => 2
      }

      assert {:ok, trait} = Trait.from_json(json)
      assert trait.name == "Brave"
      assert trait.opposite == 2
    end

    test "from_json with required only" do
      json = %{"id" => 1, "name" => "Brave"}
      assert {:ok, trait} = Trait.from_json(json)
      assert trait.description == nil
    end

    test "from_json fails without required" do
      assert {:error, :invalid_trait_json} = Trait.from_json(%{"id" => 1})
    end
  end

  describe "all models enforce_keys" do
    test "Building requires id and name" do
      assert_raise ArgumentError, fn -> struct!(Building, name: "Farm") end
      assert_raise ArgumentError, fn -> struct!(Building, id: 1) end
    end

    test "Character requires id and name" do
      assert_raise ArgumentError, fn -> struct!(Character, name: "John") end
    end

    test "CharacterRelation requires target_id and type_id" do
      assert_raise ArgumentError, fn -> struct!(CharacterRelation, target_id: 1) end
    end

    test "Civic requires id and name" do
      assert_raise ArgumentError, fn -> struct!(Civic, id: 1) end
    end

    test "Culture requires id and name" do
      assert_raise ArgumentError, fn -> struct!(Culture, id: 1) end
    end

    test "Edge requires to_id" do
      assert_raise ArgumentError, fn -> struct!(Edge, distance: 10) end
    end

    test "Polity requires id and name" do
      assert_raise ArgumentError, fn -> struct!(Polity, id: 1) end
    end

    test "Population requires id" do
      assert_raise ArgumentError, fn -> struct!(Population, size: 100) end
    end

    test "ProductionChains requires id and name" do
      assert_raise ArgumentError, fn -> struct!(ProductionChains, id: 1) end
    end

    test "RelationshipType requires id and name" do
      assert_raise ArgumentError, fn -> struct!(RelationshipType, id: 1) end
    end

    test "Religion requires id and name" do
      assert_raise ArgumentError, fn -> struct!(Religion, id: 1) end
    end

    test "SocialClasses requires id and name" do
      assert_raise ArgumentError, fn -> struct!(SocialClasses, id: 1) end
    end

    test "Specie requires id and name" do
      assert_raise ArgumentError, fn -> struct!(Specie, id: 1) end
    end

    test "Terrain requires id and name" do
      assert_raise ArgumentError, fn -> struct!(Terrain, id: 1) end
    end

    test "Trait requires id and name" do
      assert_raise ArgumentError, fn -> struct!(Trait, id: 1) end
    end
  end
end
