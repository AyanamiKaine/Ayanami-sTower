#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include <string.h> // Needed for memcpy

// Define the number of elements in our arrays.
#define NUM_ELEMENTS 20000000

// Define a simple data structure.
typedef struct {
    long id;
    int value;
    char name[8];
} DataObject;

// --- Scenario 1: Array of Structs (Optimal Cache Performance) ---
long process_array_of_structs(DataObject* array) {
    long sum = 0;
    for (int i = 0; i < NUM_ELEMENTS; ++i) {
        sum += array[i].value;
    }
    return sum;
}

double run_struct_array_scenario() {
    printf("--- Scenario 1: Array of Structs ---\n");
    DataObject* data_array = (DataObject*)malloc(NUM_ELEMENTS * sizeof(DataObject));
    if (!data_array) { /* ... error handling ... */ return -1.0; }
    for (int i = 0; i < NUM_ELEMENTS; ++i) { data_array[i].value = i % 100; }

    printf("Processing array with optimal data locality...\n");
    clock_t start = clock();
    long sum = process_array_of_structs(data_array);
    clock_t end = clock();
    double time_spent = (double)(end - start) / CLOCKS_PER_SEC;
    printf("Sum: %ld, Time: %f seconds\n\n", sum, time_spent);

    free(data_array);
    return time_spent;
}

// --- Scenario 2: Array of Pointers to Scattered Structs (Worst Cache Performance) ---
void shuffle_pointers(DataObject** array, int n) {
    srand(time(NULL));
    for (int i = n - 1; i > 0; i--) {
        int j = rand() % (i + 1);
        DataObject* temp = array[i];
        array[i] = array[j];
        array[j] = temp;
    }
}

long process_array_of_pointers(DataObject** array) {
    long sum = 0;
    for (int i = 0; i < NUM_ELEMENTS; ++i) {
        sum += array[i]->value;
    }
    return sum;
}

double run_pointer_array_scenario() {
    printf("--- Scenario 2: Pointers to Scattered Structs ---\n");
    DataObject** pointer_array = (DataObject**)malloc(NUM_ELEMENTS * sizeof(DataObject*));
    if (!pointer_array) { /* ... error handling ... */ return -1.0; }
    printf("Individually allocating %d structs (creating fragmentation)...\n", NUM_ELEMENTS);
    for (int i = 0; i < NUM_ELEMENTS; ++i) {
        pointer_array[i] = (DataObject*)malloc(sizeof(DataObject));
        if (!pointer_array[i]) { /* ... error handling ... */ return -1.0; }
        pointer_array[i]->value = i % 100;
    }

    printf("Shuffling pointers to ensure random memory access...\n");
    shuffle_pointers(pointer_array, NUM_ELEMENTS);

    printf("Processing pointers with worst-case data locality...\n");
    clock_t start = clock();
    long sum = process_array_of_pointers(pointer_array);
    clock_t end = clock();
    double time_spent = (double)(end - start) / CLOCKS_PER_SEC;
    printf("Sum: %ld, Time: %f seconds\n\n", sum, time_spent);

    for (int i = 0; i < NUM_ELEMENTS; ++i) { free(pointer_array[i]); }
    free(pointer_array);
    return time_spent;
}

// --- Scenario 3: Simulating GC Compaction ---
// We start with scattered data, then manually compact it and re-measure.
double run_compacted_pointer_scenario() {
    printf("--- Scenario 3: Pointers to Compacted Structs (Simulating GC) ---\n");
    DataObject** pointer_array = (DataObject**)malloc(NUM_ELEMENTS * sizeof(DataObject*));
    if (!pointer_array) { /* ... error handling ... */ return -1.0; }
    printf("Individually allocating %d structs (initial fragmented state)...\n", NUM_ELEMENTS);
    for (int i = 0; i < NUM_ELEMENTS; ++i) {
        pointer_array[i] = (DataObject*)malloc(sizeof(DataObject));
        if (!pointer_array[i]) { /* ... error handling ... */ return -1.0; }
        pointer_array[i]->value = i % 100;
    }
    shuffle_pointers(pointer_array, NUM_ELEMENTS);

    // *** THE COMPACTION SIMULATION STEP ***
    printf("Simulating GC: Allocating a new contiguous block...\n");
    DataObject* compacted_block = (DataObject*)malloc(NUM_ELEMENTS * sizeof(DataObject));
    if (!compacted_block) { /* ... error handling ... */ return -1.0; }

    printf("Simulating GC: Copying scattered objects to contiguous block...\n");
    for (int i = 0; i < NUM_ELEMENTS; ++i) {
        // Copy the data from the scattered location to the new contiguous block.
        memcpy(&compacted_block[i], pointer_array[i], sizeof(DataObject));
        // We must free the original scattered block now that its data is moved.
        free(pointer_array[i]);
        // Update the pointer to point to the new, compacted location.
        pointer_array[i] = &compacted_block[i];
    }

    printf("Processing pointers now pointing to contiguous data...\n");
    clock_t start = clock();
    long sum = process_array_of_pointers(pointer_array);
    clock_t end = clock();
    double time_spent = (double)(end - start) / CLOCKS_PER_SEC;
    printf("Sum: %ld, Time: %f seconds\n\n", sum, time_spent);

    // Cleanup: The pointers now point inside compacted_block, so we just
    // need to free the block itself and the array of pointers.
    free(compacted_block);
    free(pointer_array);
    return time_spent;
}

int main() {
    double time_structs = run_struct_array_scenario();
    double time_pointers_scattered = run_pointer_array_scenario();
    double time_pointers_compacted = run_compacted_pointer_scenario();

    if (time_structs > 0 && time_pointers_scattered > 0 && time_pointers_compacted > 0) {
        printf("--- Performance Summary ---\n");
        printf("1. Struct Array (Optimal):      %f seconds\n", time_structs);
        printf("2. Pointers Scattered (Worst):    %f seconds (%.2f%% slower than optimal)\n", time_pointers_scattered, ((time_pointers_scattered / time_structs) - 1) * 100.0);
        printf("3. Pointers Compacted (GC Sim): %f seconds (%.2f%% slower than optimal)\n", time_pointers_compacted, ((time_pointers_compacted / time_structs) - 1) * 100.0);
    } else {
        printf("Could not run one or more scenarios, cannot calculate difference.\n");
    }

    return 0;
}
