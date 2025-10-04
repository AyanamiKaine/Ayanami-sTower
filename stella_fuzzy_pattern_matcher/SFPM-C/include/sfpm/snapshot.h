/**
 * @file snapshot.h
 * @brief Memory snapshot/image-based persistence for SFPM interpreters
 * 
 * Provides Smalltalk/Lisp-style image-based persistence:
 * - Save entire interpreter state to disk (memory dump)
 * - Restore interpreter from saved image
 * - Hot reload by saving image and restarting
 * - Preserves ALL state: rules, VM, hooks, data
 * 
 * This is more powerful than serialization as it captures the complete
 * runtime state, enabling true "modify-save-reload" workflows.
 */

#ifndef SFPM_SNAPSHOT_H
#define SFPM_SNAPSHOT_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Opaque snapshot handle
 */
typedef struct sfpm_snapshot sfpm_snapshot_t;

/**
 * @brief Snapshot metadata
 */
typedef struct {
    uint32_t version;          /**< Snapshot format version */
    uint64_t timestamp;        /**< When snapshot was created */
    size_t total_size;         /**< Total snapshot size in bytes */
    uint32_t num_regions;      /**< Number of memory regions */
    char description[256];     /**< User description */
} sfpm_snapshot_metadata_t;

/**
 * @brief Memory region descriptor
 * 
 * Represents a contiguous region of memory to be saved/restored.
 */
typedef struct {
    void *base_address;        /**< Start address of region */
    size_t size;               /**< Size in bytes */
    const char *name;          /**< Region name (for debugging) */
    bool is_dynamic;           /**< True if heap-allocated */
} sfpm_memory_region_t;

/**
 * @brief Create a new snapshot builder
 * 
 * @return Snapshot handle, or NULL on failure
 */
sfpm_snapshot_t *sfpm_snapshot_create(void);

/**
 * @brief Add a memory region to the snapshot
 * 
 * Register a memory region to be included in the snapshot.
 * Regions are saved in the order they're added.
 * 
 * @param snapshot The snapshot builder
 * @param region Region descriptor
 * @return true on success, false on failure
 */
bool sfpm_snapshot_add_region(sfpm_snapshot_t *snapshot, 
                               const sfpm_memory_region_t *region);

/**
 * @brief Set snapshot metadata
 * 
 * @param snapshot The snapshot builder
 * @param description User-provided description
 */
void sfpm_snapshot_set_description(sfpm_snapshot_t *snapshot, 
                                    const char *description);

/**
 * @brief Write snapshot to file
 * 
 * Saves all registered memory regions to a file.
 * Format: [metadata][region1][region2]...[regionN]
 * 
 * @param snapshot The snapshot to save
 * @param filename Path to output file (e.g., "interpreter.img")
 * @return true on success, false on failure
 */
bool sfpm_snapshot_save(const sfpm_snapshot_t *snapshot, const char *filename);

/**
 * @brief Load snapshot metadata without loading data
 * 
 * Useful for inspecting snapshot before loading.
 * 
 * @param filename Path to snapshot file
 * @param metadata Output metadata structure
 * @return true on success, false on failure
 */
bool sfpm_snapshot_read_metadata(const char *filename, 
                                  sfpm_snapshot_metadata_t *metadata);

/**
 * @brief Restore memory from snapshot file
 * 
 * Loads all memory regions from the snapshot.
 * WARNING: This overwrites memory at the specified addresses!
 * 
 * Typical usage:
 * 1. Allocate same memory regions as when snapshot was created
 * 2. Call sfpm_snapshot_restore() to populate them
 * 3. Resume execution
 * 
 * @param filename Path to snapshot file
 * @param snapshot Pre-configured snapshot with matching regions
 * @return true on success, false on failure
 */
bool sfpm_snapshot_restore(const char *filename, sfpm_snapshot_t *snapshot);

/**
 * @brief Destroy snapshot builder
 * 
 * @param snapshot The snapshot to destroy
 */
void sfpm_snapshot_destroy(sfpm_snapshot_t *snapshot);

/**
 * @brief Helper: Create snapshot for common interpreter structure
 * 
 * Convenience function that registers typical interpreter regions:
 * - Stack memory
 * - Heap/data segment
 * - Rule structures
 * - VM state
 * 
 * You still need to add custom regions specific to your interpreter.
 * 
 * @param stack_base Base address of stack
 * @param stack_size Stack size in bytes
 * @param heap_base Base address of heap
 * @param heap_size Heap size in bytes
 * @return Snapshot with common regions registered
 */
sfpm_snapshot_t *sfpm_snapshot_create_for_interpreter(void *stack_base, 
                                                       size_t stack_size,
                                                       void *heap_base, 
                                                       size_t heap_size);

/**
 * @brief Incremental snapshot (delta from previous)
 * 
 * Creates a snapshot containing only memory that changed since
 * a previous snapshot. Useful for efficient updates.
 * 
 * @param snapshot Current snapshot
 * @param previous_filename Previous snapshot to compare against
 * @param output_filename Where to save delta
 * @return true on success, false on failure
 */
bool sfpm_snapshot_save_delta(const sfpm_snapshot_t *snapshot,
                               const char *previous_filename,
                               const char *output_filename);

#ifdef __cplusplus
}
#endif

#endif /* SFPM_SNAPSHOT_H */
