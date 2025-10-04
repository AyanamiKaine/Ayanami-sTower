# SFPM-C (Stella Fuzzy Pattern Matcher - C Port)

A lightweight, portable C11 implementation of the Stella Fuzzy Pattern Matcher for rule-based pattern matching with priority selection.

## Features

-   **Rule-based pattern matching** over dynamic fact sources
-   **Type-safe fact system** with support for int, float, double, string, and bool
-   **Flexible criteria** with comparison operators and custom predicates
-   **Priority-based selection** with random tie-breaking
-   **Optimized matching** by sorting rules by specificity
-   **Before/after hooks** for aspect-oriented programming (logging, security, metrics)
-   **Hook chaining** for composable pipelines (auth → validation → execution → audit)
-   **Middleware hooks** for wrapping behavior (transactions, timing, monitoring)
-   **Image-based hot reload** for persisting runtime modifications (Smalltalk/Lisp-style) ✨
-   **Memory snapshots** for instant save/restore of complete interpreter state
-   **Portable C11** with no external dependencies
-   **CMake build system** for easy integration

## Building

### Basic Build

```bash
cmake -S . -B build
cmake --build build --config Release
```

### Running Tests

```bash
ctest --test-dir build -C Release --output-on-failure
```

**Test Suites:**

-   `sfpm_basic` - Core SFPM functionality (values, criteria, rules)
-   `sfpm_advanced` - Advanced pattern matching features
-   `sfpm_hook_chaining` - Hook chaining with 15 comprehensive tests ✨

See `TEST_COVERAGE_REPORT.md` for detailed test coverage information.

### Running Examples

```bash
# Basic pattern matching example
./build/Release/sfpm_example.exe

# Interpreter comparison: Switch vs SFPM
./build/Release/sfpm_comparison.exe

# Optimized interpreter with caching strategies
./build/Release/sfpm_cached.exe

# Tiered interpreter with automatic mode switching
./build/Release/sfpm_tiered.exe

# Practical game AI examples
./build/Release/sfpm_game_ai.exe

# Aspect-oriented programming with hooks
./build/Release/sfpm_hooks.exe

# Hook chaining for security pipelines and observability
./build/Release/sfpm_hook_chaining.exe

# Image-based hot reload with memory snapshots ✨
./build/Release/sfpm_hot_reload.exe
```

See `README_INTERPRETER.md` for details on using SFPM to build runtime-modifiable interpreters.  
See `README_CACHING.md` for caching optimizations that reduce overhead from ~470x to ~3.5x.  
See `README_TIERED.md` for automatic tier system with mode switching.  
See `README_GAME_AI.md` for practical game AI examples.  
See `README_HOOKS.md` for aspect-oriented programming with before/after hooks.  
See `README_HOOK_CHAINING.md` for multiple hook chains and middleware patterns.  
See `README_HOT_RELOAD.md` for image-based persistence and hot code reloading. ✨

## Quick Start

```c
#include <sfpm/sfpm.h>

/* Define payload functions */
void handle_critical(void *user_data) {
    printf("Critical situation detected!\n");
}

int main(void) {
    /* Create fact source */
    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(10);
    sfpm_dict_fact_source_add(facts, "health", sfpm_value_from_int(30));
    sfpm_dict_fact_source_add(facts, "inCombat", sfpm_value_from_bool(true));

    /* Create criteria */
    sfpm_criteria_t *low_health = sfpm_criteria_create(
        "health", SFPM_OP_LESS_THAN, sfpm_value_from_int(50));
    sfpm_criteria_t *in_combat = sfpm_criteria_create(
        "inCombat", SFPM_OP_EQUAL, sfpm_value_from_bool(true));

    /* Create rule */
    sfpm_criteria_t *criterias[] = {low_health, in_combat};
    sfpm_rule_t *rule = sfpm_rule_create(
        criterias, 2, handle_critical, NULL, "critical");

    /* Match and execute */
    sfpm_rule_t *rules[] = {rule};
    sfpm_match(rules, 1, facts, true);

    /* Cleanup */
    sfpm_rule_destroy(rule);
    sfpm_fact_source_destroy(facts);
    return 0;
}
```

## API Overview

### Fact Sources

Facts are stored in a type-safe container with tagged unions:

```c
/* Create fact source */
sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(capacity);

/* Add facts */
sfpm_dict_fact_source_add(facts, "health", sfpm_value_from_int(100));
sfpm_dict_fact_source_add(facts, "name", sfpm_value_from_string("Player"));
sfpm_dict_fact_source_add(facts, "active", sfpm_value_from_bool(true));
sfpm_dict_fact_source_add(facts, "temperature", sfpm_value_from_float(98.6f));

/* Cleanup */
sfpm_fact_source_destroy(facts);
```

