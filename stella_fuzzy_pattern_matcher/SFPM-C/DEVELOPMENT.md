# SFPM-C Development Notes

## Project Structure

```
SFPM-C/
├── include/sfpm/          # Public headers
│   ├── sfpm.h            # Main header (includes all others)
│   ├── fact_source.h     # Fact storage and retrieval
│   ├── criteria.h        # Matching criteria definitions
│   ├── rule.h            # Rule definitions
│   └── matcher.h         # Rule matching engine
├── src/                   # Implementation files
│   ├── fact_source.c
│   ├── criteria.c
│   ├── rule.c
│   └── matcher.c
├── examples/              # Example programs
│   └── basic_example.c
├── tests/                 # Test suite
│   ├── test_basic.c
│   └── test_advanced.c
├── CMakeLists.txt         # Build configuration
├── README.md              # Documentation
├── LICENSE                # MIT License
└── .gitignore             # Git ignore rules
```

## Key Design Decisions

### 1. Tagged Unions for Type Safety

Instead of C++'s templates or C#'s generics, we use tagged unions (`sfpm_value_t`) to provide type-safe fact storage while maintaining C compatibility.

### 2. Opaque Structs

All main types (criteria, rule, fact_source) are opaque pointers, hiding implementation details and allowing future changes without breaking the API.

### 3. Ownership Model

-   **Rules own their criteria arrays**: When a rule is destroyed, it frees its criteria
-   **Criteria ownership transfers**: Pass malloc'd arrays to rule_create
-   **Fact sources are independent**: Must be destroyed separately

### 4. Function Pointers for Extensibility

The fact source uses function pointers, allowing users to implement custom fact sources optimized for their use case.

### 5. No External Dependencies

The library uses only standard C11 features, making it highly portable.

## Memory Management Guidelines

### Creating and Destroying Rules

```c
/* CORRECT: Allocate criteria array on heap */
sfpm_criteria_t **criterias = malloc(sizeof(sfpm_criteria_t*) * count);
criterias[0] = criterion1;
criterias[1] = criterion2;
sfpm_rule_t *rule = sfpm_rule_create(criterias, count, payload, NULL, "name");

/* Rule now owns the array and will free it */
sfpm_rule_destroy(rule); /* Frees array AND all criteria */

/* INCORRECT: Stack-allocated array */
sfpm_criteria_t *bad_array[] = {c1, c2}; /* Will cause crash on destroy */
sfpm_rule_t *bad_rule = sfpm_rule_create(bad_array, 2, ...); /* DON'T DO THIS */
```

### Sharing Criteria Between Rules

```c
/* INCORRECT: Reusing same criteria instance */
sfpm_criteria_t *shared = sfpm_criteria_create(...);
/* Both rules will try to free the same criteria - double free! */

/* CORRECT: Create separate instances */
sfpm_criteria_t *c1 = sfpm_criteria_create("health", SFPM_OP_LESS_THAN, ...);
sfpm_criteria_t *c2 = sfpm_criteria_create("health", SFPM_OP_LESS_THAN, ...);
/* Each rule owns its own criteria */
```

## Testing

### Test Coverage

-   **test_basic.c**: Core functionality

    -   Value constructors
    -   Fact source operations
    -   Comparison operators
    -   Predicates
    -   Rule evaluation
    -   Specificity matching
    -   Priority selection

-   **test_advanced.c**: Integration tests
    -   Type safety
    -   Float/string comparisons
    -   Optimization
    -   Complex scenarios
    -   Edge cases

### Running Tests

```bash
# All tests
ctest --test-dir build -C Release

# Verbose output
ctest --test-dir build -C Release -V

# Specific test
ctest --test-dir build -C Release -R sfpm_basic
```

## Performance Characteristics

| Operation           | Complexity | Notes                                          |
| ------------------- | ---------- | ---------------------------------------------- |
| Fact lookup (dict)  | O(n)       | Linear search; consider hash table for large n |
| Criteria evaluation | O(1)       | Single comparison or predicate call            |
| Rule evaluation     | O(c)       | c = criteria count, early exit on failure      |
| Match (unsorted)    | O(r \* c)  | r = rules, c = avg criteria per rule           |
| Match (sorted)      | O(r' \* c) | r' ≤ r, early exit possible                    |
| Optimize rules      | O(r log r) | qsort by criteria count                        |

## Extending the Library

### Custom Fact Sources

Implement your own fact source for optimal performance:

```c
typedef struct {
    /* Your custom data structure */
    HashMap *map;  /* Example: hash table */
} custom_data_t;

bool custom_try_get(const sfpm_fact_source_t *source,
                    const char *fact_name,
                    sfpm_value_t *out_value) {
    custom_data_t *data = (custom_data_t*)source->user_data;
    /* Your optimized lookup */
    return hashmap_get(data->map, fact_name, out_value);
}

void custom_destroy(sfpm_fact_source_t *source) {
    custom_data_t *data = (custom_data_t*)source->user_data;
    hashmap_destroy(data->map);
    free(data);
    free(source);
}

sfpm_fact_source_t *create_custom_source(void) {
    sfpm_fact_source_t *source = malloc(sizeof(sfpm_fact_source_t));
    custom_data_t *data = malloc(sizeof(custom_data_t));
    data->map = hashmap_create();

    source->user_data = data;
    source->try_get_fact = custom_try_get;
    source->destroy = custom_destroy;
    return source;
}
```

### Adding New Value Types

To add support for new types (e.g., vectors, custom structs):

1. Add enum value to `sfpm_type_t` in `fact_source.h`
2. Add field to union in `sfpm_value_t`
3. Add constructor function
4. Update comparison logic in `criteria.c`

## Common Pitfalls

1. **Stack-allocated criteria arrays**: Always use malloc
2. **Sharing criteria between rules**: Create separate instances
3. **String lifetime**: Ensure strings outlive the fact source
4. **Null checks**: Always check return values
5. **Double-free**: Don't manually free criteria owned by rules

## Comparison with Other Ports

| Feature       | C                     | C++             | C#                 |
| ------------- | --------------------- | --------------- | ------------------ |
| Type system   | Tagged unions         | Templates       | Generics           |
| Memory        | Manual                | Smart pointers  | GC                 |
| Predicates    | Function pointers     | std::function   | Func<T, bool>      |
| Collections   | Raw arrays            | std::vector     | List<T>            |
| Extensibility | Virtual table pattern | Virtual methods | Interfaces         |
| Portability   | Highest               | High            | Platform-dependent |
| Safety        | Manual                | RAII            | Automatic          |

## Build System Notes

The CMakeLists.txt is designed to be:

-   **Embeddable**: Works as subdirectory
-   **Configurable**: Options for tests/examples
-   **Cross-platform**: MSVC and GCC/Clang support
-   **Warning-strict**: Treats warnings as errors

## Future Enhancements

Potential improvements (contributions welcome):

-   [ ] Hash table-based fact source for O(1) lookup
-   [ ] Serialization/deserialization of rules
-   [ ] Rule compilation for even faster matching
-   [ ] Thread-safe variants
-   [ ] Logging/tracing hooks
-   [ ] Benchmarking suite
-   [ ] More value types (arrays, nested structs)
-   [ ] Rule builder API
-   [ ] vcpkg/Conan package

## Contributing

When contributing:

1. Follow existing code style (K&R-ish)
2. Add tests for new features
3. Update documentation
4. Ensure all tests pass
5. Check for memory leaks (valgrind, ASAN)
6. Update this document with design decisions
