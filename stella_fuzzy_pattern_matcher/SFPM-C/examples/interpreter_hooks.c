/**
 * interpreter_hooks.c
 * Demonstrates SFPM's before/after hook system for aspect-oriented programming.
 * 
 * This example shows four practical hook use cases:
 * 1. Logging - Trace execution of every opcode
 * 2. Validation - Prevent dangerous operations based on security level
 * 3. Performance - Time each opcode execution
 * 4. Debugging - Track stack depth and state changes
 * 
 * Hooks enable cross-cutting concerns without modifying core logic.
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
 * VIRTUAL MACHINE
 * ============================================================================ */

typedef enum {
    OP_PUSH = 1,
    OP_ADD,
    OP_SUB,
    OP_MUL,
    OP_DIV,
    OP_PRINT,
    OP_HALT,
    OP_STORE,     /* Store to memory (requires security) */
    OP_LOAD,      /* Load from memory */
    OP_SYSCALL,   /* System call (requires high security) */
} opcode_t;

typedef struct {
    int stack[256];
    int sp;               /* Stack pointer */
    int memory[256];      /* Simulated memory */
    int pc;               /* Program counter */
    uint8_t *program;
    int program_size;
    int security_level;   /* 0=low, 1=medium, 2=high */
    bool halted;
    
    /* Hook statistics */
    int exec_count;
    long long total_time_us;
    int max_stack_depth;
} vm_t;

/* Opcode context for hooks */
typedef struct {
    vm_t *vm;
    opcode_t opcode;
    int operand;
    
    /* Performance tracking */
    long long start_time_us;
} opcode_context_t;

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
 * VM OPERATIONS
 * ============================================================================ */

static void vm_init(vm_t *vm, uint8_t *program, int size, int security_level) {
    memset(vm, 0, sizeof(vm_t));
    vm->program = program;
    vm->program_size = size;
    vm->security_level = security_level;
    vm->sp = -1;
}

