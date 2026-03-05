from icontract import require, ensure


class Criteria():
    def __init__(self, fact_name, predicate):
        self.fact_name = fact_name
        self.predicate = predicate
    
    @require(lambda self, facts: facts[self.fact_name] is not None, "fact value cannot be None")
    @require(lambda self, facts: self.fact_name in facts, "fact_name must exist in facts")
    @require(lambda self, facts: self.predicate is not None, "There must exist a predicate")
    @ensure(lambda result: isinstance(result, bool))
    def evaluate(self, facts):
        result = self.predicate(facts[self.fact_name])
        return result