### Criteria

Create matching criteria with operators or custom predicates:

```c
/* Comparison-based criteria */
sfpm_criteria_t *health_check = sfpm_criteria_create(
    "health",
    SFPM_OP_GREATER_THAN,
    sfpm_value_from_int(50)
);

/* Custom predicate */
bool is_low_health(const sfpm_value_t *value, void *user_data) {
    return value->type == SFPM_TYPE_INT && value->data.int_value < 50;
}

sfpm_criteria_t *custom_check = sfpm_criteria_create_predicate(
    "health",
    is_low_health,
    NULL,
    "health < 50"
);
```

### Rules

Combine criteria into rules with payloads:

```c
void my_action(void *user_data) {
    printf("Rule matched!\n");
}

sfpm_criteria_t *criterias[] = {criteria1, criteria2};
sfpm_rule_t *rule = sfpm_rule_create(
    criterias,          /* Array of criteria (takes ownership) */
    2,                  /* Criteria count */
    my_action,          /* Payload function */
    NULL,               /* User data for payload */
    "my_rule"           /* Optional name for debugging */
);

/* Set priority (higher = preferred) */
sfpm_rule_set_priority(rule, 10);

/* Cleanup */
sfpm_rule_destroy(rule); /* Also destroys owned criteria */
```

### Matching

Match rules against facts and execute the best match:

```c
sfpm_rule_t *rules[] = {rule1, rule2, rule3};

/* Match with optimization (sorts by criteria count) */
sfpm_match(rules, 3, facts, true);

/* Helper functions */
sfpm_optimize_rules(rules, 3);
sfpm_rule_t *most = sfpm_most_specific_rule(rules, 3);
sfpm_rule_t *least = sfpm_least_specific_rule(rules, 3);
```

## Matching Algorithm

The matcher follows this selection logic:

1. **Evaluate all rules** against the fact source
2. **Select by specificity**: Rules with the most matching criteria win
3. **Break ties by priority**: Among equal specificity, highest priority wins
4. **Random selection**: If still tied, randomly pick one
5. **Execute payload**: Run the selected rule's action

## Supported Operators

-   `SFPM_OP_EQUAL` - Equality comparison
-   `SFPM_OP_NOT_EQUAL` - Inequality comparison
-   `SFPM_OP_GREATER_THAN` - Greater than
-   `SFPM_OP_LESS_THAN` - Less than
-   `SFPM_OP_GREATER_THAN_OR_EQUAL` - Greater than or equal
-   `SFPM_OP_LESS_THAN_OR_EQUAL` - Less than or equal
-   `SFPM_OP_PREDICATE` - Custom predicate function

## Supported Types

-   `SFPM_TYPE_INT` - Integer values
-   `SFPM_TYPE_FLOAT` - Single-precision floating point
-   `SFPM_TYPE_DOUBLE` - Double-precision floating point
-   `SFPM_TYPE_STRING` - Null-terminated strings (not owned)
-   `SFPM_TYPE_BOOL` - Boolean values

## Integration

### CMake Subdirectory

```cmake
add_subdirectory(path/to/SFPM-C)
target_link_libraries(MyProject PRIVATE sfpm)
```

### CMake FetchContent

```cmake
include(FetchContent)
FetchContent_Declare(sfpm
    GIT_REPOSITORY <repo-url>
    GIT_TAG main
)
FetchContent_MakeAvailable(sfpm)
target_link_libraries(MyProject PRIVATE sfpm)
```

### Manual Integration

Copy the `include/` and `src/` directories to your project and add to your build system.

## Memory Management

-   **Fact sources**: Call `sfpm_fact_source_destroy()` when done
-   **Rules**: Call `sfpm_rule_destroy()` - automatically destroys owned criteria
-   **Standalone criteria**: Only destroy if not owned by a rule
-   **String values**: SFPM does not take ownership of string pointers

## Design Differences from C#/C++ Versions

| Concept     | C#            | C++            | C                        |
| ----------- | ------------- | -------------- | ------------------------ |
| Type system | Generics      | Templates      | Tagged unions            |
| Fact source | Interface     | Abstract class | Function pointers        |
| Memory      | GC            | Smart pointers | Manual (ownership rules) |
| Criteria    | Generic class | Template class | Opaque struct            |
| Collections | List<T>       | std::vector    | Raw arrays               |

## Examples

See the `examples/` directory for complete examples:

-   **basic_example.c** - Simple game AI scenario with health and combat

## Testing

The test suite includes 4 comprehensive suites with 59 total tests:

### Core Functionality Tests

-   **test_basic.c** (8 tests) - Value constructors, fact sources, operators
-   **test_advanced.c** (10 tests) - Custom predicates, specificity, priorities

