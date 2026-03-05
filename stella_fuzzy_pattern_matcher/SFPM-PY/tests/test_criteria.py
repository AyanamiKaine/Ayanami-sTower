import pytest
import icontract

from SFPM import Criteria

def test_criteria_evaluation_true():
    facts = {"player_level" : 10}
    criteria = Criteria("player_level", lambda player_level : player_level >= 10)
    
    result = criteria.evaluate(facts)
    
    assert result is True

def test_criteria_evaluation_false():
    facts = {"player_level" : 5}
    criteria = Criteria("player_level", lambda player_level : player_level >= 10)
    
    result = criteria.evaluate(facts)
    
    assert result is False

def test_criteria_evaluation_precondition_false_missing_key_fail():
    facts = {}
    criteria = Criteria("player_level", lambda player_level : player_level >= 10)
    
    with pytest.raises(icontract.ViolationError):
            criteria.evaluate(facts)

def test_criteria_evaluation_precondition_false_missing_predicate_fail():
    facts = {"player_level" : 10}
    criteria = Criteria("player_level", None)
    
    with pytest.raises(icontract.ViolationError):
            criteria.evaluate(facts)


def test_criteria_evaluation_postcondition_predicate_must_return_boolean_fail():
    facts = {"player_level" : 10}
    criteria = Criteria("player_level", lambda level: 5)
    
    with pytest.raises(icontract.ViolationError):
            criteria.evaluate(facts)

def test_criteria_evaluation_postcondition_predicate_must_return_boolean_success():
    facts = {"player_level": 10}
    criteria = Criteria("player_level", lambda level: False)
    
    result = criteria.evaluate(facts)
    
    assert result is False

def test_criteria_evaluation_postcondition_predicate_must_return_boolean_success():
    facts = {"player_level": 10}
    criteria = Criteria("player_level", lambda level: True)
    
    result = criteria.evaluate(facts)
    
    assert result is True