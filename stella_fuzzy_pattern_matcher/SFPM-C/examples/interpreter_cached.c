/**
 * @file interpreter_cached.c
 * @brief SFPM-based interpreter with performance optimizations and caching
 * 
 * Demonstrates various caching strategies to reduce SFPM overhead:
 * 1. Opcode-to-rule direct mapping (O(1) lookup)
 * 2. Fact source reuse (eliminate allocation overhead)
 * 3. Pre-evaluated rules (skip pattern matching)
 * 4. Function pointer cache (direct dispatch)
 */

#include <sfpm/sfpm.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

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
    OP_MAX = 128  /* Maximum opcode value for cache sizing */
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
    int sp;  /* Stack pointer */
    int pc;  /* Program counter */
    bool halted;
    bool quiet;  /* Suppress output when benchmarking */
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
    if (!vm->quiet) printf("[PUSH %d]\n", operand);
    vm_push(vm, operand);
}

static void op_add(vm_t *vm, int operand) {
    (void)operand;
    int b = vm_pop(vm);
    int a = vm_pop(vm);
    int result = a + b;
    if (!vm->quiet) printf("[ADD] %d + %d = %d\n", a, b, result);
    vm_push(vm, result);
}

static void op_sub(vm_t *vm, int operand) {
    (void)operand;
    int b = vm_pop(vm);
    int a = vm_pop(vm);
    int result = a - b;
    if (!vm->quiet) printf("[SUB] %d - %d = %d\n", a, b, result);
    vm_push(vm, result);
}

static void op_mul(vm_t *vm, int operand) {
    (void)operand;
    int b = vm_pop(vm);
    int a = vm_pop(vm);
    int result = a * b;
    if (!vm->quiet) printf("[MUL] %d * %d = %d\n", a, b, result);
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
    if (!vm->quiet) printf("[DIV] %d / %d = %d\n", a, b, result);
    vm_push(vm, result);
}

static void op_print(vm_t *vm, int operand) {
    (void)operand;
    int value = vm_peek(vm);
    if (!vm->quiet) printf("[PRINT] => %d\n", value);
}

static void op_halt(vm_t *vm, int operand) {
    (void)operand;
    if (!vm->quiet) printf("[HALT] Stopping\n");
    vm->halted = true;
}

/* ============================================================================
 *                      OPTIMIZATION 1: DIRECT FUNCTION POINTER CACHE
 * ============================================================================ */

typedef struct {
    opcode_handler_fn handlers[OP_MAX];
    vm_t *vm;
} direct_dispatch_cache_t;

static void direct_cache_init(direct_dispatch_cache_t *cache, vm_t *vm) {
    memset(cache->handlers, 0, sizeof(cache->handlers));
    cache->vm = vm;
    
    /* Register handlers */
    cache->handlers[OP_PUSH] = op_push;
    cache->handlers[OP_ADD] = op_add;
    cache->handlers[OP_SUB] = op_sub;
    cache->handlers[OP_MUL] = op_mul;
    cache->handlers[OP_DIV] = op_div;
    cache->handlers[OP_PRINT] = op_print;
    cache->handlers[OP_HALT] = op_halt;
}

static void run_direct_cached_interpreter(instruction_t *program, int program_size) {
    vm_t vm;
    vm_init(&vm);
    
    direct_dispatch_cache_t cache;
    direct_cache_init(&cache, &vm);
    
    while (vm.pc < program_size && !vm.halted) {
        instruction_t instr = program[vm.pc++];
        
        /* Direct dispatch - O(1) lookup, no SFPM overhead */
        opcode_handler_fn handler = cache.handlers[instr.op];
        if (handler) {
            handler(&vm, instr.operand);
        } else {
            fprintf(stderr, "Unknown opcode: %d\n", instr.op);
            exit(1);
        }
    }
    
    printf("\nResult: %d\n", vm_peek(&vm));
}

/* ============================================================================
 *                      OPTIMIZATION 2: SFPM WITH RULE CACHE
 * ============================================================================ */

typedef struct {
    sfpm_rule_t *rules[OP_MAX];  /* Direct opcode-to-rule mapping */
    int rule_count;
    vm_t *vm;
} sfpm_rule_cache_t;

