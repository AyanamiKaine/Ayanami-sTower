import { run, bench, summary } from "mitata";
import { Rule } from "../src/Rule.js";
import { Criteria, Operator } from "../src/Criteria.js";
import { Query } from "../src/Query.js";
import { DictionaryFactSource } from "../src/FactSource.js";

// --- Global Setup ---

const facts = new Map([
    ["concept", "onHit"],
    ["who", "Nick"],
    ["health", 75.5],
    ["maxHealth", 100.0],
    ["mana", 50],
    ["stamina", 80],
    ["isAlive", true],
    ["isStunned", false],
    ["isBurning", true],
    ["poisonLevel", 3],
    ["strength", 15],
    ["intelligence", 20],
    ["level", 8],
    ["experience", 1250],
    ["timeOfDay", "Night"],
    ["weather", "Rainy"],
    ["locationType", "Forest"],
    ["isCombatActive", false],
    ["numberOfEnemiesNearby", 3],
    ["isPlayerInTown", true],
    ["lastAction", "Attack"],
    ["isSprinting", false],
    ["isJumping", false],
    ["inventory_hasPotion_healing", true],
    ["equippedWeapon_type", "Sword"],
    ["hasKey_dungeonLevel2", true],
]);

const customFacts = new Map(facts);
customFacts.set("numberOfEnemiesNearby", { Count: 3 });
customFacts.set("stamina", { Value: 80 });

const query = new Query(facts);
const factSource = new DictionaryFactSource(facts);

const largeFacts = new Map(facts);
for (let i = 0; i < 1000; i++) {
    largeFacts.set(`fact_${i}`, i);
}
const largeFactSource = new DictionaryFactSource(largeFacts);
const largeFactQuery = new Query(largeFacts);

// --- Individual Rules for granular benchmarks ---

const operatorBasedRule1Criteria = new Rule(
    [new Criteria("who", "Nick", Operator.Equal)],
    () => {}
);

const predicateBasedRule1Criteria = new Rule(
    [new Criteria("who", (who) => who === "Nick", Operator.Predicate)],
    () => {}
);

const bigOperatorRule10Criteria = new Rule(
    [
        new Criteria("who", "Nick", Operator.Equal),
        new Criteria("concept", "onHit", Operator.Equal),
        new Criteria("timeOfDay", "Night", Operator.Equal),
        new Criteria("weather", "Rainy", Operator.Equal),
        new Criteria("numberOfEnemiesNearby", 1, Operator.GreaterThanOrEqual),
        new Criteria("equippedWeapon_type", "Sword", Operator.Equal),
        new Criteria("isSprinting", true, Operator.NotEqual),
        new Criteria("stamina", 5, Operator.GreaterThan),
        new Criteria("isAlive", true, Operator.Equal),
        new Criteria("poisonLevel", 5, Operator.LessThan),
    ],
    () => {}
);

const bigPredicateRule10Criteria = new Rule(
    [
        new Criteria("who", (who) => who === "Nick", Operator.Predicate),
        new Criteria(
            "concept",
            (concept) => concept === "onHit",
            Operator.Predicate
        ),
        new Criteria(
            "timeOfDay",
            (timeOfDay) => timeOfDay === "Night",
            Operator.Predicate
        ),
        new Criteria(
            "weather",
            (weather) => weather === "Rainy",
            Operator.Predicate
        ),
        new Criteria(
            "numberOfEnemiesNearby",
            (enemies) => enemies >= 1,
            Operator.Predicate
        ),
        new Criteria(
            "equippedWeapon_type",
            (weapon) => weapon === "Sword",
            Operator.Predicate
        ),
        new Criteria(
            "isSprinting",
            (sprinting) => sprinting === false,
            Operator.Predicate
        ),
        new Criteria("stamina", (stamina) => stamina > 5, Operator.Predicate),
        new Criteria(
            "isAlive",
            (isAlive) => isAlive === true,
            Operator.Predicate
        ),
        new Criteria(
            "poisonLevel",
            (poisonLevel) => poisonLevel < 5,
            Operator.Predicate
        ),
    ],
    () => {}
);

