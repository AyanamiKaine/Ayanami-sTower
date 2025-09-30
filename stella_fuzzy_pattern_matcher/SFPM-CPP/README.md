# SFPM-CPP (Stella Fuzzy Pattern Matcher - C++ Port)

AI-GENERATED-README

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

## Integrating Into Your Project

SFPM-CPP is header-only. You just need the `include/` directory and to link the `sfpm` interface target (if you use the provided CMakeLists). Below are several common integration approaches.

### 1. add_subdirectory (recommended for monorepos / vendoring)

Copy or add this repository as a subfolder, then:

```cmake
add_subdirectory(path/to/SFPM-CPP)
target_link_libraries(MyTarget PRIVATE sfpm)
```

Disable the library's tests in your super-build:

```cmake
set(SFPM_BUILD_TESTS OFF CACHE BOOL "" FORCE)
add_subdirectory(external/SFPM-CPP)
```

### 2. Git Submodule + add_subdirectory

```bash
git submodule add <repo-url> external/SFPM-CPP
git submodule update --init --recursive
```
Then in your root `CMakeLists.txt`:

```cmake
set(SFPM_BUILD_TESTS OFF CACHE BOOL "" FORCE)
add_subdirectory(external/SFPM-CPP)
target_link_libraries(MyTarget PRIVATE sfpm)
```

### 3. CMake FetchContent

```cmake
include(FetchContent)
FetchContent_Declare(sfpm-cpp
    GIT_REPOSITORY <repo-url>
    GIT_TAG        <commit-or-tag>)
FetchContent_MakeAvailable(sfpm-cpp)

target_link_libraries(MyTarget PRIVATE sfpm)
```

### 4. Manual Include (no CMake target)

Copy `SFPM-CPP/include/sfpm` into your project and add:

```cmake
target_include_directories(MyTarget PRIVATE path/to/include)
```
You do not need to link anything because it is header-only, but you also won't benefit from centrally managed warnings flags the interface target provides.

### 5. Package Manager (future)

You can wrap the existing `CMakeLists.txt` into a package for vcpkg / Conan. The interface nature makes that trivial (only includes + option flag). This repository does not yet ship a manifest; contributions welcome.

### Configuration Options

| Option | Default | Purpose |
|--------|---------|---------|
| `SFPM_BUILD_TESTS` | ON | Build the provided parity tests. Turn OFF in consumers. |

Override example:

```bash
cmake -S SFPM-CPP -B build -DSFPM_BUILD_TESTS=OFF
```

### Using the API

```cpp
#include <sfpm/RuleMatcher.hpp>
#include <sfpm/Rule.hpp>

using namespace sfpm;

int main() {
        std::vector<Rule> rules;
        rules.emplace_back(std::vector<std::shared_ptr<CriteriaBase>>{
                make_predicate<int>("health", [](int h){ return h < 50; }),
                make_predicate<bool>("isInCombat", [](bool b){ return b; })
        }, []{ /* ... */ }, "critical");

        MapFactSource facts; facts.add("health", 42).add("isInCombat", true);
        match(rules, facts);
}
```

### Warning Levels / Toolchains

The target adds `-Wall -Wextra -Wpedantic` (GCC/Clang) and `/W4` (MSVC). If you need to relax or extend these, adjust after linking:

```cmake
target_compile_options(MyTarget PRIVATE $<$<CXX_COMPILER_ID:MSVC>:/W3>)
```

### Header-Only Rationale

All logic lives in templates / inline functions for simplicity and zero build-time linkage overhead. If performance profiling later shows heavy compile times, you can refactor implementation bodies into `.cpp` translation units without changing the public interface.


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