/* Context for opcode handlers */
typedef struct {
    vm_t *vm;
    int operand;
    opcode_handler_fn handler;
} opcode_context_t;

static opcode_context_t g_contexts[OP_MAX];

static void execute_opcode_handler(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    ctx->handler(ctx->vm, ctx->operand);
}

static sfpm_rule_t *create_opcode_rule(opcode_t opcode, opcode_handler_fn handler) {
    g_contexts[opcode].handler = handler;
    
    sfpm_criteria_t **criterias = malloc(sizeof(sfpm_criteria_t*) * 1);
    criterias[0] = sfpm_criteria_create("opcode", SFPM_OP_EQUAL, sfpm_value_from_int(opcode));
    
    char name[64];
    snprintf(name, sizeof(name), "opcode_%d", opcode);
    
    return sfpm_rule_create(criterias, 1, execute_opcode_handler, &g_contexts[opcode], name);
}

static void sfpm_cache_init(sfpm_rule_cache_t *cache, vm_t *vm) {
    memset(cache->rules, 0, sizeof(cache->rules));
    cache->rule_count = 0;
    cache->vm = vm;
    
    /* Register opcodes */
    cache->rules[OP_PUSH] = create_opcode_rule(OP_PUSH, op_push);
    cache->rules[OP_ADD] = create_opcode_rule(OP_ADD, op_add);
    cache->rules[OP_SUB] = create_opcode_rule(OP_SUB, op_sub);
    cache->rules[OP_MUL] = create_opcode_rule(OP_MUL, op_mul);
    cache->rules[OP_DIV] = create_opcode_rule(OP_DIV, op_div);
    cache->rules[OP_PRINT] = create_opcode_rule(OP_PRINT, op_print);
    cache->rules[OP_HALT] = create_opcode_rule(OP_HALT, op_halt);
}

static void sfpm_cache_destroy(sfpm_rule_cache_t *cache) {
    for (int i = 0; i < OP_MAX; i++) {
        if (cache->rules[i]) {
            sfpm_rule_destroy(cache->rules[i]);
        }
    }
}

static void run_sfpm_cached_interpreter(instruction_t *program, int program_size) {
    vm_t vm;
    vm_init(&vm);
    
    sfpm_rule_cache_t cache;
    sfpm_cache_init(&cache, &vm);
    
    while (vm.pc < program_size && !vm.halted) {
        instruction_t instr = program[vm.pc++];
        
        /* Direct rule lookup - skip pattern matching overhead */
        sfpm_rule_t *rule = cache.rules[instr.op];
        if (rule) {
            g_contexts[instr.op].vm = &vm;
            g_contexts[instr.op].operand = instr.operand;
            sfpm_rule_execute_payload(rule);
        } else {
            fprintf(stderr, "Unknown opcode: %d\n", instr.op);
            exit(1);
        }
    }
    
    printf("\nResult: %d\n", vm_peek(&vm));
    sfpm_cache_destroy(&cache);
}

/* ============================================================================
 *                      OPTIMIZATION 3: SFPM WITH FACT REUSE
 * ============================================================================ */

static void run_sfpm_fact_reuse_interpreter(instruction_t *program, int program_size) {
    vm_t vm;
    vm_init(&vm);
    
    sfpm_rule_cache_t cache;
    sfpm_cache_init(&cache, &vm);
    
    /* Create fact source ONCE and reuse */
    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(2);
    
    /* Collect all rules into array for matching */
    sfpm_rule_t *rule_array[OP_MAX];
    int rule_count = 0;
    for (int i = 0; i < OP_MAX; i++) {
        if (cache.rules[i]) {
            rule_array[rule_count++] = cache.rules[i];
        }
    }
    
    while (vm.pc < program_size && !vm.halted) {
        instruction_t instr = program[vm.pc++];
        
        /* Reuse fact source - just update the opcode value */
        sfpm_dict_fact_source_add(facts, "opcode", sfpm_value_from_int(instr.op));
        
        g_contexts[instr.op].vm = &vm;
        g_contexts[instr.op].operand = instr.operand;
        
        /* Use SFPM matching with reused facts */
        sfpm_match(rule_array, rule_count, facts, false);
    }
    
    printf("\nResult: %d\n", vm_peek(&vm));
    
    sfpm_fact_source_destroy(facts);
    sfpm_cache_destroy(&cache);
}

