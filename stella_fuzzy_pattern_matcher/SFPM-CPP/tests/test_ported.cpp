#include "sfpm/RuleMatcher.hpp"
#include "sfpm/Rule.hpp"
#include "sfpm/Query.hpp"

#include <cassert>
#include <iostream>
#include <unordered_set>

using namespace sfpm;

namespace {

// Helper to build a rule from predicate criteria quickly
template <typename... C>
Rule makeRule(std::string name, std::function<void()> payload, C... cs) {
    std::vector<std::shared_ptr<CriteriaBase>> v{cs...};
    return Rule(std::move(v), std::move(payload), std::move(name));
}

void test_SimpleOneRuleTwoCriteriaStrictMatch() {
    Rule r({
                make_predicate<std::string>("who", [](const std::string &s) { return s == "Nick"; }),
                make_predicate<std::string>("concept", [](const std::string &s) { return s == "onHit"; }),
            },
            [] {},
            "twoCriteria");

    MapFactSource facts;
    facts.add("concept", std::string("onHit"))
         .add("who", std::string("Nick"));

    auto [matched, count] = r.evaluate(facts);
    assert(matched);
    assert(count == 2);
}

void test_SimpleOneRuleTwoCriteriaPredicateBasedStrictMatch() {
    // Same as previous since we already use predicates
    test_SimpleOneRuleTwoCriteriaStrictMatch();
}

void test_SimpleOneRuleOneCriteriaStrictMatch() {
    Rule r({
                make_predicate<std::string>("who", [](const std::string &s) { return s == "Nick"; }),
                make_predicate<std::string>("concept", [](const std::string &s) { return s == "onHit"; }),
            },
            [] {},
            "missingOneFact");

    MapFactSource facts; // only concept fact provided
    facts.add("concept", std::string("onHit"));
    auto [matched, count] = r.evaluate(facts);
    assert(!matched);
    assert(count == 0);
}

void test_RandomRuleSelectionIfMultipleRulesMatch() {
    MapFactSource facts;
    facts.add("who", std::string("Nick"))
         .add("concept", std::string("onHit"))
         .add("curMap", std::string("circus"))
         .add("health", 0.66)
         .add("nearAllies", 2)
         .add("hitBy", std::string("zombieClown"));

    bool r1 = false, r2 = false, r3 = false, r4 = false;
    std::vector<Rule> rules;
    rules.push_back(makeRule("r1", [&] { r1 = true; },
                             make_predicate<std::string>("who", [](const std::string &s) { return s == "Nick"; }),
                             make_predicate<std::string>("concept", [](const std::string &s) { return s == "onHit"; })));
    rules.push_back(makeRule("r2", [&] { r2 = true; },
                             make_predicate<std::string>("who", [](const std::string &s) { return s == "Nick"; }),
                             make_predicate<std::string>("concept", [](const std::string &s) { return s == "onHit"; }),
                             make_predicate<int>("nearAllies", [](int v) { return v > 1; })));
    rules.push_back(makeRule("r3", [&] { r3 = true; },
                             make_predicate<std::string>("who", [](const std::string &s) { return s == "Nick"; }),
                             make_predicate<std::string>("concept", [](const std::string &s) { return s == "onHit"; }),
                             make_predicate<std::string>("curMap", [](const std::string &s) { return s == "circus"; })));
    rules.push_back(makeRule("r4", [&] { r4 = true; },
                             make_predicate<std::string>("who", [](const std::string &s) { return s == "Nick"; }),
                             make_predicate<std::string>("concept", [](const std::string &s) { return s == "onHit"; }),
                             make_predicate<std::string>("hitBy", [](const std::string &s) { return s == "zombieClown"; })));

    // Run many times; rule1 should never execute; others at least once.
    for (int i = 0; i < 1000; ++i) {
        r2 = r2 && true; // keep state
        match(rules, facts, false);
    }
    assert(!r1);
    assert(r2 || r3 || r4); // at least one executed
    assert(r2 && r3 && r4); // extremely high probability; assumed
}

void test_LeftForDeadExample() {
    MapFactSource facts;
    facts.add("who", std::string("Nick"))
         .add("concept", std::string("onHit"))
         .add("curMap", std::string("circus"))
         .add("health", 0.66)
         .add("nearAllies", 2)
         .add("hitBy", std::string("zombieClown"));

    bool r1=false,r2=false,r3=false,r4=false,r5=false;
    std::vector<Rule> rules;
    rules.push_back(makeRule("r1", [&]{ r1=true; },
                             make_predicate<std::string>("who",[](const std::string&s){return s=="Nick";}),
                             make_predicate<std::string>("concept",[](const std::string&s){return s=="onHit";})));
    rules.push_back(makeRule("r2", [&]{ r2=true; },
                             make_predicate<std::string>("who",[](const std::string&s){return s=="Nick";}),
                             make_predicate<std::string>("concept",[](const std::string&s){return s=="onHit";}),
                             make_predicate<int>("nearAllies",[](int v){return v>1;})));
    rules.push_back(makeRule("r3", [&]{ r3=true; },
                             make_predicate<std::string>("who",[](const std::string&s){return s=="Nick";}),
                             make_predicate<std::string>("concept",[](const std::string&s){return s=="onHit";}),
                             make_predicate<std::string>("curMap",[](const std::string&s){return s=="circus";})));
    rules.push_back(makeRule("r4", [&]{ r4=true; },
                             make_predicate<std::string>("who",[](const std::string&s){return s=="Nick";}),
                             make_predicate<std::string>("concept",[](const std::string&s){return s=="onHit";}),
                             make_predicate<std::string>("hitBy",[](const std::string&s){return s=="zombieClown";})));
    rules.push_back(makeRule("r5", [&]{ r5=true; },
                             make_predicate<std::string>("who",[](const std::string&s){return s=="Nick";}),
                             make_predicate<std::string>("concept",[](const std::string&s){return s=="onHit";}),
                             make_predicate<std::string>("hitBy",[](const std::string&s){return s=="zombieClown";}),
                             make_predicate<std::string>("curMap",[](const std::string&s){return s=="circus";})));

    match(rules, facts, false);
    assert(!r1 && !r2 && !r3 && !r4 && r5);
}

void test_QueryMatchingARule_and_AddingMemory() {
    MapFactSource facts;
    facts.add("concept", std::string("OnHit"))
         .add("attacker", std::string("Hunter"))
         .add("damage", 12.4);

    bool executed = false;
    bool eventRuleExecuted = false;
    std::vector<Rule> rules;
    // Non-matching rule (case sensitive difference on concept)
    rules.push_back(makeRule("r1", [&] { executed = false; },
                             make_predicate<std::string>("who",[](const std::string&s){return s=="Nick";}),
                             make_predicate<std::string>("concept",[](const std::string&s){return s=="onHit";})));
    // Most specific (3 criteria)
    rules.push_back(makeRule("r2", [&] { executed = true; },
                             make_predicate<std::string>("attacker",[](const std::string&s){return s=="Hunter";}),
                             make_predicate<std::string>("concept",[](const std::string&s){return s=="OnHit";}),
                             make_predicate<double>("damage",[](double d){return d==12.4;})));
    // Less specific alt rule (2 criteria)
    rules.push_back(makeRule("r3", [&] { executed = false; },
                             make_predicate<std::string>("concept",[](const std::string&s){return s=="OnHit";}),
                             make_predicate<double>("damage",[](double d){return d>10.0;})));
    // Another less specific alt
    rules.push_back(makeRule("r4", [&] { executed = false; },
                             make_predicate<std::string>("attacker",[](const std::string&s){return !s.empty() && s[0]=='H';}),
                             make_predicate<double>("damage",[](double d){return d<20.0;})));
    match(rules, facts, false);
    assert(executed);

    // AddingMemoryToQuery test
    MapFactSource facts2 = facts; // copy
    std::vector<Rule> rules2;
    rules2.push_back(makeRule("addMemory", [&] { facts2.add("EventAHappened", true); },
                              make_predicate<std::string>("attacker",[](const std::string&s){return s=="Hunter";}),
                              make_predicate<std::string>("concept",[](const std::string&s){return s=="OnHit";}),
                              make_predicate<double>("damage",[](double d){return d==12.4;})));
    rules2.push_back(makeRule("memoryDependent", [&] { eventRuleExecuted = true; },
                              make_predicate<std::string>("attacker",[](const std::string&s){return s=="Hunter";}),
                              make_predicate<std::string>("concept",[](const std::string&s){return s=="OnHit";}),
                              make_predicate<double>("damage",[](double d){return d==12.4;}),
                              make_predicate<bool>("EventAHappened",[](bool b){return b;})));

    // First match adds memory
    match(rules2, facts2, false);
    // Second match should fire memory dependent rule
    match(rules2, facts2, false);
    assert(eventRuleExecuted);
}

void test_MostAndLeastSpecificRuleHelpers() {
    std::vector<Rule> rules;
    rules.push_back(makeRule("twoA", []{},
                             make_predicate<std::string>("who",[](const std::string&s){return s=="Nick";}),
                             make_predicate<std::string>("concept",[](const std::string&s){return s=="onHit";})));
    rules.push_back(makeRule("twoB", []{},
                             make_predicate<std::string>("who",[](const std::string&s){return s=="Nick";}),
                             make_predicate<int>("nearAllies",[](int v){return v>1;})));
    rules.push_back(makeRule("three", []{},
                             make_predicate<std::string>("who",[](const std::string&s){return s=="Nick";}),
                             make_predicate<std::string>("concept",[](const std::string&s){return s=="onHit";}),
                             make_predicate<std::string>("curMap",[](const std::string&s){return s=="circus";})));
    rules.push_back(makeRule("one", []{},
                             make_predicate<std::string>("who",[](const std::string&s){return s=="Nick";})));

    const Rule &most = mostSpecificRule(rules);
    const Rule &least = leastSpecificRule(rules);
    assert(most.criteriaCount() == 3);
    assert(least.criteriaCount() == 1);
}

} // namespace

int main() {
    test_SimpleOneRuleTwoCriteriaStrictMatch();
    test_SimpleOneRuleTwoCriteriaPredicateBasedStrictMatch();
    test_SimpleOneRuleOneCriteriaStrictMatch();
    test_RandomRuleSelectionIfMultipleRulesMatch();
    test_LeftForDeadExample();
    test_QueryMatchingARule_and_AddingMemory();
    test_MostAndLeastSpecificRuleHelpers();
    std::cout << "All ported C# parity tests passed.\n";
    return 0;
}
