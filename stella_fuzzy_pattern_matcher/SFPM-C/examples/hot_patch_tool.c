#include <sfpm/sfpm.h>
#include <sfpm/snapshot.h>
#include <stdio.h>
#include <stdint.h>
#include <stdbool.h>
#include <string.h>

/* Reuse the vm_t and functions from interpreter_hot_reload.c by
   copying the minimal VM implementation we need here. This keeps the
   example self-contained. */

typedef enum {
    OP_PUSH,
    OP_ADD,
    OP_SUB,
    OP_MUL,
    OP_PRINT,
    OP_HALT,
    OP_NOP
} opcode_t;

#define STACK_SIZE 256
#define PROGRAM_SIZE 1024

typedef struct {
    int stack[STACK_SIZE];
    int sp;
    uint8_t program[PROGRAM_SIZE];
    size_t program_size;
    size_t pc;
    bool halted;
    int iteration_count;
} vm_t;

void vm_init(vm_t *vm) {
    memset(vm, 0, sizeof(vm_t));
    vm->sp = -1;
    vm->pc = 0;
    vm->halted = false;
    vm->iteration_count = 0;
}

void vm_load_program(vm_t *vm, const uint8_t *program, size_t size) {
    if (size > PROGRAM_SIZE) size = PROGRAM_SIZE;
    memcpy(vm->program, program, size);
    vm->program_size = size;
    vm->pc = 0;
}

bool vm_step(vm_t *vm) {
    if (vm->pc >= vm->program_size || vm->halted) return false;
    opcode_t op = (opcode_t)vm->program[vm->pc++];
    switch (op) {
        case OP_PUSH: {
            if (vm->pc >= vm->program_size) return false;
            int value = (int)vm->program[vm->pc++];
            vm->stack[++vm->sp] = value;
            break;
        }
        case OP_ADD: {
            if (vm->sp < 1) return false;
            int b = vm->stack[vm->sp--];
            int a = vm->stack[vm->sp--];
            vm->stack[++vm->sp] = a + b;
            break;
        }
        case OP_SUB: {
            if (vm->sp < 1) return false;
            int b = vm->stack[vm->sp--];
            int a = vm->stack[vm->sp--];
            vm->stack[++vm->sp] = a - b;
            break;
        }
        case OP_MUL: {
            if (vm->sp < 1) return false;
            int b = vm->stack[vm->sp--];
            int a = vm->stack[vm->sp--];
            vm->stack[++vm->sp] = a * b;
            break;
        }
        case OP_PRINT: {
            if (vm->sp < 0) return false;
            printf("Result: %d\n", vm->stack[vm->sp]);
            break;
        }
        case OP_HALT:
            vm->halted = true;
            return false;
        case OP_NOP:
            break;
        default:
            fprintf(stderr, "Unknown opcode: %d\n", op);
            return false;
    }
    return true;
}

void vm_run(vm_t *vm) {
    vm->iteration_count++;
    while (vm_step(vm)) { }
}

void vm_patch_program(vm_t *vm, size_t offset, uint8_t value) {
    if (offset < vm->program_size) {
        printf("[PATCH] program[%zu]: %u -> %u\n", offset, vm->program[offset], value);
        vm->program[offset] = value;
    } else {
        fprintf(stderr, "[PATCH] offset out of range\n");
    }
}

bool vm_save_snapshot(vm_t *vm, const char *filename) {
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    if (!snapshot) return false;
    sfpm_memory_region_t region = {
        .base_address = vm,
        .size = sizeof(vm_t),
        .name = "vm_state",
        .is_dynamic = false
    };
    if (!sfpm_snapshot_add_region(snapshot, &region)) {
        sfpm_snapshot_destroy(snapshot);
        return false;
    }
    char desc[256];
    snprintf(desc, sizeof(desc), "Program snapshot (iter=%d, pc=%zu)", vm->iteration_count, vm->pc);
    sfpm_snapshot_set_description(snapshot, desc);
    bool ok = sfpm_snapshot_save(snapshot, filename);
    sfpm_snapshot_destroy(snapshot);
    return ok;
}

int main(void) {
    vm_t vm;
    vm_init(&vm);

    uint8_t program[] = {
        OP_PUSH, 10,
        OP_PUSH, 5,
        OP_ADD,
        OP_PRINT,
        OP_HALT
    };

    vm_load_program(&vm, program, sizeof(program));

    printf("Initial run (expected 15):\n");
    vm.pc = 0; vm.sp = -1; vm.halted = false;
    vm_run(&vm);

    /* Patch ADD -> MUL (offset 4 -> opcode) */
    vm_patch_program(&vm, 4, OP_MUL);

    printf("After patch (expected 50):\n");
    vm.pc = 0; vm.sp = -1; vm.halted = false;
    vm_run(&vm);

    if (vm_save_snapshot(&vm, "interpreter.img")) {
        printf("Snapshot written to interpreter.img\n");
    } else {
        fprintf(stderr, "Failed to write snapshot\n");
        return 1;
    }

    return 0;
}
