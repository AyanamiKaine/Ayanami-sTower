import pytest
from SFPM import Event, Rule, Query, Criteria
from functools import partial

def test_victoria_2_event_trigger():
    
    event_happened = [False]    # Here we are using a List so 
                                # your event_function can mutate it
                                # Its just done for convenience, not that you wonder

    def social_reform_movement_event():
        event_happened[0] = True
        print("A SOCIAL MOVEMENT IS HAPPENING!")
    
    # Criteria based on game conditions
    high_literacy = Criteria("literacy_rate", ">", 0.7)
    low_militancy = Criteria("militancy", "<", 0.3)

    # Rules representing different events and their conditions
    trigger_social_reform_event = Event([high_literacy, low_militancy], social_reform_movement_event)

    query = Query([trigger_social_reform_event])

    # Scenario where a literate, peaceful great power exists
    facts = {
        "literacy_rate": 0.8,
        "militancy": 0.2,
        "great_power_status": True,
        "at_war": False
    }

    # I really dislike the fact that we have to either take the result if its just an variable
    # or execute it if its an event. (Value vs Function Execution)
    # We should probably move the execute function into the Rule and Event object.
    # Query could simply return those rules which we should run execute on.
    result = query.execute(facts)
    result()
    assert event_happened[0] == True