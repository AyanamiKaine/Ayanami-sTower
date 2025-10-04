# SFPM Interpreter Caching Optimizations

## Overview

This example demonstrates **three caching strategies** to dramatically reduce SFPM overhead in interpreters while retaining the benefits of runtime modification and extensibility.

## Performance Results

Testing 1,000,000 iterations of `(100 + 50) * 2`:

| Approach                   | Time   | Overhead | Improvement       |
| -------------------------- | ------ | -------- | ----------------- |
| Switch (baseline)          | 0.004s | 1.0x     | -                 |
| **Direct cache** (no SFPM) | 0.011s | **2.8x** | **98%** reduction |
| **SFPM + Rule cache**      | 0.014s | **3.5x** | **97%** reduction |
| SFPM + Fact reuse          | 0.555s | 138.8x   | 70% reduction     |
| Naive SFPM (no cache)      | 1.9s   | 470x     | baseline          |

### 🎉 Key Achievement

Caching reduces overhead from **~470x to ~3.5x** while retaining SFPM's flexibility!

## Caching Strategies

### 1. Direct Function Pointer Cache

**How it works:**

```c
typedef struct {
    opcode_handler_fn handlers[OP_MAX];  // Direct array lookup
} direct_dispatch_cache_t;

// Register handlers
cache.handlers[OP_ADD] = op_add;

// Dispatch (O(1))
handler = cache.handlers[instr.op];
handler(vm, operand);
```

**Performance:** 2.8x overhead (vs 470x naive)

**Pros:**

-   ✅ Fastest possible approach (~71M iterations/sec)
-   ✅ O(1) dispatch
-   ✅ Runtime modification still possible
-   ✅ Minimal memory overhead

**Cons:**

-   ❌ Loses SFPM pattern matching
-   ❌ No multi-criteria rules
-   ❌ Can't use SFPM's priority system

**Best for:**

-   Static opcode sets
-   Maximum performance requirements
-   Simple dispatch (opcode → handler)

### 2. SFPM with Rule Cache (Recommended)

**How it works:**

```c
typedef struct {
    sfpm_rule_t *rules[OP_MAX];  // Opcode-indexed rules
} sfpm_rule_cache_t;

// Register rule
cache.rules[OP_ADD] = create_opcode_rule(OP_ADD, op_add);

// Direct rule execution (skip pattern matching)
rule = cache.rules[instr.op];
sfpm_rule_execute_payload(rule);
```

**Performance:** 3.5x overhead (vs 470x naive)

**Pros:**

-   ✅ Near-optimal performance (~71M iterations/sec)
-   ✅ Retains SFPM rule infrastructure
-   ✅ Hot-swapping support
-   ✅ Isolated testing
-   ✅ Best of both worlds

**Cons:**

-   ❌ Requires known opcode range
-   ❌ Memory overhead for sparse opcode spaces

**Best for:**

-   **Game scripting engines** 🎮
-   Runtime-extensible systems
-   Known opcode sets with hot-swapping
-   **This is the sweet spot for most use cases**

### 3. SFPM with Fact Reuse

**How it works:**

```c
// Create fact source ONCE
sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(2);

while (running) {
    // Reuse same fact source, just update values
    sfpm_dict_fact_source_add(facts, "opcode", sfpm_value_from_int(op));
    sfpm_match(rules, rule_count, facts, false);
}
```

**Performance:** 138.8x overhead (vs 470x naive)

**Pros:**

-   ✅ Eliminates allocation overhead
-   ✅ Full pattern matching preserved
-   ✅ Multi-criteria rules work
-   ✅ Dynamic dispatch logic

**Cons:**

-   ❌ Still performs full pattern matching
-   ❌ Slower than direct approaches
-   ❌ Marginal improvement

**Best for:**

-   Complex pattern matching scenarios
-   Multi-criteria rules (e.g., `opcode == ADD && mode == DEBUG`)
-   Dynamic dispatch based on multiple facts
-   Scenarios where full SFPM power is needed

## Implementation Patterns

### Pattern 1: Hybrid Approach (Recommended)

Combine caching with selective SFPM matching:

```c
// Fast path: Direct cache for common opcodes
if (instr.op < OP_MAX && cache.rules[instr.op]) {
    sfpm_rule_execute_payload(cache.rules[instr.op]);
}
// Slow path: Full SFPM for complex cases
else {
    sfpm_fact_source_t *facts = create_facts_for(instr);
    sfpm_match(all_rules, rule_count, facts, false);
}
```

**Benefits:**

-   Fast path for 99% of instructions
-   Fallback to full SFPM for edge cases
-   Best performance + maximum flexibility

### Pattern 2: Lazy Cache Population

