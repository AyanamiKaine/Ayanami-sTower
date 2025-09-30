#pragma once

#include "Rule.hpp"

#include <optional>
#include <random>
#include <vector>

namespace sfpm {

// Matches rules against a fact source and executes the payload of the best match.
inline void match(std::vector<Rule> rules, const IFactSource &facts, bool assumeOptimized = false) {
    if (!assumeOptimized) optimizeRules(rules);

    std::vector<const Rule *> accepted;
    int bestScore = 0;

    for (const auto &rule : rules) {
        if (assumeOptimized && rule.criteriaCount() < bestScore) {
            break; // remaining rules can't beat current best
        }
        auto [matched, score] = rule.evaluate(facts);
        if (!matched) continue;
        if (score > bestScore) {
            bestScore = score;
            accepted.clear();
            accepted.push_back(&rule);
        } else if (score == bestScore && score > 0) {
            accepted.push_back(&rule);
        }
    }

    if (accepted.empty()) return; // no match
    const Rule *selected = nullptr;
    if (accepted.size() == 1) {
        selected = accepted.front();
    } else {
        // Select by highest priority then random tie-breaker
        int highestPriority = (*std::max_element(accepted.begin(), accepted.end(), [](const Rule *a, const Rule *b) {
                                    return a->priority() < b->priority();
                                }))
                                   ->priority();
        std::vector<const Rule *> priorityCandidates;
        for (auto *r : accepted) if (r->priority() == highestPriority) priorityCandidates.push_back(r);
        if (priorityCandidates.size() == 1) {
            selected = priorityCandidates.front();
        } else {
            std::mt19937 rng(std::random_device{}());
            std::uniform_int_distribution<std::size_t> dist(0, priorityCandidates.size() - 1);
            selected = priorityCandidates[dist(rng)];
        }
    }
    if (selected) selected->executePayload();
}

} // namespace sfpm