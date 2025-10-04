#include <sfpm/snapshot.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>
#include <time.h>

/* Simple test framework */
static int tests_run = 0;
static int tests_passed = 0;

#define TEST(name) \
    static void name(void); \
    static void run_##name(void) { \
        printf("Running: %s...", #name); \
        tests_run++; \
        name(); \
        tests_passed++; \
        printf(" PASSED\n"); \
    } \
    static void name(void)

#define ASSERT(condition) \
    do { \
        if (!(condition)) { \
            fprintf(stderr, "\nAssertion failed: %s\n", #condition); \
            fprintf(stderr, "  at %s:%d\n", __FILE__, __LINE__); \
            exit(1); \
        } \
    } while (0)

/* Test data structures */
typedef struct {
    int value1;
    int value2;
    char text[64];
    double decimal;
} test_data_t;

/* Test snapshot file */
#define TEST_SNAPSHOT_FILE "test_snapshot.img"
#define TEST_SNAPSHOT_FILE_2 "test_snapshot_2.img"

/* Cleanup helper */
static void cleanup_test_files(void) {
    remove(TEST_SNAPSHOT_FILE);
    remove(TEST_SNAPSHOT_FILE_2);
}

/* Tests */

TEST(test_snapshot_create_destroy) {
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    ASSERT(snapshot != NULL);
    
    sfpm_snapshot_destroy(snapshot);
    /* If we get here without crash, destroy worked */
}

TEST(test_snapshot_destroy_null) {
    /* Should handle NULL gracefully without crashing */
    sfpm_snapshot_destroy(NULL);
}

TEST(test_add_single_region) {
    test_data_t data = {42, 99, "hello", 3.14};
    
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    ASSERT(snapshot != NULL);
    
    sfpm_memory_region_t region = {
        .base_address = &data,
        .size = sizeof(data),
        .name = "test_data",
        .is_dynamic = false
    };
    
    bool result = sfpm_snapshot_add_region(snapshot, &region);
    ASSERT(result == true);
    
    sfpm_snapshot_destroy(snapshot);
}

TEST(test_add_multiple_regions) {
    test_data_t data1 = {1, 2, "first", 1.0};
    test_data_t data2 = {3, 4, "second", 2.0};
    test_data_t data3 = {5, 6, "third", 3.0};
    
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    ASSERT(snapshot != NULL);
    
    sfpm_memory_region_t regions[] = {
        { .base_address = &data1, .size = sizeof(data1), .name = "data1", .is_dynamic = false },
        { .base_address = &data2, .size = sizeof(data2), .name = "data2", .is_dynamic = false },
        { .base_address = &data3, .size = sizeof(data3), .name = "data3", .is_dynamic = false }
    };
    
    for (int i = 0; i < 3; i++) {
        bool result = sfpm_snapshot_add_region(snapshot, &regions[i]);
        ASSERT(result == true);
    }
    
    sfpm_snapshot_destroy(snapshot);
}

TEST(test_add_region_null_snapshot) {
    test_data_t data = {0};
    sfpm_memory_region_t region = {
        .base_address = &data,
        .size = sizeof(data),
        .name = "test"
    };
    
    bool result = sfpm_snapshot_add_region(NULL, &region);
    ASSERT(result == false);
}

TEST(test_add_region_null_region) {
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    ASSERT(snapshot != NULL);
    
    bool result = sfpm_snapshot_add_region(snapshot, NULL);
    ASSERT(result == false);
    
    sfpm_snapshot_destroy(snapshot);
}

TEST(test_add_region_null_base_address) {
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    ASSERT(snapshot != NULL);
    
    sfpm_memory_region_t region = {
        .base_address = NULL,
        .size = 100,
        .name = "invalid"
    };
    
    bool result = sfpm_snapshot_add_region(snapshot, &region);
    ASSERT(result == false);
    
    sfpm_snapshot_destroy(snapshot);
}

TEST(test_add_region_zero_size) {
    test_data_t data = {0};
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    ASSERT(snapshot != NULL);
    
    sfpm_memory_region_t region = {
        .base_address = &data,
        .size = 0,
        .name = "zero_size"
    };
    
    bool result = sfpm_snapshot_add_region(snapshot, &region);
    ASSERT(result == false);
    
    sfpm_snapshot_destroy(snapshot);
}

TEST(test_set_description) {
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    ASSERT(snapshot != NULL);
    
    const char *desc = "Test snapshot description";
    sfpm_snapshot_set_description(snapshot, desc);
    
    /* Description is set internally, we can't verify directly,
     * but we can check it doesn't crash */
    
    sfpm_snapshot_destroy(snapshot);
}

TEST(test_set_description_null_snapshot) {
    /* Should handle gracefully */
    sfpm_snapshot_set_description(NULL, "test");
}

TEST(test_set_description_null_string) {
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    ASSERT(snapshot != NULL);
    
    /* Should handle gracefully */
    sfpm_snapshot_set_description(snapshot, NULL);
    
    sfpm_snapshot_destroy(snapshot);
}

TEST(test_save_and_restore_single_region) {
    cleanup_test_files();
    
    /* Original data */
    test_data_t original = {42, 99, "original text", 3.14159};
    
    /* Create and save snapshot */
    sfpm_snapshot_t *save_snapshot = sfpm_snapshot_create();
    ASSERT(save_snapshot != NULL);
    
    sfpm_memory_region_t region = {
        .base_address = &original,
        .size = sizeof(original),
        .name = "test_data",
        .is_dynamic = false
    };
    
    ASSERT(sfpm_snapshot_add_region(save_snapshot, &region) == true);
    sfpm_snapshot_set_description(save_snapshot, "Test save/restore");
    
    bool save_result = sfpm_snapshot_save(save_snapshot, TEST_SNAPSHOT_FILE);
    ASSERT(save_result == true);
    
    sfpm_snapshot_destroy(save_snapshot);
    
    /* Modify original data */
    original.value1 = 0;
    original.value2 = 0;
    strcpy(original.text, "modified");
    original.decimal = 0.0;
    
    /* Create restore snapshot and load */
    test_data_t restored = {0};
    
    sfpm_snapshot_t *load_snapshot = sfpm_snapshot_create();
    ASSERT(load_snapshot != NULL);
    
    sfpm_memory_region_t restore_region = {
        .base_address = &restored,
        .size = sizeof(restored),
        .name = "test_data",
        .is_dynamic = false
    };
    
    ASSERT(sfpm_snapshot_add_region(load_snapshot, &restore_region) == true);
    
    bool restore_result = sfpm_snapshot_restore(TEST_SNAPSHOT_FILE, load_snapshot);
    ASSERT(restore_result == true);
    
    /* Verify restored data matches original */
    ASSERT(restored.value1 == 42);
    ASSERT(restored.value2 == 99);
    ASSERT(strcmp(restored.text, "original text") == 0);
    ASSERT(restored.decimal > 3.14 && restored.decimal < 3.15);
    
    sfpm_snapshot_destroy(load_snapshot);
    cleanup_test_files();
}

TEST(test_save_and_restore_multiple_regions) {
    cleanup_test_files();
    
    /* Original data */
    test_data_t data1 = {10, 20, "first", 1.1};
    test_data_t data2 = {30, 40, "second", 2.2};
    test_data_t data3 = {50, 60, "third", 3.3};
    
    /* Save snapshot */
    sfpm_snapshot_t *save_snapshot = sfpm_snapshot_create();
    ASSERT(save_snapshot != NULL);
    
    sfpm_memory_region_t save_regions[] = {
        { .base_address = &data1, .size = sizeof(data1), .name = "data1", .is_dynamic = false },
        { .base_address = &data2, .size = sizeof(data2), .name = "data2", .is_dynamic = false },
        { .base_address = &data3, .size = sizeof(data3), .name = "data3", .is_dynamic = false }
    };
    
    for (int i = 0; i < 3; i++) {
        ASSERT(sfpm_snapshot_add_region(save_snapshot, &save_regions[i]) == true);
    }
    
    ASSERT(sfpm_snapshot_save(save_snapshot, TEST_SNAPSHOT_FILE) == true);
    sfpm_snapshot_destroy(save_snapshot);
    
    /* Restore to new data structures */
    test_data_t restored1 = {0};
    test_data_t restored2 = {0};
    test_data_t restored3 = {0};
    
    sfpm_snapshot_t *load_snapshot = sfpm_snapshot_create();
    ASSERT(load_snapshot != NULL);
    
    sfpm_memory_region_t restore_regions[] = {
        { .base_address = &restored1, .size = sizeof(restored1), .name = "data1", .is_dynamic = false },
        { .base_address = &restored2, .size = sizeof(restored2), .name = "data2", .is_dynamic = false },
        { .base_address = &restored3, .size = sizeof(restored3), .name = "data3", .is_dynamic = false }
    };
    
    for (int i = 0; i < 3; i++) {
        ASSERT(sfpm_snapshot_add_region(load_snapshot, &restore_regions[i]) == true);
    }
    
    ASSERT(sfpm_snapshot_restore(TEST_SNAPSHOT_FILE, load_snapshot) == true);
    
    /* Verify all data */
    ASSERT(restored1.value1 == 10 && restored1.value2 == 20);
    ASSERT(strcmp(restored1.text, "first") == 0);
    
    ASSERT(restored2.value1 == 30 && restored2.value2 == 40);
    ASSERT(strcmp(restored2.text, "second") == 0);
    
    ASSERT(restored3.value1 == 50 && restored3.value2 == 60);
    ASSERT(strcmp(restored3.text, "third") == 0);
    
    sfpm_snapshot_destroy(load_snapshot);
    cleanup_test_files();
}

TEST(test_read_metadata) {
    cleanup_test_files();
    
    /* Create and save a snapshot */
    test_data_t data = {123, 456, "metadata test", 7.89};
    
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    ASSERT(snapshot != NULL);
    
    sfpm_memory_region_t region = {
        .base_address = &data,
        .size = sizeof(data),
        .name = "test_data",
        .is_dynamic = false
    };
    
    ASSERT(sfpm_snapshot_add_region(snapshot, &region) == true);
    sfpm_snapshot_set_description(snapshot, "Metadata test snapshot");
    
    ASSERT(sfpm_snapshot_save(snapshot, TEST_SNAPSHOT_FILE) == true);
    sfpm_snapshot_destroy(snapshot);
    
    /* Read metadata */
    sfpm_snapshot_metadata_t metadata;
    bool result = sfpm_snapshot_read_metadata(TEST_SNAPSHOT_FILE, &metadata);
    
    ASSERT(result == true);
    ASSERT(metadata.version == 1);
    ASSERT(metadata.num_regions == 1);
    ASSERT(metadata.total_size == sizeof(data));
    ASSERT(strcmp(metadata.description, "Metadata test snapshot") == 0);
    ASSERT(metadata.timestamp > 0);
    
    cleanup_test_files();
}

TEST(test_read_metadata_nonexistent_file) {
    sfpm_snapshot_metadata_t metadata;
    bool result = sfpm_snapshot_read_metadata("nonexistent.img", &metadata);
    
    ASSERT(result == false);
}

TEST(test_read_metadata_null_params) {
    sfpm_snapshot_metadata_t metadata;
    
    /* NULL filename */
    ASSERT(sfpm_snapshot_read_metadata(NULL, &metadata) == false);
    
    /* NULL metadata */
    ASSERT(sfpm_snapshot_read_metadata(TEST_SNAPSHOT_FILE, NULL) == false);
}

TEST(test_save_null_snapshot) {
    bool result = sfpm_snapshot_save(NULL, TEST_SNAPSHOT_FILE);
    ASSERT(result == false);
}

TEST(test_save_null_filename) {
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    ASSERT(snapshot != NULL);
    
    bool result = sfpm_snapshot_save(snapshot, NULL);
    ASSERT(result == false);
    
    sfpm_snapshot_destroy(snapshot);
}

TEST(test_restore_null_filename) {
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    ASSERT(snapshot != NULL);
    
    bool result = sfpm_snapshot_restore(NULL, snapshot);
    ASSERT(result == false);
    
    sfpm_snapshot_destroy(snapshot);
}

TEST(test_restore_null_snapshot) {
    bool result = sfpm_snapshot_restore(TEST_SNAPSHOT_FILE, NULL);
    ASSERT(result == false);
}

TEST(test_restore_nonexistent_file) {
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create();
    ASSERT(snapshot != NULL);
    
    bool result = sfpm_snapshot_restore("nonexistent.img", snapshot);
    ASSERT(result == false);
    
    sfpm_snapshot_destroy(snapshot);
}

TEST(test_restore_region_count_mismatch) {
    cleanup_test_files();
    
    /* Save with 2 regions */
    test_data_t data1 = {1, 2, "one", 1.0};
    test_data_t data2 = {3, 4, "two", 2.0};
    
    sfpm_snapshot_t *save_snapshot = sfpm_snapshot_create();
    ASSERT(save_snapshot != NULL);
    
    sfpm_memory_region_t save_regions[] = {
        { .base_address = &data1, .size = sizeof(data1), .name = "data1", .is_dynamic = false },
        { .base_address = &data2, .size = sizeof(data2), .name = "data2", .is_dynamic = false }
    };
    
    for (int i = 0; i < 2; i++) {
        ASSERT(sfpm_snapshot_add_region(save_snapshot, &save_regions[i]) == true);
    }
    
    ASSERT(sfpm_snapshot_save(save_snapshot, TEST_SNAPSHOT_FILE) == true);
    sfpm_snapshot_destroy(save_snapshot);
    
    /* Try to restore with only 1 region (should fail) */
    test_data_t restored = {0};
    
    sfpm_snapshot_t *load_snapshot = sfpm_snapshot_create();
    ASSERT(load_snapshot != NULL);
    
    sfpm_memory_region_t restore_region = {
        .base_address = &restored,
        .size = sizeof(restored),
        .name = "data1",
        .is_dynamic = false
    };
    
    ASSERT(sfpm_snapshot_add_region(load_snapshot, &restore_region) == true);
    
    /* Should fail due to region count mismatch */
    bool result = sfpm_snapshot_restore(TEST_SNAPSHOT_FILE, load_snapshot);
    ASSERT(result == false);
    
    sfpm_snapshot_destroy(load_snapshot);
    cleanup_test_files();
}

TEST(test_restore_region_size_mismatch) {
    cleanup_test_files();
    
    /* Save with specific size */
    test_data_t data = {1, 2, "test", 1.0};
    
    sfpm_snapshot_t *save_snapshot = sfpm_snapshot_create();
    ASSERT(save_snapshot != NULL);
    
    sfpm_memory_region_t save_region = {
        .base_address = &data,
        .size = sizeof(data),
        .name = "data",
        .is_dynamic = false
    };
    
    ASSERT(sfpm_snapshot_add_region(save_snapshot, &save_region) == true);
    ASSERT(sfpm_snapshot_save(save_snapshot, TEST_SNAPSHOT_FILE) == true);
    sfpm_snapshot_destroy(save_snapshot);
    
    /* Try to restore with different size (should fail) */
    char smaller_buffer[10] = {0};
    
    sfpm_snapshot_t *load_snapshot = sfpm_snapshot_create();
    ASSERT(load_snapshot != NULL);
    
    sfpm_memory_region_t restore_region = {
        .base_address = smaller_buffer,
        .size = sizeof(smaller_buffer),  /* Different size! */
        .name = "data",
        .is_dynamic = false
    };
    
    ASSERT(sfpm_snapshot_add_region(load_snapshot, &restore_region) == true);
    
    /* Should fail due to size mismatch */
    bool result = sfpm_snapshot_restore(TEST_SNAPSHOT_FILE, load_snapshot);
    ASSERT(result == false);
    
    sfpm_snapshot_destroy(load_snapshot);
    cleanup_test_files();
}

TEST(test_snapshot_preserves_exact_bytes) {
    cleanup_test_files();
    
    /* Create data with specific byte pattern */
    unsigned char data[256];
    for (int i = 0; i < 256; i++) {
        data[i] = (unsigned char)i;
    }
    
    /* Save */
    sfpm_snapshot_t *save_snapshot = sfpm_snapshot_create();
    ASSERT(save_snapshot != NULL);
    
    sfpm_memory_region_t region = {
        .base_address = data,
        .size = sizeof(data),
        .name = "byte_pattern",
        .is_dynamic = false
    };
    
    ASSERT(sfpm_snapshot_add_region(save_snapshot, &region) == true);
    ASSERT(sfpm_snapshot_save(save_snapshot, TEST_SNAPSHOT_FILE) == true);
    sfpm_snapshot_destroy(save_snapshot);
    
    /* Restore to different buffer */
    unsigned char restored[256] = {0};
    
    sfpm_snapshot_t *load_snapshot = sfpm_snapshot_create();
    ASSERT(load_snapshot != NULL);
    
    sfpm_memory_region_t restore_region = {
        .base_address = restored,
        .size = sizeof(restored),
        .name = "byte_pattern",
        .is_dynamic = false
    };
    
    ASSERT(sfpm_snapshot_add_region(load_snapshot, &restore_region) == true);
    ASSERT(sfpm_snapshot_restore(TEST_SNAPSHOT_FILE, load_snapshot) == true);
    
    /* Verify every byte */
    for (int i = 0; i < 256; i++) {
        ASSERT(restored[i] == (unsigned char)i);
    }
    
    sfpm_snapshot_destroy(load_snapshot);
    cleanup_test_files();
}

TEST(test_create_for_interpreter) {
    int stack[100];
    int heap[200];
    
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create_for_interpreter(
        stack, sizeof(stack),
        heap, sizeof(heap)
    );
    
    ASSERT(snapshot != NULL);
    
    /* Can't verify internal state, but should not crash */
    sfpm_snapshot_destroy(snapshot);
}

TEST(test_create_for_interpreter_null_regions) {
    /* Should handle NULL regions gracefully */
    sfpm_snapshot_t *snapshot = sfpm_snapshot_create_for_interpreter(
        NULL, 0,
        NULL, 0
    );
    
    ASSERT(snapshot != NULL);
    sfpm_snapshot_destroy(snapshot);
}

/* Main test runner */
int main(void) {
    printf("========================================\n");
    printf("SFPM SNAPSHOT TESTS\n");
    printf("========================================\n\n");
    
    /* Creation and destruction */
    run_test_snapshot_create_destroy();
    run_test_snapshot_destroy_null();
    
    /* Adding regions */
    run_test_add_single_region();
    run_test_add_multiple_regions();
    run_test_add_region_null_snapshot();
    run_test_add_region_null_region();
    run_test_add_region_null_base_address();
    run_test_add_region_zero_size();
    
    /* Description */
    run_test_set_description();
    run_test_set_description_null_snapshot();
    run_test_set_description_null_string();
    
    /* Save and restore */
    run_test_save_and_restore_single_region();
    run_test_save_and_restore_multiple_regions();
    
    /* Metadata */
    run_test_read_metadata();
    run_test_read_metadata_nonexistent_file();
    run_test_read_metadata_null_params();
    
    /* Error cases */
    run_test_save_null_snapshot();
    run_test_save_null_filename();
    run_test_restore_null_filename();
    run_test_restore_null_snapshot();
    run_test_restore_nonexistent_file();
    run_test_restore_region_count_mismatch();
    run_test_restore_region_size_mismatch();
    
    /* Data integrity */
    run_test_snapshot_preserves_exact_bytes();
    
    /* Helper functions */
    run_test_create_for_interpreter();
    run_test_create_for_interpreter_null_regions();
    
    printf("\n========================================\n");
    printf("RESULTS: %d/%d tests passed\n", tests_passed, tests_run);
    printf("========================================\n");
    
    return (tests_passed == tests_run) ? 0 : 1;
}