/* ============================================================================
 *                      BASELINE: SWITCH-BASED INTERPRETER
 * ============================================================================ */

static void run_switch_interpreter(instruction_t *program, int program_size) {
    vm_t vm;
    vm_init(&vm);
    
    while (vm.pc < program_size && !vm.halted) {
        instruction_t instr = program[vm.pc++];
        
        switch (instr.op) {
            case OP_PUSH: {
                printf("[PUSH %d]\n", instr.operand);
                vm_push(&vm, instr.operand);
                break;
            }
            case OP_ADD: {
                int b = vm_pop(&vm);
                int a = vm_pop(&vm);
                int result = a + b;
                printf("[ADD] %d + %d = %d\n", a, b, result);
                vm_push(&vm, result);
                break;
            }
            case OP_MUL: {
                int b = vm_pop(&vm);
                int a = vm_pop(&vm);
                int result = a * b;
                printf("[MUL] %d * %d = %d\n", a, b, result);
                vm_push(&vm, result);
                break;
            }
            case OP_PRINT: {
                int value = vm_peek(&vm);
                printf("[PRINT] => %d\n", value);
                break;
            }
            case OP_HALT: {
                printf("[HALT] Stopping\n");
                vm.halted = true;
                break;
            }
            default: {
                fprintf(stderr, "Unknown opcode: %d\n", instr.op);
                exit(1);
            }
        }
    }
    
    printf("\nResult: %d\n", vm_peek(&vm));
}

/* ============================================================================
 *                      PERFORMANCE BENCHMARKING
 * ============================================================================ */

static double benchmark_switch(instruction_t *program, int program_size, int iterations) {
    clock_t start = clock();
    
    for (int i = 0; i < iterations; i++) {
        vm_t vm;
        vm_init(&vm);
        vm.quiet = true;
        
        while (vm.pc < program_size && !vm.halted) {
            instruction_t instr = program[vm.pc++];
            
            switch (instr.op) {
                case OP_PUSH: vm_push(&vm, instr.operand); break;
                case OP_ADD: {
                    int b = vm_pop(&vm);
                    int a = vm_pop(&vm);
                    vm_push(&vm, a + b);
                    break;
                }
                case OP_MUL: {
                    int b = vm_pop(&vm);
                    int a = vm_pop(&vm);
                    vm_push(&vm, a * b);
                    break;
                }
                case OP_PRINT: break;
                case OP_HALT: vm.halted = true; break;
                default: break;
            }
        }
    }
    
    clock_t end = clock();
    return ((double)(end - start)) / CLOCKS_PER_SEC;
}

static double benchmark_direct_cache(instruction_t *program, int program_size, int iterations) {
    direct_dispatch_cache_t cache;
    vm_t vm_setup;
    vm_init(&vm_setup);
    direct_cache_init(&cache, &vm_setup);
    
    clock_t start = clock();
    
    for (int iter = 0; iter < iterations; iter++) {
        vm_t vm;
        vm_init(&vm);
        vm.quiet = true;
        cache.vm = &vm;
        
        while (vm.pc < program_size && !vm.halted) {
            instruction_t instr = program[vm.pc++];
            opcode_handler_fn handler = cache.handlers[instr.op];
            if (handler) {
                handler(&vm, instr.operand);
            }
        }
    }
    
    clock_t end = clock();
    return ((double)(end - start)) / CLOCKS_PER_SEC;
}

static double benchmark_sfpm_cached(instruction_t *program, int program_size, int iterations) {
    sfpm_rule_cache_t cache;
    vm_t vm_setup;
    vm_init(&vm_setup);
    sfpm_cache_init(&cache, &vm_setup);
    
    clock_t start = clock();
    
    for (int iter = 0; iter < iterations; iter++) {
        vm_t vm;
        vm_init(&vm);
        vm.quiet = true;
        
        while (vm.pc < program_size && !vm.halted) {
            instruction_t instr = program[vm.pc++];
            
            sfpm_rule_t *rule = cache.rules[instr.op];
            if (rule) {
                g_contexts[instr.op].vm = &vm;
                g_contexts[instr.op].operand = instr.operand;
                sfpm_rule_execute_payload(rule);
            }
        }
    }
    
    clock_t end = clock();
    sfpm_cache_destroy(&cache);
    return ((double)(end - start)) / CLOCKS_PER_SEC;
}

