# SFPM Hook Chaining - Quick Reference

## Adding Hooks

```c
/* Add hooks to chains (execute in order added) */
sfpm_rule_add_before_hook(rule, auth_check, &auth_ctx);        /* Runs 1st */
sfpm_rule_add_before_hook(rule, validate, &val_ctx);           /* Runs 2nd */
sfpm_rule_add_middleware_hook(rule, begin_tx, &tx_ctx);        /* Runs 3rd */
/* [PAYLOAD EXECUTES] */
sfpm_rule_add_after_hook(rule, commit_tx, &tx_ctx);            /* Runs 4th */
sfpm_rule_add_after_hook(rule, log_result, NULL);              /* Runs 5th */
```

## Hook Function Signature

```c
/* Return true to continue, false to abort (before/middleware only) */
static bool my_hook(void *hook_user_data, void *payload_user_data) {
    /* Your logic here */
    return true;  /* Continue execution */
}
```

## Execution Order

```
Before chain → Middleware chain → PAYLOAD → After chain
      ↓               ↓                         ↓
Abort if any    Abort if any            Always runs
returns false   returns false
```

## Common Patterns

### Security Pipeline

```c
sfpm_rule_add_before_hook(rule, authenticate, &auth);
sfpm_rule_add_before_hook(rule, authorize, &authz);
sfpm_rule_add_before_hook(rule, validate, &val);
```

### Observability

```c
sfpm_rule_add_before_hook(rule, start_timer, &timer);
sfpm_rule_add_after_hook(rule, record_metrics, &metrics);
sfpm_rule_add_after_hook(rule, log_result, &logger);
```

### Transaction

```c
sfpm_rule_add_middleware_hook(rule, begin_tx, &db);
sfpm_rule_add_after_hook(rule, commit_tx, &db);
```

### Rate Limiting

```c
sfpm_rule_add_before_hook(rule, check_rate_limit, &limiter);
```

### Caching

```c
sfpm_rule_add_before_hook(rule, check_cache, &cache);  /* May abort if hit */
sfpm_rule_add_after_hook(rule, update_cache, &cache);
```

## Hook Management

```c
/* Get counts */
int before_count = sfpm_rule_get_before_hook_count(rule);
int after_count = sfpm_rule_get_after_hook_count(rule);
int middleware_count = sfpm_rule_get_middleware_hook_count(rule);

/* Clear all hooks */
sfpm_rule_clear_hooks(rule);
```

## Performance

-   **Overhead per hook**: ~1-2 µs
-   **Memory per hook**: 24 bytes
-   **Recommended chain length**: < 10 hooks
-   **Zero cost**: When no hooks attached

## Best Practices

✅ **DO:**

-   Order hooks cheapest-first (fail fast)
-   Use middleware for begin/end pairs
-   Return false early on validation failures
-   Document hook dependencies
-   Keep chains < 10 hooks

❌ **DON'T:**

-   Put expensive ops first
-   Forget to handle NULL in hook functions
-   Use in tight inner loops (millions/sec)
-   Modify global state without locks

## Examples

```bash
# See basic hooks
./build/Release/sfpm_hooks.exe

# See hook chaining
./build/Release/sfpm_hook_chaining.exe
```

## Documentation

-   `README_HOOKS.md` - Basic hooks guide
-   `README_HOOK_CHAINING.md` - Complete chaining guide
-   `HOOK_CHAINING_SUMMARY.md` - Implementation details

## Quick Copy-Paste Templates

### Security Template

```c
static bool authenticate(void *hook_data, void *payload_data) {
    auth_ctx_t *ctx = (auth_ctx_t *)hook_data;
    if (!ctx->is_authenticated) {
        log_error("Authentication failed");
        return false;  /* Abort */
    }
    return true;
}

/* Usage */
sfpm_rule_add_before_hook(rule, authenticate, &auth_ctx);
sfpm_rule_add_before_hook(rule, authorize, &authz_ctx);
```

### Metrics Template

```c
static bool start_metrics(void *hook_data, void *payload_data) {
    metrics_t *m = (metrics_t *)hook_data;
    m->start_time = get_time_microseconds();
    return true;
}

static bool end_metrics(void *hook_data, void *payload_data) {
    metrics_t *m = (metrics_t *)hook_data;
    m->duration_us = get_time_microseconds() - m->start_time;
    m->count++;
    return true;
}

/* Usage */
sfpm_rule_add_before_hook(rule, start_metrics, &metrics);
sfpm_rule_add_after_hook(rule, end_metrics, &metrics);
```

### Transaction Template

```c
static bool begin_tx(void *hook_data, void *payload_data) {
    db_t *db = (db_t *)hook_data;
    db_begin_transaction(db);
    return true;
}

static bool commit_tx(void *hook_data, void *payload_data) {
    db_t *db = (db_t *)hook_data;
    db_commit_transaction(db);
    return true;
}

/* Usage */
sfpm_rule_add_middleware_hook(rule, begin_tx, &db);
sfpm_rule_add_after_hook(rule, commit_tx, &db);
```

---

**Need more help?** See full documentation in `README_HOOK_CHAINING.md`
