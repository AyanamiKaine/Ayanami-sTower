# Native Function Hot-Reload Demo

This example demonstrates **hot-reloading of native C functions** loaded from dynamic libraries (DLLs/.so files) while the VM is running.

## What This Demonstrates

1. **Loading native functions** from compiled C libraries into the VM
2. **Calling native functions** from VM bytecode via `OP_CALL_NATIVE` opcode
3. **Hot-reloading libraries** by recompiling and reloading without VM restart
4. **Live behavior changes** - modify the C function, recompile, reload, see new behavior

## Files

-   **`math_ops.c`** - Simple math library with `math_add()` function
-   **`interpreter_native_reload.c`** - VM with native function call support
-   **`demo_native_hot_reload.ps1`** - Automated demo script

## Quick Start (Automated)

```powershell
cd examples
.\demo_native_hot_reload.ps1
```

This script will:

1. Run VM with initial library (returns 15 from 10 + 5)
2. Modify `math_ops.c` to multiply instead of add
3. Recompile `math_ops.dll`
4. Hot-reload in the VM
5. Run again (now returns 50 from 10 \* 5)
6. Restore original files

## Manual Walkthrough

### Step 1: Build Everything

```powershell
cd build
cmake --build . --config Release --target math_ops
cmake --build . --config Release --target sfpm_native_reload
```

This creates:

-   `build\Release\math_ops.dll` - The native function library
-   `build\Release\sfpm_native_reload.exe` - The VM executable

### Step 2: Run Initial VM

```powershell
cd build\Release
.\sfpm_native_reload.exe
```

You should see:

```
[INFO] Initialized VM with native call program
[INFO] Successfully loaded math_ops library
[TIP] Edit math_ops.c, recompile, then use option 2 to hot-reload!
```

### Step 3: Run the Program (Initial State)

Choose option `1` (Run program):

```
Choice: 1

========== Iteration 1 ==========
[NATIVE] math_add(10, 5) called
Result: 15
```

The VM calls the native `math_add` function which returns `10 + 5 = 15`.

### Step 4: Modify the Native Function

**Keep the VM running!** In a separate terminal or editor:

Edit `examples\math_ops.c` and change line 19:

```c
// FROM:
return a + b;  /* Change this line and recompile! */

// TO:
return a * b;  /* Now multiplies instead! */
```

Also increment the version (optional but helpful):

```c
// FROM:
return 1;  /* Increment this after each recompile */

// TO:
return 2;  /* Incremented! */
```

### Step 5: Recompile the DLL

In a **new terminal** (keep VM running):

```powershell
cd build\Release

# Windows (MSVC)
cl /LD ..\..\examples\math_ops.c /Fe:math_ops.dll

# Or use CMake
cd ..
cmake --build . --config Release --target math_ops
```

The DLL is now updated on disk.

### Step 6: Hot-Reload in the Running VM

Back in the **VM terminal**, choose option `2` (Load/Reload library):

```
Choice: 2
Library path [math_ops.dll]: math_ops.dll
Function name [math_add]: math_add
Slot number [0]: 0

[VM] Unloading library from slot 0
[VM] Loaded 'math_add' from math_ops.dll into slot 0
```

### Step 7: Run Again (See New Behavior)

Choose option `1` (Run program):

```
Choice: 1

========== Iteration 2 ==========
[NATIVE] math_add(10, 5) called
Result: 50
```

**The same VM bytecode now produces different results!**  
`10 * 5 = 50` instead of `10 + 5 = 15`

No VM restart was needed — the function was hot-reloaded.

## How It Works

### VM Bytecode

The initial program loaded by the VM:

```
OP_PUSH    10      # Push 10 onto stack
OP_PUSH    5       # Push 5 onto stack
OP_CALL_NATIVE 0   # Call native function in slot 0
OP_PRINT           # Print result
OP_HALT            # Stop
```

### Native Function Loading

