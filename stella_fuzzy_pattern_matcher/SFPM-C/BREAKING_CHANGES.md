# Breaking Changes in SFPM-C

## Version 2.0 - Hook API Simplification

### What Changed

**REMOVED:** Single hook API (backward compatibility layer)

-   `sfpm_rule_set_before_hook()` - ❌ Removed
-   `sfpm_rule_set_after_hook()` - ❌ Removed

**USE INSTEAD:** Hook chaining API

-   `sfpm_rule_add_before_hook()` - ✅ Use this
-   `sfpm_rule_add_after_hook()` - ✅ Use this
-   `sfpm_rule_add_middleware_hook()` - ✅ New capability

### Migration Guide

#### Before (Old API - No longer supported)

```c
// This code will NOT compile anymore
sfpm_rule_set_before_hook(rule, my_hook, data);
sfpm_rule_set_after_hook(rule, cleanup_hook, NULL);
```

#### After (New API - Required)

```c
// Use the add functions instead
sfpm_rule_add_before_hook(rule, my_hook, data);
sfpm_rule_add_after_hook(rule, cleanup_hook, NULL);
```

### Why This Change?

1. **Simpler API**: One way to add hooks, not two
2. **More powerful**: Chain unlimited hooks, not just one
3. **Clearer semantics**: `add` implies you can call multiple times
4. **Reduced code**: Removed ~40 lines of compatibility code

### Benefits of New API

```c
/* You can now chain multiple hooks! */
sfpm_rule_add_before_hook(rule, authenticate, &auth);
sfpm_rule_add_before_hook(rule, validate, &val);
sfpm_rule_add_before_hook(rule, rate_limit, &limiter);

/* Middleware for wrapping behavior */
sfpm_rule_add_middleware_hook(rule, begin_transaction, &db);

/* Multiple after hooks */
sfpm_rule_add_after_hook(rule, commit_transaction, &db);
sfpm_rule_add_after_hook(rule, log_metrics, &logger);
```

### Execution Order

```
Before chain → Middleware chain → PAYLOAD → After chain
      ↓               ↓                         ↓
Abort if any    Abort if any            Always runs
returns false   returns false
```

### Quick Migration Checklist

-   [ ] Replace all `sfpm_rule_set_before_hook()` with `sfpm_rule_add_before_hook()`
-   [ ] Replace all `sfpm_rule_set_after_hook()` with `sfpm_rule_add_after_hook()`
-   [ ] Recompile your code
-   [ ] Run tests

That's it! The function signatures are identical, just the names changed.

### Documentation

-   See `README_HOOK_CHAINING.md` for full hook chaining guide
-   See `HOOK_CHAINING_QUICKREF.md` for quick reference
-   See examples:
    -   `examples/interpreter_hooks.c` - Basic hooks usage
    -   `examples/interpreter_hook_chaining.c` - Advanced chaining patterns

### Support

If you have questions or need help migrating, please open an issue on GitHub.
