import { test, expect, describe } from "bun:test";
import { Rule } from "../src/Rule.js";
import { Criteria, Operator } from "../src/Criteria.js";
import { DictionaryFactSource } from "../src/FactSource.js";
import {
    mostSpecificRule,
    leastSpecificRule,
    match,
} from "../src/RuleMatcher.js";

describe("RuleMatcher Utilities", () => {
    // Helper function to create a standard set of rules for tests
    const createTestRules = (payload = () => {}) => [
        new Rule(
            [
                // 2 criteria
                new Criteria(
                    "who",
                    (who) => who === "Nick",
                    Operator.Predicate
                ),
                new Criteria(
                    "concept",
                    (concept) => concept === "onHit",
                    Operator.Predicate
                ),
            ],
            payload
        ),
        new Rule(
            [
                // 2 criteria
                new Criteria(
                    "who",
                    (who) => who === "Nick",
                    Operator.Predicate
                ),
                new Criteria(
                    "nearAllies",
                    (nearAllies) => nearAllies > 1,
                    Operator.Predicate
                ),
            ],
            payload
        ),
        new Rule(
            [
                // 3 criteria
                new Criteria(
                    "who",
                    (who) => who === "Nick",
                    Operator.Predicate
                ),
                new Criteria(
                    "concept",
                    (concept) => concept === "onHit",
                    Operator.Predicate
                ),
                new Criteria(
                    "curMap",
                    (curMap) => curMap === "circus",
                    Operator.Predicate
                ),
            ],
            payload
        ),
        new Rule(
            [
                // 1 criterion
                new Criteria(
                    "who",
                    (who) => who === "Nick",
                    Operator.Predicate
                ),
            ],
            payload
        ),
    ];

    test("MostSpecificRuleTest", () => {
        const rules = createTestRules();
        const mostSpecific = mostSpecificRule(rules);
        expect(mostSpecific.criteriaCount).toBe(3);
    });

    test("LeastSpecificRuleTest", () => {
        const rules = createTestRules();
        const leastSpecific = leastSpecificRule(rules);
        expect(leastSpecific.criteriaCount).toBe(1);
    });

    test("MatchOnRulesList", () => {
        let ruleExecuted = false;

        const rules = [
            new Rule(
                [
                    new Criteria(
                        "who",
                        (who) => who === "Nick",
                        Operator.Predicate
                    ),
                    new Criteria(
                        "concept",
                        (concept) => concept === "onHit",
                        Operator.Predicate
                    ),
                ],
                () => {}
            ),
            new Rule(
                [
                    new Criteria(
                        "who",
                        (who) => who === "Nick",
                        Operator.Predicate
                    ),
                    new Criteria(
                        "nearAllies",
                        (nearAllies) => nearAllies > 1,
                        Operator.Predicate
                    ),
                ],
                () => {}
            ),
            new Rule(
                [
                    new Criteria(
                        "who",
                        (who) => who === "Nick",
                        Operator.Predicate
                    ),
                    new Criteria(
                        "concept",
                        (concept) => concept === "onHit",
                        Operator.Predicate
                    ),
                    new Criteria(
                        "curMap",
                        (curMap) => curMap === "circus",
                        Operator.Predicate
                    ),
                ],
                () => {
                    ruleExecuted = true; // This is the payload for the most specific rule
                }
            ),
            new Rule(
                [
                    new Criteria(
                        "who",
                        (who) => who === "Nick",
                        Operator.Predicate
                    ),
                ],
                () => {}
            ),
        ];

        const facts = new DictionaryFactSource(
            new Map([
                ["concept", "onHit"],
                ["who", "Nick"],
                ["curMap", "circus"],
            ])
        );

        match(rules, facts);

        expect(ruleExecuted).toBe(true);
    });
});
