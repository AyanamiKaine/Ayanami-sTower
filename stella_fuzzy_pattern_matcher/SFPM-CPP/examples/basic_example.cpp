#include "sfpm/Query.hpp"
#include "sfpm/Rule.hpp"
#include "sfpm/RuleMatcher.hpp"

#include <iostream>

using namespace sfpm;

int main() {
    // Build some rules similar to the C# examples
    std::vector<Rule> rules;

    int dragonCount = 0;
    int bigDragonCount = 0;

    // Basic dragon encounter rule
    rules.emplace_back(
        std::vector<std::shared_ptr<CriteriaBase>>{
            make_predicate<int>("PlayerLevel", [](int v) { return v >= 10; }),
            make_predicate<std::string>("HasItem", [](const std::string &s) { return s == "MagicSword"; }),
            make_predicate<std::string>("QuestStatus", [](const std::string &s) { return s != "DragonSlayerComplete"; }),
            make_predicate<std::string>("TimeOfDay", [](const std::string &s) { return s == "Night"; }),
            make_predicate<std::string>("Location", [](const std::string &s) { return s == "MysticalForest"; }),
            make_predicate<int>("Health", [](int h) { return h > 50; }),
            make_predicate<int>("MagicPoints", [](int mp) { return mp >= 30; }),
            make_predicate<std::string>("Status", [](const std::string &s) { return s != "Cursed"; }),
            make_predicate<int>("Reputation", [](int r) { return r > 100; })},
        [&] { ++dragonCount; std::cout << "Spawn Ancient Dragon\n"; },
        "dragonEncounter");
    rules.back().setPriority(1);

    // Big dragon rule (more specific by raising level requirement)
    rules.emplace_back(
        std::vector<std::shared_ptr<CriteriaBase>>{
            make_predicate<int>("PlayerLevel", [](int v) { return v >= 15; }),
            make_predicate<std::string>("HasItem", [](const std::string &s) { return s == "MagicSword"; }),
            make_predicate<std::string>("QuestStatus", [](const std::string &s) { return s != "DragonSlayerComplete"; }),
            make_predicate<std::string>("TimeOfDay", [](const std::string &s) { return s == "Night"; }),
            make_predicate<std::string>("Location", [](const std::string &s) { return s == "MysticalForest"; }),
            make_predicate<int>("Health", [](int h) { return h > 50; }),
            make_predicate<int>("MagicPoints", [](int mp) { return mp >= 30; }),
            make_predicate<std::string>("Status", [](const std::string &s) { return s != "Cursed"; }),
            make_predicate<int>("Reputation", [](int r) { return r > 100; })},
        [&] { ++bigDragonCount; std::cout << "Spawn BIG Ancient Dragon\n"; },
        "bigDragonEncounter");
    rules.back().setPriority(2); // Higher priority

    // Facts
    MapFactSource facts;
    facts.add("PlayerLevel", 16)
         .add("HasItem", std::string("MagicSword"))
         .add("QuestStatus", std::string("InProgress"))
         .add("TimeOfDay", std::string("Night"))
         .add("Location", std::string("MysticalForest"))
         .add("Health", 80)
         .add("MagicPoints", 40)
         .add("Status", std::string("Healthy"))
         .add("Reputation", 150);

    match(rules, facts); // Should trigger big dragon due to higher specificity (same count but higher level predicate also matches)

    std::cout << "Dragon count: " << dragonCount << ", Big dragon count: " << bigDragonCount << "\n";
    return 0;
}