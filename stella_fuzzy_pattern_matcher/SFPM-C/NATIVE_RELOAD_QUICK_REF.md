# Native Hot-Reload - Quick Reference

## 🚀 Quick Start (60 seconds)

```powershell
# 1. Build (from SFPM-C directory)
cd build
cmake --build . --config Release

# 2. Run demo
cd ..\examples
.\demo_native_simple.ps1

# Watch it:
#   - Run with ADD function (result: 15)
#   - Modify to MULTIPLY
#   - Recompile DLL
#   - Hot-reload
#   - Run again (result: 50)
```

---

## 📁 Files

| File                          | Purpose              |
| ----------------------------- | -------------------- |
| `math_ops.c`                  | Example C library    |
| `interpreter_native_reload.c` | VM with native calls |
| `demo_native_simple.ps1`      | Automated demo       |
| `NATIVE_HOT_RELOAD.md`        | Full documentation   |

---

## 🔧 Manual Hot-Reload Workflow

### Step 1: Run VM

```powershell
cd build\Release
.\sfpm_native_reload.exe
```

### Step 2: Test Initial (in VM)

```
Choice: 1  (Run program)
Result: 15  (10 + 5)
```

### Step 3: Edit Source (keep VM running!)

Edit `examples\math_ops.c` line 19:

```c
return a * b;  // Changed from: a + b
```

### Step 4: Recompile (new terminal)

```powershell
cd build
cmake --build . --config Release --target math_ops
```

### Step 5: Hot-Reload (back in VM)

```
Choice: 2  (Load/Reload library)
Library path: math_ops.dll
Function name: math_add
Slot: 0
```

### Step 6: Test Again

```
Choice: 1  (Run program)
Result: 50  (10 * 5)  ✨ NEW BEHAVIOR!
```

---

## 💻 Build Commands

### Windows (MSVC)

```powershell
# From build directory
cmake --build . --config Release --target math_ops
cmake --build . --config Release --target sfpm_native_reload

# Or manually compile DLL
cl /LD ..\examples\math_ops.c /Fe:Release\math_ops.dll
```

### Linux (GCC)

```bash
# From build directory
cmake --build . --config Release --target math_ops
cmake --build . --config Release --target sfpm_native_reload

# Or manually compile .so
gcc -shared -fPIC -o libmath_ops.so ../examples/math_ops.c
```

---

## 🎯 Key Concepts

### VM Opcodes

```
OP_PUSH 10           # Push value
OP_PUSH 5            # Push value
OP_CALL_NATIVE 0     # Call function in slot 0
OP_PRINT             # Print result
OP_HALT              # Stop
```

### Library Loading

```c
vm_load_library(&vm, "math_ops.dll", "math_add", 0);
// Slot 0 now has math_add function
```

### Hot-Reload

```c
vm_load_library(&vm, "math_ops.dll", "math_add", 0);  // Reload
// Old DLL unloaded, new DLL loaded
// Same slot, new implementation
```

---

## 🔥 Use Cases

| Scenario               | Benefit                         |
| ---------------------- | ------------------------------- |
| **Live Development**   | Edit→Compile→Test (no restart)  |
| **Plugin System**      | Load user-provided DLLs         |
| **A/B Testing**        | Swap implementations on-the-fly |
| **Performance Tuning** | Try different algorithms live   |
| **Modding**            | Users extend functionality      |

---

## ⚡ Demo Scripts

### Interactive Demo (recommended)

```powershell
cd examples
.\demo_native_simple.ps1
```

**Shows:** Full workflow with pauses

### Fully Automated

```powershell
cd examples
.\demo_native_hot_reload.ps1
```

**Shows:** End-to-end without interaction

---

## 🛠️ Creating Custom Libraries

### 1. Write Function

```c
// my_math.c
#ifdef _WIN32
    #define EXPORT __declspec(dllexport)
#else
    #define EXPORT __attribute__((visibility("default")))
#endif

EXPORT int my_function(int a, int b) {
    return a * a + b * b;  // x² + y²
}
```

### 2. Compile

```powershell
# Windows
cl /LD my_math.c /Fe:my_math.dll

# Linux
gcc -shared -fPIC -o libmy_math.so my_math.c
```

### 3. Load in VM

```
Choice: 2
Library path: my_math.dll
Function name: my_function
Slot: 1
```

### 4. Use

Bytecode calls slot 1 → your function executes

---

## 🐛 Troubleshooting

### "Failed to load library"

```powershell
# Check file exists
dir build\Release\math_ops.dll

# Verify exports (Windows)
dumpbin /exports build\Release\math_ops.dll
```

### "Function not found"

-   Check `EXPORT` macro is used
-   Verify function name spelling
-   Check function signature matches

### Can't recompile (Windows)

-   Unload library first (VM option 5: Reset)
-   Close VM completely
-   Or use option 2 (reload unloads automatically)

---

## 📊 Performance

| Operation   | Time         |
| ----------- | ------------ |
| Native call | ~5-10 cycles |
| Hot-reload  | ~2-10ms      |
| DLL load    | ~1-5ms       |

**Conclusion:** Negligible overhead for interactive use.

---

## 📚 Further Reading

-   **Full Guide:** `NATIVE_HOT_RELOAD.md`
-   **Implementation:** `NATIVE_RELOAD_IMPLEMENTATION_SUMMARY.md`
-   **Examples Index:** `README.md`

---

## ✅ Quick Verification

**Is it working?**

Run this:

```powershell
cd build\Release
echo "1`n6" | .\sfpm_native_reload.exe
```

**Should see:**

```
[NATIVE] math_add(10, 5) called
Result: 15
```

✅ If you see this, everything works!

---

## 🎓 Learning Path

1. ✅ Run `demo_native_simple.ps1`
2. ✅ Manually modify and reload
3. ✅ Create your own function
4. ✅ Load multiple libraries
5. ✅ Read `NATIVE_HOT_RELOAD.md`

---

**Time to first hot-reload: 60 seconds! 🚀**

_Windows 10, MSVC 17.14.19 - Tested 2025-10-05_
