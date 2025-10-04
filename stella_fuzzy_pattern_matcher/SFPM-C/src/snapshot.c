#include "sfpm/snapshot.h"
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <stdio.h>

/* Snapshot file magic number to identify valid snapshots */
#define SFPM_SNAPSHOT_MAGIC 0x5346504D  /* "SFPM" */
#define SFPM_SNAPSHOT_VERSION 1

/* Maximum regions per snapshot */
#define MAX_REGIONS 64

struct sfpm_snapshot {
    sfpm_memory_region_t regions[MAX_REGIONS];
    uint32_t region_count;
    char description[256];
};

sfpm_snapshot_t *sfpm_snapshot_create(void) {
    sfpm_snapshot_t *snapshot = (sfpm_snapshot_t *)calloc(1, sizeof(sfpm_snapshot_t));
    if (!snapshot) {
        return NULL;
    }
    
    snapshot->region_count = 0;
    strncpy(snapshot->description, "SFPM Snapshot", sizeof(snapshot->description) - 1);
    
    return snapshot;
}

bool sfpm_snapshot_add_region(sfpm_snapshot_t *snapshot, 
                               const sfpm_memory_region_t *region) {
    if (!snapshot || !region || !region->base_address || region->size == 0) {
        return false;
    }
    
    if (snapshot->region_count >= MAX_REGIONS) {
        fprintf(stderr, "Snapshot error: Maximum regions (%d) exceeded\n", MAX_REGIONS);
        return false;
    }
    
    /* Copy region descriptor */
    snapshot->regions[snapshot->region_count] = *region;
    
    /* Duplicate name string if provided */
    if (region->name) {
        /* Note: We're storing the pointer, not copying. 
         * Caller must ensure name string lifetime */
        snapshot->regions[snapshot->region_count].name = region->name;
    } else {
        snapshot->regions[snapshot->region_count].name = "unnamed";
    }
    
    snapshot->region_count++;
    
    return true;
}

void sfpm_snapshot_set_description(sfpm_snapshot_t *snapshot, 
                                    const char *description) {
    if (!snapshot || !description) {
        return;
    }
    
    strncpy(snapshot->description, description, sizeof(snapshot->description) - 1);
    snapshot->description[sizeof(snapshot->description) - 1] = '\0';
}

bool sfpm_snapshot_save(const sfpm_snapshot_t *snapshot, const char *filename) {
    if (!snapshot || !filename) {
        return false;
    }
    
    FILE *file = fopen(filename, "wb");
    if (!file) {
        fprintf(stderr, "Failed to open snapshot file: %s\n", filename);
        return false;
    }
    
    /* Write file header */
    uint32_t magic = SFPM_SNAPSHOT_MAGIC;
    if (fwrite(&magic, sizeof(magic), 1, file) != 1) {
        fclose(file);
        return false;
    }
    
    /* Write metadata */
    sfpm_snapshot_metadata_t metadata = {0};
    metadata.version = SFPM_SNAPSHOT_VERSION;
    metadata.timestamp = (uint64_t)time(NULL);
    metadata.num_regions = snapshot->region_count;
    
    /* Calculate total size */
    metadata.total_size = 0;
    for (uint32_t i = 0; i < snapshot->region_count; i++) {
        metadata.total_size += snapshot->regions[i].size;
    }
    
    strncpy(metadata.description, snapshot->description, sizeof(metadata.description) - 1);
    
    if (fwrite(&metadata, sizeof(metadata), 1, file) != 1) {
        fclose(file);
        return false;
    }
    
    /* Write region descriptors */
    for (uint32_t i = 0; i < snapshot->region_count; i++) {
        const sfpm_memory_region_t *region = &snapshot->regions[i];
        
        /* Write region metadata */
        uint64_t region_size = region->size;
        uint8_t is_dynamic = region->is_dynamic ? 1 : 0;
        uint32_t name_len = region->name ? (uint32_t)strlen(region->name) : 0;
        
        if (fwrite(&region_size, sizeof(region_size), 1, file) != 1 ||
            fwrite(&is_dynamic, sizeof(is_dynamic), 1, file) != 1 ||
            fwrite(&name_len, sizeof(name_len), 1, file) != 1) {
            fclose(file);
            return false;
        }
        
        /* Write region name */
        if (name_len > 0) {
            if (fwrite(region->name, 1, name_len, file) != name_len) {
                fclose(file);
                return false;
            }
        }
        
        /* Write actual memory content */
        if (fwrite(region->base_address, 1, region->size, file) != region->size) {
            fprintf(stderr, "Failed to write region %s (%zu bytes)\n", 
                    region->name, region->size);
            fclose(file);
            return false;
        }
        
        printf("[SNAPSHOT] Saved region '%s': %zu bytes\n", region->name, region->size);
    }
    
    fclose(file);
    
    printf("[SNAPSHOT] Successfully saved to %s\n", filename);
    printf("[SNAPSHOT] Total size: %zu bytes, Regions: %u\n", 
           metadata.total_size, metadata.num_regions);
    
    return true;
}

