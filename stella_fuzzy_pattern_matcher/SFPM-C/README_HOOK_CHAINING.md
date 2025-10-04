# SFPM Hook Chaining System

## Overview

The SFPM hook chaining system extends the basic hook functionality by allowing **multiple hooks to be attached to a single rule**. Hooks execute in the order they are added, enabling powerful composition patterns like authentication pipelines, observability stacks, and transaction management.

## Key Features

### Multiple Hooks Per Rule

-   **Before Hooks Chain**: Multiple hooks execute before the payload, in order
-   **After Hooks Chain**: Multiple hooks execute after the payload, in order
-   **Middleware Hooks**: Special hooks that wrap execution for cross-cutting concerns
-   **Early Abortion**: Any hook in the chain can abort execution by returning `false`
-   **Unlimited Hooks**: Add as many hooks as needed to each chain

### Execution Flow

```
┌─────────────────────────────────────────────────────┐
│ HOOK EXECUTION ORDER                                │
├─────────────────────────────────────────────────────┤
│ 1. Before hook chain (in order added)              │
│ 2. Middleware hook chain (in order added)          │
│ 3. ★ PAYLOAD EXECUTION ★                           │
│ 4. After hook chain (in order added)               │
└─────────────────────────────────────────────────────┘

Note: If any before/middleware hook returns false,
      execution aborts immediately!
```

## API Reference

### Adding Hooks to Chains

```c
/**
 * Add a before hook to the chain
 * Executes before payload, can abort by returning false
 */
bool sfpm_rule_add_before_hook(sfpm_rule_t *rule,
                                sfpm_hook_fn hook,
                                void *user_data);

/**
 * Add an after hook to the chain
 * Executes after payload completes
 */
bool sfpm_rule_add_after_hook(sfpm_rule_t *rule,
                               sfpm_hook_fn hook,
                               void *user_data);

/**
 * Add a middleware hook to the chain
 * Executes between before hooks and after hooks, wraps payload
 */
bool sfpm_rule_add_middleware_hook(sfpm_rule_t *rule,
                                    sfpm_hook_fn hook,
                                    void *user_data);
```

### Hook Management

```c
/**
 * Clear all hooks from a rule
 * Removes single hooks and all hook chains
 */
void sfpm_rule_clear_hooks(sfpm_rule_t *rule);

/**
 * Get counts of hooks in each chain
 */
int sfpm_rule_get_before_hook_count(const sfpm_rule_t *rule);
int sfpm_rule_get_after_hook_count(const sfpm_rule_t *rule);
int sfpm_rule_get_middleware_hook_count(const sfpm_rule_t *rule);
```

## Usage Patterns

### 1. Security Pipeline

Build a complete security stack with authentication, authorization, and validation:

```c
sfpm_rule_t *rule = /* ... */;

/* Add security hooks in order */
sfpm_rule_add_before_hook(rule, authenticate_user, &auth_ctx);
sfpm_rule_add_before_hook(rule, check_authorization, &authz_ctx);
sfpm_rule_add_before_hook(rule, validate_input, &validation_ctx);

/* If ANY hook returns false, payload won't execute */
```

**Execution:**

```
1. authenticate_user()  -> returns true (user logged in)
2. check_authorization() -> returns true (has permission)
3. validate_input()      -> returns true (data valid)
4. [PAYLOAD EXECUTES]
```

**Abortion Example:**

```
1. authenticate_user()  -> returns FALSE (not logged in)
2. [EXECUTION ABORTED - remaining hooks and payload skipped]
```

### 2. Observability Stack

Layer logging, metrics, and tracing:

```c
sfpm_rule_t *rule = /* ... */;

/* Before hooks: Start monitoring */
sfpm_rule_add_before_hook(rule, log_entry, "operation_start");
sfpm_rule_add_before_hook(rule, start_timer, &timer_ctx);
sfpm_rule_add_before_hook(rule, increment_counter, &counter_ctx);

/* After hooks: Record results */
sfpm_rule_add_after_hook(rule, stop_timer, &timer_ctx);
sfpm_rule_add_after_hook(rule, record_metrics, &metrics_ctx);
sfpm_rule_add_after_hook(rule, log_exit, "operation_complete");
```

**Output:**

```
[LOG] operation_start
[TIMER] Started at 1234567890
[COUNTER] Incremented to 42
[PAYLOAD EXECUTES]
[TIMER] Took 125 microseconds
[METRICS] Recorded execution time
[LOG] operation_complete
```

### 3. Transaction Management

Wrap operations in transaction boundaries:

```c
sfpm_rule_t *rule = /* ... */;

/* Middleware hooks act as transaction boundaries */
sfpm_rule_add_middleware_hook(rule, begin_transaction, &tx_ctx);
sfpm_rule_add_middleware_hook(rule, acquire_lock, &lock_ctx);

/* Payload executes here */

sfpm_rule_add_after_hook(rule, release_lock, &lock_ctx);
sfpm_rule_add_after_hook(rule, commit_transaction, &tx_ctx);
```

### 4. Rate Limiting

Prevent excessive operations:

