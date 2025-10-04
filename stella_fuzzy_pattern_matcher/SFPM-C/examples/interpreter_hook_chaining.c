/**
 * interpreter_hook_chaining.c
 * Demonstrates SFPM's hook chaining system with multiple before/after/middleware hooks.
 * 
 * This example shows:
 * 1. Multiple before hooks executing in sequence
 * 2. Multiple after hooks executing in sequence
 * 3. Middleware hooks for wrapping behavior
 * 4. Early abortion in hook chains
 * 5. Practical use case: authentication -> logging -> validation -> metrics
 */

#include <sfpm/sfpm.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>
#include <stdint.h>

#ifdef _WIN32
#include <windows.h>
#else
#include <sys/time.h>
#endif

/* ============================================================================
 * TIMING UTILITIES
 * ============================================================================ */

static long long get_time_microseconds(void) {
#ifdef _WIN32
    static LARGE_INTEGER frequency = {0};
    LARGE_INTEGER counter;
    
    if (frequency.QuadPart == 0) {
        QueryPerformanceFrequency(&frequency);
    }
    
    QueryPerformanceCounter(&counter);
    return (long long)((counter.QuadPart * 1000000LL) / frequency.QuadPart);
#else
    struct timeval tv;
    gettimeofday(&tv, NULL);
    return (long long)(tv.tv_sec * 1000000LL + tv.tv_usec);
#endif
}

/* ============================================================================
 * VIRTUAL MACHINE (Simplified)
 * ============================================================================ */

typedef enum {
    OP_PUSH = 1,
    OP_ADD,
    OP_MUL,
    OP_PRINT,
    OP_HALT,
} opcode_t;

typedef struct {
    int stack[256];
    int sp;
    int pc;
    uint8_t *program;
    int program_size;
    bool halted;
    
    /* Security context */
    int user_id;
    bool authenticated;
    int permission_level;
} vm_t;

typedef struct {
    vm_t *vm;
    opcode_t opcode;
    int operand;
    long long start_time;
} opcode_context_t;

static opcode_context_t g_opcode_contexts[128];

static void vm_init(vm_t *vm, uint8_t *program, int size) {
    memset(vm, 0, sizeof(vm_t));
    vm->program = program;
    vm->program_size = size;
    vm->sp = -1;
    vm->user_id = 0;
    vm->authenticated = false;
    vm->permission_level = 0;
}

static void vm_push(vm_t *vm, int value) {
    if (vm->sp < 255) {
        vm->stack[++vm->sp] = value;
    }
}

static int vm_pop(vm_t *vm) {
    return (vm->sp >= 0) ? vm->stack[vm->sp--] : 0;
}

/* ============================================================================
 * OPCODE HANDLERS
 * ============================================================================ */

static void op_push(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    vm_push(ctx->vm, ctx->operand);
}

static void op_add(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    int b = vm_pop(ctx->vm);
    int a = vm_pop(ctx->vm);
    vm_push(ctx->vm, a + b);
}

static void op_mul(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    int b = vm_pop(ctx->vm);
    int a = vm_pop(ctx->vm);
    vm_push(ctx->vm, a * b);
}

static void op_print(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    if (ctx->vm->sp >= 0) {
        printf("    [PAYLOAD] Result: %d\n", ctx->vm->stack[ctx->vm->sp]);
    }
}

static void op_halt(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    ctx->vm->halted = true;
}

/* ============================================================================
 * HOOK CHAIN EXAMPLES
 * ============================================================================ */

/* --- Authentication Hook --- */
static bool auth_before_hook(void *hook_data, void *payload_data) {
    const char *label = (const char *)hook_data;
    opcode_context_t *ctx = (opcode_context_t *)payload_data;
    
    printf("  [%s] Checking authentication...\n", label);
    
    if (!ctx->vm->authenticated) {
        printf("  [%s] DENIED: User not authenticated\n", label);
        return false;  /* Abort execution */
    }
    
    printf("  [%s] Authenticated as user %d\n", label, ctx->vm->user_id);
    return true;
}

/* --- Logging Hooks --- */
static bool logging_before_hook_1(void *hook_data, void *payload_data) {
    printf("  [LOG-1] Before hook executing\n");
    return true;
}

