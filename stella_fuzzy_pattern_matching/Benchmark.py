import random
import time

from SFPM import Event, Rule, Query, Criteria


def test_benchmark():
    num_facts = 10000000
    num_criteria_per_rule = 10
    num_rules = 100

    # Generate random facts
    facts = {f"fact_{i}": random.randint(0, 100) for i in range(num_facts)}

    # Generate random criteria and rules
    rules = []
    for _ in range(num_rules):
        criteria = [
            Criteria(f"fact_{random.randint(0, num_facts - 1)}", random.choice(["==", ">", "<"]), random.randint(0, 100))
            for _ in range(num_criteria_per_rule)
        ]
        rules.append(Rule(criteria, f"Rule {_}"))

    query = Query(rules)

    start_time = time.time()
    for _ in range(1000):  # Run the query multiple times
        query.execute(facts)
    end_time = time.time()

    elapsed_time = end_time - start_time
    print(f"Benchmark completed in {elapsed_time:.2f} seconds")

test_benchmark()