```c
typedef struct {
    int operations_per_second;
    int current_count;
    long long last_reset_time;
} rate_limiter_t;

static bool rate_limit_hook(void *user_data, void *payload_data) {
    rate_limiter_t *limiter = (rate_limiter_t *)user_data;

    /* Reset counter every second */
    long long now = get_time_microseconds();
    if (now - limiter->last_reset_time > 1000000) {
        limiter->current_count = 0;
        limiter->last_reset_time = now;
    }

    /* Check limit */
    if (limiter->current_count >= limiter->operations_per_second) {
        printf("[RATE_LIMIT] Exceeded limit, aborting\n");
        return false;  /* Abort! */
    }

    limiter->current_count++;
    return true;
}

/* Usage */
rate_limiter_t limiter = { .operations_per_second = 100 };
sfpm_rule_add_before_hook(rule, rate_limit_hook, &limiter);
```

### 5. Caching Layer

Implement read-through caching:

```c
typedef struct {
    cache_t *cache;
    bool cache_hit;
} cache_ctx_t;

static bool check_cache_hook(void *user_data, void *payload_data) {
    cache_ctx_t *ctx = (cache_ctx_t *)user_data;

    void *cached = cache_get(ctx->cache, get_cache_key(payload_data));
    if (cached) {
        printf("[CACHE] Hit! Skipping payload\n");
        /* Use cached value directly */
        ctx->cache_hit = true;
        return false;  /* Skip payload execution */
    }

    ctx->cache_hit = false;
    return true;  /* Execute payload to compute */
}

static bool update_cache_hook(void *user_data, void *payload_data) {
    cache_ctx_t *ctx = (cache_ctx_t *)user_data;

    if (!ctx->cache_hit) {
        /* Payload was executed, cache the result */
        cache_put(ctx->cache, get_cache_key(payload_data),
                  get_result(payload_data));
        printf("[CACHE] Updated with fresh value\n");
    }

    return true;
}

/* Usage */
cache_ctx_t ctx = { .cache = my_cache };
sfpm_rule_add_before_hook(rule, check_cache_hook, &ctx);
sfpm_rule_add_after_hook(rule, update_cache_hook, &ctx);
```

### 6. Debug Instrumentation

Add comprehensive debugging without modifying core code:

```c
sfpm_rule_t *rule = /* ... */;

#ifdef DEBUG
    /* Only in debug builds */
    sfpm_rule_add_before_hook(rule, print_stack_trace, NULL);
    sfpm_rule_add_before_hook(rule, validate_invariants, &state);
    sfpm_rule_add_before_hook(rule, log_parameters, NULL);

    sfpm_rule_add_after_hook(rule, log_result, NULL);
    sfpm_rule_add_after_hook(rule, verify_postconditions, &state);
    sfpm_rule_add_after_hook(rule, check_memory_leaks, NULL);
#endif
```

## Performance Considerations

### Overhead Per Hook

-   **Before hook**: ~1-2 µs (function call + bool check)
-   **Middleware hook**: ~1-2 µs (function call + bool check)
-   **After hook**: ~1-2 µs (function call)

### Measured Performance (from Demo 2)

```
Hook chain: auth -> validation -> timer -> [PAYLOAD] -> metrics
Total overhead: ~42 µs for entire chain
Individual hooks: ~10 µs each
```

### Optimization Tips

1. **Order matters**: Put cheap checks first (auth before expensive validation)
2. **Limit chain length**: Each hook adds overhead, keep chains < 10 hooks
3. **Use abort early**: Failed auth should return false immediately
4. **Conditional compilation**: Remove hooks in release builds if not needed
5. **Batch operations**: Apply hooks at coarse granularity, not per-item

### When to Use Hook Chaining

✅ **Good for:**

-   API endpoints (request/response pipelines)
-   Database operations (transaction management)
-   Security-critical operations (auth → authz → audit)
-   Development/debugging instrumentation
-   Plugin architectures

❌ **Avoid for:**

-   Tight inner loops (millions of iterations)
-   Real-time critical paths (audio/video processing)
-   Simple operations that don't need complexity

## Architecture Benefits

### Separation of Concerns

Core business logic remains clean:

```c
/* Business logic - no security/logging clutter */
void process_payment(void *user_data) {
    payment_t *payment = (payment_t *)user_data;
    charge_card(payment->card, payment->amount);
}

/* Security/monitoring added via hooks */
sfpm_rule_add_before_hook(rule, authenticate, &auth);
sfpm_rule_add_before_hook(rule, authorize, &authz);
sfpm_rule_add_before_hook(rule, audit_log, &audit);
sfpm_rule_add_after_hook(rule, record_metrics, &metrics);
```

### Composability

Build reusable hook libraries:

