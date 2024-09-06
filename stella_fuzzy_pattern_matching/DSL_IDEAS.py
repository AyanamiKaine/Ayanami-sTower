

"""
Hypothetical syntax (not valid Python)

Having a domain specific language where we could write rules like this:
{
    "literacy_rate": literacy if literacy > 0.7, 
    "militancy": militancy if militancy < 0.3
}

The current implementation works like this:
    high_literacy = Criteria("literacy_rate", ">", 0.7)
    low_militancy = Criteria("militancy", "<", 0.3)

    trigger_social_reform_event = Rule([high_literacy, low_militancy], "Social Reform Movement")

    query = Query([trigger_social_reform_event)

    facts = {
        "literacy_rate": 0.8,
        "militancy": 0.2,
        "great_power_status": True,
        "at_war": False
    }

    result = query.execute(facts) -> Returns "Social Reform Movement"

But maybe this is actually better its much more explicit.

def match(facts):
    match facts:
        case {"literacy_rate": l if l > 0.7, "militancy": m if m < 0.3}:
            return "Social Reform Movement"
        case {"great_power_status": True, "at_war": True}:
            return "Great Power Intervention"
        case {"literacy_rate": l if l > 0.7, "at_war": False}:
            return "Economic Boom"
        case _:  # Default case
            return None
"""