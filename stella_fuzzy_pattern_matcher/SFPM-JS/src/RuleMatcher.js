// src/RuleMatcher.js

/**
 * @param {import('./Rule').Rule[]} rules
 * @returns {import('./Rule').Rule[]}
 */
export function orderBySpecificity(rules) {
    return [...rules].sort((a, b) => b.criteriaCount - a.criteriaCount);
}

/**
 * @param {import('./Rule').Rule[]} rules
 * @returns {import('./Rule').Rule}
 */
export function mostSpecificRule(rules) {
    if (rules.length === 0) {
        throw new Error("Rules collection cannot be empty.");
    }
    return rules.reduce((prev, current) =>
        prev.criteriaCount > current.criteriaCount ? prev : current
    );
}

/**
 * @param {import('./Rule').Rule[]} rules
 * @returns {import('./Rule').Rule}
 */
export function leastSpecificRule(rules) {
    if (rules.length === 0) {
        throw new Error("Rules collection cannot be empty.");
    }
    return rules.reduce((prev, current) =>
        prev.criteriaCount < current.criteriaCount ? prev : current
    );
}

/**
 * @param {import('./Rule').Rule[]} rules
 * @param {import('./FactSource').FactSource} facts
 */
export function match(rules, facts, ...data) {
    const matchedRules = rules.filter((rule) => rule.evaluate(facts).isTrue);

    if (matchedRules.length === 0) {
        return;
    }

    if (matchedRules.length === 1) {
        matchedRules[0].executePayload(...data);
        return;
    }

    const highestScore = Math.max(...matchedRules.map((r) => r.criteriaCount));
    const bestRules = matchedRules.filter(
        (r) => r.criteriaCount === highestScore
    );

    if (bestRules.length === 1) {
        bestRules[0].executePayload(...data);
        return;
    }

    const highestPriority = Math.max(...bestRules.map((r) => r.priority));
    const priorityCandidates = bestRules.filter(
        (r) => r.priority === highestPriority
    );

    if (priorityCandidates.length === 1) {
        priorityCandidates[0].executePayload(...data);
        return;
    }

    const randomIndex = Math.floor(Math.random() * priorityCandidates.length);
    priorityCandidates[randomIndex].executePayload(...data);
}
