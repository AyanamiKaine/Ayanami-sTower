#include <sfpm/sfpm.h>
#include <sfpm/snapshot.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>

/* Simple VM with hot reload capability */
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
    int iteration_count;  /* Track how many times we've run */
} vm_t;

/* Snapshot filename */
#define SNAPSHOT_FILE "interpreter.img"

/* Initialize VM */
void vm_init(vm_t *vm) {
    memset(vm, 0, sizeof(vm_t));
    vm->sp = -1;
    vm->pc = 0;
    vm->halted = false;
    vm->iteration_count = 0;
}

/* Load program */
void vm_load_program(vm_t *vm, const uint8_t *program, size_t size) {
    if (size > PROGRAM_SIZE) {
        size = PROGRAM_SIZE;
    }
    memcpy(vm->program, program, size);
    vm->program_size = size;
    vm->pc = 0;
}

/* Execute one instruction */
bool vm_step(vm_t *vm) {
    if (vm->pc >= vm->program_size || vm->halted) {
        return false;
    }
    
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

/* Run until halt */
void vm_run(vm_t *vm) {
    vm->iteration_count++;
    printf("\n========== Iteration %d ==========\n", vm->iteration_count);
    
    while (vm_step(vm)) {
        /* Keep stepping */
    }
}

/* Save VM state to snapshot */
bool vm_save_snapshot(vm_t *vm, const char *filename) {
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    if (!snapshot) {
        return false;
    }
    
    /* Add VM memory region */
    sfpm_memory_region_t vm_region = {
        .base_address = vm,
        .size = sizeof(vm_t),
        .name = "vm_state",
        .is_dynamic = false
    };
    
    if (!sfpm_snapshot_add_region(snapshot, &vm_region)) {
        sfpm_snapshot_destroy(snapshot);
        return false;
    }
    
    char description[256];
    snprintf(description, sizeof(description), 
             "VM snapshot - iteration %d, PC=%zu, SP=%d",
             vm->iteration_count, vm->pc, vm->sp);
    sfpm_snapshot_set_description(snapshot, description);
    
    bool result = sfpm_snapshot_save(snapshot, filename);
    sfpm_snapshot_destroy(snapshot);
    
    return result;
}

/* Load VM state from snapshot */
bool vm_load_snapshot(vm_t *vm, const char *filename) {
    /* First check if snapshot exists */
    sfpm_snapshot_metadata_t metadata;
    if (!sfpm_snapshot_read_metadata(filename, &metadata)) {
        printf("[INFO] No existing snapshot found, starting fresh\n");
        return false;
    }
    
    printf("\n========== Loading Snapshot ==========\n");
    printf("Description: %s\n", metadata.description);
    printf("Created: %llu seconds ago\n", (unsigned long long)(time(NULL) - metadata.timestamp));
    printf("======================================\n\n");
    
    /* Create snapshot with matching region */
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    if (!snapshot) {
        return false;
    }
    
    sfpm_memory_region_t vm_region = {
        .base_address = vm,
        .size = sizeof(vm_t),
        .name = "vm_state",
        .is_dynamic = false
    };
    
    if (!sfpm_snapshot_add_region(snapshot, &vm_region)) {
        sfpm_snapshot_destroy(snapshot);
        return false;
    }
    
    bool result = sfpm_snapshot_restore(filename, snapshot);
    sfpm_snapshot_destroy(snapshot);
    
    return result;
}

/* Modify program at runtime */
void vm_patch_program(vm_t *vm, size_t offset, uint8_t value) {
    if (offset < vm->program_size) {
        printf("[PATCH] Changed program[%zu]: %u -> %u\n", 
               offset, vm->program[offset], value);
        vm->program[offset] = value;
    }
}

/* Interactive menu */
void show_menu(void) {
    printf("\n=== VM Hot Reload Demo ===\n");
    printf("1. Run program\n");
    printf("2. Patch program (modify instruction)\n");
    printf("3. Save snapshot\n");
    printf("4. Load snapshot\n");
    printf("5. View program\n");
    printf("6. Reset VM\n");
    printf("7. Quit (save snapshot on exit)\n");
    printf("Choice: ");
}

void view_program(const vm_t *vm) {
    printf("\n=== Current Program ===\n");
    printf("PC: %zu, SP: %d, Iterations: %d\n", 
           vm->pc, vm->sp, vm->iteration_count);
    printf("Program (%zu bytes):\n", vm->program_size);
    
    for (size_t i = 0; i < vm->program_size; i++) {
        const char *op_name = "UNKNOWN";
        switch ((opcode_t)vm->program[i]) {
            case OP_PUSH: op_name = "PUSH"; break;
            case OP_ADD: op_name = "ADD"; break;
            case OP_SUB: op_name = "SUB"; break;
            case OP_MUL: op_name = "MUL"; break;
            case OP_PRINT: op_name = "PRINT"; break;
            case OP_HALT: op_name = "HALT"; break;
            case OP_NOP: op_name = "NOP"; break;
        }
        
        printf("  [%3zu] %3u  %s", i, vm->program[i], op_name);
        if (i == vm->pc) {
            printf("  <- PC");
        }
        printf("\n");
        
        /* If PUSH, show the next byte as value */
        if ((opcode_t)vm->program[i] == OP_PUSH && i + 1 < vm->program_size) {
            i++;
            printf("  [%3zu] %3u  (value)\n", i, vm->program[i]);
        }
    }
    printf("======================\n");
}

int main(void) {
    vm_t vm;
    
    /* Try to load existing snapshot */
    if (vm_load_snapshot(&vm, SNAPSHOT_FILE)) {
        printf("[SUCCESS] Loaded previous session!\n");
        printf("Resuming from iteration %d\n", vm.iteration_count);
    } else {
        /* Initialize fresh VM */
        vm_init(&vm);
        
        /* Load initial program: PUSH 10, PUSH 5, ADD, PRINT, HALT */
        uint8_t initial_program[] = {
            OP_PUSH, 10,
            OP_PUSH, 5,
            OP_ADD,
            OP_PRINT,
            OP_HALT
        };
        
        vm_load_program(&vm, initial_program, sizeof(initial_program));
        printf("[INFO] Initialized with default program\n");
    }
    
    /* Interactive loop */
    int choice;
    while (1) {
        show_menu();
        
        if (scanf("%d", &choice) != 1) {
            /* Clear input buffer */
            while (getchar() != '\n');
            continue;
        }
        
        switch (choice) {
            case 1:  /* Run */
                vm.pc = 0;
                vm.sp = -1;
                vm.halted = false;
                vm_run(&vm);
                break;
                
            case 2: {  /* Patch */
                size_t offset;
                int value;
                printf("Offset to patch: ");
                if (scanf("%zu", &offset) == 1) {
                    printf("New value (0-255): ");
                    if (scanf("%d", &value) == 1 && value >= 0 && value <= 255) {
                        vm_patch_program(&vm, offset, (uint8_t)value);
                    }
                }
                break;
            }
                
            case 3:  /* Save */
                if (vm_save_snapshot(&vm, SNAPSHOT_FILE)) {
                    printf("[SUCCESS] Snapshot saved to %s\n", SNAPSHOT_FILE);
                } else {
                    printf("[ERROR] Failed to save snapshot\n");
                }
                break;
                
            case 4:  /* Load */
                if (vm_load_snapshot(&vm, SNAPSHOT_FILE)) {
                    printf("[SUCCESS] Snapshot loaded!\n");
                } else {
                    printf("[ERROR] Failed to load snapshot\n");
                }
                break;
                
            case 5:  /* View */
                view_program(&vm);
                break;
                
            case 6:  /* Reset */
                vm_init(&vm);
                printf("[INFO] VM reset\n");
                break;
                
            case 7:  /* Quit */
                printf("Save snapshot before quitting? (y/n): ");
                {
                    char answer;
                    if (scanf(" %c", &answer) == 1 && (answer == 'y' || answer == 'Y')) {
                        vm_save_snapshot(&vm, SNAPSHOT_FILE);
                        printf("Snapshot saved. Restart to resume from this point!\n");
                    }
                }
                printf("Goodbye!\n");
                return 0;
                
            default:
                printf("Invalid choice\n");
        }
    }
    
    return 0;
}
