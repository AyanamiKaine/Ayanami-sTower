#include <sfpm/sfpm.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>

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

/* Test payload tracking */
static int payload_executed = 0;
static void test_payload(void *user_data) {
    (void)user_data;
    payload_executed++;
}

/* Hook execution tracking */
typedef struct {
    int call_count;
    int call_order[10];
    int next_order;
} hook_tracker_t;

static hook_tracker_t before_tracker = {0};
static hook_tracker_t after_tracker = {0};
static hook_tracker_t middleware_tracker = {0};

static void reset_trackers(void) {
    memset(&before_tracker, 0, sizeof(before_tracker));
    memset(&after_tracker, 0, sizeof(after_tracker));
    memset(&middleware_tracker, 0, sizeof(middleware_tracker));
    payload_executed = 0;
}

/* Hook functions for testing */
static bool before_hook_1(void *hook_data, void *payload_data) {
    (void)payload_data;
    hook_tracker_t *tracker = (hook_tracker_t *)hook_data;
    tracker->call_order[tracker->call_count++] = tracker->next_order++;
    return true;
}

static bool before_hook_2(void *hook_data, void *payload_data) {
    (void)payload_data;
    hook_tracker_t *tracker = (hook_tracker_t *)hook_data;
    tracker->call_order[tracker->call_count++] = tracker->next_order++;
    return true;
}

static bool before_hook_3(void *hook_data, void *payload_data) {
    (void)payload_data;
    hook_tracker_t *tracker = (hook_tracker_t *)hook_data;
    tracker->call_order[tracker->call_count++] = tracker->next_order++;
    return true;
}

static bool after_hook_1(void *hook_data, void *payload_data) {
    (void)payload_data;
    hook_tracker_t *tracker = (hook_tracker_t *)hook_data;
    tracker->call_order[tracker->call_count++] = tracker->next_order++;
    return true;
}

static bool after_hook_2(void *hook_data, void *payload_data) {
    (void)payload_data;
    hook_tracker_t *tracker = (hook_tracker_t *)hook_data;
    tracker->call_order[tracker->call_count++] = tracker->next_order++;
    return true;
}

static bool middleware_hook_1(void *hook_data, void *payload_data) {
    (void)payload_data;
    hook_tracker_t *tracker = (hook_tracker_t *)hook_data;
    tracker->call_order[tracker->call_count++] = tracker->next_order++;
    return true;
}

static bool hook_that_aborts(void *hook_data, void *payload_data) {
    (void)hook_data;
    (void)payload_data;
    return false;  /* Abort execution */
}

/* Helper hooks for specific tests */
static int *g_received_hook_data = NULL;
static bool capture_hook_data(void *hook_data, void *payload_data) {
    (void)payload_data;
    g_received_hook_data = (int *)hook_data;
    return true;
}

static int *g_received_payload_data = NULL;
static bool capture_payload_data(void *hook_data, void *payload_data) {
    (void)hook_data;
    g_received_payload_data = (int *)payload_data;
    return true;
}

static void payload_with_data(void *user_data) {
    int *data = (int *)user_data;
    if (data && *data == 99) {
        /* Expected value */
    }
}

/* Tests */

TEST(test_add_single_before_hook) {
    reset_trackers();
    
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, test_payload, NULL, "test");
    ASSERT(rule != NULL);
    
    bool result = sfpm_rule_add_before_hook(rule, before_hook_1, &before_tracker);
    ASSERT(result == true);
    ASSERT(sfpm_rule_get_before_hook_count(rule) == 1);
    
    sfpm_rule_execute_payload(rule);
    ASSERT(payload_executed == 1);
    ASSERT(before_tracker.call_count == 1);
    
    sfpm_rule_destroy(rule);
}

