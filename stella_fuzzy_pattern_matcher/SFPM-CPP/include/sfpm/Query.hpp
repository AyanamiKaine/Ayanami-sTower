#pragma once

#include "FactSource.hpp"
#include "RuleMatcher.hpp"

#include <any>
#include <memory>
#include <unordered_map>
#include <vector>

namespace sfpm {

class Query {
public:
    explicit Query(std::shared_ptr<IFactSource> factSource) : factSource_(std::move(factSource)) {}

    void match(const std::vector<Rule> &rules) const { sfpm::match(rules, *factSource_, false); }

    static Query fromMap(const std::unordered_map<std::string, std::any> &map) {
        auto src = std::make_shared<MapFactSource>();
        for (auto &kv : map) src->add(kv.first, kv.second);
        return Query(src);
    }

private:
    std::shared_ptr<IFactSource> factSource_;
};

} // namespace sfpm