from icontract import require, ensure

class Rule():
    def __init__(self, priority = 0, criterias = None, payload = None):
        self.priority = priority
        self.criterias = criterias
        self.payload = payload

    def execute(self, *args):
        self.payload(*args);
    
    def count(self):
        """Returns the number of criterias the rule has"""
        return len(self.criterias)

    @require(lambda self, facts: self.criterias, "Criterias cannot be an empty list")
    @ensure(lambda result: isinstance(result[0], bool))
    def evaluate(self, facts):

        matched_criteria_number = 0
        matched_all_criteria = False
        for criteria in self.criterias:
            result = criteria.evaluate(facts)
            if result == True:
                matched_criteria_number += 1

        if matched_criteria_number == len(self.criterias):
            matched_all_criteria = True

        return matched_all_criteria, matched_criteria_number
