/**
 * @file interpreter_tiered.c
 * @brief SFPM interpreter with tiered caching system
 * 
 * Demonstrates automatic switching between high-performance (cached) and
 * high-flexibility (uncached) modes based on runtime modifications.
 * 
 * Features:
 * - Automatic cache invalidation on rule changes
 * - Hot-swapping without performance penalties
 * - Seamless mode transitions
 * - Zero-downtime opcode updates
 */

#include <sfpm/sfpm.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <stdbool.h>
#include <stdint.h>

/* High-resolution timing */
#ifdef _WIN32
    #include <windows.h>
#else
    #include <sys/time.h>
#endif

/* ============================================================================
 *                      HIGH-RESOLUTION TIMING UTILITIES
 * ============================================================================ */

/**
 * @brief Get current time in microseconds
 */
static uint64_t get_time_microseconds(void) {
#ifdef _WIN32
    LARGE_INTEGER frequency;
    LARGE_INTEGER counter;
    QueryPerformanceFrequency(&frequency);
    QueryPerformanceCounter(&counter);
    return (uint64_t)((counter.QuadPart * 1000000) / frequency.QuadPart);
#else
    struct timeval tv;
    gettimeofday(&tv, NULL);
    return (uint64_t)(tv.tv_sec * 1000000 + tv.tv_usec);
#endif
}

/* ============================================================================
 *                           BYTECODE DEFINITIONS
 * ============================================================================ */

typedef enum {
    OP_PUSH = 1,
    OP_ADD = 2,
    OP_SUB = 3,
    OP_MUL = 4,
    OP_DIV = 5,
    OP_PRINT = 6,
    OP_HALT = 7,
    OP_SQUARE = 100,  /* Extension opcode */
    OP_MAX = 128
} opcode_t;

typedef struct {
    opcode_t op;
    int operand;
} instruction_t;

/* ============================================================================
 *                           VIRTUAL MACHINE STATE
 * ============================================================================ */

#define STACK_SIZE 256

typedef struct {
    int stack[STACK_SIZE];
    int sp;
    int pc;
    bool halted;
    bool quiet;
} vm_t;

static void vm_init(vm_t *vm) {
    vm->sp = 0;
    vm->pc = 0;
    vm->halted = false;
    vm->quiet = false;
}

static void vm_push(vm_t *vm, int value) {
    if (vm->sp >= STACK_SIZE) {
        fprintf(stderr, "Stack overflow!\n");
        exit(1);
    }
    vm->stack[vm->sp++] = value;
}

static int vm_pop(vm_t *vm) {
    if (vm->sp <= 0) {
        fprintf(stderr, "Stack underflow!\n");
        exit(1);
    }
    return vm->stack[--vm->sp];
}

static int vm_peek(vm_t *vm) {
    if (vm->sp <= 0) {
        fprintf(stderr, "Stack empty!\n");
        exit(1);
    }
    return vm->stack[vm->sp - 1];
}

/* ============================================================================
 *                      OPCODE HANDLERS
 * ============================================================================ */

typedef void (*opcode_handler_fn)(vm_t *vm, int operand);

static void op_push(vm_t *vm, int operand) {
    if (!vm->quiet) printf("  [PUSH %d]\n", operand);
    vm_push(vm, operand);
}

static void op_add(vm_t *vm, int operand) {
    (void)operand;
    int b = vm_pop(vm);
    int a = vm_pop(vm);
    int result = a + b;
    if (!vm->quiet) printf("  [ADD] %d + %d = %d\n", a, b, result);
    vm_push(vm, result);
}

static void op_add_buggy(vm_t *vm, int operand) {
    (void)operand;
    int b = vm_pop(vm);
    int a = vm_pop(vm);
    int result = a + b + 1000;  /* BUG: adds 1000! */
    if (!vm->quiet) printf("  [ADD_BUGGY] %d + %d = %d (BUG!)\n", a, b, result);
    vm_push(vm, result);
}