1. **Load**: `vm_load_library()` uses `LoadLibrary()` (Windows) or `dlopen()` (Linux)
2. **Resolve**: `GetProcAddress()` or `dlsym()` gets the function pointer
3. **Cache**: Function pointer stored in `vm->cached_functions[slot]`
4. **Call**: `OP_CALL_NATIVE` pops arguments, calls cached function, pushes result

### Hot-Reload Process

1. **Unload**: Call `FreeLibrary()` to release the old DLL
2. **Reload**: Call `LoadLibrary()` to load the newly compiled DLL
3. **Re-resolve**: Get new function pointer
4. **Replace**: Update cached function pointer

The VM bytecode doesn't change — only the implementation it calls.

## Advanced Usage

### Loading Multiple Libraries

```c
vm_load_library(&vm, "math_ops.dll", "math_add", 0);
vm_load_library(&vm, "math_ops.dll", "math_mul", 1);
vm_load_library(&vm, "string_ops.dll", "str_concat", 2);
```

### Creating Custom Libraries

Your library needs exported functions:

```c
#ifdef _WIN32
    #define EXPORT __declspec(dllexport)
#else
    #define EXPORT __attribute__((visibility("default")))
#endif

EXPORT int my_function(int a, int b) {
    // Your implementation
    return a + b;
}
```

Compile:

```bash
# Windows
cl /LD my_lib.c /Fe:my_lib.dll

# Linux
gcc -shared -fPIC -o libmy_lib.so my_lib.c
```

### Function Signature

Currently supports: `int function(int, int)`

To extend, modify `native_func_t` typedef and `vm_call_native()`.

## Use Cases

1. **Live Development** - Edit and test functions without restarting
2. **A/B Testing** - Swap implementations to compare performance
3. **Plugin Systems** - Load different algorithm implementations
4. **Hot Fixes** - Fix bugs in production without downtime
5. **Modding Support** - Users can provide custom implementations

## Limitations

1. **Function signature must match** - Type checking is minimal
2. **Platform-specific DLLs** - Not cross-platform portable
3. **No state migration** - Function-local static variables reset
4. **File locks** (Windows) - DLL must be unloaded before recompile
5. **Symbol resolution** - Function name must exist in new DLL

## Safety Notes

-   Always unload before reloading to avoid stale pointers
-   Check function pointers before calling
-   Handle loading failures gracefully
-   Don't hold references to unloaded library memory
-   Test signature compatibility

## Troubleshooting

### "Failed to load library"

-   Check DLL exists in expected path
-   On Linux, try `./libmath_ops.so` (include `./`)
-   Check file permissions

### "Failed to find function"

-   Verify function is exported (`EXPORT` macro)
-   Check function name spelling
-   Use `dumpbin /exports` (Windows) or `nm -D` (Linux) to verify

### "Access violation" or crash

-   Library was unloaded but pointer still used
-   Reload library before calling
-   Check `vm->loaded_libs[slot] != NULL`

## Example Modifications

### Change to Subtraction

```c
EXPORT int math_add(int a, int b) {
    return a - b;  /* Subtraction */
}
```

### Add Logging

```c
EXPORT int math_add(int a, int b) {
    FILE *f = fopen("calls.log", "a");
    fprintf(f, "Called with %d, %d\n", a, b);
    fclose(f);
    return a + b;
}
```

### Add Error Checking

```c
EXPORT int math_add(int a, int b) {
    if (a > 1000 || b > 1000) {
        fprintf(stderr, "Values too large!\n");
        return 0;
    }
    return a + b;
}
```

## Next Steps

-   Extend to support more function signatures (float, strings, etc.)
-   Add versioning checks (`get_version()` function)
-   Implement delta snapshots (save library paths and load on restore)
-   Add script-based function definitions (JIT compilation)
-   Support hot-reload of multiple functions simultaneously

---

_See `demo_native_hot_reload.ps1` for automated demonstration_
