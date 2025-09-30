#include "sfpm/RuleMatcher.hpp"
#include "sfpm/Rule.hpp"
#include "sfpm/Query.hpp"

#include <cassert>
#include <iostream>

using namespace sfpm;

int main() {
    int executedA = 0;
    int executedB = 0;

    std::vector<Rule> rules;
    rules.emplace_back(std::vector<std::shared_ptr<CriteriaBase>>{
                            make_predicate<int>("health", [](int h) { return h < 50; }),
                            make_predicate<bool>("isInCombat", [](bool v) { return v; })},
                        [&] { ++executedA; }, "critical");
    rules.back().setPriority(1);

    rules.emplace_back(std::vector<std::shared_ptr<CriteriaBase>>{
                            make_predicate<int>("health", [](int h) { return h < 80; })},
                        [&] { ++executedB; }, "warning");
    rules.back().setPriority(2); // Higher priority but less specific

    MapFactSource facts;
    facts.add("health", 40).add("isInCombat", true);

    match(rules, facts); // The first rule is more specific (2 criteria vs 1) -> executedA

    assert(executedA == 1);
    assert(executedB == 0);
    std::cout << "All basic tests passed.\n";
    return 0;
}