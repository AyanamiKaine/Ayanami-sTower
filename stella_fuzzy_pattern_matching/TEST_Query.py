import random
import time
import pytest
from SFPM import Event, Rule, Query, Criteria


def test_match_rule():
    """
    Tests if we can match a rule in a query against a list of facts
    """
    criteria = Criteria("who", "==", "nick")
    rule = Rule([criteria], "Hello Nick!")
    query = Query([rule])

    facts = {"who": "nick"}
    result = query.execute(facts)
    assert (result, "Hello Nick!")

def test_no_match():
    criteria = Criteria("who", "==", "alice")
    rule = Rule([criteria], "Hello Alice!")
    query = Query([rule])

    facts = {"who": "nick"}
    result = query.execute(facts)
    assert result == None

def test_multiple_criteria():
    criteria1 = Criteria("who", "==", "nick")
    criteria2 = Criteria("concept", "==", "onHit")
    rule = Rule([criteria1, criteria2], "Ouch!")
    query = Query([rule])

    facts = {"who": "nick", "concept": "onHit"}
    result = query.execute(facts)
    assert result == "Ouch!"

def test_priority():
    criteria1 = Criteria("who", "==", "nick")
    criteria2 = Criteria("concept", "==", "onHit")

    rule1 = Rule([criteria1], "Hello Nick!", priority=0)
    rule2 = Rule([criteria1, criteria2], "Ouch!", priority=5)
    query = Query([rule1, rule2])

    facts = {"who": "nick", "concept": "onHit"}
    result = query.execute(facts)
    assert result == "Ouch!"  # Higher score wins

def test_random_tiebreaker():
    criteria1 = Criteria("who", "==", "nick")
    criteria2 = Criteria("Health", "==", 20)
    
    
    rule1 = Rule([criteria1], "Option 1")
    rule2 = Rule([criteria1], "Option 2")
    rule3 = Rule([criteria1, criteria2], "Option 3") # Should be rejected

    query = Query([rule1, rule2, rule3])

    facts = {"who": "nick"}
    results = set()
    for _ in range(100):  # Run many times to check for randomness
        results.add(query.execute(facts))

    assert len(results) == 2  # Both options should appear


def test_complex_facts():
    criteria1 = Criteria("who", "==", "nick")
    criteria2 = Criteria("Health", ">", 20)
    # Criterion to check if a value is within a range
    criteria_range = Criteria("Level", ">", 5)  
    criteria_range2 = Criteria("Level", "<=", 15)   
    
    rule1 = Rule([criteria1], "Option 1")
    rule2 = Rule([criteria1], "Option 2")
    rule3 = Rule([criteria1, criteria2, criteria_range, criteria_range2], "Option 3") # Should be chosen

    query = Query([rule1, rule2, rule3])

    facts = {"who": "nick", "Health": 50, "Weapon": "Sword", "Level": 10} 
    
    result = query.execute(facts)
    assert result == "Option 3"

def test_crusader_kings_scenario():
    # Criteria based on character traits and situations
    is_powerful_vassal = Criteria("vassal_power", ">", 0.7)
    is_disloyal = Criteria("loyalty", "<", 0.3)
    has_casus_belli = Criteria("casus_belli", "==", True)
    is_at_war = Criteria("at_war", "==", False)

    # Rules representing different actions and their conditions
    revoke_title = Rule([is_powerful_vassal, is_disloyal, has_casus_belli], "Revoke Title", priority=5)
    imprison = Rule([is_powerful_vassal, is_disloyal], "Imprison")
    declare_war = Rule([is_powerful_vassal, is_disloyal, is_at_war], "Declare War")
    appease = Rule([is_powerful_vassal], "Appease")

    query = Query([revoke_title, imprison, declare_war, appease])

    # Scenario where a powerful vassal is disloyal and you have a reason to act
    facts = {
        "vassal_power": 0.8,
        "loyalty": 0.2,
        "casus_belli": True,
        "at_war": False
    }

    result = query.execute(facts)
    
    # In this scenario, we expect the 'revoke_title' rule to trigger as it has the most matching criteria
    assert result == "Revoke Title" 

def test_victoria_2_event_trigger():
    # Criteria based on game conditions
    high_literacy = Criteria("literacy_rate", ">", 0.7)
    low_militancy = Criteria("militancy", "<", 0.3)
    is_great_power = Criteria("great_power_status", "==", True)
    is_at_peace = Criteria("at_war", "==", False)
    is_at_war = Criteria("at_war", "==", True)

    # Rules representing different events and their conditions
    trigger_social_reform_event = Rule([high_literacy, low_militancy], "Social Reform Movement")
    trigger_great_power_intervention_event = Rule([is_great_power, is_at_war], "Great Power Intervention")
    trigger_economic_boom_event = Rule([high_literacy, is_at_peace], "Economic Boom")

    query = Query([trigger_social_reform_event, 
                   trigger_great_power_intervention_event, 
                   trigger_economic_boom_event])

    # Scenario where a literate, peaceful great power exists
    facts = {
        "literacy_rate": 0.8,
        "militancy": 0.2,
        "great_power_status": True,
        "at_war": False
    }

    result = query.execute(facts)

    # In this scenario, we expect the 'trigger_social_reform_event' or 'trigger_economic_boom_event' to trigger 
    # as they both have the maximum number of matching criteria. 
    assert result in ["Social Reform Movement", "Economic Boom"]

