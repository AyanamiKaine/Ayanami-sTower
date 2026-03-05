from SFPM import Rule, Criteria
import pytest
import icontract

def test_rule_creation():

    rule = Rule()

    assert rule is not None


def test_rule_payload():
    payload_fired = False

    def payload():
        nonlocal payload_fired
        payload_fired = True

    rule = Rule(
        priority=0,
        criterias=[Criteria("player_level", lambda level: level >= 10)],
        payload=payload
    )

    rule.payload()

    assert payload_fired == True

def test_rule_evaluated_true():
    facts = {"player_level" : 10}

    rule = Rule(
        priority=0,
        criterias=[Criteria("player_level", lambda player_level: player_level >= 10)],
        payload= lambda: False
    )

    result, _ = rule.evaluate(facts)

    assert result == True

def test_rule_criteria_number_matched():
    facts = {"player_level" : 10}

    rule = Rule(
        priority=0,
        criterias=[Criteria("player_level", lambda player_level: player_level >= 10),
                   Criteria("player_level", lambda player_level: player_level >= 10),
                   Criteria("player_level", lambda player_level: player_level >= 10),
                   Criteria("player_level", lambda player_level: player_level >= 10),
                   Criteria("player_level", lambda player_level: player_level >= 10),
                   Criteria("player_level", lambda player_level: player_level >= 10)],
        payload= lambda: False
    )

    _, criteria_number_matched = rule.evaluate(facts)

    assert criteria_number_matched == 6

def test_rule_criteria_number_matched_0():
    facts = {"player_level" : 1}

    rule = Rule(
        priority=0,
        criterias=[Criteria("player_level", lambda player_level: player_level >= 10),
                   Criteria("player_level", lambda player_level: player_level >= 10),
                   Criteria("player_level", lambda player_level: player_level >= 10),
                   Criteria("player_level", lambda player_level: player_level >= 10),
                   Criteria("player_level", lambda player_level: player_level >= 10),
                   Criteria("player_level", lambda player_level: player_level >= 10)],
        payload= lambda: False
    )

    _, criteria_number_matched = rule.evaluate(facts)

    assert criteria_number_matched == 0

def test_rule_evaluated_false():
    facts = {"player_level" : 5}

    rule = Rule(
        priority=0,
        criterias=[Criteria("player_level", lambda player_level: player_level >= 10)],
        payload= lambda: False
    )

    result, _ = rule.evaluate(facts)

    assert result == False

def test_rule_no_criterias_precondition_fail():
    facts = {"player_level" : 5}

    rule = Rule(
        priority=0,
        criterias=[],
        payload= lambda: False
    )
    
    with pytest.raises(icontract.ViolationError):
            rule.evaluate(facts)