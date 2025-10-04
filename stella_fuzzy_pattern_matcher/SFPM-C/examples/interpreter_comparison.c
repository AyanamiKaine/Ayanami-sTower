/**
 * @file interpreter_comparison.c
 * @brief Comprehensive comparison: Switch-based vs SFPM-based interpreter
 * 
 * Demonstrates how SFPM can replace switch statements in virtual machine interpreters,
 * enabling runtime modification, hot-swapping, and extensibility.
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
    OP_SQUARE = 100  /* Extension opcode for demonstration */
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
 *                      SWITCH-BASED INTERPRETER
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
            
            case OP_SUB: {
                int b = vm_pop(&vm);
                int a = vm_pop(&vm);
                int result = a - b;
                printf("[SUB] %d - %d = %d\n", a, b, result);
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
            
            case OP_DIV: {
                int b = vm_pop(&vm);
                int a = vm_pop(&vm);
                if (b == 0) {
                    fprintf(stderr, "Division by zero!\n");
                    exit(1);
                }
                int result = a / b;
                printf("[DIV] %d / %d = %d\n", a, b, result);
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
 *                      SFPM-BASED INTERPRETER
 * ============================================================================ */

/* Opcode handler function type */
typedef void (*opcode_handler_fn)(vm_t *vm, int operand);

/* Opcode handlers */
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

static void op_square(vm_t *vm, int operand) {
    (void)operand;
    int value = vm_pop(vm);
    int result = value * value;
    printf("[SQUARE] %d² = %d\n", value, result);
    vm_push(vm, result);
}

/* Context for opcode handlers */
typedef struct {
    vm_t *vm;
    int operand;
    opcode_handler_fn handler;
} opcode_context_t;

/* Global contexts for each opcode (simplified for demo) */
static opcode_context_t g_contexts[128];

/* Wrapper payload for SFPM rules */
static void execute_opcode_handler(void *user_data) {
    opcode_context_t *ctx = (opcode_context_t *)user_data;
    ctx->handler(ctx->vm, ctx->operand);
}

/* Create an SFPM rule for an opcode */
static sfpm_rule_t *create_opcode_rule(opcode_t opcode, opcode_handler_fn handler) {
    /* Setup context */
    g_contexts[opcode].handler = handler;
    
    /* Create criteria for matching opcode */
    sfpm_criteria_t **criterias = malloc(sizeof(sfpm_criteria_t*) * 1);
    criterias[0] = sfpm_criteria_create("opcode", SFPM_OP_EQUAL, sfpm_value_from_int(opcode));
    
    /* Create rule */
    char name[64];
    snprintf(name, sizeof(name), "opcode_%d", opcode);
    
    sfpm_rule_t *rule = sfpm_rule_create(criterias, 1, execute_opcode_handler, &g_contexts[opcode], name);
    return rule;
}

static void run_sfpm_interpreter(instruction_t *program, int program_size) {
    vm_t vm;
    vm_init(&vm);
    
    /* Create opcode rules */
    sfpm_rule_t *rules[10];
    int rule_count = 0;
    
    rules[rule_count++] = create_opcode_rule(OP_PUSH, op_push);
    rules[rule_count++] = create_opcode_rule(OP_ADD, op_add);
    rules[rule_count++] = create_opcode_rule(OP_SUB, op_sub);
    rules[rule_count++] = create_opcode_rule(OP_MUL, op_mul);
    rules[rule_count++] = create_opcode_rule(OP_DIV, op_div);
    rules[rule_count++] = create_opcode_rule(OP_PRINT, op_print);
    rules[rule_count++] = create_opcode_rule(OP_HALT, op_halt);
    
    /* Execute program */
    while (vm.pc < program_size && !vm.halted) {
        instruction_t instr = program[vm.pc++];
        
        /* Create fact source for current instruction */
        sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(2);
        sfpm_dict_fact_source_add(facts, "opcode", sfpm_value_from_int(instr.op));
        
        /* Update context for this instruction */
        g_contexts[instr.op].vm = &vm;
        g_contexts[instr.op].operand = instr.operand;
        
        /* Match and execute */
        sfpm_match(rules, rule_count, facts, false);
        
        sfpm_fact_source_destroy(facts);
    }
    
    printf("\nResult: %d\n", vm_peek(&vm));
    
    /* Cleanup */
    for (int i = 0; i < rule_count; i++) {
        sfpm_rule_destroy(rules[i]);
    }
}

/* ============================================================================
 *                      PERFORMANCE BENCHMARKING
 * ============================================================================ */

static double benchmark_switch(instruction_t *program, int program_size, int iterations) {
    clock_t start = clock();
    
    for (int i = 0; i < iterations; i++) {
        vm_t vm;
        vm_init(&vm);
        vm.quiet = true;  /* Disable output for benchmarking */
        
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

static double benchmark_sfpm(instruction_t *program, int program_size, int iterations) {
    /* Setup rules once */
    sfpm_rule_t *rules[10];
    int rule_count = 0;
    
    rules[rule_count++] = create_opcode_rule(OP_PUSH, op_push);
    rules[rule_count++] = create_opcode_rule(OP_ADD, op_add);
    rules[rule_count++] = create_opcode_rule(OP_MUL, op_mul);
    rules[rule_count++] = create_opcode_rule(OP_PRINT, op_print);
    rules[rule_count++] = create_opcode_rule(OP_HALT, op_halt);
    
    clock_t start = clock();
    
    for (int iter = 0; iter < iterations; iter++) {
        vm_t vm;
        vm_init(&vm);
        vm.quiet = true;  /* Disable output for benchmarking */
        
        while (vm.pc < program_size && !vm.halted) {
            instruction_t instr = program[vm.pc++];
            
            sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(2);
            sfpm_dict_fact_source_add(facts, "opcode", sfpm_value_from_int(instr.op));
            
            /* Update context */
            g_contexts[instr.op].vm = &vm;
            g_contexts[instr.op].operand = instr.operand;
            
            sfpm_match(rules, rule_count, facts, false);
            sfpm_fact_source_destroy(facts);
        }
    }
    
    clock_t end = clock();
    
    /* Cleanup */
    for (int i = 0; i < rule_count; i++) {
        sfpm_rule_destroy(rules[i]);
    }
    
    return ((double)(end - start)) / CLOCKS_PER_SEC;
}

/* ============================================================================
 *                              MAIN PROGRAM
 * ============================================================================ */

static void print_header(const char *title) {
    printf("\n+==============================================================+\n");
    printf("|                                                              |\n");
    printf("|  %-58s  |\n", title);
    printf("|                                                              |\n");
    printf("+==============================================================+\n\n");
}

static void print_section(const char *title) {
    printf("\n+==========================================================+\n");
    printf("|  %-54s  |\n", title);
    printf("+==========================================================+\n\n");
}

int main(void) {
    print_header("Switch-Based vs SFPM-Based Interpreter");
    printf("                   Comprehensive Comparison\n");
    
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
    
    printf("> Program: (10 + 5) * 3 = 45\n");
    
    /* Run switch-based interpreter */
    printf("\n=== SWITCH-BASED INTERPRETER ===\n\n");
    run_switch_interpreter(program, program_size);
    
    /* Run SFPM-based interpreter */
    printf("\n=== SFPM-BASED INTERPRETER ===\n\n");
    run_sfpm_interpreter(program, program_size);
    
    /* Demonstration: Runtime Extension */
    print_section("DEMONSTRATION: Runtime Extension");
    printf("> Task: Add a new SQUARE opcode (opcode 100) that squares TOS\n\n");
    printf("X SWITCH APPROACH:\n");
    printf("   1. Edit source code to add 'case 100:'\n");
    printf("   2. Recompile entire program\n");
    printf("   3. Restart program with new binary\n");
    printf("   4. Cannot hot-swap in running program\n\n");
    printf("* SFPM APPROACH:\n");
    printf("   1. Define new handler function:\n");
    printf("      void op_square(void *vm) { /* ... */ }\n");
    printf("   2. Add to rule table:\n");
    printf("      rules[n] = create_opcode_rule(100, op_square, vm);\n");
    printf("   3. Done! No recompilation, no restart needed\n");
    printf("   4. Can add/remove at runtime\n");
    
    /* Demonstration: Hot Swapping */
    print_section("DEMONSTRATION: Hot Swapping");
    printf("> Task: Fix a bug in the ADD opcode while program is running\n\n");
    printf("X SWITCH APPROACH:\n");
    printf("   1. Stop the program\n");
    printf("   2. Edit the switch case\n");
    printf("   3. Recompile\n");
    printf("   4. Restart and lose state\n\n");
    printf("* SFPM APPROACH:\n");
    printf("   1. Keep program running\n");
    printf("   2. Destroy old rule:\n");
    printf("      sfpm_rule_destroy(rules[OP_ADD]);\n");
    printf("   3. Create fixed rule:\n");
    printf("      rules[OP_ADD] = create_opcode_rule(OP_ADD, fixed_add, vm);\n");
    printf("   4. Next ADD instruction uses new implementation\n");
    printf("   5. State preserved, zero downtime\n");
    
    /* Demonstration: Isolated Testing */
    print_section("DEMONSTRATION: Isolated Testing");
    printf("> Task: Unit test the ADD opcode\n\n");
    printf("X SWITCH APPROACH:\n");
    printf("   1. Must test through entire VM execution\n");
    printf("   2. Need to construct valid bytecode\n");
    printf("   3. Hard to isolate just ADD logic\n");
    printf("   4. Coupled to switch statement\n\n");
    printf("* SFPM APPROACH:\n");
    printf("   1. Test handler function directly:\n");
    printf("      vm_t vm; vm_push(&vm, 5); vm_push(&vm, 3);\n");
    printf("      op_add(&vm, 0);\n");
    printf("      assert(vm_pop(&vm) == 8);\n");
    printf("   2. No bytecode needed\n");
    printf("   3. Complete isolation\n");
    printf("   4. Fast, focused tests\n");
    
    /* Demonstration: Conditional Opcodes */
    print_section("DEMONSTRATION: Conditional Opcodes");
    printf("> Task: Disable dangerous opcodes in sandbox mode\n\n");
    printf("X SWITCH APPROACH:\n");
    printf("   case OP_SYSCALL:\n");
    printf("       if (!vm->sandbox_mode) {\n");
    printf("           /* execute */\n");
    printf("       } else {\n");
    printf("           /* error */\n");
    printf("       }\n");
    printf("   - Must check in every case\n");
    printf("   - Easy to forget\n");
    printf("   - Security risk\n\n");
    printf("* SFPM APPROACH:\n");
    printf("   if (vm->sandbox_mode) {\n");
    printf("       /* Don't register dangerous opcodes */\n");
    printf("   } else {\n");
    printf("       rules[n] = create_opcode_rule(OP_SYSCALL, ...);\n");
    printf("   }\n");
    printf("   - Physically impossible to execute unregistered opcodes\n");
    printf("   - Fail-safe by design\n");
    
    /* Performance Comparison */
    print_section("PERFORMANCE COMPARISON");
    printf("> Testing pure computational performance (no I/O)\n");
    printf("> Program: (100 + 50) * 2 = 300\n\n");
    
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
    
    printf("%d iterations of (100 + 50) * 2:\n\n", iterations);
    
    double switch_time = benchmark_switch(bench_program, bench_size, iterations);
    double sfpm_time = benchmark_sfpm(bench_program, bench_size, iterations);
    double overhead = sfpm_time / switch_time;
    
    printf("  Switch-based: %.3f seconds (%d iterations/sec)\n", 
           switch_time, (int)(iterations / switch_time));
    printf("  SFPM-based:   %.3f seconds (%d iterations/sec)\n",
           sfpm_time, (int)(iterations / sfpm_time));
    printf("  Overhead:     %.1fx\n", overhead);
    
    printf("\n! Analysis:\n");
    printf("   - SFPM has ~%.0fx overhead for simple opcodes\n", overhead);
    printf("   - Overhead is from pattern matching + function pointers\n");
    printf("   - Overhead decreases with complex opcode logic\n");
    printf("   - Trade-off: flexibility vs raw speed\n");
    printf("   - Acceptable for scripting/config languages\n");
    printf("   - NOT recommended for hot-path performance code\n");
    
    /* Conclusion */
    print_header("CONCLUSION");
    printf("|  SFPM provides:                                              |\n");
    printf("|    * Runtime extensibility                                  |\n");
    printf("|    * Hot swapping                                           |\n");
    printf("|    * Isolated testing                                       |\n");
    printf("|    * Conditional execution                                  |\n");
    printf("|    * Plugin architecture                                    |\n");
    printf("|    * Fail-safe security                                     |\n");
    printf("|                                                              |\n");
    printf("|  At the cost of:                                             |\n");
    printf("|    !  Significant performance overhead (~%.0fx)              |\n", overhead);
    printf("|                                                              |\n");
    printf("|  Perfect for:                                                |\n");
    printf("|    - Game scripting engines (non-critical path)            |\n");
    printf("|    - Configuration languages                                |\n");
    printf("|    - Plugin-extensible systems                              |\n");
    printf("|    - Debuggable/instrumented VMs                            |\n");
    printf("|    - AI behavior trees / decision systems                   |\n");
    printf("|                                                              |\n");
    printf("|  NOT suitable for:                                           |\n");
    printf("|    - Hot-path game loops                                    |\n");
    printf("|    - High-frequency trading systems                         |\n");
    printf("|    - Real-time audio/video processing                       |\n");
    printf("+==============================================================+\n");
    
    return 0;
}
