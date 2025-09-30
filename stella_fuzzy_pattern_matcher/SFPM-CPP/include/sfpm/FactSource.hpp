#pragma once

#include <any>
#include <string>
#include <unordered_map>
#include <utility>

namespace sfpm {

// Abstract fact source similar to C# IFactSource
class IFactSource {
public:
    virtual ~IFactSource() = default;
    // Retrieve a fact as std::any. Returns true if found.
    virtual bool tryGet(const std::string &factName, std::any &value) const = 0;
};

// A simple map-backed fact source.
class MapFactSource : public IFactSource {
public:
    MapFactSource() = default;
    MapFactSource(std::initializer_list<std::pair<const std::string, std::any>> init) : data_(init) {}

    MapFactSource &add(std::string key, std::any value) {
        data_[std::move(key)] = std::move(value);
        return *this;
    }

    bool tryGet(const std::string &factName, std::any &value) const override {
        auto it = data_.find(factName);
        if (it == data_.end()) return false;
        value = it->second;
        return true;
    }

private:
    std::unordered_map<std::string, std::any> data_;
};

} // namespace sfpm