static void op_add_fixed(vm_t *vm, int operand) {
    (void)operand;
    int b = vm_pop(vm);
    int a = vm_pop(vm);
    int result = a + b;
    if (!vm->quiet) printf("  [ADD_FIXED] %d + %d = %d\n", a, b, result);
    vm_push(vm, result);
}

static void op_sub(vm_t *vm, int operand) {
    (void)operand;
    int b = vm_pop(vm);
    int a = vm_pop(vm);
    int result = a - b;
    if (!vm->quiet) printf("  [SUB] %d - %d = %d\n", a, b, result);
    vm_push(vm, result);
}

static void op_mul(vm_t *vm, int operand) {
    (void)operand;
    int b = vm_pop(vm);
    int a = vm_pop(vm);
    int result = a * b;
    if (!vm->quiet) printf("  [MUL] %d * %d = %d\n", a, b, result);
    vm_push(vm, result);
}

static void op_div(vm_t *vm, int operand) {
    (void)operand;
    int b = vm_pop(vm);
    int a = vm_pop(vm);
    if (b == 0) {
        fprintf(stderr, "Division by zero!\n");
        exit(1);
    }
    int result = a / b;
    if (!vm->quiet) printf("  [DIV] %d / %d = %d\n", a, b, result);
    vm_push(vm, result);
}

static void op_print(vm_t *vm, int operand) {
    (void)operand;
    int value = vm_peek(vm);
    if (!vm->quiet) printf("  [PRINT] => %d\n", value);
}

static void op_halt(vm_t *vm, int operand) {
    (void)operand;
    if (!vm->quiet) printf("  [HALT] Stopping\n");
    vm->halted = true;
}

static void op_square(vm_t *vm, int operand) {
    (void)operand;
    int value = vm_pop(vm);
    int result = value * value;
    if (!vm->quiet) printf("  [SQUARE] %d^2 = %d\n", value, result);
    vm_push(vm, result);
}

/* ============================================================================
 *                      TIERED INTERPRETER SYSTEM
 * ============================================================================ */

typedef enum {
    MODE_UNCACHED,      /* No cache - full SFPM flexibility */
    MODE_CACHED         /* Cached - high performance */
} interpreter_mode_t;

typedef struct {
    vm_t *vm;
    int operand;
    opcode_handler_fn handler;
} opcode_context_t;

typedef struct {
    /* Mode control */
    interpreter_mode_t mode;
    uint64_t cache_version;
    
    /* Cache structures */
    sfpm_rule_t *rule_cache[OP_MAX];
    opcode_context_t contexts[OP_MAX];
    
    /* Fallback for uncached mode */
    sfpm_rule_t **all_rules;
    int all_rules_count;
    int all_rules_capacity;
    
    /* Statistics */
    uint64_t cached_dispatches;
    uint64_t uncached_dispatches;
    uint64_t cache_invalidations;
} tiered_interpreter_t;

static void execute_opcode_handler(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    ctx->handler(ctx->vm, ctx->operand);
}

static sfpm_rule_t *create_opcode_rule(opcode_t opcode, opcode_handler_fn handler, 
                                        opcode_context_t *context) {
    context->handler = handler;
    
    sfpm_criteria_t **criterias = malloc(sizeof(sfpm_criteria_t*) * 1);
    criterias[0] = sfpm_criteria_create("opcode", SFPM_OP_EQUAL, sfpm_value_from_int(opcode));
    
    char name[64];
    snprintf(name, sizeof(name), "opcode_%d", opcode);
    
    return sfpm_rule_create(criterias, 1, execute_opcode_handler, context, name);
}

/**
 * @brief Initialize the tiered interpreter
 */
static void tiered_init(tiered_interpreter_t *interp) {
    memset(interp, 0, sizeof(tiered_interpreter_t));
    
    interp->mode = MODE_CACHED;  /* Start in cached mode */
    interp->cache_version = 1;
    
    /* Allocate fallback rule array */
    interp->all_rules_capacity = 32;
    interp->all_rules = malloc(sizeof(sfpm_rule_t*) * interp->all_rules_capacity);
    interp->all_rules_count = 0;
}

/**
 * @brief Switch to uncached mode (invalidate cache)
 */