static bool logging_before_hook_2(void *hook_data, void *payload_data) {
    opcode_context_t *ctx = (opcode_context_t *)payload_data;
    const char *opcodes[] = {"???", "PUSH", "ADD", "MUL", "PRINT", "HALT"};
    const char *name = (ctx->opcode > 0 && ctx->opcode <= OP_HALT) 
                       ? opcodes[ctx->opcode] : "UNKNOWN";
    printf("  [LOG-2] Executing opcode: %s\n", name);
    return true;
}

static bool logging_after_hook_1(void *hook_data, void *payload_data) {
    printf("  [LOG-1] After hook executing\n");
    return true;
}

static bool logging_after_hook_2(void *hook_data, void *payload_data) {
    opcode_context_t *ctx = (opcode_context_t *)payload_data;
    printf("  [LOG-2] Stack pointer: %d\n", ctx->vm->sp);
    return true;
}

/* --- Validation Hook --- */
static bool validation_before_hook(void *hook_data, void *payload_data) {
    opcode_context_t *ctx = (opcode_context_t *)payload_data;
    
    printf("  [VALIDATION] Checking preconditions...\n");
    
    /* Check stack overflow */
    if (ctx->vm->sp > 250) {
        printf("  [VALIDATION] DENIED: Stack near overflow\n");
        return false;
    }
    
    /* Check permissions for certain opcodes */
    if (ctx->opcode == OP_PRINT && ctx->vm->permission_level < 1) {
        printf("  [VALIDATION] DENIED: Insufficient permissions for PRINT\n");
        return false;
    }
    
    printf("  [VALIDATION] Preconditions satisfied\n");
    return true;
}

/* --- Performance Metrics Hooks --- */
typedef struct {
    int total_operations;
    long long total_time_us;
} metrics_t;

static bool metrics_before_hook(void *hook_data, void *payload_data) {
    opcode_context_t *ctx = (opcode_context_t *)payload_data;
    ctx->start_time = get_time_microseconds();
    printf("  [METRICS] Starting timer\n");
    return true;
}

static bool metrics_after_hook(void *hook_data, void *payload_data) {
    metrics_t *metrics = (metrics_t *)hook_data;
    opcode_context_t *ctx = (opcode_context_t *)payload_data;
    
    long long elapsed = get_time_microseconds() - ctx->start_time;
    metrics->total_operations++;
    metrics->total_time_us += elapsed;
    
    printf("  [METRICS] Execution time: %lld us (Total ops: %d, Avg: %.2f us)\n",
           elapsed, metrics->total_operations,
           (double)metrics->total_time_us / metrics->total_operations);
    return true;
}

/* --- Middleware Hook (Transaction wrapper) --- */
static bool transaction_middleware_hook(void *hook_data, void *payload_data) {
    const char *phase = (const char *)hook_data;
    printf("  [TRANSACTION-%s] Transaction boundary\n", phase);
    return true;
}

/* --- Early Abortion Demo --- */
static int abort_counter = 0;

static bool abort_after_3_hook(void *hook_data, void *payload_data) {
    abort_counter++;
    printf("  [ABORT-DEMO] Operation count: %d\n", abort_counter);
    
    if (abort_counter >= 4) {
        printf("  [ABORT-DEMO] Reached limit (3), aborting!\n");
        return false;
    }
    
    return true;
}

/* ============================================================================
 * INTERPRETER
 * ============================================================================ */

#define MAX_OPCODES 11

typedef struct {
    sfpm_rule_t *rules[MAX_OPCODES];
    int rule_count;
    vm_t *vm;
} interpreter_t;

static void interpreter_init(interpreter_t *interp, vm_t *vm) {
    interp->vm = vm;
    interp->rule_count = 0;
    memset(interp->rules, 0, sizeof(interp->rules));
}

static void interpreter_destroy(interpreter_t *interp) {
    for (int i = 0; i < interp->rule_count; i++) {
        if (interp->rules[i]) {
            sfpm_rule_destroy(interp->rules[i]);
        }
    }
}

static void interpreter_register_opcode(interpreter_t *interp,
                                       opcode_t opcode,
                                       sfpm_payload_fn handler) {
    sfpm_criteria_t **criterias = malloc(sizeof(sfpm_criteria_t*) * 1);
    criterias[0] = sfpm_criteria_create("opcode", SFPM_OP_EQUAL, sfpm_value_from_int(opcode));
    
    char name[64];
    snprintf(name, sizeof(name), "opcode_%d", opcode);
    
    sfpm_rule_t *rule = sfpm_rule_create(criterias, 1, handler, &g_opcode_contexts[opcode], name);
    
    interp->rules[interp->rule_count++] = rule;
}

