import { test, expect, describe } from "bun:test";
import { Rule } from "../src/Rule.js";
import { Criteria, Operator } from "../src/Criteria.js";
import { DictionaryFactSource } from "../src/FactSource.js";
import { Query } from "../src/Query.js";
import { match } from "../src/RuleMatcher.js";

describe("Rule Evaluation", () => {
    test("SimpleOneRuleTwoCriteriaStrictMatch", () => {
        const rule1 = new Rule(
            [
                new Criteria("who", "Nick", Operator.Equal),
                new Criteria("concept", "onHit", Operator.Equal),
            ],
            () => {}
        );

        const facts = new DictionaryFactSource(
            new Map([
                ["concept", "onHit"],
                ["who", "Nick"],
            ])
        );

        const { isTrue, matchedCriteriaCount } = rule1.evaluate(facts);

        expect(isTrue).toBe(true);
        expect(matchedCriteriaCount).toBe(2);
    });

    test("SimpleOneRuleTwoCriteriaPredicateBasedStrictMatch", () => {
        const rule1 = new Rule(
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
        );

        const facts = new DictionaryFactSource(
            new Map([
                ["concept", "onHit"],
                ["who", "Nick"],
            ])
        );

        const { isTrue, matchedCriteriaCount } = rule1.evaluate(facts);

        expect(isTrue).toBe(true);
        expect(matchedCriteriaCount).toBe(2);
    });

    test("SimpleOneRuleOneCriteriaStrictMatch", () => {
        const rule1 = new Rule(
            [
                new Criteria("who", "Nick", Operator.Equal),
                new Criteria("concept", "onHit", Operator.Equal),
            ],
            () => {}
        );

        const facts = new DictionaryFactSource(new Map([["concept", "onHit"]]));

        const { isTrue, matchedCriteriaCount } = rule1.evaluate(facts);

        expect(isTrue).toBe(false);
        expect(matchedCriteriaCount).toBe(0);
    });
});

describe("Rule Matching Logic", () => {
    test("RandomRuleSelectionIfMultipleRulesMatchWithSamePriority", () => {
        let rule1Executed = false;
        let rule2Executed = false;
        let rule3Executed = false;
        let rule4Executed = false;

        const rules = [
            // Rule 1: Less specific, should not be chosen
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
                () => {
                    rule1Executed = true;
                }
            ),

            // Rules 2, 3, 4: Same highest specificity and same priority, one should be chosen randomly
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
                        "nearAllies",
                        (nearAllies) => nearAllies > 1,
                        Operator.Predicate
                    ),
                ],
                () => {
                    rule2Executed = true;
                }
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
                    rule3Executed = true;
                }
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
                        "hitBy",
                        (hitBy) => hitBy === "zombieClown",
                        Operator.Predicate
                    ),
                ],
                () => {
                    rule4Executed = true;
                }
            ),
        ];

        const facts = new DictionaryFactSource(
            new Map([
                ["who", "Nick"],
                ["concept", "onHit"],
                ["curMap", "circus"],
                ["health", 0.66],
                ["nearAllies", 2],
                ["hitBy", "zombieClown"],
            ])
        );

        // Run match enough times to have a high probability of selecting each random candidate
        for (let i = 0; i < 1000; i++) {
            match(rules, facts);
        }

        expect(rule1Executed).toBe(false); // The less specific rule should never be executed
        // All rules with the highest specificity and priority should have been executed at least once
        expect(rule2Executed).toBe(true);
        expect(rule3Executed).toBe(true);
        expect(rule4Executed).toBe(true);
    });

    test("LeftForDeadExample - Most Specific Rule Wins", () => {
        let rule1Executed = false; // 2 criteria
        let rule2Executed = false; // 3 criteria
        let rule3Executed = false; // 3 criteria
        let rule4Executed = false; // 3 criteria
        let rule5Executed = false; // 4 criteria - SHOULD WIN

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
                () => {
                    rule1Executed = true;
                }
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
                        "nearAllies",
                        (nearAllies) => nearAllies > 1,
                        Operator.Predicate
                    ),
                ],
                () => {
                    rule2Executed = true;
                }
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
                    rule3Executed = true;
                }
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
                        "hitBy",
                        (hitBy) => hitBy === "zombieClown",
                        Operator.Predicate
                    ),
                ],
                () => {
                    rule4Executed = true;
                }
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
                        "hitBy",
                        (hitBy) => hitBy === "zombieClown",
                        Operator.Predicate
                    ),
                    new Criteria(
                        "curMap",
                        (curMap) => curMap === "circus",
                        Operator.Predicate
                    ),
                ],
                () => {
                    rule5Executed = true;
                }
            ),
        ];

        const facts = new DictionaryFactSource(
            new Map([
                ["who", "Nick"],
                ["concept", "onHit"],
                ["curMap", "circus"],
                ["health", 0.66],
                ["nearAllies", 2],
                ["hitBy", "zombieClown"],
            ])
        );

        match(rules, facts);

        expect(rule1Executed).toBe(false);
        expect(rule2Executed).toBe(false);
        expect(rule3Executed).toBe(false);
        expect(rule4Executed).toBe(false);
        expect(rule5Executed).toBe(true); // Only the most specific rule should execute
    });
});
