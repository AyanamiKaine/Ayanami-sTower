# SFPM-C Hook API Simplification - Change Summary

## Date

October 4, 2025

## What Was Done

Removed backward compatibility layer for single before/after hooks in favor of unified hook chaining API.

## Files Modified

### Core Implementation (src/)

1. **src/rule.c**
    - ❌ Removed: `before_hook`, `before_hook_user_data`, `after_hook`, `after_hook_user_data` fields from `struct sfpm_rule`
    - ❌ Removed: `sfpm_rule_set_before_hook()` function
    - ❌ Removed: `sfpm_rule_set_after_hook()` function
    - ✅ Simplified: `sfpm_rule_execute_payload()` - removed single hook execution
    - ✅ Simplified: `sfpm_rule_create()` - removed single hook initialization
    - ✅ Simplified: `sfpm_rule_clear_hooks()` - removed single hook clearing
    - ✅ Simplified: `sfpm_rule_get_before_hook_count()` - removed single hook count
    - ✅ Simplified: `sfpm_rule_get_after_hook_count()` - removed single hook count
    - **Lines removed**: ~40 lines of compatibility code

### Public API (include/)

2. **include/sfpm/rule.h**
    - ❌ Removed: `sfpm_rule_set_before_hook()` declaration
    - ❌ Removed: `sfpm_rule_set_after_hook()` declaration
    - **Lines removed**: ~24 lines of documentation and declarations

### Examples (examples/)

3. **examples/interpreter_hooks.c**
    - ✅ Updated: `interpreter_set_hooks()` to use `sfpm_rule_add_before_hook()`
    - ✅ Updated: `interpreter_set_hooks()` to use `sfpm_rule_add_after_hook()`
    - **Lines changed**: 2 function calls

### Documentation (\*.md)

4. **BREAKING_CHANGES.md** (NEW)

    - ✅ Created migration guide for users
    - Explains old API → new API transition
    - Provides quick migration checklist

5. **HOOK_CHAINING_QUICKREF.md**

    - ✅ Updated: Removed "Backward Compatibility" section
    - ✅ Updated: Simplified execution order diagram

6. **README_HOOK_CHAINING.md**

    - ✅ Updated: Removed backward compatibility section from API reference
    - ✅ Updated: Simplified execution flow diagram
    - ✅ Updated: Removed comparison table with single hooks
    - ✅ Updated: Removed mention of "100% backward compatibility"

7. **HOOK_CHAINING_SUMMARY.md**
    - ✅ Updated: Removed "Backward Compatibility" section

## API Changes

### Removed Functions (Breaking Change)

```c
// ❌ REMOVED - Use sfpm_rule_add_before_hook() instead
void sfpm_rule_set_before_hook(sfpm_rule_t *rule,
                                sfpm_hook_fn hook,
                                void *user_data);

// ❌ REMOVED - Use sfpm_rule_add_after_hook() instead
void sfpm_rule_set_after_hook(sfpm_rule_t *rule,
                               sfpm_hook_fn hook,
                               void *user_data);
```

### Current API (Use This)

```c
// ✅ USE THIS - Add hooks to chain
bool sfpm_rule_add_before_hook(sfpm_rule_t *rule,
                                sfpm_hook_fn hook,
                                void *user_data);

bool sfpm_rule_add_after_hook(sfpm_rule_t *rule,
                               sfpm_hook_fn hook,
                               void *user_data);

bool sfpm_rule_add_middleware_hook(sfpm_rule_t *rule,
                                    sfpm_hook_fn hook,
                                    void *user_data);
```

## Execution Order Changes

### Before (with backward compatibility)

```
Single before → Chain before → Middleware → PAYLOAD → Chain after → Single after
```

### After (simplified)

```
Chain before → Middleware → PAYLOAD → Chain after
```

## Code Size Impact

-   **Removed**: ~64 lines total
    -   40 lines from rule.c
    -   24 lines from rule.h
-   **Added**: 1 new documentation file (BREAKING_CHANGES.md)
-   **Net effect**: Cleaner, simpler codebase

## Migration Impact

**Who is affected**: Anyone using `sfpm_rule_set_before_hook()` or `sfpm_rule_set_after_hook()`

**Migration effort**: Minimal - just rename function calls

-   `set_before_hook` → `add_before_hook`
-   `set_after_hook` → `add_after_hook`
-   Function signatures are identical

**Example**:

```c
// Before
sfpm_rule_set_before_hook(rule, my_hook, data);

// After (just rename!)
sfpm_rule_add_before_hook(rule, my_hook, data);
```

## Testing Results

✅ **All tests passing**: 2/2 (100%)

-   sfpm_basic: PASSED
-   sfpm_advanced: PASSED

✅ **All examples building**: 8/8

-   sfpm_comparison
-   sfpm_cached
-   sfpm_tiered
-   sfpm_game_ai
-   sfpm_hooks ← Updated to use new API
-   sfpm_hook_chaining ← Already using new API
-   sfpm_example
-   sfpm*test*\*

✅ **Examples running correctly**:

-   interpreter_hooks.c - All 4 demos working
-   interpreter_hook_chaining.c - All 5 demos working

## Benefits

1. **Simpler API**: One way to add hooks, not two
2. **Clearer semantics**: `add` implies multiple calls possible
3. **Less code**: 64 lines removed
4. **More maintainable**: No duplicate execution paths
5. **Better docs**: Less confusion about which API to use

## Version Bump Recommendation

**Suggested**: 2.0.0 (Major version bump due to breaking changes)

## Rollback Procedure

If needed, restore these commits:

1. Restore `src/rule.c` struct fields (before_hook, after_hook, user_data)
2. Restore `src/rule.c` functions (set_before_hook, set_after_hook)
3. Restore `include/sfpm/rule.h` declarations
4. Restore single hook execution in sfpm_rule_execute_payload()
5. Restore hook counting in get\_\*\_hook_count() functions
6. Update examples/interpreter*hooks.c back to set*\* functions

## Notes

-   The new API is more powerful (unlimited hooks vs 1)
-   The new API is equally simple for single-hook use cases
-   Migration is a simple find-replace operation
-   No semantic changes to hook behavior
-   All existing hook chains continue to work identically

---

**Summary**: Successfully removed 64 lines of backward compatibility code while maintaining all functionality through the more powerful hook chaining API. All tests pass, all examples work.