static void interpreter_run(interpreter_t *interp) {
    vm_t *vm = interp->vm;
    
    while (vm->pc < vm->program_size && !vm->halted) {
        opcode_t opcode = (opcode_t)vm->program[vm->pc++];
        int operand = 0;
        
        if (opcode == OP_PUSH && vm->pc < vm->program_size) {
            operand = vm->program[vm->pc++];
        }
        
        g_opcode_contexts[opcode].vm = vm;
        g_opcode_contexts[opcode].opcode = opcode;
        g_opcode_contexts[opcode].operand = operand;
        
        sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(1);
        sfpm_dict_fact_source_add(facts, "opcode", sfpm_value_from_int(opcode));
        
        sfpm_match(interp->rules, interp->rule_count, facts, false);
        
        sfpm_fact_source_destroy(facts);
    }
}

/* ============================================================================
 * DEMO SCENARIOS
 * ============================================================================ */

static void demo_multiple_before_after_hooks(void) {
    printf("\n");
    printf("+----------------------------------------------------------------+\n");
    printf("| DEMO 1: MULTIPLE BEFORE/AFTER HOOKS IN CHAIN                  |\n");
    printf("| Shows hooks executing in order: LOG-1 -> LOG-2 -> PAYLOAD     |\n");
    printf("+----------------------------------------------------------------+\n\n");
    
    uint8_t program[] = {
        OP_PUSH, 10,
        OP_PUSH, 5,
        OP_ADD,
        OP_PRINT,
        OP_HALT
    };
    
    vm_t vm;
    vm_init(&vm, program, sizeof(program));
    vm.authenticated = true;  /* Allow execution */
    vm.user_id = 123;
    vm.permission_level = 2;
    
    interpreter_t interp;
    interpreter_init(&interp, &vm);
    
    interpreter_register_opcode(&interp, OP_PUSH, op_push);
    interpreter_register_opcode(&interp, OP_ADD, op_add);
    interpreter_register_opcode(&interp, OP_PRINT, op_print);
    interpreter_register_opcode(&interp, OP_HALT, op_halt);
    
    /* Add multiple hooks to the PRINT opcode */
    sfpm_rule_t *print_rule = interp.rules[3];  /* PRINT is the 4th registered */
    
    printf("Adding multiple before hooks...\n");
    sfpm_rule_add_before_hook(print_rule, logging_before_hook_1, NULL);
    sfpm_rule_add_before_hook(print_rule, logging_before_hook_2, NULL);
    
    printf("Adding multiple after hooks...\n");
    sfpm_rule_add_after_hook(print_rule, logging_after_hook_1, NULL);
    sfpm_rule_add_after_hook(print_rule, logging_after_hook_2, NULL);
    
    printf("\n=== Hook counts ===\n");
    printf("Before hooks: %d\n", sfpm_rule_get_before_hook_count(print_rule));
    printf("After hooks: %d\n", sfpm_rule_get_after_hook_count(print_rule));
    printf("Middleware hooks: %d\n\n", sfpm_rule_get_middleware_hook_count(print_rule));
    
    printf("=== Running program ===\n");
    interpreter_run(&interp);
    
    interpreter_destroy(&interp);
}

