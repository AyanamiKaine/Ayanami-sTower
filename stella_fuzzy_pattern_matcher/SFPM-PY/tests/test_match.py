from SFPM import Rule, Criteria
import SFPM

def test_simple_match_payload():

    facts = {"player_level" : 10}


    payload_fired = False

    def payload():
        nonlocal payload_fired
        payload_fired = True

    rule = Rule(
        priority=0,
        criterias=[Criteria("player_level", lambda level: level >= 10)],
        payload=payload
    )

    SFPM.match(facts, [rule])

    assert payload_fired == True

def test_match_priority_sort():

    facts = {
                "player_level" : 10,
                "current_health": 20
            }


    payload_fired = False

    def payloadA(works = False):
        nonlocal payload_fired
        payload_fired = works

    def payloadB(works = False):
        nonlocal payload_fired
        payload_fired = works

    ruleA = Rule(
        priority=0,
        criterias=[Criteria("player_level", lambda level: level >= 10)],
        payload=payloadA
    )

    ruleb = Rule(
        priority=10,
        criterias=[Criteria("current_health", lambda health: health == 20)],
        payload=payloadB
    )

    SFPM.match(facts, [ruleA, ruleb], True)

    assert payload_fired == True


def test_only_use_rules_with_the_most_criteria():

    facts = {
                "player_level" : 10,
                "current_health": 20
            }


    payload_fired = False

    def payloadA():
        nonlocal payload_fired
        payload_fired = True

    def payloadB():
        nonlocal payload_fired
        payload_fired = False

    ruleA = Rule(
        priority=0,
        criterias=[Criteria("player_level", lambda level: level >= 10),
                   Criteria("current_health", lambda health: health == 20)],
        
        
        payload=payloadA
    )

    ruleb = Rule(
        priority=10,
        criterias=[Criteria("current_health", lambda health: health == 20)],
        payload=payloadB
    )

    SFPM.match(facts, [ruleA, ruleb])

    assert payload_fired == True