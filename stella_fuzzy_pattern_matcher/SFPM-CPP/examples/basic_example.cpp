#include "sfpm/Query.hpp"
#include "sfpm/Rule.hpp"
#include "sfpm/RuleMatcher.hpp"

#include <chrono>
#include <iostream>
#include <random>
#include <string_view>
#include <atomic>
#include <thread>

using namespace sfpm;

static void runBasicExample() {
    std::vector<Rule> rules;
    int dragonCount = 0;
    int bigDragonCount = 0;

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
    rules.back().setPriority(2);

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

    match(rules, facts);
    std::cout << "Dragon count: " << dragonCount << ", Big dragon count: " << bigDragonCount << "\n";
}

static void runStressTest(int ruleCount = 5000, int iterations = 10000) {
    std::cout << "Running stress test with " << ruleCount << " rules and " << iterations << " iterations...\n";
    std::vector<Rule> rules;
    rules.reserve(ruleCount);

    std::mt19937 rng(std::random_device{}());
    std::uniform_int_distribution<int> levelDist(1, 50);
    std::uniform_int_distribution<int> critCountDist(1, 5);
    std::uniform_int_distribution<int> priorityDist(0, 5);

    std::atomic<int> executions{0};

    for (int i = 0; i < ruleCount; ++i) {
        int cCount = critCountDist(rng);
        std::vector<std::shared_ptr<CriteriaBase>> cs;
        cs.reserve(cCount);
        // Each criterion checks a pseudo fact: LevelX == some threshold or FlagX conditions
        for (int c = 0; c < cCount; ++c) {
            std::string fact = "Level" + std::to_string((i + c) % 10); // limited variety encourages collisions
            int threshold = levelDist(rng);
            cs.push_back(make_predicate<int>(fact, [threshold](int v) { return v >= threshold; }));
        }
        rules.emplace_back(std::move(cs), [&executions] { ++executions; }, "autoRule" + std::to_string(i));
        rules.back().setPriority(priorityDist(rng));
    }

    // Pre-generate facts map that mutates over time
    MapFactSource facts;
    for (int i = 0; i < 10; ++i) facts.add("Level" + std::to_string(i), levelDist(rng));

    auto start = std::chrono::high_resolution_clock::now();
    for (int iter = 0; iter < iterations; ++iter) {
        // Randomly mutate some facts to vary matches
        int idx = iter % 10;
        facts.add("Level" + std::to_string(idx), levelDist(rng));
        match(rules, facts, false);
    }
    auto end = std::chrono::high_resolution_clock::now();
    auto total = end - start;
    auto total_ms = std::chrono::duration_cast<std::chrono::milliseconds>(total).count();
    long long total_ns = std::chrono::duration_cast<std::chrono::nanoseconds>(total).count();
    double avg_ns = static_cast<double>(total_ns) / iterations;
    double avg_us = avg_ns / 1000.0;
    double avg_ms = avg_ns / 1'000'000.0;
    std::cout << "Stress test completed: executions=" << executions.load()
              << ", elapsed=" << total_ms << " ms"
              << ", avg/iter=" << avg_ns << " ns (" << avg_us << " us, " << avg_ms << " ms)" << std::endl;
}

int main(int argc, char **argv) {
    std::string mode = "basic";
    int ruleCount = 5000;
    int iterations = 10000;
    for (int i = 1; i < argc; ++i) {
        std::string_view arg{argv[i]};
        if (arg == "--stress" || arg == "-s") mode = "stress";
        else if (arg.rfind("--rules=", 0) == 0) ruleCount = std::stoi(std::string(arg.substr(8)));
        else if (arg.rfind("--iterations=", 0) == 0) iterations = std::stoi(std::string(arg.substr(13)));
        else if (arg == "--help" || arg == "-h") {
            std::cout << "Usage: sfpm_example [--stress|-s] [--rules=N] [--iterations=M]\n";
            return 0;
        }
    }

    if (mode == "stress") {
        runStressTest(ruleCount, iterations);
    } else {
        runBasicExample();
    }
    return 0;
}