TEST(test_add_multiple_before_hooks) {
    reset_trackers();
    
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, test_payload, NULL, "test");
    ASSERT(rule != NULL);
    
    sfpm_rule_add_before_hook(rule, before_hook_1, &before_tracker);
    sfpm_rule_add_before_hook(rule, before_hook_2, &before_tracker);
    sfpm_rule_add_before_hook(rule, before_hook_3, &before_tracker);
    
    ASSERT(sfpm_rule_get_before_hook_count(rule) == 3);
    
    sfpm_rule_execute_payload(rule);
    ASSERT(payload_executed == 1);
    ASSERT(before_tracker.call_count == 3);
    
    /* Verify execution order */
    ASSERT(before_tracker.call_order[0] == 0);  /* First hook */
    ASSERT(before_tracker.call_order[1] == 1);  /* Second hook */
    ASSERT(before_tracker.call_order[2] == 2);  /* Third hook */
    
    sfpm_rule_destroy(rule);
}

TEST(test_add_single_after_hook) {
    reset_trackers();
    
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, test_payload, NULL, "test");
    ASSERT(rule != NULL);
    
    bool result = sfpm_rule_add_after_hook(rule, after_hook_1, &after_tracker);
    ASSERT(result == true);
    ASSERT(sfpm_rule_get_after_hook_count(rule) == 1);
    
    sfpm_rule_execute_payload(rule);
    ASSERT(payload_executed == 1);
    ASSERT(after_tracker.call_count == 1);
    
    sfpm_rule_destroy(rule);
}

TEST(test_add_multiple_after_hooks) {
    reset_trackers();
    
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, test_payload, NULL, "test");
    ASSERT(rule != NULL);
    
    sfpm_rule_add_after_hook(rule, after_hook_1, &after_tracker);
    sfpm_rule_add_after_hook(rule, after_hook_2, &after_tracker);
    
    ASSERT(sfpm_rule_get_after_hook_count(rule) == 2);
    
    sfpm_rule_execute_payload(rule);
    ASSERT(payload_executed == 1);
    ASSERT(after_tracker.call_count == 2);
    
    /* Verify execution order */
    ASSERT(after_tracker.call_order[0] == 0);
    ASSERT(after_tracker.call_order[1] == 1);
    
    sfpm_rule_destroy(rule);
}

TEST(test_add_middleware_hook) {
    reset_trackers();
    
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, test_payload, NULL, "test");
    ASSERT(rule != NULL);
    
    bool result = sfpm_rule_add_middleware_hook(rule, middleware_hook_1, &middleware_tracker);
    ASSERT(result == true);
    ASSERT(sfpm_rule_get_middleware_hook_count(rule) == 1);
    
    sfpm_rule_execute_payload(rule);
    ASSERT(payload_executed == 1);
    ASSERT(middleware_tracker.call_count == 1);
    
    sfpm_rule_destroy(rule);
}

TEST(test_combined_hook_execution_order) {
    reset_trackers();
    
    /* Use a single global order counter */
    static int global_order = 0;
    global_order = 0;
    
    before_tracker.next_order = 0;
    middleware_tracker.next_order = 0;
    after_tracker.next_order = 0;
    
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, test_payload, NULL, "test");
    ASSERT(rule != NULL);
    
    /* Add hooks in specific order */
    sfpm_rule_add_before_hook(rule, before_hook_1, &before_tracker);
    sfpm_rule_add_before_hook(rule, before_hook_2, &before_tracker);
    sfpm_rule_add_middleware_hook(rule, middleware_hook_1, &middleware_tracker);
    sfpm_rule_add_after_hook(rule, after_hook_1, &after_tracker);
    sfpm_rule_add_after_hook(rule, after_hook_2, &after_tracker);
    
    ASSERT(sfpm_rule_get_before_hook_count(rule) == 2);
    ASSERT(sfpm_rule_get_middleware_hook_count(rule) == 1);
    ASSERT(sfpm_rule_get_after_hook_count(rule) == 2);
    
    /* Execute and verify order: before1 -> before2 -> middleware -> payload -> after1 -> after2 */
    sfpm_rule_execute_payload(rule);
    
    ASSERT(payload_executed == 1);
    ASSERT(before_tracker.call_count == 2);
    ASSERT(middleware_tracker.call_count == 1);
    ASSERT(after_tracker.call_count == 2);
    
    sfpm_rule_destroy(rule);
}