static void demo_full_pipeline(void) {
    printf("\n");
    printf("+----------------------------------------------------------------+\n");
    printf("| DEMO 2: FULL AUTHENTICATION -> VALIDATION -> METRICS PIPELINE |\n");
    printf("| Production-ready hook chain for secure operations             |\n");
    printf("+----------------------------------------------------------------+\n\n");
    
    uint8_t program[] = {
        OP_PUSH, 100,
        OP_PUSH, 50,
        OP_MUL,
        OP_PRINT,
        OP_HALT
    };
    
    vm_t vm;
    vm_init(&vm, program, sizeof(program));
    vm.authenticated = true;
    vm.user_id = 456;
    vm.permission_level = 2;
    
    interpreter_t interp;
    interpreter_init(&interp, &vm);
    
    interpreter_register_opcode(&interp, OP_PUSH, op_push);
    interpreter_register_opcode(&interp, OP_MUL, op_mul);
    interpreter_register_opcode(&interp, OP_PRINT, op_print);
    interpreter_register_opcode(&interp, OP_HALT, op_halt);
    
    metrics_t metrics = {0};
    
    /* Build comprehensive hook chain for PRINT */
    sfpm_rule_t *print_rule = interp.rules[2];
    
    printf("Building security & monitoring pipeline:\n");
    printf("  1. Authentication check\n");
    printf("  2. Validation check\n");
    printf("  3. Performance timer start\n");
    printf("  4. [PAYLOAD EXECUTION]\n");
    printf("  5. Performance metrics collection\n\n");
    
    sfpm_rule_add_before_hook(print_rule, auth_before_hook, "AUTH");
    sfpm_rule_add_before_hook(print_rule, validation_before_hook, NULL);
    sfpm_rule_add_before_hook(print_rule, metrics_before_hook, &metrics);
    sfpm_rule_add_after_hook(print_rule, metrics_after_hook, &metrics);
    
    printf("=== Running secured program ===\n");
    interpreter_run(&interp);
    
    printf("\n=== Final Metrics ===\n");
    printf("Total operations: %d\n", metrics.total_operations);
    printf("Total time: %lld us\n", metrics.total_time_us);
    
    interpreter_destroy(&interp);
}

static void demo_middleware_hooks(void) {
    printf("\n");
    printf("+----------------------------------------------------------------+\n");
    printf("| DEMO 3: MIDDLEWARE HOOKS                                       |\n");
    printf("| Middleware wraps payload execution (like transaction bounds)  |\n");
    printf("+----------------------------------------------------------------+\n\n");
    
    uint8_t program[] = {
        OP_PUSH, 7,
        OP_PUSH, 6,
        OP_MUL,
        OP_PRINT,
        OP_HALT
    };
    
    vm_t vm;
    vm_init(&vm, program, sizeof(program));
    vm.authenticated = true;
    vm.user_id = 789;
    vm.permission_level = 2;
    
    interpreter_t interp;
    interpreter_init(&interp, &vm);
    
    interpreter_register_opcode(&interp, OP_PUSH, op_push);
    interpreter_register_opcode(&interp, OP_MUL, op_mul);
    interpreter_register_opcode(&interp, OP_PRINT, op_print);
    interpreter_register_opcode(&interp, OP_HALT, op_halt);
    
    sfpm_rule_t *print_rule = interp.rules[2];
    
    printf("Adding middleware hooks (transaction boundaries):\n\n");
    
    sfpm_rule_add_before_hook(print_rule, transaction_middleware_hook, "BEGIN");
    sfpm_rule_add_middleware_hook(print_rule, transaction_middleware_hook, "MIDDLEWARE");
    sfpm_rule_add_after_hook(print_rule, transaction_middleware_hook, "COMMIT");
    
    printf("Execution order:\n");
    printf("  Before hooks -> Middleware hooks -> Payload -> After hooks\n\n");
    
    printf("=== Running program with middleware ===\n");
    interpreter_run(&interp);
    
    interpreter_destroy(&interp);
}

static void demo_early_abortion(void) {
    printf("\n");
    printf("+----------------------------------------------------------------+\n");
    printf("| DEMO 4: EARLY ABORTION IN HOOK CHAIN                          |\n");
    printf("| A before hook aborts execution after 3 operations             |\n");
    printf("+----------------------------------------------------------------+\n\n");
    
    uint8_t program[] = {
        OP_PUSH, 1,  /* Op 1 */
        OP_PUSH, 2,  /* Op 2 */
        OP_ADD,      /* Op 3 */
        OP_PUSH, 3,  /* Op 4 - Should be aborted */
        OP_MUL,      /* Op 5 - Should be aborted */
        OP_PRINT,    /* Op 6 - Should be aborted */
        OP_HALT
    };
    
    vm_t vm;
    vm_init(&vm, program, sizeof(program));
    vm.authenticated = true;
    vm.user_id = 999;
    vm.permission_level = 2;
    
    interpreter_t interp;
    interpreter_init(&interp, &vm);
    
    interpreter_register_opcode(&interp, OP_PUSH, op_push);
    interpreter_register_opcode(&interp, OP_ADD, op_add);
    interpreter_register_opcode(&interp, OP_MUL, op_mul);
    interpreter_register_opcode(&interp, OP_PRINT, op_print);
    interpreter_register_opcode(&interp, OP_HALT, op_halt);
    
    /* Add abort hook to ALL operations */
    for (int i = 0; i < interp.rule_count; i++) {
        sfpm_rule_add_before_hook(interp.rules[i], abort_after_3_hook, NULL);
    }
    
    abort_counter = 0;  /* Reset global counter */
    
    printf("=== Running program (will abort after 3 operations) ===\n");
    interpreter_run(&interp);
    
    printf("\n=== Result ===\n");
    printf("Program halted at operation: %d\n", abort_counter);
    printf("Expected: Should stop at operation 4 (after PUSH, PUSH, ADD)\n");
    
    interpreter_destroy(&interp);
}

