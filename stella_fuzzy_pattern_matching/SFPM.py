import random


class Rule:
    """
    A tuple of criteria to match against a query.
    - If all criterion are true, the rule "matches"
    - If any criterion has no matching fact in the query, it rejects

    Many rules may match a query, so:
    - A scoring function (to pick a specific rules over general ones)
    - We use a simple one: number of criteria matched.

    Represents a rule to be matched against an dictionary consisting of facts

    If a rule is matched we execute its functionality
    """
    def __init__(self, criteria, functionality, priority=0):
        self.criteria = criteria
        self.functionality = functionality
        self.priority = priority
    
    def match(self, facts):
        for criterion in self.criteria:
            if not criterion.match(facts):
                return False  # Early termination if a criterion fails
        return True  # All criteria matched

    """
    def AND(criteria):
        pass
    This is already implicity implemented as we always match the rest of the criterions
    """

    def OR(criteria):
        """
        We wish to be able to say something like
        "hitpoints": >30 OR "hitpoints" : <60

        The problem lies in the question how we can implement such a construct. 

        The api must be nice using this otherwise it would be pointless implementing this in such a way.
        As it should reduce cognitive load not increase it.
        """
        pass

    def score(self, facts):
        return sum(criterion.match(facts) for criterion in self.criteria)


class Event(Rule):
    """
    A special type of Rule that represents an event trigger. 
    When its criteria are met, it executes an associated action without returning any value.
    """

    def __init__(self, criteria, action, priority=0):
        super().__init__(criteria, functionality=action, priority=priority)  # Store the action in functionality

class Criteria:
    """
    A function that tests a fact

    - eg a key->func() pair that "matches" or "fails" a fact.
    - The predicate may be an equality, a range, or a function

    - {Query ["who"]=="bill" }
    - {Query ["hitpoints"] >30 && Query["hitpoints"] <60 }
    - {IsPrimeNumber( Query["nearbyEnemies"]) == true }
    """
    def __init__(self, key, operator, value):
        self.key = key
        self.operator = operator
        self.value = value

    def match(self, facts):
        fact_value = facts.get(self.key)
        if fact_value is None:
            return False  # Fact not present in the query

        if self.operator == '==':
            return fact_value == self.value
        elif self.operator == '>':
            return fact_value > self.value
        elif self.operator == '<':
            return fact_value < self.value
        elif self.operator == '>=':
            return fact_value >= self.value
        elif self.operator == '<=':
            return fact_value <= self.value
        elif self.operator == '!=':
            return fact_value != self.value

        raise ValueError(f"Unsupported operator: {self.operator}")


class Query:
    """
    A query is a list of rules that are matched against a 
    dictionary picking the rule with the most matched values 
    and if two or more rules matched the same number of values
    we pick the rule with the highest priority or if the have
    the same we pick at random.

    Query Example:
    { who: nick, concept: onHit, curMap:circus, health: 0.66, nearAllies: 2, hitby: zombieclown }
    
    Rules :
    1. { who = nick, concept = onHit }      -> "Ouch"                               PASS Matching 2 Values
    2. { who = nick, concept = onReload }   -> "Changing Clips!"                    FAIL Matching 1 Value and Rejecting 1 Value
    3. { who = nick, concept = onHit, nearAllies > 1 }  -> "aaarhg Im in Danger!"   PASS Matching 3 Values
    """
    def __init__(self, rules):
        self.rules = rules
        
    def execute(self, facts):
        matching_rules = [rule for rule in self.rules if rule.match(facts)]
        if not matching_rules:
            return None  # No rule matched

        max_score = max(rule.score(facts) for rule in matching_rules)  # Find the maximum score
        best_rules = [rule for rule in matching_rules if rule.score(facts) == max_score]


        # Always ensure best_rules is a list
        best_rules = [best_rules] if not isinstance(best_rules, list) else best_rules 

        # Filter by score first
        best_rules = [rule for rule in best_rules if rule.score(facts) == best_rules[0].score(facts)]

        if len(best_rules) > 1:
            # Then filter by priority
            highest_priority = max(rule.priority for rule in best_rules)
            best_rules = [rule for rule in best_rules if rule.priority == highest_priority]

        selected_rule = random.choice(best_rules)
        return selected_rule.functionality