const predicateCriteriaCustomType = new Rule(
    [
        new Criteria(
            "numberOfEnemiesNearby",
            (enemies) => enemies.Count >= 1,
            Operator.Predicate
        ),
        new Criteria(
            "stamina",
            (stamina) => stamina.Value > 5,
            Operator.Predicate
        ),
    ],
    () => {}
);

// --- Rule sets for query matching benchmarks ---

const smallRuleSet = [bigOperatorRule10Criteria, bigPredicateRule10Criteria];

const tenThousandRules = [];
for (let i = 0; i < 5000; i++) {
    tenThousandRules.push(
        new Rule(
            [
                new Criteria("who", "Nick", Operator.Equal),
                new Criteria("weather", "Rainy", Operator.Equal),
            ],
            () => {}
        ),
        bigPredicateRule10Criteria
    );
}

// --- Benchmarks ---

console.log("--- Single Rule Evaluation ---");
await summary(() => {
    bench("1-Criteria Operator-Based", () => {
        operatorBasedRule1Criteria.evaluate(factSource);
    });
    bench("1-Criteria Predicate-Based", () => {
        predicateBasedRule1Criteria.evaluate(factSource);
    });
    bench("10-Criteria Operator-Based", () => {
        bigOperatorRule10Criteria.evaluate(factSource);
    });
    bench("10-Criteria Predicate-Based", () => {
        bigPredicateRule10Criteria.evaluate(factSource);
    }).gc("inner"); // Correct: Chained .gc() call
    bench("Predicate-Based with Custom Types", () => {
        predicateCriteriaCustomType.evaluate(
            new DictionaryFactSource(customFacts)
        );
    });
});

console.log("\n--- Full Query Matching ---");
await summary(() => {
    bench("Match against a small rule set", () => {
        query.match(smallRuleSet);
    });

    bench("Match against 10,000 rules", () => {
        query.match(tenThousandRules);
    });
});

console.log("\n--- Fact Scale Impact ---");
await summary(() => {
    bench("Evaluate rule with 1000+ facts", () => {
        bigPredicateRule10Criteria.evaluate(largeFactSource);
    });

    bench("Match query with 1000+ facts", () => {
        largeFactQuery.match(smallRuleSet);
    });
});

console.log("\n--- Predicate Complexity ---");
await summary(() => {
    bench("Simple predicate (equality check)", () => {
        const rule = new Rule(
            [new Criteria("level", (level) => level === 8, Operator.Predicate)],
            () => {}
        );
        rule.evaluate(factSource);
    });

    bench("Moderate predicate (string startsWith)", () => {
        const rule = new Rule(
            [
                new Criteria(
                    "locationType",
                    (loc) => loc.startsWith("For"),
                    Operator.Predicate
                ),
            ],
            () => {}
        );
        rule.evaluate(factSource);
    });

    bench("Complex predicate (regex test)", () => {
        const complexRegex = /^(rain|snow|sun).*y$/i;
        const rule = new Rule(
            [
                new Criteria(
                    "weather",
                    (weather) => complexRegex.test(weather),
                    Operator.Predicate
                ),
            ],
            () => {}
        );
        rule.evaluate(factSource);
    });
});

console.log("\n--- Initialization Cost ---");
await summary(() => {
    bench("Create 10,000 rules", () => {
        const rules = [];
        for (let i = 0; i < 5000; i++) {
            rules.push(
                new Rule(
                    [
                        new Criteria("who", "Nick", Operator.Equal),
                        new Criteria("weather", "Rainy", Operator.Equal),
                    ],
                    () => {}
                ),
                new Rule(
                    [
                        new Criteria(
                            "OnHit",
                            (concept) => concept === "OnHit",
                            Operator.Predicate
                        ),
                    ],
                    () => {}
                )
            );
        }
    }).gc("inner"); // Good to measure GC pressure during heavy allocation
});

// To run all defined benchmarks
await run();