static void vm_push(vm_t *vm, int value) {
    if (vm->sp < 255) {
        vm->stack[++vm->sp] = value;
        if (vm->sp > vm->max_stack_depth) {
            vm->max_stack_depth = vm->sp;
        }
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

static void op_sub(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    int b = vm_pop(ctx->vm);
    int a = vm_pop(ctx->vm);
    vm_push(ctx->vm, a - b);
}

static void op_mul(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    int b = vm_pop(ctx->vm);
    int a = vm_pop(ctx->vm);
    vm_push(ctx->vm, a * b);
}

static void op_div(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    int b = vm_pop(ctx->vm);
    int a = vm_pop(ctx->vm);
    vm_push(ctx->vm, (b != 0) ? (a / b) : 0);
}

static void op_print(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    if (ctx->vm->sp >= 0) {
        printf("Result: %d\n", ctx->vm->stack[ctx->vm->sp]);
    }
}

static void op_store(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    int addr = vm_pop(ctx->vm);
    int value = vm_pop(ctx->vm);
    if (addr >= 0 && addr < 256) {
        ctx->vm->memory[addr] = value;
    }
}

static void op_load(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    int addr = vm_pop(ctx->vm);
    if (addr >= 0 && addr < 256) {
        vm_push(ctx->vm, ctx->vm->memory[addr]);
    }
}

static void op_syscall(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    printf("[SYSCALL] System call executed (dangerous!)\n");
}

static void op_halt(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    ctx->vm->halted = true;
}

/* ============================================================================
 * HOOK IMPLEMENTATIONS
 * ============================================================================ */

/* Logging hook - traces every operation */
static bool logging_before_hook(void *hook_data, void *payload_data) {
    const char *hook_name = (const char *)hook_data;
    opcode_context_t *ctx = (opcode_context_t *)payload_data;
    
    const char *opcode_names[] = {
        "???", "PUSH", "ADD", "SUB", "MUL", "DIV", "PRINT", "HALT",
        "STORE", "LOAD", "SYSCALL"
    };
    
    const char *name = (ctx->opcode > 0 && ctx->opcode <= OP_SYSCALL) 
                       ? opcode_names[ctx->opcode] : "UNKNOWN";
    
    printf("[LOG:%s] Executing %s", hook_name, name);
    if (ctx->opcode == OP_PUSH) {
        printf(" %d", ctx->operand);
    }
    printf(" (sp=%d)\n", ctx->vm->sp);
    
    return true;  /* Allow execution */
}

static bool logging_after_hook(void *hook_data, void *payload_data) {
    const char *hook_name = (const char *)hook_data;
    opcode_context_t *ctx = (opcode_context_t *)payload_data;
    
    printf("[LOG:%s] Completed (sp=%d)\n", hook_name, ctx->vm->sp);
    return true;
}

/* Security validation hook - prevents dangerous ops at low security levels */
static bool security_before_hook(void *hook_data, void *payload_data) {
    (void)hook_data;  /* Unused */
    opcode_context_t *ctx = (opcode_context_t *)payload_data;
    
    /* STORE requires medium security */
    if (ctx->opcode == OP_STORE && ctx->vm->security_level < 1) {
        printf("[SECURITY] DENIED: STORE requires medium security level\n");
        return false;  /* Abort execution */
    }
    
    /* SYSCALL requires high security */
    if (ctx->opcode == OP_SYSCALL && ctx->vm->security_level < 2) {
        printf("[SECURITY] DENIED: SYSCALL requires high security level\n");
        return false;  /* Abort execution */
    }
    
    return true;  /* Allow execution */
}

/* Performance timing hook */
static bool perf_before_hook(void *hook_data, void *payload_data) {
    (void)hook_data;
    opcode_context_t *ctx = (opcode_context_t *)payload_data;
    ctx->start_time_us = get_time_microseconds();
    return true;
}

static bool perf_after_hook(void *hook_data, void *payload_data) {
    (void)hook_data;
    opcode_context_t *ctx = (opcode_context_t *)payload_data;
    
    long long elapsed = get_time_microseconds() - ctx->start_time_us;
    ctx->vm->total_time_us += elapsed;
    ctx->vm->exec_count++;
    
    if (elapsed > 100) {  /* Report slow operations (>100 microseconds) */
        printf("[PERF] Slow operation detected: %lld us\n", elapsed);
    }
    
    return true;
}

/* Debugging hook - tracks stack depth */
static bool debug_before_hook(void *hook_data, void *payload_data) {
    (void)hook_data;
    opcode_context_t *ctx = (opcode_context_t *)payload_data;
    
    if (ctx->vm->sp > 200) {  /* Warn about stack overflow risk */
        printf("[DEBUG] WARNING: Stack depth is %d (close to overflow!)\n", 
               ctx->vm->sp);
    }
    
    return true;
}

/* ============================================================================
 * INTERPRETER
 * ============================================================================ */

/* Global contexts for each opcode */
static opcode_context_t g_opcode_contexts[128];

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
    /* Create criteria for matching opcode */
    sfpm_criteria_t **criterias = malloc(sizeof(sfpm_criteria_t*) * 1);
    criterias[0] = sfpm_criteria_create("opcode", SFPM_OP_EQUAL, sfpm_value_from_int(opcode));
    
    /* Create rule */
    char name[64];
    snprintf(name, sizeof(name), "opcode_%d", opcode);
    
    sfpm_rule_t *rule = sfpm_rule_create(criterias, 1, handler, &g_opcode_contexts[opcode], name);
    /* Note: criterias array is owned by the rule, don't free */
    
    interp->rules[interp->rule_count++] = rule;
}

static void interpreter_set_hooks(interpreter_t *interp,
                                 sfpm_hook_fn before_hook,
                                 void *before_data,
                                 sfpm_hook_fn after_hook,
                                 void *after_data) {
    /* Apply hooks to all registered rules */
    for (int i = 0; i < interp->rule_count; i++) {
        if (interp->rules[i]) {
            if (before_hook) {
                sfpm_rule_add_before_hook(interp->rules[i], before_hook, before_data);
            }
            if (after_hook) {
                sfpm_rule_add_after_hook(interp->rules[i], after_hook, after_data);
            }
        }
    }
}

static void interpreter_run(interpreter_t *interp) {
    vm_t *vm = interp->vm;
    
    while (vm->pc < vm->program_size && !vm->halted) {
        opcode_t opcode = (opcode_t)vm->program[vm->pc++];
        int operand = 0;
        
        /* Read operand for PUSH */
        if (opcode == OP_PUSH && vm->pc < vm->program_size) {
            operand = vm->program[vm->pc++];
        }
        
        /* Update global context for this opcode */
        g_opcode_contexts[opcode].vm = vm;
        g_opcode_contexts[opcode].opcode = opcode;
        g_opcode_contexts[opcode].operand = operand;
        
        /* Create fact source for current opcode */
        sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(1);
        sfpm_dict_fact_source_add(facts, "opcode", sfpm_value_from_int(opcode));
        
        /* Execute through SFPM (hooks will fire automatically) */
        sfpm_match(interp->rules, interp->rule_count, facts, false);
        
        sfpm_fact_source_destroy(facts);
    }
}

/* ============================================================================
 * DEMO SCENARIOS
 * ============================================================================ */

static void demo_logging(void) {
    printf("\n");
    printf("+----------------------------------------------------------+\n");
    printf("| DEMO 1: LOGGING HOOKS                                    |\n");
    printf("| Traces every opcode execution with before/after logging |\n");
    printf("+----------------------------------------------------------+\n\n");
    
    uint8_t program[] = {
        OP_PUSH, 10,
        OP_PUSH, 5,
        OP_ADD,
        OP_PUSH, 3,
        OP_MUL,
        OP_PRINT,
        OP_HALT
    };
    
    vm_t vm;
    vm_init(&vm, program, sizeof(program), 2);
    
    interpreter_t interp;
    interpreter_init(&interp, &vm);
    
    /* Register opcodes */
    interpreter_register_opcode(&interp, OP_PUSH, op_push);
    interpreter_register_opcode(&interp, OP_ADD, op_add);
    interpreter_register_opcode(&interp, OP_MUL, op_mul);
    interpreter_register_opcode(&interp, OP_PRINT, op_print);
    interpreter_register_opcode(&interp, OP_HALT, op_halt);
    
    /* Install logging hooks */
    interpreter_set_hooks(&interp,
                         logging_before_hook, (void *)"TRACE",
                         logging_after_hook, (void *)"TRACE");
    
    printf("Program: PUSH 10, PUSH 5, ADD, PUSH 3, MUL, PRINT\n");
    printf("Expected: (10 + 5) * 3 = 45\n\n");
    
    interpreter_run(&interp);
    
    interpreter_destroy(&interp);
}

static void demo_security(void) {
    printf("\n");
    printf("+----------------------------------------------------------+\n");
    printf("| DEMO 2: SECURITY VALIDATION HOOKS                        |\n");
    printf("| Prevents dangerous operations based on security level   |\n");
    printf("+----------------------------------------------------------+\n\n");
    
    uint8_t program[] = {
        OP_PUSH, 42,
        OP_PUSH, 10,
        OP_STORE,     /* Try to store - requires medium security */
        OP_SYSCALL,   /* Try syscall - requires high security */
        OP_HALT
    };
    
    printf("--- Running with LOW security level (0) ---\n");
    vm_t vm_low;
    vm_init(&vm_low, program, sizeof(program), 0);  /* Low security */
    
    interpreter_t interp_low;
    interpreter_init(&interp_low, &vm_low);
    
    interpreter_register_opcode(&interp_low, OP_PUSH, op_push);
    interpreter_register_opcode(&interp_low, OP_STORE, op_store);
    interpreter_register_opcode(&interp_low, OP_SYSCALL, op_syscall);
    interpreter_register_opcode(&interp_low, OP_HALT, op_halt);
    
    interpreter_set_hooks(&interp_low,
                         security_before_hook, NULL,
                         NULL, NULL);
    
    interpreter_run(&interp_low);
    interpreter_destroy(&interp_low);
    
    printf("\n--- Running with HIGH security level (2) ---\n");
    vm_t vm_high;
    vm_init(&vm_high, program, sizeof(program), 2);  /* High security */
    
    interpreter_t interp_high;
    interpreter_init(&interp_high, &vm_high);
    
    interpreter_register_opcode(&interp_high, OP_PUSH, op_push);
    interpreter_register_opcode(&interp_high, OP_STORE, op_store);
    interpreter_register_opcode(&interp_high, OP_SYSCALL, op_syscall);
    interpreter_register_opcode(&interp_high, OP_HALT, op_halt);
    
    interpreter_set_hooks(&interp_high,
                         security_before_hook, NULL,
                         NULL, NULL);
    
    interpreter_run(&interp_high);
    interpreter_destroy(&interp_high);
}

static void demo_performance(void) {
    printf("\n");
    printf("+----------------------------------------------------------+\n");
    printf("| DEMO 3: PERFORMANCE MONITORING HOOKS                     |\n");
    printf("| Times each opcode and tracks overall statistics         |\n");
    printf("+----------------------------------------------------------+\n\n");
    
    /* Create a program with many operations */
    uint8_t program[1024];
    int idx = 0;
    
    /* Calculate: sum of 1..10 = 55 */
    for (int i = 1; i <= 10; i++) {
        program[idx++] = OP_PUSH;
        program[idx++] = i;
        if (i > 1) {
            program[idx++] = OP_ADD;
        }
    }
    program[idx++] = OP_PRINT;
    program[idx++] = OP_HALT;
    
    vm_t vm;
    vm_init(&vm, program, idx, 2);
    
    interpreter_t interp;
    interpreter_init(&interp, &vm);
    
    interpreter_register_opcode(&interp, OP_PUSH, op_push);
    interpreter_register_opcode(&interp, OP_ADD, op_add);
    interpreter_register_opcode(&interp, OP_PRINT, op_print);
    interpreter_register_opcode(&interp, OP_HALT, op_halt);
    
    /* Install performance hooks */
    interpreter_set_hooks(&interp,
                         perf_before_hook, NULL,
                         perf_after_hook, NULL);
    
    printf("Program: sum of 1..10\n");
    printf("Expected: 55\n\n");
    
    interpreter_run(&interp);
    
    printf("\n[PERF REPORT]\n");
    printf("  Total operations: %d\n", vm.exec_count);
    printf("  Total time: %lld us\n", vm.total_time_us);
    printf("  Average per op: %.2f us\n", 
           (double)vm.total_time_us / vm.exec_count);
    
    interpreter_destroy(&interp);
}

static void demo_debugging(void) {
    printf("\n");
    printf("+----------------------------------------------------------+\n");
    printf("| DEMO 4: DEBUGGING HOOKS                                  |\n");
    printf("| Tracks stack depth and warns about potential issues     |\n");
    printf("+----------------------------------------------------------+\n\n");
    
    /* Create a program that builds up stack depth */
    uint8_t program[512];
    int idx = 0;
    
    /* Push 50 values onto stack */
    for (int i = 0; i < 50; i++) {
        program[idx++] = OP_PUSH;
        program[idx++] = i;
    }
    program[idx++] = OP_PRINT;
    program[idx++] = OP_HALT;
    
    vm_t vm;
    vm_init(&vm, program, idx, 2);
    
    interpreter_t interp;
    interpreter_init(&interp, &vm);
    
    interpreter_register_opcode(&interp, OP_PUSH, op_push);
    interpreter_register_opcode(&interp, OP_PRINT, op_print);
    interpreter_register_opcode(&interp, OP_HALT, op_halt);
    
    /* Install debug hooks (and logging to see progress) */
    interpreter_set_hooks(&interp,
                         debug_before_hook, NULL,
                         NULL, NULL);
    
    printf("Program: Push 50 values onto stack\n");
    printf("Stack warning threshold: 200\n\n");
    
    interpreter_run(&interp);
    
    printf("\n[DEBUG REPORT]\n");
    printf("  Max stack depth reached: %d\n", vm.max_stack_depth);
    
    interpreter_destroy(&interp);
}

/* ============================================================================
 * MAIN
 * ============================================================================ */

int main(void) {
    printf("========================================================\n");
    printf("SFPM INTERPRETER WITH HOOKS DEMONSTRATION\n");
    printf("========================================================\n");
    printf("\n");
    printf("This example demonstrates aspect-oriented programming\n");
    printf("using before/after hooks in SFPM:\n");
    printf("\n");
    printf("  - Logging: Trace execution flow\n");
    printf("  - Security: Validate permissions before execution\n");
    printf("  - Performance: Time and profile operations\n");
    printf("  - Debugging: Monitor runtime state\n");
    printf("\n");
    printf("Hooks enable cross-cutting concerns without modifying\n");
    printf("the core interpreter logic!\n");
    
    demo_logging();
    demo_security();
    demo_performance();
    demo_debugging();
    
    printf("\n");
    printf("+----------------------------------------------------------+\n");
    printf("| SUMMARY: HOOK CAPABILITIES                              |\n");
    printf("+----------------------------------------------------------+\n");
    printf("| Before hooks can:                                        |\n");
    printf("|   - Log/trace execution                                 |\n");
    printf("|   - Validate preconditions                              |\n");
    printf("|   - Abort execution (return false)                      |\n");
    printf("|   - Start timers                                        |\n");
    printf("|   - Modify context before payload                       |\n");
    printf("|                                                          |\n");
    printf("| After hooks can:                                         |\n");
    printf("|   - Log completion                                      |\n");
    printf("|   - Collect metrics                                     |\n");
    printf("|   - Verify postconditions                               |\n");
    printf("|   - Transform results                                   |\n");
    printf("|   - Clean up resources                                  |\n");
    printf("+----------------------------------------------------------+\n");
    
    return 0;
}