bool sfpm_snapshot_read_metadata(const char *filename, 
                                  sfpm_snapshot_metadata_t *metadata) {
    if (!filename || !metadata) {
        return false;
    }
    
    FILE *file = fopen(filename, "rb");
    if (!file) {
        return false;
    }
    
    /* Read and verify magic number */
    uint32_t magic;
    if (fread(&magic, sizeof(magic), 1, file) != 1 || magic != SFPM_SNAPSHOT_MAGIC) {
        fclose(file);
        return false;
    }
    
    /* Read metadata */
    if (fread(metadata, sizeof(*metadata), 1, file) != 1) {
        fclose(file);
        return false;
    }
    
    fclose(file);
    return true;
}

bool sfpm_snapshot_restore(const char *filename, sfpm_snapshot_t *snapshot) {
    if (!filename || !snapshot) {
        return false;
    }
    
    FILE *file = fopen(filename, "rb");
    if (!file) {
        fprintf(stderr, "Failed to open snapshot file: %s\n", filename);
        return false;
    }
    
    /* Read and verify magic number */
    uint32_t magic;
    if (fread(&magic, sizeof(magic), 1, file) != 1 || magic != SFPM_SNAPSHOT_MAGIC) {
        fprintf(stderr, "Invalid snapshot file (bad magic number)\n");
        fclose(file);
        return false;
    }
    
    /* Read metadata */
    sfpm_snapshot_metadata_t metadata;
    if (fread(&metadata, sizeof(metadata), 1, file) != 1) {
        fprintf(stderr, "Failed to read snapshot metadata\n");
        fclose(file);
        return false;
    }
    
    if (metadata.version != SFPM_SNAPSHOT_VERSION) {
        fprintf(stderr, "Snapshot version mismatch (expected %u, got %u)\n",
                SFPM_SNAPSHOT_VERSION, metadata.version);
        fclose(file);
        return false;
    }
    
    if (metadata.num_regions != snapshot->region_count) {
        fprintf(stderr, "Region count mismatch (expected %u, snapshot has %u)\n",
                snapshot->region_count, metadata.num_regions);
        fclose(file);
        return false;
    }
    
    printf("[SNAPSHOT] Restoring from %s\n", filename);
    printf("[SNAPSHOT] Description: %s\n", metadata.description);
    printf("[SNAPSHOT] Regions: %u, Total: %zu bytes\n", 
           metadata.num_regions, metadata.total_size);
    
    /* Restore each region */
    for (uint32_t i = 0; i < metadata.num_regions; i++) {
        /* Read region metadata */
        uint64_t region_size;
        uint8_t is_dynamic;
        uint32_t name_len;
        
        if (fread(&region_size, sizeof(region_size), 1, file) != 1 ||
            fread(&is_dynamic, sizeof(is_dynamic), 1, file) != 1 ||
            fread(&name_len, sizeof(name_len), 1, file) != 1) {
            fclose(file);
            return false;
        }
        
        /* Skip region name */
        if (name_len > 0) {
            fseek(file, name_len, SEEK_CUR);
        }
        
        /* Verify region size matches */
        if (region_size != snapshot->regions[i].size) {
            fprintf(stderr, "Region %u size mismatch\n", i);
            fclose(file);
            return false;
        }
        
        /* Read directly into target memory */
        if (fread(snapshot->regions[i].base_address, 1, region_size, file) != region_size) {
            fprintf(stderr, "Failed to restore region %u\n", i);
            fclose(file);
            return false;
        }
        
        printf("[SNAPSHOT] Restored region '%s': %zu bytes\n", 
               snapshot->regions[i].name, (size_t)region_size);
    }
    
    fclose(file);
    printf("[SNAPSHOT] Restore complete!\n");
    
    return true;
}

void sfpm_snapshot_destroy(sfpm_snapshot_t *snapshot) {
    if (!snapshot) {
        return;
    }
    
    /* Note: We don't free the actual memory regions,
     * only the snapshot descriptor itself */
    free(snapshot);
}

sfpm_snapshot_t *sfpm_snapshot_create_for_interpreter(void *stack_base, 
                                                       size_t stack_size,
                                                       void *heap_base, 
                                                       size_t heap_size) {
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    if (!snapshot) {
        return NULL;
    }
    
    /* Add stack region */
    if (stack_base && stack_size > 0) {
        sfpm_memory_region_t stack_region = {
            .base_address = stack_base,
            .size = stack_size,
            .name = "stack",
            .is_dynamic = false
        };
        sfpm_snapshot_add_region(snapshot, &stack_region);
    }
    
    /* Add heap region */
    if (heap_base && heap_size > 0) {
        sfpm_memory_region_t heap_region = {
            .base_address = heap_base,
            .size = heap_size,
            .name = "heap",
            .is_dynamic = true
        };
        sfpm_snapshot_add_region(snapshot, &heap_region);
    }
    
    return snapshot;
}

bool sfpm_snapshot_save_delta(const sfpm_snapshot_t *snapshot,
                               const char *previous_filename,
                               const char *output_filename) {
    /* Delta snapshots not yet implemented */
    /* This would compare current memory with previous snapshot
     * and only save changed regions */
    (void)snapshot;
    (void)previous_filename;
    (void)output_filename;
    
    fprintf(stderr, "Delta snapshots not yet implemented\n");
    return false;
}
