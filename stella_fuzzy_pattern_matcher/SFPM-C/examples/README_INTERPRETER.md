# SFPM Interpreter Comparison Example

## Overview

This example demonstrates how **SFPM (Stella Fuzzy Pattern Matcher)** can replace traditional switch statements in virtual machine interpreters, enabling runtime modification, hot-swapping, and extensibility.

## The Problem: Traditional Switch-Based Interpreters

A typical bytecode interpreter uses a switch statement to dispatch opcodes:

```c
while (vm.pc < program_size) {
    instruction_t instr = program[vm.pc++];

    switch (instr.op) {
        case OP_PUSH: /* ... */ break;
        case OP_ADD:  /* ... */ break;
        case OP_MUL:  /* ... */ break;
        // ... more opcodes
    }
}
```

**Limitations:**

-   ❌ Cannot add new opcodes at runtime
-   ❌ Cannot hot-swap opcode implementations
-   ❌ Difficult to unit test individual opcodes
-   ❌ Security: must check permissions in every case
-   ❌ Plugin systems require recompilation

## The Solution: SFPM-Based Interpreters

SFPM allows you to represent opcodes as **rules** that can be dynamically registered, modified, and removed:

```c
// Define opcode handlers
void op_add(vm_t *vm, int operand) {
    int b = vm_pop(vm);
    int a = vm_pop(vm);
    vm_push(vm, a + b);
}

// Register opcode as a rule
rules[0] = create_opcode_rule(OP_ADD, op_add);

// Execute: SFPM matches opcode and calls handler
sfpm_match(rules, rule_count, facts, false);
```

## Key Benefits

### 1. **Runtime Extension**

Add new opcodes without recompilation:

```c
// At runtime, add a new SQUARE opcode
rules[n] = create_opcode_rule(OP_SQUARE, op_square);
```

### 2. **Hot Swapping**

Replace buggy implementations while the program runs:

```c
// Fix a bug in ADD without stopping the VM
sfpm_rule_destroy(rules[OP_ADD]);
rules[OP_ADD] = create_opcode_rule(OP_ADD, fixed_add);
```

### 3. **Isolated Testing**

Test opcode handlers directly:

```c
// Unit test without VM infrastructure
vm_t vm;
vm_push(&vm, 5);
vm_push(&vm, 3);
op_add(&vm, 0);
assert(vm_pop(&vm) == 8);  // ✓
```

### 4. **Fail-Safe Security**

Sandbox mode by not registering dangerous opcodes:

```c
if (!vm->sandbox_mode) {
    rules[n] = create_opcode_rule(OP_SYSCALL, op_syscall);
}
// Physically impossible to execute unregistered opcodes
```

### 5. **Plugin Architecture**

Load opcode handlers from shared libraries:

```c
void *plugin = dlopen("my_opcodes.so", RTLD_NOW);
opcode_fn handler = dlsym(plugin, "op_custom");
rules[n] = create_opcode_rule(OP_CUSTOM, handler);
```

## Performance Trade-offs

Based on benchmarking 1,000,000 iterations of `(100 + 50) * 2`:

| Approach              | Time   | Iterations/sec | Overhead |
| --------------------- | ------ | -------------- | -------- |
| Switch-based          | 0.004s | 250,000,000    | 1x       |
| **SFPM + Rule cache** | 0.014s | 71,000,000     | **3.5x** |
| SFPM (naive)          | 1.924s | 519,750        | ~481x    |

> 💡 **Caching optimization reduces overhead from ~481x to ~3.5x!**  
> See `interpreter_cached.c` and `README_CACHING.md` for details.

**Analysis:**

-   ✅ **Acceptable for:** Game scripting, config languages, plugin systems, AI decision trees
-   ❌ **NOT for:** Hot-path game loops, HFT systems, real-time audio/video

The overhead comes from:

1. Pattern matching (criteria evaluation)
2. Dynamic rule selection
3. Function pointer indirection
4. Fact source creation/destruction per instruction

## Running the Example

```bash
cd build
cmake --build . --config Release --target sfpm_comparison
./Release/sfpm_comparison.exe

# With caching optimizations:
cmake --build . --config Release --target sfpm_cached
./Release/sfpm_cached.exe
```

## Use Cases

### ✅ Perfect For:

-   **Game Scripting Engines** (Lua/Python-like VMs for game logic)
-   **Configuration Languages** (DSLs for game data, AI behaviors)
-   **Debuggable VMs** (instrospection, profiling, breakpoints)
-   **Modding Systems** (players add custom behaviors)
-   **AI Behavior Trees** (runtime editing of NPC logic)

### ❌ Not Suitable For:

-   Main game loop (60+ FPS required)
-   Physics simulation (tight loops)
-   High-frequency trading
-   Real-time audio/video processing

## Architecture Insights

### Switch-Based Interpreter

```
Bytecode → Switch Statement → Inline Handler Code
          (compile-time dispatch)
```

**Pros:** Fast, simple, predictable  
**Cons:** Inflexible, monolithic, hard to extend

### SFPM-Based Interpreter

```
Bytecode → Fact Source → Pattern Matcher → Rule → Handler Function
          (runtime dispatch)
```

**Pros:** Flexible, extensible, testable  
**Cons:** Slower, more complex setup

## Implementation Pattern

1. **Define handler functions** with a consistent signature:

    ```c
    typedef void (*opcode_handler_fn)(vm_t *vm, int operand);
    ```

2. **Create rules** that map opcodes to handlers:

    ```c
    sfpm_rule_t *create_opcode_rule(opcode_t opcode, opcode_handler_fn handler);
    ```

3. **Execute** by matching facts against rules:
    ```c
    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(2);
    sfpm_dict_fact_source_add(facts, "opcode", sfpm_value_from_int(instr.op));
    sfpm_match(rules, rule_count, facts, false);
    ```

## Conclusion

SFPM transforms interpreters from **static, monolithic switch statements** into **dynamic, extensible rule systems**. This enables:

-   Runtime modification
-   Plugin architectures
-   Hot-swapping
-   Better testing
-   Fail-safe security

The ~481x performance overhead is acceptable for non-critical paths like scripting, configuration, and AI decision-making, but not for performance-critical code.

---

**See also:**

-   `basic_example.c` - Introduction to SFPM concepts
-   `sfpm.h` - API documentation
-   Main README - Library overview