static double benchmark_sfpm_fact_reuse(instruction_t *program, int program_size, int iterations) {
    sfpm_rule_cache_t cache;
    vm_t vm_setup;
    vm_init(&vm_setup);
    sfpm_cache_init(&cache, &vm_setup);
    
    sfpm_rule_t *rule_array[OP_MAX];
    int rule_count = 0;
    for (int i = 0; i < OP_MAX; i++) {
        if (cache.rules[i]) {
            rule_array[rule_count++] = cache.rules[i];
        }
    }
    
    clock_t start = clock();
    
    for (int iter = 0; iter < iterations; iter++) {
        vm_t vm;
        vm_init(&vm);
        vm.quiet = true;
        
        sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(2);
        
        while (vm.pc < program_size && !vm.halted) {
            instruction_t instr = program[vm.pc++];
            
            sfpm_dict_fact_source_add(facts, "opcode", sfpm_value_from_int(instr.op));
            g_contexts[instr.op].vm = &vm;
            g_contexts[instr.op].operand = instr.operand;
            
            sfpm_match(rule_array, rule_count, facts, false);
        }
        
        sfpm_fact_source_destroy(facts);
    }
    
    clock_t end = clock();
    sfpm_cache_destroy(&cache);
    return ((double)(end - start)) / CLOCKS_PER_SEC;
}

/* ============================================================================
 *                              MAIN PROGRAM
 * ============================================================================ */

static void print_header(const char *title) {
    printf("\n+================================================================+\n");
    printf("|  %-60s  |\n", title);
    printf("+================================================================+\n\n");
}

