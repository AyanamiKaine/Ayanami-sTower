import { test, expect, describe } from "bun:test";
import { Rule } from "../src/Rule.js";
import { Criteria, Operator } from "../src/Criteria.js";
import { Query } from "../src/Query.js";

describe("Query", () => {
    test("Creation", () => {
        const query = new Query();
        expect(query).toBeInstanceOf(Query);
    });

    test("AddingKeyValue", () => {
        const query = new Query();

        // This demonstrates the fluent .add() method
        query
            .add("concept", "OnHit")
            .add("attacker", "Hunter")
            .add("damage", 12.4);

        // This test just confirms the methods can be called without error,
        // similar to the C# version which has no assertions.
    });

    test("QueryMatchingARule", () => {
        let ruleExecuted = false;

        const query = new Query();
        query
            .add("concept", "OnHit")
            .add("attacker", "Hunter")
            .add("damage", 12.4);

        const rules = [
            new Rule(
                [
                    // Does not match
                    new Criteria("who", "Nick", Operator.Equal),
                    new Criteria("concept", "onHit", Operator.Equal), // C# was case-insensitive here, JS is strict
                ],
                () => {
                    ruleExecuted = false;
                }
            ),

            new Rule(
                [
                    // Should be the most specific match
                    new Criteria("attacker", "Hunter", Operator.Equal),
                    new Criteria("concept", "OnHit", Operator.Equal),
                    new Criteria("damage", 12.4, Operator.Equal),
                ],
                () => {
                    ruleExecuted = true;
                }
            ),

            new Rule(
                [
                    // Less specific
                    new Criteria("concept", "OnHit", Operator.Equal),
                    new Criteria("damage", (d) => d > 10.0, Operator.Predicate),
                ],
                () => {
                    ruleExecuted = false;
                }
            ),

            new Rule(
                [
                    // Less specific
                    new Criteria(
                        "attacker",
                        (a) => a.startsWith("H"),
                        Operator.Predicate
                    ),
                    new Criteria("damage", (d) => d < 20.0, Operator.Predicate),
                ],
                () => {
                    ruleExecuted = false;
                }
            ),
        ];

        query.match(rules);

        expect(ruleExecuted).toBe(true);
    });

    test("AddingMemoryToQuery", () => {
        let finalRuleExecuted = false;
        const query = new Query();

        query
            .add("concept", "OnHit")
            .add("attacker", "Hunter")
            .add("damage", 12.4);

        const rules = [
            new Rule(
                [
                    new Criteria("attacker", "Hunter", Operator.Equal),
                    new Criteria("concept", "OnHit", Operator.Equal),
                    new Criteria("damage", 12.4, Operator.Equal),
                ],
                () => {
                    // This payload modifies the query's state
                    query.add("EventAHappened", true);
                },
                "Rule A"
            ),

            new Rule(
                [
                    new Criteria("attacker", "Hunter", Operator.Equal),
                    new Criteria("concept", "OnHit", Operator.Equal),
                    new Criteria("damage", 12.4, Operator.Equal),
                    new Criteria("EventAHappened", true, Operator.Equal), // This rule requires the new fact
                ],
                () => {
                    finalRuleExecuted = true;
                },
                "Rule B"
            ),
        ];

        // First match: Executes "Rule A", which adds "EventAHappened" to the query's facts.
        // "Rule B" does not match yet because the fact isn't present.
        query.match(rules);

        // At this point, "EventAHappened" is true, but finalRuleExecuted is still false.
        expect(finalRuleExecuted).toBe(false);

        // Second match: Now that "EventAHappened" is a fact, "Rule B" is the most specific match.
        query.match(rules);

        // Assert that the final, most specific rule was executed on the second run.
        expect(finalRuleExecuted).toBe(true);
    });
});