Build cache on first use:

```c
sfpm_rule_t *get_or_create_rule(opcode_t op) {
    if (!cache.rules[op]) {
        // Cache miss - create and cache rule
        cache.rules[op] = create_opcode_rule(op, lookup_handler(op));
    }
    return cache.rules[op];
}
```

**Benefits:**

-   No upfront cost for unused opcodes
-   Sparse opcode spaces handled efficiently
-   JIT-like compilation feel

### Pattern 3: Versioned Cache

Invalidate cache when rules change:

```c
typedef struct {
    sfpm_rule_t *rules[OP_MAX];
    uint64_t version;
} versioned_cache_t;

void update_opcode(opcode_t op, opcode_handler_fn new_handler) {
    sfpm_rule_destroy(cache.rules[op]);
    cache.rules[op] = create_opcode_rule(op, new_handler);
    cache.version++;  // Invalidate cached lookups
}
```

## Memory Overhead Analysis

### Direct Cache

```
Memory: OP_MAX * sizeof(void*) = 128 * 8 = 1 KB
```

### SFPM Rule Cache

```
Memory: OP_MAX * sizeof(sfpm_rule_t*) + rule structures
      = 128 * 8 + N * ~100 bytes
      ≈ 1 KB + 700 bytes (for 7 opcodes)
      ≈ 1.7 KB total
```

### Naive SFPM (no cache)

```
Memory: Per-instruction fact allocation
      = 1M instructions * ~200 bytes
      = 200 MB temporary allocations!
```

**Caching eliminates ~200 MB of allocations per million instructions!**

## When to Use Each Strategy

### Use Direct Cache When:

-   ✅ Performance is critical (near-native speed needed)
-   ✅ Opcode set is static or rarely changes
-   ✅ Don't need pattern matching features
-   ✅ Simple opcode → handler dispatch

**Example:** Production game engine main loop

### Use SFPM + Rule Cache When:

-   ✅ Need runtime extensibility (plugins, modding)
-   ✅ Want hot-swapping capability
-   ✅ Opcode set is known but handlers may change
-   ✅ Want SFPM benefits with minimal overhead

**Example:** Game scripting language, configuration DSL

### Use SFPM + Fact Reuse When:

-   ✅ Complex multi-criteria dispatch
-   ✅ Rules depend on multiple facts
-   ✅ Dynamic dispatch logic needed
-   ✅ Full SFPM pattern matching required

**Example:** AI decision system, rule-based expert system

### Use Naive SFPM (no cache) When:

-   ✅ Prototyping/development
-   ✅ Maximum flexibility during iteration
-   ✅ Performance not critical
-   ✅ Rules change very frequently

**Example:** Research, experimentation, debugging

## Code Organization

```c
// 1. Define your opcode handlers
void op_add(vm_t *vm, int operand) { /* ... */ }

// 2. Choose caching strategy
sfpm_rule_cache_t cache;
sfpm_cache_init(&cache, &vm);

// 3. Execute with direct lookup
while (running) {
    instruction_t instr = program[pc++];
    sfpm_rule_t *rule = cache.rules[instr.op];
    sfpm_rule_execute_payload(rule);
}

// 4. Cleanup
sfpm_cache_destroy(&cache);
```

## Performance Tips

1. **Pre-allocate caches** before hot loops
2. **Use direct cache** for performance-critical sections
3. **Batch rule updates** rather than frequent changes
4. **Profile your specific workload** - results vary
5. **Consider hybrid approaches** for best of both worlds

## Comparison with Other Approaches

| Feature               | Switch | Direct Cache | SFPM Cache | Naive SFPM |
| --------------------- | ------ | ------------ | ---------- | ---------- |
| Overhead              | 1x     | 2.8x         | 3.5x       | 470x       |
| Runtime extensibility | ❌     | ✅           | ✅         | ✅         |
| Hot-swapping          | ❌     | ✅           | ✅         | ✅         |
| Pattern matching      | ❌     | ❌           | Limited    | ✅         |
| Memory efficient      | ✅     | ✅           | ✅         | ❌         |
| Isolated testing      | ❌     | ✅           | ✅         | ✅         |
| Opcode versioning     | ❌     | ✅           | ✅         | ✅         |

## Conclusion

**Rule cache** strikes the perfect balance:

-   **135x faster** than naive SFPM
-   **Only 3.5x slower** than switch statements
-   **Retains all SFPM benefits** (hot-swapping, testing, extensibility)

For most interpreter use cases, **SFPM with rule caching is the recommended approach**.

---

**See also:**

-   `interpreter_comparison.c` - Full SFPM vs switch comparison
-   `basic_example.c` - SFPM fundamentals