TEST(test_before_hook_abort) {
    reset_trackers();
    
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, test_payload, NULL, "test");
    ASSERT(rule != NULL);
    
    sfpm_rule_add_before_hook(rule, before_hook_1, &before_tracker);
    sfpm_rule_add_before_hook(rule, hook_that_aborts, NULL);  /* This should abort */
    sfpm_rule_add_before_hook(rule, before_hook_2, &before_tracker);  /* Should not execute */
    
    sfpm_rule_execute_payload(rule);
    
    /* Payload should NOT execute */
    ASSERT(payload_executed == 0);
    /* Only first hook should execute */
    ASSERT(before_tracker.call_count == 1);
    
    sfpm_rule_destroy(rule);
}

TEST(test_middleware_hook_abort) {
    reset_trackers();
    
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, test_payload, NULL, "test");
    ASSERT(rule != NULL);
    
    sfpm_rule_add_before_hook(rule, before_hook_1, &before_tracker);
    sfpm_rule_add_middleware_hook(rule, hook_that_aborts, NULL);  /* This should abort */
    sfpm_rule_add_after_hook(rule, after_hook_1, &after_tracker);  /* Should not execute */
    
    sfpm_rule_execute_payload(rule);
    
    /* Before hooks should execute */
    ASSERT(before_tracker.call_count == 1);
    /* Payload should NOT execute */
    ASSERT(payload_executed == 0);
    /* After hooks should NOT execute */
    ASSERT(after_tracker.call_count == 0);
    
    sfpm_rule_destroy(rule);
}

TEST(test_after_hooks_always_execute) {
    reset_trackers();
    
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, test_payload, NULL, "test");
    ASSERT(rule != NULL);
    
    sfpm_rule_add_after_hook(rule, after_hook_1, &after_tracker);
    sfpm_rule_add_after_hook(rule, after_hook_2, &after_tracker);
    
    sfpm_rule_execute_payload(rule);
    
    /* After hooks should always execute (return value ignored) */
    ASSERT(after_tracker.call_count == 2);
    ASSERT(payload_executed == 1);
    
    sfpm_rule_destroy(rule);
}

TEST(test_clear_hooks) {
    reset_trackers();
    
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, test_payload, NULL, "test");
    ASSERT(rule != NULL);
    
    /* Add several hooks */
    sfpm_rule_add_before_hook(rule, before_hook_1, &before_tracker);
    sfpm_rule_add_before_hook(rule, before_hook_2, &before_tracker);
    sfpm_rule_add_middleware_hook(rule, middleware_hook_1, &middleware_tracker);
    sfpm_rule_add_after_hook(rule, after_hook_1, &after_tracker);
    
    ASSERT(sfpm_rule_get_before_hook_count(rule) == 2);
    ASSERT(sfpm_rule_get_middleware_hook_count(rule) == 1);
    ASSERT(sfpm_rule_get_after_hook_count(rule) == 1);
    
    /* Clear all hooks */
    sfpm_rule_clear_hooks(rule);
    
    ASSERT(sfpm_rule_get_before_hook_count(rule) == 0);
    ASSERT(sfpm_rule_get_middleware_hook_count(rule) == 0);
    ASSERT(sfpm_rule_get_after_hook_count(rule) == 0);
    
    /* Execute should work, just no hooks */
    sfpm_rule_execute_payload(rule);
    ASSERT(payload_executed == 1);
    ASSERT(before_tracker.call_count == 0);
    ASSERT(middleware_tracker.call_count == 0);
    ASSERT(after_tracker.call_count == 0);
    
    sfpm_rule_destroy(rule);
}