### Advanced Feature Tests

-   **test_hook_chaining.c** (15 tests) - Hook chains, execution order, abortion
-   **test_snapshot.c** (26 tests) - Memory snapshots, save/restore, data integrity

**Total Coverage:**

-   59 tests, 100% pass rate
-   All public APIs tested
-   NULL safety, edge cases, error paths
-   Data integrity verification

Run all tests: `ctest --test-dir build -C Release --output-on-failure`

**Detailed Reports:**

-   [Hook Chaining Tests](TEST_COVERAGE_REPORT.md)
-   [Snapshot System Tests](SNAPSHOT_TEST_REPORT.md)

## Best Practices

1. **Always check return values** - Functions return NULL or false on failure
2. **Destroy in reverse order** - Rules before facts, as rules don't own facts
3. **Use optimization** - Call `sfpm_match()` with `optimize=true` for sorted rules
4. **Name your rules** - Helps with debugging and understanding matching
5. **Manage string lifetimes** - Ensure fact strings outlive the fact source
6. **Avoid circular references** - Don't store fact sources in rule user data

## Use Cases

### Replacing Switch Statements in Interpreters

SFPM can replace traditional switch statements in virtual machine interpreters, enabling:

-   **Runtime extensibility** - Add new opcodes without recompilation
-   **Hot swapping** - Replace buggy implementations while running
-   **Isolated testing** - Test opcode handlers independently
-   **Plugin architecture** - Load opcodes from shared libraries
-   **Fail-safe security** - Unregistered opcodes physically cannot execute

**Performance:**

-   Naive SFPM: ~481x overhead compared to switch statements
-   **With caching: ~3.5x overhead** (135x faster than naive!)

Acceptable for:

-   ✅ Game scripting engines (non-critical path)
-   ✅ Configuration languages
-   ✅ AI behavior trees / decision systems
-   ❌ NOT for hot-path game loops or real-time processing

**Caching Strategies:**

1. **Direct function pointer cache** - 2.8x overhead, loses pattern matching
2. **SFPM + rule cache** - 3.5x overhead, retains all SFPM benefits ⭐ **Recommended**
3. **SFPM + fact reuse** - 138x overhead, for complex multi-criteria rules

See `examples/interpreter_comparison.c`, `examples/interpreter_cached.c`, and `examples/README_CACHING.md` for comprehensive demonstrations.

### Game AI Decision Systems

```c
// Runtime-modifiable NPC behavior
sfpm_criteria_t *see_enemy = sfpm_criteria_create("enemyVisible", SFPM_OP_EQUAL, sfpm_value_from_bool(true));
sfpm_criteria_t *low_ammo = sfpm_criteria_create("ammo", SFPM_OP_LESS_THAN, sfpm_value_from_int(10));

sfpm_rule_t *retreat_rule = sfpm_rule_create(..., retreat_behavior, ...);
// Modify behavior at runtime based on difficulty, player feedback, etc.
```

## Performance Considerations

-   **Optimization**: Pre-sort rules by criteria count for early exit
-   **Fact lookup**: Dictionary implementation is O(n) linear search
-   **Memory allocation**: Minimal allocations during matching
-   **Custom fact sources**: Implement for optimal lookup in your domain

## Custom Fact Sources

You can implement custom fact sources for domain-specific optimizations:

```c
bool my_try_get(const sfpm_fact_source_t *source,
                const char *fact_name,
                sfpm_value_t *out_value) {
    /* Your custom lookup logic */
    return true;
}

void my_destroy(sfpm_fact_source_t *source) {
    /* Your cleanup logic */
}

sfpm_fact_source_t *create_custom_source(void) {
    sfpm_fact_source_t *source = malloc(sizeof(sfpm_fact_source_t));
    source->user_data = /* your data */;
    source->try_get_fact = my_try_get;
    source->destroy = my_destroy;
    return source;
}
```

## License

MIT (same as the original C# implementation)

## Parity with Other Versions

This C port maintains conceptual parity with the C# and C++ versions while adapting to C idioms:

-   ✅ Rule-based matching with criteria
-   ✅ Specificity and priority selection
-   ✅ Custom predicates
-   ✅ Optimization by sorting
-   ✅ Type-safe fact storage
-   ✅ All comparison operators
-   ✅ Random tie-breaking

## Contributing

Contributions welcome! Please:

-   Follow C11 standard
-   Match existing code style
-   Add tests for new features
-   Update documentation
-   Ensure all tests pass

## Related Projects

-   **SFPM (C#)** - Original implementation
-   **SFPM-CPP** - C++20 header-only port
-   **SFPM-Kotlin** - Kotlin/JVM port
