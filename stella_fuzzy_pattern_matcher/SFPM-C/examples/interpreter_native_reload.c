#include <sfpm/sfpm.h>
#include <sfpm/snapshot.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>

#ifdef _WIN32
    #include <windows.h>
    typedef HMODULE lib_handle_t;
    #define LOAD_LIBRARY(path) LoadLibraryA(path)
    #define GET_FUNCTION(handle, name) GetProcAddress(handle, name)
    #define FREE_LIBRARY(handle) FreeLibrary(handle)
#else
    #include <dlfcn.h>
    typedef void* lib_handle_t;
    #define LOAD_LIBRARY(path) dlopen(path, RTLD_NOW)
    #define GET_FUNCTION(handle, name) dlsym(handle, name)
    #define FREE_LIBRARY(handle) dlclose(handle)
#endif

/* VM opcodes - extended with native call support */
typedef enum {
    OP_PUSH,
    OP_ADD,
    OP_SUB,
    OP_MUL,
    OP_PRINT,
    OP_HALT,
    OP_NOP,
    OP_CALL_NATIVE,  /* New: call a native function from loaded DLL */
    OP_LOAD_LIB,     /* New: load a dynamic library */
    OP_RELOAD_LIB    /* New: reload a library (hot-reload) */
} opcode_t;

#define STACK_SIZE 256
#define PROGRAM_SIZE 1024
#define MAX_LIBS 8

/* Function pointer type for native functions */
typedef int (*native_func_t)(int, int);

typedef struct {
    int stack[STACK_SIZE];
    int sp;
    uint8_t program[PROGRAM_SIZE];
    size_t program_size;
    size_t pc;
    bool halted;
    int iteration_count;
    
    /* Dynamic library support */
    lib_handle_t loaded_libs[MAX_LIBS];
    int lib_count;
    native_func_t cached_functions[MAX_LIBS];
} vm_t;

#define SNAPSHOT_FILE "interpreter_native.img"

void vm_init(vm_t *vm) {
    memset(vm, 0, sizeof(vm_t));
    vm->sp = -1;
    vm->pc = 0;
    vm->halted = false;
    vm->iteration_count = 0;
    vm->lib_count = 0;
}

void vm_load_program(vm_t *vm, const uint8_t *program, size_t size) {
    if (size > PROGRAM_SIZE) size = PROGRAM_SIZE;
    memcpy(vm->program, program, size);
    vm->program_size = size;
    vm->pc = 0;
}

/* Load or reload a dynamic library */
bool vm_load_library(vm_t *vm, const char *path, const char *func_name, int lib_slot) {
    if (lib_slot >= MAX_LIBS) {
        fprintf(stderr, "[ERROR] Library slot %d out of range\n", lib_slot);
        return false;
    }
    
    /* Free existing library if reloading */
    if (vm->loaded_libs[lib_slot] != NULL) {
        printf("[VM] Unloading library from slot %d\n", lib_slot);
        FREE_LIBRARY(vm->loaded_libs[lib_slot]);
        vm->loaded_libs[lib_slot] = NULL;
        vm->cached_functions[lib_slot] = NULL;
    }
    
    /* Load the library */
    lib_handle_t handle = LOAD_LIBRARY(path);
    if (!handle) {
        fprintf(stderr, "[ERROR] Failed to load library: %s\n", path);
        return false;
    }
    
    /* Get the function */
    native_func_t func = (native_func_t)GET_FUNCTION(handle, func_name);
    if (!func) {
        fprintf(stderr, "[ERROR] Failed to find function '%s' in library\n", func_name);
        FREE_LIBRARY(handle);
        return false;
    }
    
    vm->loaded_libs[lib_slot] = handle;
    vm->cached_functions[lib_slot] = func;
    
    if (lib_slot >= vm->lib_count) {
        vm->lib_count = lib_slot + 1;
    }
    
    printf("[VM] Loaded '%s' from %s into slot %d\n", func_name, path, lib_slot);
    return true;
}