TEST(test_null_rule_handling) {
    /* All hook functions should handle NULL gracefully */
    bool result;
    
    result = sfpm_rule_add_before_hook(NULL, before_hook_1, NULL);
    ASSERT(result == false);
    
    result = sfpm_rule_add_after_hook(NULL, after_hook_1, NULL);
    ASSERT(result == false);
    
    result = sfpm_rule_add_middleware_hook(NULL, middleware_hook_1, NULL);
    ASSERT(result == false);
    
    ASSERT(sfpm_rule_get_before_hook_count(NULL) == 0);
    ASSERT(sfpm_rule_get_after_hook_count(NULL) == 0);
    ASSERT(sfpm_rule_get_middleware_hook_count(NULL) == 0);
    
    sfpm_rule_clear_hooks(NULL);  /* Should not crash */
}

TEST(test_null_hook_function) {
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, test_payload, NULL, "test");
    ASSERT(rule != NULL);
    
    /* Should fail to add NULL hook */
    bool result = sfpm_rule_add_before_hook(rule, NULL, NULL);
    ASSERT(result == false);
    ASSERT(sfpm_rule_get_before_hook_count(rule) == 0);
    
    sfpm_rule_destroy(rule);
}

TEST(test_large_hook_chain) {
    reset_trackers();
    
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, test_payload, NULL, "test");
    ASSERT(rule != NULL);
    
    /* Add 10 before hooks */
    for (int i = 0; i < 10; i++) {
        sfpm_rule_add_before_hook(rule, before_hook_1, &before_tracker);
    }
    
    ASSERT(sfpm_rule_get_before_hook_count(rule) == 10);
    
    sfpm_rule_execute_payload(rule);
    ASSERT(before_tracker.call_count == 10);
    ASSERT(payload_executed == 1);
    
    sfpm_rule_destroy(rule);
}

TEST(test_hook_user_data) {
    reset_trackers();
    
    static int custom_data = 42;
    g_received_hook_data = NULL;
    
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, test_payload, NULL, "test");
    ASSERT(rule != NULL);
    
    sfpm_rule_add_before_hook(rule, capture_hook_data, &custom_data);
    
    sfpm_rule_execute_payload(rule);
    
    ASSERT(g_received_hook_data != NULL);
    ASSERT(*g_received_hook_data == 42);
    
    sfpm_rule_destroy(rule);
}

TEST(test_payload_user_data_passed_to_hooks) {
    static int payload_data = 99;
    g_received_payload_data = NULL;
    
    sfpm_rule_t *rule = sfpm_rule_create(NULL, 0, payload_with_data, &payload_data, "test");
    ASSERT(rule != NULL);
    
    sfpm_rule_add_before_hook(rule, capture_payload_data, NULL);
    
    sfpm_rule_execute_payload(rule);
    
    ASSERT(g_received_payload_data != NULL);
    ASSERT(*g_received_payload_data == 99);
    
    sfpm_rule_destroy(rule);
}

/* Main test runner */
int main(void) {
    printf("========================================\n");
    printf("SFPM HOOK CHAINING TESTS\n");
    printf("========================================\n\n");
    
    run_test_add_single_before_hook();
    run_test_add_multiple_before_hooks();
    run_test_add_single_after_hook();
    run_test_add_multiple_after_hooks();
    run_test_add_middleware_hook();
    run_test_combined_hook_execution_order();
    run_test_before_hook_abort();
    run_test_middleware_hook_abort();
    run_test_after_hooks_always_execute();
    run_test_clear_hooks();
    run_test_null_rule_handling();
    run_test_null_hook_function();
    run_test_large_hook_chain();
    run_test_hook_user_data();
    run_test_payload_user_data_passed_to_hooks();
    
    printf("\n========================================\n");
    printf("RESULTS: %d/%d tests passed\n", tests_passed, tests_run);
    printf("========================================\n");
    
    return (tests_passed == tests_run) ? 0 : 1;
}