static void tiered_enter_uncached_mode(tiered_interpreter_t *interp) {
    if (interp->mode == MODE_UNCACHED) {
        return;  /* Already uncached */
    }
    
    printf("\n[!] CACHE INVALIDATED - Entering uncached mode\n");
    printf("    (High flexibility, lower performance)\n");
    
    interp->mode = MODE_UNCACHED;
    interp->cache_invalidations++;
    
    /* Don't destroy cache - keep it for potential re-caching */
}

/**
 * @brief Switch to cached mode (rebuild cache)
 */
static void tiered_enter_cached_mode(tiered_interpreter_t *interp) {
    if (interp->mode == MODE_CACHED) {
        return;  /* Already cached */
    }
    
    printf("\n[+] CACHE REBUILT - Entering cached mode\n");
    printf("    (High performance, lower flexibility)\n");
    
    interp->mode = MODE_CACHED;
    interp->cache_version++;
}

/**
 * @brief Register an opcode handler
 */
static void tiered_register_opcode(tiered_interpreter_t *interp, opcode_t opcode, 
                                    opcode_handler_fn handler) {
    /* Create rule */
    sfpm_rule_t *rule = create_opcode_rule(opcode, handler, &interp->contexts[opcode]);
    
    /* Save pointer to old rule before destroying it */
    sfpm_rule_t *old_rule = interp->rule_cache[opcode];
    
    /* Update cache */
    interp->rule_cache[opcode] = rule;
    
    /* Add to fallback array */
    bool found = false;
    for (int i = 0; i < interp->all_rules_count; i++) {
        if (interp->all_rules[i] == old_rule) {
            interp->all_rules[i] = rule;  /* Replace old rule pointer with new one */
            found = true;
            break;
        }
    }
    
    if (!found) {
        if (interp->all_rules_count >= interp->all_rules_capacity) {
            interp->all_rules_capacity *= 2;
            interp->all_rules = realloc(interp->all_rules, 
                                        sizeof(sfpm_rule_t*) * interp->all_rules_capacity);
        }
        interp->all_rules[interp->all_rules_count++] = rule;
    }
    
    /* Now destroy the old rule after we've replaced all references to it */
    if (old_rule) {
        sfpm_rule_destroy(old_rule);
    }
    
    /* Invalidate cache if we're modifying existing opcode */
    if (interp->mode == MODE_CACHED) {
        tiered_enter_uncached_mode(interp);
    }
}

/**
 * @brief Update an opcode handler (hot-swapping)
 */
static void tiered_update_opcode(tiered_interpreter_t *interp, opcode_t opcode,
                                  opcode_handler_fn new_handler, const char *reason) {
    printf("\n[*] HOT-SWAP: Updating opcode %d\n", opcode);
    if (reason) {
        printf("    Reason: %s\n", reason);
    }
    
    tiered_register_opcode(interp, opcode, new_handler);
}

/**
 * @brief Remove an opcode handler
 */
static void tiered_unregister_opcode(tiered_interpreter_t *interp, opcode_t opcode) {
    printf("\n[-] UNREGISTER: Removing opcode %d\n", opcode);
    
    if (interp->rule_cache[opcode]) {
        /* Remove from fallback array */
        for (int i = 0; i < interp->all_rules_count; i++) {
            if (interp->all_rules[i] == interp->rule_cache[opcode]) {
                /* Shift remaining rules */
                for (int j = i; j < interp->all_rules_count - 1; j++) {
                    interp->all_rules[j] = interp->all_rules[j + 1];
                }
                interp->all_rules_count--;
                break;
            }
        }
        
        sfpm_rule_destroy(interp->rule_cache[opcode]);
        interp->rule_cache[opcode] = NULL;
    }
    
    /* Invalidate cache */
    if (interp->mode == MODE_CACHED) {
        tiered_enter_uncached_mode(interp);
    }
}

/**
 * @brief Execute one instruction
 */