/* Call a native function */
bool vm_call_native(vm_t *vm, int lib_slot) {
    if (lib_slot >= vm->lib_count || vm->cached_functions[lib_slot] == NULL) {
        fprintf(stderr, "[ERROR] No function loaded in slot %d\n", lib_slot);
        return false;
    }
    
    if (vm->sp < 1) {
        fprintf(stderr, "[ERROR] Stack underflow for native call\n");
        return false;
    }
    
    int b = vm->stack[vm->sp--];
    int a = vm->stack[vm->sp--];
    
    int result = vm->cached_functions[lib_slot](a, b);
    vm->stack[++vm->sp] = result;
    
    return true;
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
        
        case OP_CALL_NATIVE: {
            if (vm->pc >= vm->program_size) return false;
            int lib_slot = (int)vm->program[vm->pc++];
            if (!vm_call_native(vm, lib_slot)) {
                fprintf(stderr, "[ERROR] Native call failed\n");
                return false;
            }
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
    printf("\n========== Iteration %d ==========\n", vm->iteration_count);
    
    while (vm_step(vm)) { }
}

void vm_cleanup(vm_t *vm) {
    for (int i = 0; i < vm->lib_count; i++) {
        if (vm->loaded_libs[i] != NULL) {
            FREE_LIBRARY(vm->loaded_libs[i]);
            vm->loaded_libs[i] = NULL;
        }
    }
}

void show_menu(void) {
    printf("\n=== VM Native Hot Reload Demo ===\n");
    printf("1. Run program\n");
    printf("2. Load/Reload library\n");
    printf("3. View program\n");
    printf("4. View loaded libraries\n");
    printf("5. Reset VM\n");
    printf("6. Quit\n");
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
            case OP_CALL_NATIVE: op_name = "CALL_NATIVE"; break;
        }
        
        printf("  [%3zu] %3u  %s", i, vm->program[i], op_name);
        if (i == vm->pc) printf("  <- PC");
        printf("\n");
        
        if ((opcode_t)vm->program[i] == OP_PUSH && i + 1 < vm->program_size) {
            i++;
            printf("  [%3zu] %3u  (value)\n", i, vm->program[i]);
        } else if ((opcode_t)vm->program[i] == OP_CALL_NATIVE && i + 1 < vm->program_size) {
            i++;
            printf("  [%3zu] %3u  (lib_slot)\n", i, vm->program[i]);
        }
    }
    printf("======================\n");
}

void view_libraries(const vm_t *vm) {
    printf("\n=== Loaded Libraries ===\n");
    printf("Total slots used: %d/%d\n", vm->lib_count, MAX_LIBS);
    for (int i = 0; i < vm->lib_count; i++) {
        if (vm->loaded_libs[i] != NULL) {
            printf("  Slot %d: LOADED (function at %p)\n", i, (void*)vm->cached_functions[i]);
        } else {
            printf("  Slot %d: EMPTY\n", i);
        }
    }
    printf("========================\n");
}

int main(void) {
    vm_t vm;
    vm_init(&vm);
    
    /* Initial program: PUSH 10, PUSH 5, CALL_NATIVE 0, PRINT, HALT */
    uint8_t initial_program[] = {
        OP_PUSH, 10,
        OP_PUSH, 5,
        OP_CALL_NATIVE, 0,  /* Call function in slot 0 */
        OP_PRINT,
        OP_HALT
    };
    
    vm_load_program(&vm, initial_program, sizeof(initial_program));
    printf("[INFO] Initialized VM with native call program\n");
    printf("[INFO] Program will call native function in slot 0\n");
    
#ifdef _WIN32
    const char *lib_path = "math_ops.dll";
#else
    const char *lib_path = "./libmath_ops.so";
#endif
    
    /* Try to load the library initially */
    if (vm_load_library(&vm, lib_path, "math_add", 0)) {
        printf("[INFO] Successfully loaded math_ops library\n");
        printf("[TIP] Edit math_ops.c, recompile, then use option 2 to hot-reload!\n");
    } else {
        printf("[WARNING] Failed to load library. Compile it first:\n");
#ifdef _WIN32
        printf("  cl /LD math_ops.c /Fe:math_ops.dll\n");
#else
        printf("  gcc -shared -fPIC -o libmath_ops.so math_ops.c\n");
#endif
    }
    
    int choice;
    while (1) {
        show_menu();
        
        if (scanf("%d", &choice) != 1) {
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
                
            case 2: {  /* Load/Reload library */
                printf("Library path [%s]: ", lib_path);
                char path[256];
                if (scanf("%255s", path) == 1) {
                    printf("Function name [math_add]: ");
                    char func[128];
                    if (scanf("%127s", func) == 1) {
                        printf("Slot number [0]: ");
                        int slot;
                        if (scanf("%d", &slot) == 1) {
                            vm_load_library(&vm, path, func, slot);
                        }
                    }
                }
                break;
            }
                
            case 3:  /* View program */
                view_program(&vm);
                break;
                
            case 4:  /* View libraries */
                view_libraries(&vm);
                break;
                
            case 5:  /* Reset */
                vm_cleanup(&vm);
                vm_init(&vm);
                vm_load_program(&vm, initial_program, sizeof(initial_program));
                printf("[INFO] VM reset\n");
                break;
                
            case 6:  /* Quit */
                vm_cleanup(&vm);
                printf("Goodbye!\n");
                return 0;
                
            default:
                printf("Invalid choice\n");
        }
    }
    
    return 0;
}
