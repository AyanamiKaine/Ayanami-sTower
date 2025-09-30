#pragma once

#include "Criteria.hpp"

#include <algorithm>
#include <functional>
#include <memory>
#include <string>
#include <utility>
#include <vector>

namespace sfpm {

class Rule {
public:
    Rule(std::vector<std::shared_ptr<CriteriaBase>> criterias, std::function<void()> payload, std::string name = {})
        : criterias_(std::move(criterias)), payload_(std::move(payload)), name_(std::move(name)) {}

    int criteriaCount() const { return static_cast<int>(criterias_.size()); }
    int priority() const { return priority_; }
    void setPriority(int p) { priority_ = p; }
    const std::string &name() const { return name_; }

    std::pair<bool, int> evaluate(const IFactSource &facts) const {
        for (const auto &c : criterias_) {
            if (!c || !c->evaluate(facts)) return {false, 0};
        }
        return {true, criteriaCount()};
    }

    void executePayload() const {
        if (payload_) payload_();
    }

private:
    std::vector<std::shared_ptr<CriteriaBase>> criterias_;
    std::function<void()> payload_;
    std::string name_;
    int priority_ = 0;
};

inline void optimizeRules(std::vector<Rule> &rules) {
    std::sort(rules.begin(), rules.end(), [](const Rule &a, const Rule &b) {
        return a.criteriaCount() > b.criteriaCount();
    });
}

inline const Rule &mostSpecificRule(const std::vector<Rule> &rules) {
    if (rules.empty()) throw std::runtime_error("mostSpecificRule: rules empty");
    return *std::max_element(rules.begin(), rules.end(), [](const Rule &a, const Rule &b) {
        return a.criteriaCount() < b.criteriaCount();
    });
}

inline const Rule &leastSpecificRule(const std::vector<Rule> &rules) {
    if (rules.empty()) throw std::runtime_error("leastSpecificRule: rules empty");
    return *std::min_element(rules.begin(), rules.end(), [](const Rule &a, const Rule &b) {
        return a.criteriaCount() < b.criteriaCount();
    });
}

} // namespace sfpm