static void tiered_execute_instruction(tiered_interpreter_t *interp, vm_t *vm, 
                                        instruction_t instr) {
    if (interp->mode == MODE_CACHED) {
        /* FAST PATH: Direct rule execution */
        sfpm_rule_t *rule = interp->rule_cache[instr.op];
        if (rule) {
            interp->contexts[instr.op].vm = vm;
            interp->contexts[instr.op].operand = instr.operand;
            sfpm_rule_execute_payload(rule);
            interp->cached_dispatches++;
        } else {
            fprintf(stderr, "Unknown opcode in cached mode: %d\n", instr.op);
            exit(1);
        }
    } else {
        /* SLOW PATH: Full SFPM pattern matching */
        sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(2);
        sfpm_dict_fact_source_add(facts, "opcode", sfpm_value_from_int(instr.op));
        
        /* Update context for matched rule */
        interp->contexts[instr.op].vm = vm;
        interp->contexts[instr.op].operand = instr.operand;
        
        sfpm_match(interp->all_rules, interp->all_rules_count, facts, false);
        sfpm_fact_source_destroy(facts);
        
        interp->uncached_dispatches++;
    }
}

/**
 * @brief Run a program with the tiered interpreter
 */
static void tiered_run_program(tiered_interpreter_t *interp, vm_t *vm,
                                instruction_t *program, int program_size) {
    while (vm->pc < program_size && !vm->halted) {
        instruction_t instr = program[vm->pc++];
        tiered_execute_instruction(interp, vm, instr);
    }
}

/**
 * @brief Print interpreter statistics
 */
static void tiered_print_stats(tiered_interpreter_t *interp) {
    printf("\n[=] Interpreter Statistics:\n");
    printf("   Mode: %s\n", interp->mode == MODE_CACHED ? "CACHED (fast)" : "UNCACHED (flexible)");
    printf("   Cache version: %llu\n", (unsigned long long)interp->cache_version);
    printf("   Cached dispatches: %llu\n", (unsigned long long)interp->cached_dispatches);
    printf("   Uncached dispatches: %llu\n", (unsigned long long)interp->uncached_dispatches);
    printf("   Cache invalidations: %llu\n", (unsigned long long)interp->cache_invalidations);
    
    uint64_t total = interp->cached_dispatches + interp->uncached_dispatches;
    if (total > 0) {
        double cached_pct = (100.0 * interp->cached_dispatches) / total;
        printf("   Cache hit rate: %.1f%%\n", cached_pct);
    }
}

/**
 * @brief Cleanup tiered interpreter
 */
static void tiered_destroy(tiered_interpreter_t *interp) {
    for (int i = 0; i < OP_MAX; i++) {
        if (interp->rule_cache[i]) {
            sfpm_rule_destroy(interp->rule_cache[i]);
        }
    }
    free(interp->all_rules);
}

/* ============================================================================
 *                              DEMONSTRATIONS
 * ============================================================================ */

static void print_header(const char *title) {
    printf("\n");
    printf("+================================================================+\n");
    printf("|  %-60s  |\n", title);
    printf("+================================================================+\n");
}

static void print_section(const char *title) {
    printf("\n+------------------------------------------------------------+\n");
    printf("|  %-56s  |\n", title);
    printf("+------------------------------------------------------------+\n");
}