```c
/* security_hooks.h */
void attach_security_pipeline(sfpm_rule_t *rule, security_config_t *cfg) {
    sfpm_rule_add_before_hook(rule, authenticate, cfg->auth);
    sfpm_rule_add_before_hook(rule, authorize, cfg->authz);
    sfpm_rule_add_before_hook(rule, validate, cfg->validation);
}

/* observability_hooks.h */
void attach_observability(sfpm_rule_t *rule, obs_config_t *cfg) {
    sfpm_rule_add_before_hook(rule, start_span, cfg->tracer);
    sfpm_rule_add_after_hook(rule, end_span, cfg->tracer);
    sfpm_rule_add_after_hook(rule, record_metrics, cfg->metrics);
}

/* Usage - compose multiple concerns */
attach_security_pipeline(rule, &security_cfg);
attach_observability(rule, &obs_cfg);
```

### Dynamic Reconfiguration

Add/remove hooks at runtime:

```c
/* Development mode: Add debugging hooks */
if (dev_mode) {
    sfpm_rule_add_before_hook(rule, log_verbose, NULL);
    sfpm_rule_add_after_hook(rule, dump_state, NULL);
}

/* A/B testing: Different metrics collection */
if (user_in_experiment_group(user_id)) {
    sfpm_rule_add_after_hook(rule, metrics_experiment, &exp_ctx);
} else {
    sfpm_rule_add_after_hook(rule, metrics_control, &ctl_ctx);
}

/* Clean up when done */
sfpm_rule_clear_hooks(rule);  /* Remove all hooks */
```

## Best Practices

### 1. Order Hooks by Cost

Put cheap checks first to fail fast:

```c
/* GOOD: Cheap checks first */
sfpm_rule_add_before_hook(rule, check_cache, &cache);      /* Fast: O(1) lookup */
sfpm_rule_add_before_hook(rule, validate_auth, &auth);     /* Medium: token check */
sfpm_rule_add_before_hook(rule, database_check, &db);      /* Slow: DB query */

/* BAD: Expensive operation first */
sfpm_rule_add_before_hook(rule, database_check, &db);      /* Slow! */
sfpm_rule_add_before_hook(rule, check_cache, &cache);      /* Wasted if DB fails */
```

### 2. Use Middleware for Symmetric Operations

Middleware is ideal for begin/end pairs:

```c
/* Middleware hook called BEFORE payload */
static bool transaction_begin(void *user_data, void *payload_data) {
    db_begin_transaction(user_data);
    return true;
}

/* After hook for commit */
static bool transaction_commit(void *user_data, void *payload_data) {
    db_commit_transaction(user_data);
    return true;
}

sfpm_rule_add_middleware_hook(rule, transaction_begin, &db);
sfpm_rule_add_after_hook(rule, transaction_commit, &db);
```

### 3. Document Hook Dependencies

If hooks depend on each other, document it:

```c
/*
 * Hook chain for user operations:
 * 1. authenticate - MUST be first (sets user_id)
 * 2. load_profile - DEPENDS on user_id from step 1
 * 3. check_quota  - DEPENDS on profile from step 2
 */
sfpm_rule_add_before_hook(rule, authenticate, &auth_ctx);
sfpm_rule_add_before_hook(rule, load_profile, &profile_ctx);
sfpm_rule_add_before_hook(rule, check_quota, &quota_ctx);
```

### 4. Handle Errors Gracefully

Return clear error messages when aborting:

```c
static bool validate_input(void *user_data, void *payload_data) {
    if (!is_valid(payload_data)) {
        log_error("Validation failed: %s", get_error(payload_data));
        set_error_response(payload_data, "Invalid input");
        return false;  /* Abort with clear error */
    }
    return true;
}
```

### 5. Use Metrics to Monitor Chains

Track hook performance:

```c
typedef struct {
    const char *hook_name;
    long long total_time_us;
    int invocation_count;
} hook_metrics_t;

static bool metered_hook(void *user_data, void *payload_data) {
    hook_metrics_t *metrics = (hook_metrics_t *)user_data;

    long long start = get_time_microseconds();
    bool result = actual_hook_logic(payload_data);
    long long elapsed = get_time_microseconds() - start;

    metrics->total_time_us += elapsed;
    metrics->invocation_count++;

    if (elapsed > 1000) {  /* Warn if slow */
        printf("[METRICS] Hook '%s' took %lld us (slow!)\n",
               metrics->hook_name, elapsed);
    }

    return result;
}
```

## Running the Example

The `interpreter_hook_chaining.c` example demonstrates all these patterns:

```bash
cd build/Release
./sfpm_hook_chaining
```

The example shows:

1. **Multiple before/after hooks** - Logging chain with 2 before, 2 after hooks
2. **Security pipeline** - Auth → validation → metrics complete stack
3. **Middleware hooks** - Transaction boundaries wrapping execution
4. **Early abortion** - Hook limiting operations to 3 max
5. **Authentication failure** - Blocked access due to failed auth hook

## Summary

Hook chaining transforms SFPM from a simple pattern matcher into a powerful **aspect-oriented programming framework**. Use it to:

-   ✨ Build security pipelines without cluttering business logic
-   📊 Layer observability (logging, metrics, tracing) seamlessly
-   🔒 Enforce policies through composable validation chains
-   🎯 Create reusable cross-cutting concern libraries
-   🔧 Add debugging instrumentation dynamically
-   ⚡ Maintain clean, testable core logic

The hook chaining system enables unlimited composition possibilities for production-ready applications!
