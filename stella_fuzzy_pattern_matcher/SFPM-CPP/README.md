# SFPM-CPP (Stella Fuzzy Pattern Matcher - C++ Port)

A lightweight C++20 header-only port of the Stella Fuzzy Pattern Matcher originally implemented in C#.

## Features

- Rule-based pattern matching over a dynamic fact source
- Criteria with comparison operators or custom predicates
- Prioritized rules with random tie-breaking
- Selection of the most specific (highest criteria count) matching rule
- Simple optimization by sorting rules by specificity
- Header-only, no external dependencies

## Building

```bash
cmake -S . -B build
cmake --build build --config Release
ctest --test-dir build -V
```

## Example

```cpp
#include "sfpm/RuleMatcher.hpp"
#include "sfpm/Rule.hpp"
#include "sfpm/Query.hpp"

using namespace sfpm;

int main() {
    std::vector<Rule> rules;
    rules.emplace_back(std::vector<std::shared_ptr<CriteriaBase>>{
                            make_predicate<int>("health", [](int h){ return h < 50; }),
                            make_predicate<bool>("isInCombat", [](bool v){ return v; })},
                        []{ std::puts("Critical situation!"); }, "critical");

    MapFactSource facts;
    facts.add("health", 40).add("isInCombat", true);
    match(rules, facts);
}
```

## Parity Notes vs C# Version

| Concept | C# | C++ |
|---------|----|-----|
| IFactSource | Interface with generic TryGetFact | Abstract class + MapFactSource using std::any |
| Criteria | Generic + Operator / Predicate | Template + runtime polymorphism via CriteriaBase |
| Rule.Evaluate | Returns (bool,int) | Same pair<bool,int> semantics |
| Match logic | Most specific -> priority -> random | Same algorithm |

## Next Steps / TODO

- Add adapter for user-defined fact sources beyond map
- Add serialization helpers
- Benchmark suite (Google Benchmark or Celero)
- Optional logging / tracing hooks

## License

MIT (inherits from original project intent).
