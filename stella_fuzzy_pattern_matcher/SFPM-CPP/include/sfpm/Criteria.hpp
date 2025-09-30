#pragma once

#include "FactSource.hpp"

#include <any>
#include <compare>
#include <functional>
#include <memory>
#include <optional>
#include <string>
#include <typeindex>
#include <typeinfo>
#include <utility>

namespace sfpm {

enum class Operator {
    Equal,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    NotEqual,
    Predicate,
};

// Base polymorphic criteria for runtime storage
class CriteriaBase {
public:
    virtual ~CriteriaBase() = default;
    virtual const std::string &factName() const = 0;
    virtual Operator op() const = 0;
    virtual bool evaluate(const IFactSource &facts) const = 0;
};

// Templated criteria implementation
template <typename T>
class Criteria : public CriteriaBase {
public:
    // Value-based comparison criterion
    Criteria(std::string factName, T expectedValue, Operator op)
        : factName_(std::move(factName)), op_(op), expectedValue_(std::move(expectedValue)) {}

    // Predicate-based criterion
    Criteria(std::string factName, std::function<bool(const T &)> predicate, std::string predicateName = {})
        : factName_(std::move(factName)), op_(Operator::Predicate), predicate_(std::move(predicate)), predicateName_(std::move(predicateName)) {}

    const std::string &factName() const override { return factName_; }
    Operator op() const override { return op_; }

    bool evaluate(const IFactSource &facts) const override {
        std::any raw;
        if (!facts.tryGet(factName_, raw)) return false; // fact not found
        if (raw.type() != typeid(T)) return false;        // type mismatch
        const T &actual = *std::any_cast<T>(&raw);

        if (op_ == Operator::Predicate) {
            if (!predicate_) return false; // defensive
            return predicate_(actual);
        }

        // Comparison-based operators
        switch (op_) {
            case Operator::Equal:
                return actual == expectedValue_;
            case Operator::NotEqual:
                return !(actual == expectedValue_);
            case Operator::GreaterThan:
                return actual > expectedValue_;
            case Operator::LessThan:
                return actual < expectedValue_;
            case Operator::GreaterThanOrEqual:
                return actual >= expectedValue_;
            case Operator::LessThanOrEqual:
                return actual <= expectedValue_;
            default:
                return false;
        }
    }

private:
    std::string factName_;
    Operator op_;
    T expectedValue_{}; // Only meaningful for non-predicate
    std::function<bool(const T &)> predicate_{}; // For predicate operator
    std::string predicateName_{};                // Helpful label (unused currently)
};

// Helper factory for equality (deduces template arg)
template <typename T>
inline std::shared_ptr<Criteria<T>> make_equal(std::string name, T value) {
    return std::make_shared<Criteria<T>>(std::move(name), std::move(value), Operator::Equal);
}

// Helper factory for predicate-based criteria
template <typename T, typename F>
inline std::shared_ptr<Criteria<T>> make_predicate(std::string name, F &&pred, std::string predName = {}) {
    return std::make_shared<Criteria<T>>(std::move(name), std::function<bool(const T &)>(std::forward<F>(pred)), std::move(predName));
}

} // namespace sfpm