static void demo_unauthenticated_access(void) {
    printf("\n");
    printf("+----------------------------------------------------------------+\n");
    printf("| DEMO 5: AUTHENTICATION FAILURE                                 |\n");
    printf("| Shows early abortion when authentication fails                |\n");
    printf("+----------------------------------------------------------------+\n\n");
    
    uint8_t program[] = {
        OP_PUSH, 42,
        OP_PRINT,  /* Should be blocked by auth */
        OP_HALT
    };
    
    vm_t vm;
    vm_init(&vm, program, sizeof(program));
    vm.authenticated = false;  /* NOT AUTHENTICATED! */
    vm.user_id = 0;
    vm.permission_level = 0;
    
    interpreter_t interp;
    interpreter_init(&interp, &vm);
    
    interpreter_register_opcode(&interp, OP_PUSH, op_push);
    interpreter_register_opcode(&interp, OP_PRINT, op_print);
    interpreter_register_opcode(&interp, OP_HALT, op_halt);
    
    sfpm_rule_t *print_rule = interp.rules[1];
    
    printf("Adding authentication hook to PRINT operation:\n\n");
    sfpm_rule_add_before_hook(print_rule, auth_before_hook, "AUTH");
    
    printf("=== Running program (user NOT authenticated) ===\n");
    interpreter_run(&interp);
    
    printf("\n=== Result ===\n");
    printf("Operation was blocked by authentication hook\n");
    
    interpreter_destroy(&interp);
}

/* ============================================================================
 * MAIN
 * ============================================================================ */

int main(void) {
    printf("====================================================================\n");
    printf("SFPM HOOK CHAINING DEMONSTRATION\n");
    printf("====================================================================\n");
    printf("\n");
    printf("This example demonstrates the power of hook chaining:\n");
    printf("\n");
    printf("  - Multiple before hooks (execute in order)\n");
    printf("  - Multiple after hooks (execute in order)\n");
    printf("  - Middleware hooks (wrap execution)\n");
    printf("  - Early abortion (any hook can stop execution)\n");
    printf("  - Production patterns (auth -> validation -> metrics)\n");
    
    demo_multiple_before_after_hooks();
    demo_full_pipeline();
    demo_middleware_hooks();
    demo_early_abortion();
    demo_unauthenticated_access();
    
    printf("\n");
    printf("+----------------------------------------------------------------+\n");
    printf("| SUMMARY: HOOK CHAINING CAPABILITIES                           |\n");
    printf("+----------------------------------------------------------------+\n");
    printf("| Execution Order:                                               |\n");
    printf("|   1. Single before hook (backward compat)                      |\n");
    printf("|   2. Before hook chain (in order added)                        |\n");
    printf("|   3. Middleware hook chain (in order added)                    |\n");
    printf("|   4. [PAYLOAD EXECUTION]                                       |\n");
    printf("|   5. After hook chain (in order added)                         |\n");
    printf("|   6. Single after hook (backward compat)                       |\n");
    printf("|                                                                |\n");
    printf("| Any before/middleware hook returning false aborts execution!   |\n");
    printf("|                                                                |\n");
    printf("| Use Cases:                                                     |\n");
    printf("|   - Security pipelines (auth -> authorization -> validation)  |\n");
    printf("|   - Observability (logging -> metrics -> tracing)             |\n");
    printf("|   - Transaction management (begin -> execute -> commit)       |\n");
    printf("|   - Rate limiting (count -> check -> throttle)                |\n");
    printf("|   - Caching (check cache -> execute -> update cache)          |\n");
    printf("+----------------------------------------------------------------+\n");
    
    return 0;
}
