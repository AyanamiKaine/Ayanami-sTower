# Image-Based Hot Reload - Persisting Runtime Changes

## Overview

The SFPM-C snapshot system provides **Smalltalk/Lisp-style image-based persistence** for interpreters. Instead of serializing individual rules, it saves the entire memory state as a binary image that can be restored instantly.

## Concept

```
┌─────────────────────────────────────────────────┐
│                                                 │
│  1. Start interpreter                           │
│  2. Modify code/rules at runtime                │
│  3. Save memory snapshot (.img file)            │
│  4. Exit process                                │
│                                                 │
│  5. Restart → Load snapshot                     │
│  6. Continue exactly where you left off!        │
│                                                 │
└─────────────────────────────────────────────────┘
```

## Why Image-Based vs Serialization?

| Feature            | Image-Based            | Serialization           |
| ------------------ | ---------------------- | ----------------------- |
| **Speed**          | Instant (memory dump)  | Slow (encode/decode)    |
| **Completeness**   | 100% state preserved   | Only serializable data  |
| **Complexity**     | Simple (memcpy)        | Complex (custom format) |
| **File size**      | Larger (includes gaps) | Smaller (compact)       |
| **Debugging**      | Easy (binary dump)     | Hard (parse format)     |
| **Cross-platform** | Same architecture      | Portable                |

**Best for:**

-   Development/prototyping with hot reload
-   Interactive REPL-style environments
-   Saving complete application state
-   Quick save/load (like game savefiles)

## API Quick Reference

```c
#include <sfpm/snapshot.h>

/* 1. Create snapshot */
sfpm_snapshot_t *snapshot = sfpm_snapshot_create();

/* 2. Register memory regions to save */
sfpm_memory_region_t region = {
    .base_address = &my_vm,
    .size = sizeof(my_vm),
    .name = "vm_state",
    .is_dynamic = false
};
sfpm_snapshot_add_region(snapshot, &region);

/* 3. Save to file */
sfpm_snapshot_save(snapshot, "app.img");

/* 4. Later: restore */
sfpm_snapshot_restore("app.img", snapshot);
```

## Example: Hot Reload Workflow

### 1. Run the Demo

```bash
cd build/Release
./sfpm_hot_reload.exe
```

### 2. First Run (No Snapshot)

```
[INFO] No existing snapshot found, starting fresh
[INFO] Initialized with default program

=== VM Hot Reload Demo ===
1. Run program
2. Patch program (modify instruction)
3. Save snapshot
4. Load snapshot
5. View program
6. Reset VM
7. Quit (save snapshot on exit)
Choice: 1

========== Iteration 1 ==========
Result: 15
```

The default program calculates 10 + 5 = 15.

### 3. Patch the Program

```
Choice: 2
Offset to patch: 3
New value (0-255): 20

[PATCH] Changed program[3]: 5 -> 20
```

We changed the second operand from 5 to 20.

### 4. Run Again

```
Choice: 1

========== Iteration 2 ==========
Result: 30
```

Now it calculates 10 + 20 = 30!

### 5. Save Snapshot

```
Choice: 3

[SNAPSHOT] Saved region 'vm_state': 5392 bytes
[SNAPSHOT] Successfully saved to interpreter.img
[SNAPSHOT] Total size: 5392 bytes, Regions: 1
[SUCCESS] Snapshot saved to interpreter.img
```

### 6. Exit and Restart

```
Choice: 7
Save snapshot before quitting? (y/n): y
Snapshot saved. Restart to resume from this point!
Goodbye!
```

### 7. Restart - Automatic Resume!

```bash
./sfpm_hot_reload.exe
```

```
========== Loading Snapshot ==========
Description: VM snapshot - iteration 2, PC=7, SP=-1
Created: 5 seconds ago
======================================

[SNAPSHOT] Restored region 'vm_state': 5392 bytes
[SNAPSHOT] Restore complete!
[SUCCESS] Loaded previous session!
Resuming from iteration 2

=== VM Hot Reload Demo ===
Choice: 1

========== Iteration 3 ==========
Result: 30
```

**The modified program persisted! No manual save/load code required!**

## How It Works

### Memory Snapshot Structure

```
┌──────────────────────────────────────┐
│ MAGIC NUMBER (0x5346504D = "SFPM")  │
├──────────────────────────────────────┤
│ METADATA                             │
│  - Version                           │
│  - Timestamp                         │
│  - Number of regions                 │
│  - Description                       │
├──────────────────────────────────────┤
│ REGION 1 DESCRIPTOR                  │
│  - Size                              │
│  - Name                              │
│  - Flags                             │
├──────────────────────────────────────┤
│ REGION 1 DATA (raw memory dump)     │
├──────────────────────────────────────┤
│ REGION 2 DESCRIPTOR                  │
├──────────────────────────────────────┤
│ REGION 2 DATA                        │
├──────────────────────────────────────┤
│ ... more regions ...                 │
└──────────────────────────────────────┘
```

### What Gets Saved?

```c
typedef struct {
    int stack[256];           // ✅ Saved
    int sp;                   // ✅ Saved
    uint8_t program[1024];    // ✅ Saved (INCLUDING PATCHES!)
    size_t program_size;      // ✅ Saved
    size_t pc;                // ✅ Saved
    bool halted;              // ✅ Saved
    int iteration_count;      // ✅ Saved
} vm_t;

// Entire struct dumped as-is to disk!
```