int main(void) {
    print_header("SFPM Interpreter with Caching Optimizations");
    
    /* Create test program: (10 + 5) * 3 = 45 */
    instruction_t program[] = {
        {OP_PUSH, 10},
        {OP_PUSH, 5},
        {OP_ADD, 0},
        {OP_PUSH, 3},
        {OP_MUL, 0},
        {OP_PRINT, 0},
        {OP_HALT, 0}
    };
    int program_size = sizeof(program) / sizeof(program[0]);
    
    printf("> Program: (10 + 5) * 3 = 45\n\n");
    
    /* Demonstrate each approach */
    printf("=== 1. SWITCH-BASED (Baseline) ===\n\n");
    run_switch_interpreter(program, program_size);
    
    printf("\n=== 2. DIRECT FUNCTION POINTER CACHE ===\n");
    printf("    (No SFPM overhead, O(1) dispatch)\n\n");
    run_direct_cached_interpreter(program, program_size);
    
    printf("\n=== 3. SFPM WITH RULE CACHE ===\n");
    printf("    (Direct rule lookup, skips pattern matching)\n\n");
    run_sfpm_cached_interpreter(program, program_size);
    
    printf("\n=== 4. SFPM WITH FACT REUSE ===\n");
    printf("    (Reuses fact source, still does pattern matching)\n\n");
    run_sfpm_fact_reuse_interpreter(program, program_size);
    
    /* Performance comparison */
    print_header("PERFORMANCE COMPARISON");
    
    instruction_t bench_program[] = {
        {OP_PUSH, 100},
        {OP_PUSH, 50},
        {OP_ADD, 0},
        {OP_PUSH, 2},
        {OP_MUL, 0},
        {OP_HALT, 0}
    };
    int bench_size = sizeof(bench_program) / sizeof(bench_program[0]);
    int iterations = 1000000;
    
    printf("> Benchmark: %d iterations of (100 + 50) * 2 = 300\n\n", iterations);
    
    double switch_time = benchmark_switch(bench_program, bench_size, iterations);
    double direct_time = benchmark_direct_cache(bench_program, bench_size, iterations);
    double sfpm_cached_time = benchmark_sfpm_cached(bench_program, bench_size, iterations);
    double sfpm_fact_reuse_time = benchmark_sfpm_fact_reuse(bench_program, bench_size, iterations);
    
    printf("┌────────────────────────────────┬─────────────┬─────────────┬──────────┐\n");
    printf("│ Approach                       │ Time (s)    │ Iter/sec    │ Overhead │\n");
    printf("├────────────────────────────────┼─────────────┼─────────────┼──────────┤\n");
    printf("│ Switch (baseline)              │ %7.3f     │ %9.0f   │   1.0x   │\n", 
           switch_time, iterations / switch_time);
    printf("│ Direct cache (no SFPM)         │ %7.3f     │ %9.0f   │   %.1fx   │\n",
           direct_time, iterations / direct_time, direct_time / switch_time);
    printf("│ SFPM + Rule cache              │ %7.3f     │ %9.0f   │   %.1fx   │\n",
           sfpm_cached_time, iterations / sfpm_cached_time, sfpm_cached_time / switch_time);
    printf("│ SFPM + Fact reuse              │ %7.3f     │ %9.0f   │   %.1fx   │\n",
           sfpm_fact_reuse_time, iterations / sfpm_fact_reuse_time, sfpm_fact_reuse_time / switch_time);
    printf("└────────────────────────────────┴─────────────┴─────────────┴──────────┘\n");
    
    print_header("OPTIMIZATION ANALYSIS");
    
    printf("1. DIRECT CACHE (Function Pointer Array)\n");
    printf("   • Fastest SFPM-like approach\n");
    printf("   • O(1) dispatch via array lookup\n");
    printf("   • ~%.0f%% of SFPM overhead eliminated\n", 
           100.0 * (1.0 - direct_time / sfpm_fact_reuse_time));
    printf("   • Still allows runtime modification\n");
    printf("   • Trade-off: Loses pattern matching capabilities\n\n");
    
    printf("2. SFPM + RULE CACHE\n");
    printf("   • Skips pattern matching step\n");
    printf("   • Direct rule execution via opcode index\n");
    printf("   • ~%.0f%% faster than full SFPM\n",
           100.0 * (1.0 - sfpm_cached_time / sfpm_fact_reuse_time));
    printf("   • Retains SFPM rule infrastructure\n");
    printf("   • Best of both worlds for known opcodes\n\n");
    
    printf("3. SFPM + FACT REUSE\n");
    printf("   • Eliminates fact source allocation overhead\n");
    printf("   • Still performs full pattern matching\n");
    printf("   • Marginal improvement over naive SFPM\n");
    printf("   • Good for scenarios with complex criteria\n\n");
    
    print_header("RECOMMENDATIONS");
    
    printf("Choose based on your needs:\n\n");
    
    printf("┌─────────────────────────┬──────────────────────────────────────┐\n");
    printf("│ Use Case                │ Recommended Approach                 │\n");
    printf("├─────────────────────────┼──────────────────────────────────────┤\n");
    printf("│ Maximum performance     │ Direct cache (%.1fx overhead)        │\n", 
           direct_time / switch_time);
    printf("│ Static opcode set       │                                      │\n");
    printf("│ Simple dispatch         │                                      │\n");
    printf("├─────────────────────────┼──────────────────────────────────────┤\n");
    printf("│ Runtime extensibility   │ SFPM + Rule cache (%.1fx overhead)   │\n",
           sfpm_cached_time / switch_time);
    printf("│ Hot-swapping needed     │                                      │\n");
    printf("│ Known opcode values     │                                      │\n");
    printf("├─────────────────────────┼──────────────────────────────────────┤\n");
    printf("│ Complex pattern matching│ SFPM + Fact reuse                    │\n");
    printf("│ Multi-criteria rules    │                                      │\n");
    printf("│ Dynamic dispatch logic  │                                      │\n");
    printf("└─────────────────────────┴──────────────────────────────────────┘\n");
    
    printf("\n💡 KEY INSIGHT:\n");
    printf("   For interpreters with known opcode sets, caching reduces\n");
    printf("   overhead from ~470x to ~%.1fx while retaining flexibility!\n",
           sfpm_cached_time / switch_time);
    
    return 0;
}
