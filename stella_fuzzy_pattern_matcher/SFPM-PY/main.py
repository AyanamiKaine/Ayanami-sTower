from SFPM import Rule, Criteria

def spawn_big_ancient_dragon():
    print("Spawing ancient dragon")

def Match(dictionary, list_of_rules):
    pass

data = {
    "PlayerLevel" : 15
}


bigDragonEncounterRule = Rule(
    priority= 10,
    criterias=
    [
        Criteria(fact_name = "PlayerLevel", predicate = lambda player_level : player_level  >= 10)
    ],
    payload= spawn_big_ancient_dragon)


Match(data, [bigDragonEncounterRule])