## Advanced Usage

### Multiple Regions

```c
/* Save VM + heap + rules separately */
sfpm_snapshot_t *snapshot = sfpm_snapshot_create();

sfpm_memory_region_t regions[] = {
    { .base_address = &vm, .size = sizeof(vm), .name = "vm" },
    { .base_address = heap, .size = heap_size, .name = "heap" },
    { .base_address = rules, .size = rules_size, .name = "rules" }
};

for (int i = 0; i < 3; i++) {
    sfpm_snapshot_add_region(snapshot, &regions[i]);
}

sfpm_snapshot_save(snapshot, "full_state.img");
```

### Inspect Snapshot Without Loading

```c
sfpm_snapshot_metadata_t metadata;
if (sfpm_snapshot_read_metadata("app.img", &metadata)) {
    printf("Snapshot created: %llu\n", metadata.timestamp);
    printf("Description: %s\n", metadata.description);
    printf("Size: %zu bytes\n", metadata.total_size);
    printf("Regions: %u\n", metadata.num_regions);
}
```

### Conditional Loading

```c
sfpm_snapshot_metadata_t metadata;
if (sfpm_snapshot_read_metadata("app.img", &metadata)) {
    time_t age = time(NULL) - metadata.timestamp;

    if (age < 3600) {  // Less than 1 hour old
        printf("Loading recent snapshot...\n");
        vm_load_snapshot(&vm, "app.img");
    } else {
        printf("Snapshot too old, starting fresh\n");
        vm_init(&vm);
    }
}
```

## Use Cases

### 1. **Interactive Development**

```
Edit → Test → Save Image → Continue
```

No recompilation needed! Perfect for experimenting with rule changes.

### 2. **Game Development**

```c
/* Save game state */
sfpm_snapshot_save(snapshot, "quicksave.sav");

/* Load game state */
sfpm_snapshot_restore("quicksave.sav", snapshot);
```

### 3. **Long-Running Processes**

```c
/* Auto-save every 5 minutes */
if (time_since_last_save > 300) {
    sfpm_snapshot_save(snapshot, "autosave.img");
}
```

### 4. **A/B Testing**

```c
/* Save baseline */
sfpm_snapshot_save(snapshot, "baseline.img");

/* Try experimental changes */
modify_rules();

/* Restore if not working */
sfpm_snapshot_restore("baseline.img", snapshot);
```

## Limitations & Warnings

### ⚠️ Platform-Specific

-   **Same architecture required** (x64 → x64, ARM → ARM)
-   **Same OS** (Windows snapshot won't work on Linux)
-   **Same compiler** (memory layout must match)

### ⚠️ Pointer Safety

-   Pointers to heap memory will be **invalid** after restore
-   Only works if you allocate at same addresses
-   Use **offsets** instead of pointers for portability

### ⚠️ External Resources

-   File handles **not preserved**
-   Network connections **not preserved**
-   Must re-open resources after restore

### ⚠️ Security

-   Snapshot contains **raw memory** (may include sensitive data)
-   No encryption by default
-   Validate snapshots from untrusted sources

## Best Practices

### ✅ DO:

-   Save small, focused state (not entire process)
-   Version your snapshot format
-   Add timestamps and descriptions
-   Test restore logic regularly
-   Use for development/prototyping

### ❌ DON'T:

-   Save pointers to heap (will break)
-   Share snapshots across platforms
-   Use for long-term persistence (prefer serialization)
-   Store sensitive data unencrypted
-   Rely on snapshots for production backup

## Comparison with Other Approaches

### vs. Checkpoint/Restore (CRIU)

-   **Snapshot**: Controlled, specific regions
-   **CRIU**: Entire process, all memory, all state
-   **Snapshot**: Portable within same binary
-   **CRIU**: Requires kernel support, complex

### vs. Serialization

-   **Snapshot**: Fast, simple, complete
-   **Serialization**: Slow, complex, portable
-   **Snapshot**: Platform-specific
-   **Serialization**: Cross-platform

### vs. Database

-   **Snapshot**: In-memory speed
-   **Database**: Persistent, queryable
-   **Snapshot**: Single-file
-   **Database**: Structured, relational

## Future Enhancements

-   [ ] Compression (zlib/lz4)
-   [ ] Encryption (AES)
-   [ ] Delta snapshots (save only changes)
-   [ ] Versioning and migration
-   [ ] Cross-platform support (normalization)
-   [ ] Pointer relocation tables

## Conclusion

Image-based persistence provides a **simple, powerful way to persist runtime modifications**. It's perfect for:

-   **Interactive development** with instant hot reload
-   **Rapid prototyping** without restart overhead
-   **Save/load functionality** for applications
-   **Checkpoint/rollback** during development

The snapshot system makes your interpreter feel **alive** - modify it, save it, restart it, and pick up exactly where you left off!

---

**Try it yourself:**

```bash
cd build/Release
./sfpm_hot_reload.exe
```

Modify the program, save, exit, restart - your changes persist! 🎉
