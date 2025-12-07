defmodule StellaInvicta.World do
  # Relation: Character -> Religion
  defstruct locations: %{},
            connections: %{},
            terrains: {},
            location_biomes: %{},
            characters: %{},
            religions: %{},
            traits: %{},
            production_chains: %{},
            character_religions: %{},
            character_traits: %{},
            polities: %{},
            character_relations: %{},
            relationship_types: %{},
            polity_relations: %{},
            date: %{},
            current_tick: 0,
            systems: %{}

  def new_galaxy_world() do
  end

  def new_planet_world() do
    %StellaInvicta.World{
      current_tick: 0,
      date: %{
        day: 1,
        month: 1,
        year: 1,
        hour: 0
      },
      production_chains: %{
        :steel_mill => %StellaInvicta.Model.ProductionChains{
          id: :steel_mill,
          name: "Steel Mill",
          input: [
            {:iron, 2},
            {:coal, 2}
          ],
          output: [
            {:steel, 2}
          ],
          workforce: [
            {:laborer, 800},
            {:engineer, 200}
          ]
        }
      },
      locations: %{
        1 => %StellaInvicta.Model.Location{
          id: 1,
          name: "Berlin",
          description: "A bustling grey city."
        },
        2 => %StellaInvicta.Model.Location{
          id: 2,
          name: "Neumark",
          description: "Smells of ale."
        },
        3 => %StellaInvicta.Model.Location{
          id: 3,
          name: "The Black Forest",
          description: "Dark and deep."
        }
      },
      traits: %{
        :brave => %StellaInvicta.Model.Trait{
          id: :brave,
          name: "Brave"
        },
        :scholar => %StellaInvicta.Model.Trait{
          id: :scholar,
          name: "Scholar"
        },
        :tall => %StellaInvicta.Model.Trait{
          id: :tall,
          name: "Tall"
        }
      },
      terrains: %{
        :urban => %StellaInvicta.Model.Terrain{
          id: :urban,
          name: "Urban"
        },
        :plains => %StellaInvicta.Model.Terrain{
          id: :plains,
          name: "Plains"
        },
        :forest => %StellaInvicta.Model.Terrain{
          id: :forest,
          name: "Forest"
        }
      },
      religions: %{
        :catholic => %StellaInvicta.Model.Religion{
          id: :catholic,
          name: "Catholic"
        },
        :monotheism => %StellaInvicta.Model.Religion{
          id: :monotheism,
          name: "The One Light"
        },
        :pagan => %StellaInvicta.Model.Religion{
          id: :pagan,
          name: "Old Gods of the Woods"
        },
        :solarism => %StellaInvicta.Model.Religion{
          id: :solarism,
          name: "Solarism"
        }
      },
      location_biomes: %{
        # Berlin is Urban
        1 => :urban,
        # Neumark is Plains
        2 => :plains,
        # Black Forest is Forest
        3 => :forest
      },
      connections: %{
        # From Berlin (1) you can go to Neumark (2)
        1 => [2],
        # From Neumark (2) you can go to Berlin (1) OR The Black Forest (3)
        2 => [1, 3],
        # The Black Forest is a dead end (just for example), or only goes back to Neumark
        3 => [2]
      },
      characters: %{
        1 => %StellaInvicta.Model.Character{
          id: 1,
          name: "Charlemagne",
          # Born year -29, month 4, day 15. At year 1, month 1, day 1 birthday hasn't passed yet.
          # So age = (1 - (-29)) - 1 = 29
          age: 29,
          birth_date: %{day: 15, month: 4, year: -29}
        },
        2 => %StellaInvicta.Model.Character{
          id: 2,
          name: "Carloman",
          # Born year -24, month 9, day 8. At year 1, month 1, day 1 birthday hasn't passed yet.
          # So age = (1 - (-24)) - 1 = 24
          age: 24,
          birth_date: %{day: 8, month: 9, year: -24}
        }
      },
      relationship_types: %{
        :war => %StellaInvicta.Model.RelationshipType{
          id: :war,
          name: "War",
          is_symmetric: true
        },
        :embargo => %StellaInvicta.Model.RelationshipType{
          id: :embargo,
          name: "Embargo",
          is_symmetric: true
        },
        :non_aggression => %StellaInvicta.Model.RelationshipType{
          id: :non_aggression,
          name: "Non-Aggression Pact",
          is_symmetric: true
        },
        :defensive_pact => %StellaInvicta.Model.RelationshipType{
          id: :defensive_pact,
          name: "Defensive Pact",
          is_symmetric: true
        },
        :full_alliance => %StellaInvicta.Model.RelationshipType{
          id: :full_alliance,
          name: "Full Alliance",
          is_symmetric: true
        },
        :vassal => %StellaInvicta.Model.RelationshipType{
          id: :vassal,
          name: "Vassal",
          is_symmetric: true
        },
        :puppet => %StellaInvicta.Model.RelationshipType{
          id: :puppet,
          name: "Puppet",
          is_symmetric: true
        },
        :charter => %StellaInvicta.Model.RelationshipType{
          id: :charter,
          name: "Charter",
          is_symmetric: true
        },
        :trade_agreement => %StellaInvicta.Model.RelationshipType{
          id: :trade_agreement,
          name: "Trade Agreement",
          is_symmetric: true
        },
        :rival => %StellaInvicta.Model.RelationshipType{
          id: :rival,
          name: "Rival",
          is_symmetric: true
        },
        :parent => %StellaInvicta.Model.RelationshipType{
          id: :parent,
          name: "Parent",
          is_symmetric: false
        },
        :brother => %StellaInvicta.Model.RelationshipType{
          id: :brother,
          name: "Brother",
          is_symmetric: true
        },
        :blood_sibling => %StellaInvicta.Model.RelationshipType{
          id: :blood_sibling,
          name: "Blood Sibling",
          is_symmetric: true
        },
        :genetic_source => %StellaInvicta.Model.RelationshipType{
          id: :genetic_source,
          name: "Genetic Source",
          is_symmetric: false
        },
        :soulmate => %StellaInvicta.Model.RelationshipType{
          id: :soulmate,
          name: "Soulmate",
          is_symmetric: true
        }
      },
      polity_relations: %{},
      character_relations: %{
        1 => [
          %StellaInvicta.Model.CharacterRelation{target_id: 2, type_id: :rival},
          %StellaInvicta.Model.CharacterRelation{target_id: 2, type_id: :brother}
        ],
        2 => [
          %StellaInvicta.Model.CharacterRelation{target_id: 1, type_id: :rival},
          %StellaInvicta.Model.CharacterRelation{target_id: 1, type_id: :brother}
        ]
      },

      # --- RELATIONSHIP CHARACTER HAS RELIGION: CHARACTER RELIGIONS (Character -> Religion) ---
      character_religions: %{
        1 => :catholic,
        2 => :solarism
      },
      # --- RELATIONSHIP D: CHARACTER TRAITS (Character -> [Trait IDs]) ---
      # This supports multiple unique traits per character.
      character_traits: %{
        # Charlemagne is both Brave and a Scholar
        1 => [:brave, :scholar]
      },
      polities: %{}
    }
  end
end