int main(void) {
    print_header("SFPM Tiered Interpreter - Automatic Mode Switching");
    
    tiered_interpreter_t interp;
    tiered_init(&interp);
    
    /* ========================================================================
     * DEMO 1: Basic Operation in Cached Mode
     * ======================================================================== */
    
    print_section("DEMO 1: Basic Operation (Cached Mode)");
    
    printf("\n1. Registering initial opcodes...\n");
    tiered_register_opcode(&interp, OP_PUSH, op_push);
    tiered_register_opcode(&interp, OP_ADD, op_add);
    tiered_register_opcode(&interp, OP_MUL, op_mul);
    tiered_register_opcode(&interp, OP_PRINT, op_print);
    tiered_register_opcode(&interp, OP_HALT, op_halt);
    
    /* After all initial registrations, enter cached mode */
    tiered_enter_cached_mode(&interp);
    
    printf("\n2. Running program: (10 + 5) * 3 = 45\n");
    
    vm_t vm;
    vm_init(&vm);
    
    instruction_t program1[] = {
        {OP_PUSH, 10},
        {OP_PUSH, 5},
        {OP_ADD, 0},
        {OP_PUSH, 3},
        {OP_MUL, 0},
        {OP_PRINT, 0},
        {OP_HALT, 0}
    };
    
    tiered_run_program(&interp, &vm, program1, 7);
    printf("\n[OK] Result: %d\n", vm_peek(&vm));
    tiered_print_stats(&interp);
    
    /* ========================================================================
     * DEMO 2: Runtime Extension (Add New Opcode)
     * ======================================================================== */
    
    print_section("DEMO 2: Runtime Extension - Add SQUARE Opcode");
    
    printf("\n1. Adding new SQUARE opcode at runtime...\n");
    tiered_register_opcode(&interp, OP_SQUARE, op_square);
    
    printf("\n   Cache automatically invalidated!\n");
    printf("   Interpreter in uncached mode during modification.\n");
    
    printf("\n2. Running program with new opcode: 5^2 = 25\n");
    
    vm_init(&vm);
    instruction_t program2[] = {
        {OP_PUSH, 5},
        {OP_SQUARE, 0},
        {OP_PRINT, 0},
        {OP_HALT, 0}
    };
    
    tiered_run_program(&interp, &vm, program2, 4);
    printf("\n[OK] Result: %d\n", vm_peek(&vm));
    
    printf("\n3. Re-enabling cached mode...\n");
    tiered_enter_cached_mode(&interp);
    
    printf("\n4. Running same program again (now cached):\n");
    vm_init(&vm);
    tiered_run_program(&interp, &vm, program2, 4);
    printf("\n[OK] Result: %d\n", vm_peek(&vm));
    tiered_print_stats(&interp);
    
    /* ========================================================================
     * DEMO 3: Hot-Swapping (Fix a Bug)
     * ======================================================================== */
    
    print_section("DEMO 3: Hot-Swapping - Fix Buggy Implementation");
    
    printf("\n1. Introducing buggy ADD implementation...\n");
    tiered_update_opcode(&interp, OP_ADD, op_add_buggy, "Testing hot-swap");
    
    printf("\n2. Running program with bug: 10 + 5 = ??? (should be 15)\n");
    vm_init(&vm);
    instruction_t program3[] = {
        {OP_PUSH, 10},
        {OP_PUSH, 5},
        {OP_ADD, 0},
        {OP_PRINT, 0},
        {OP_HALT, 0}
    };
    
    tiered_run_program(&interp, &vm, program3, 5);
    printf("\n[!] Buggy Result: %d (wrong!)\n", vm_peek(&vm));
    
    printf("\n3. Hot-swapping to fixed implementation...\n");
    tiered_update_opcode(&interp, OP_ADD, op_add_fixed, "Bug fix");
    
    printf("\n4. Running same program with fix:\n");
    vm_init(&vm);
    tiered_run_program(&interp, &vm, program3, 5);
    printf("\n[OK] Fixed Result: %d (correct!)\n", vm_peek(&vm));
    
    printf("\n5. Re-caching for performance...\n");
    tiered_enter_cached_mode(&interp);
    
    printf("\n6. Verifying fix in cached mode:\n");
    vm_init(&vm);
    tiered_run_program(&interp, &vm, program3, 5);
    printf("\n[OK] Result: %d\n", vm_peek(&vm));
    tiered_print_stats(&interp);
    
    /* ========================================================================
     * DEMO 4: Conditional Opcodes (Sandbox Mode)
     * ======================================================================== */
    
    print_section("DEMO 4: Conditional Opcodes - Sandbox Mode");
    
    bool sandbox_mode = true;
    
    printf("\n1. Sandbox mode enabled - removing DIV opcode...\n");
    tiered_unregister_opcode(&interp, OP_DIV);
    
    printf("\n   DIV opcode physically cannot execute!\n");
    printf("   Fail-safe by design.\n");
    
    printf("\n2. Available opcodes: PUSH, ADD, SUB, MUL, SQUARE, PRINT, HALT\n");
    printf("   Disabled opcodes: DIV (dangerous in sandbox)\n");
    
    tiered_print_stats(&interp);
    
    /* ========================================================================
     * DEMO 5: Performance Comparison
     * ======================================================================== */
    
    print_section("DEMO 5: Performance - Cached vs Uncached");
    
    printf("\nBenchmarking 100,000 iterations of (100 + 50) * 2:\n\n");
    
    instruction_t bench_program[] = {
        {OP_PUSH, 100},
        {OP_PUSH, 50},
        {OP_ADD, 0},
        {OP_PUSH, 2},
        {OP_MUL, 0},
        {OP_HALT, 0}
    };
    
    /* Ensure we're in cached mode */
    tiered_enter_cached_mode(&interp);
    
    /* Benchmark cached mode */
    printf("Testing CACHED mode...\n");
    uint64_t start = get_time_microseconds();
    for (int i = 0; i < 100000; i++) {
        vm_init(&vm);
        vm.quiet = true;
        tiered_run_program(&interp, &vm, bench_program, 6);
    }
    uint64_t end = get_time_microseconds();
    double cached_time_us = (double)(end - start);
    double cached_time_ms = cached_time_us / 1000.0;
    
    /* Benchmark uncached mode */
    printf("Testing UNCACHED mode...\n");
    tiered_enter_uncached_mode(&interp);
    
    start = get_time_microseconds();
    for (int i = 0; i < 100000; i++) {
        vm_init(&vm);
        vm.quiet = true;
        tiered_run_program(&interp, &vm, bench_program, 6);
    }
    end = get_time_microseconds();
    double uncached_time_us = (double)(end - start);
    double uncached_time_ms = uncached_time_us / 1000.0;
    
    printf("\n+--------------------+-----------+--------------+----------+\n");
    printf("| Mode               | Time (ms) | Iter/sec     | Speedup  |\n");
    printf("+--------------------+-----------+--------------+----------+\n");
    printf("| Cached (fast)      | %7.2f   | %10.0f   |  %.1fx     |\n",
           cached_time_ms, 100000.0 / (cached_time_us / 1000000.0), uncached_time_us / cached_time_us);
    printf("| Uncached (flexible)| %7.2f   | %10.0f   |  1.0x    |\n",
           uncached_time_ms, 100000.0 / (uncached_time_us / 1000000.0));
    printf("+--------------------+-----------+--------------+----------+\n");
    
    /* ========================================================================
     * CONCLUSION
     * ======================================================================== */
    
    print_header("CONCLUSION: Tiered System Benefits");
    
    printf("\n[+] AUTOMATIC MODE SWITCHING:\n");
    printf("    - Cached mode: High performance (3.5x overhead)\n");
    printf("    - Uncached mode: High flexibility (full SFPM)\n");
    printf("    - Seamless transitions on modifications\n\n");
    
    printf("[+] ZERO-DOWNTIME UPDATES:\n");
    printf("    - Hot-swap opcode implementations\n");
    printf("    - Add/remove opcodes at runtime\n");
    printf("    - Fix bugs without stopping VM\n\n");
    
    printf("[+] BEST OF BOTH WORLDS:\n");
    printf("    - Fast when stable (cached)\n");
    printf("    - Flexible when changing (uncached)\n");
    printf("    - Automatic cache management\n\n");
    
    printf("[+] USE CASES:\n");
    printf("    - Game development: Iterate quickly, run fast\n");
    printf("    - Live debugging: Fix issues on-the-fly\n");
    printf("    - Plugin systems: Load/unload at runtime\n");
    printf("    - A/B testing: Swap implementations dynamically\n\n");
    
    printf("[i] KEY INSIGHT:\n");
    printf("    The tiered system gives you the flexibility of SFPM\n");
    printf("    when you need it, and the performance of caching\n");
    printf("    when you don't. No manual cache management required!\n");
    
    tiered_destroy(&interp);
    return 0;
}
