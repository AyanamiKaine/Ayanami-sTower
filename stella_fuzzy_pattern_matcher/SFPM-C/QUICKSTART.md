# SFPM-C Quick Start Guide

## Installation

### Option 1: Build and Install Locally

```bash
cd stella_fuzzy_pattern_matcher/SFPM-C
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
cmake --install build --prefix /path/to/install
```

### Option 2: Use as CMake Subdirectory

```cmake
# In your CMakeLists.txt
add_subdirectory(path/to/SFPM-C)
target_link_libraries(YourProject PRIVATE sfpm)
```

### Option 3: Copy Source Files

Copy `include/sfpm/` and `src/` to your project and compile directly.

## Your First Program

Create `my_rules.c`:

```c
#include <sfpm/sfpm.h>
#include <stdio.h>

void greet_player(void *user_data) {
    printf("Welcome, hero!\n");
}

void warn_player(void *user_data) {
    printf("Danger ahead!\n");
}

int main(void) {
    /* Step 1: Create fact source */
    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(10);

    /* Step 2: Add facts */
    sfpm_dict_fact_source_add(facts, "playerLevel", sfpm_value_from_int(5));
    sfpm_dict_fact_source_add(facts, "inDanger", sfpm_value_from_bool(false));

    /* Step 3: Create criteria */
    sfpm_criteria_t *level_check = sfpm_criteria_create(
        "playerLevel",
        SFPM_OP_GREATER_THAN_OR_EQUAL,
        sfpm_value_from_int(1)
    );

    /* Step 4: Create rule */
    sfpm_criteria_t **criterias = malloc(sizeof(sfpm_criteria_t*) * 1);
    criterias[0] = level_check;

    sfpm_rule_t *greeting_rule = sfpm_rule_create(
        criterias,
        1,
        greet_player,
        NULL,
        "greeting"
    );

    /* Step 5: Match and execute */
    sfpm_rule_t *rules[] = {greeting_rule};
    sfpm_match(rules, 1, facts, false);

    /* Step 6: Cleanup */
    sfpm_rule_destroy(greeting_rule);
    sfpm_fact_source_destroy(facts);

    return 0;
}
```

Compile and run:

```bash
# If installed
gcc my_rules.c -lsfpm -o my_program

# If using source directly
gcc my_rules.c src/*.c -Iinclude -o my_program

./my_program
# Output: Welcome, hero!
```

## Common Patterns

### Pattern 1: Multiple Rules with Priority

```c
/* High priority emergency rule */
sfpm_rule_t *emergency = /* ... */;
sfpm_rule_set_priority(emergency, 100);

/* Normal priority rule */
sfpm_rule_t *normal = /* ... */;
sfpm_rule_set_priority(normal, 10);

/* Low priority default rule */
sfpm_rule_t *default_rule = /* ... */;
sfpm_rule_set_priority(default_rule, 1);
```

### Pattern 2: Using Custom Predicates

```c
bool is_critical_health(const sfpm_value_t *value, void *user_data) {
    int threshold = *(int*)user_data;
    return value->type == SFPM_TYPE_INT &&
           value->data.int_value < threshold;
}

int threshold = 20;
sfpm_criteria_t *health_crit = sfpm_criteria_create_predicate(
    "health",
    is_critical_health,
    &threshold,
    "health < 20"
);
```

### Pattern 3: Dynamic Rule Updates

```c
/* Update facts at runtime */
sfpm_dict_fact_source_add(facts, "score", sfpm_value_from_int(150));

/* Re-match with updated facts */
sfpm_match(rules, rule_count, facts, true);
```

### Pattern 4: Optimizing for Performance

```c
/* For static rule sets, optimize once */
sfpm_optimize_rules(rules, rule_count);

/* Then match many times without re-sorting */
for (int i = 0; i < 1000; i++) {
    update_facts(facts);
    sfpm_match(rules, rule_count, facts, true);  /* true = assume sorted */
}
```

## Memory Management Checklist

✅ **DO:**

-   Allocate criteria arrays with `malloc`
-   Create separate criteria for each rule
-   Destroy rules before fact sources
-   Check return values for NULL
-   Free fact sources when done

❌ **DON'T:**

-   Use stack-allocated arrays for rule criteria
-   Share criteria between multiple rules
-   Free criteria manually if owned by a rule
-   Assume strings in values will be copied

## Next Steps

-   Read the full [README.md](README.md) for detailed API documentation
-   Check [DEVELOPMENT.md](DEVELOPMENT.md) for architecture details
-   Explore [examples/basic_example.c](examples/basic_example.c) for a complete example
-   Run tests: `ctest --test-dir build -V`

## Troubleshooting

### Problem: Crash on rule_destroy

**Solution**: Ensure criteria array was malloc'd, not stack-allocated

### Problem: Double-free error

**Solution**: Don't reuse criteria between rules; create separate instances

### Problem: Fact not found

**Solution**: Check spelling and ensure fact was added before matching

### Problem: Wrong rule selected

**Solution**: Review priorities and criteria counts; more specific rules win

## Getting Help

-   Check the examples in `examples/`
-   Read test cases in `tests/` for usage patterns
-   Review `DEVELOPMENT.md` for design rationale
-   Open an issue on the repository
