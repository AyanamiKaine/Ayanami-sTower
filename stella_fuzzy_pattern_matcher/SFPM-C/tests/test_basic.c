#include <sfpm/sfpm.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

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

TEST(test_value_constructors) {
    sfpm_value_t v_int = sfpm_value_from_int(42);
    ASSERT(v_int.type == SFPM_TYPE_INT);
    ASSERT(v_int.data.int_value == 42);

    sfpm_value_t v_bool = sfpm_value_from_bool(true);
    ASSERT(v_bool.type == SFPM_TYPE_BOOL);
    ASSERT(v_bool.data.bool_value == true);

    sfpm_value_t v_string = sfpm_value_from_string("test");
    ASSERT(v_string.type == SFPM_TYPE_STRING);
    ASSERT(strcmp(v_string.data.string_value, "test") == 0);
}

TEST(test_dict_fact_source) {
    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(5);
    ASSERT(facts != NULL);

    bool added = sfpm_dict_fact_source_add(facts, "health", sfpm_value_from_int(100));
    ASSERT(added == true);

    sfpm_value_t value;
    bool found = sfpm_fact_source_try_get(facts, "health", &value);
    ASSERT(found == true);
    ASSERT(value.type == SFPM_TYPE_INT);
    ASSERT(value.data.int_value == 100);

    bool not_found = sfpm_fact_source_try_get(facts, "nonexistent", &value);
    ASSERT(not_found == false);

    sfpm_fact_source_destroy(facts);
}

TEST(test_criteria_comparison) {
    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(5);
    sfpm_dict_fact_source_add(facts, "health", sfpm_value_from_int(50));

    sfpm_criteria_t *equal = sfpm_criteria_create(
        "health",
        SFPM_OP_EQUAL,
        sfpm_value_from_int(50)
    );
    ASSERT(sfpm_criteria_evaluate(equal, facts) == true);
    sfpm_criteria_destroy(equal);

    sfpm_criteria_t *greater = sfpm_criteria_create(
        "health",
        SFPM_OP_GREATER_THAN,
        sfpm_value_from_int(30)
    );
    ASSERT(sfpm_criteria_evaluate(greater, facts) == true);
    sfpm_criteria_destroy(greater);

    sfpm_criteria_t *less = sfpm_criteria_create(
        "health",
        SFPM_OP_LESS_THAN,
        sfpm_value_from_int(30)
    );
    ASSERT(sfpm_criteria_evaluate(less, facts) == false);
    sfpm_criteria_destroy(less);

    sfpm_fact_source_destroy(facts);
}

static bool is_greater_than_40(const sfpm_value_t *value, void *user_data) {
    (void)user_data;
    if (value->type != SFPM_TYPE_INT) return false;
    return value->data.int_value > 40;
}

TEST(test_criteria_predicate) {
    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(5);
    sfpm_dict_fact_source_add(facts, "health", sfpm_value_from_int(50));

    sfpm_criteria_t *predicate = sfpm_criteria_create_predicate(
        "health",
        is_greater_than_40,
        NULL,
        "health > 40"
    );

    ASSERT(sfpm_criteria_evaluate(predicate, facts) == true);

    sfpm_dict_fact_source_add(facts, "health", sfpm_value_from_int(30));
    ASSERT(sfpm_criteria_evaluate(predicate, facts) == false);

    sfpm_criteria_destroy(predicate);
    sfpm_fact_source_destroy(facts);
}

TEST(test_rule_evaluation) {
    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(5);
    sfpm_dict_fact_source_add(facts, "health", sfpm_value_from_int(50));
    sfpm_dict_fact_source_add(facts, "combat", sfpm_value_from_bool(true));

    sfpm_criteria_t *c1 = sfpm_criteria_create(
        "health",
        SFPM_OP_GREATER_THAN,
        sfpm_value_from_int(30)
    );
    sfpm_criteria_t *c2 = sfpm_criteria_create(
        "combat",
        SFPM_OP_EQUAL,
        sfpm_value_from_bool(true)
    );

    sfpm_criteria_t **criterias = malloc(sizeof(sfpm_criteria_t*) * 2);
    criterias[0] = c1;
    criterias[1] = c2;
    sfpm_rule_t *rule = sfpm_rule_create(
        criterias,
        2,
        test_payload,
        NULL,
        "test_rule"
    );

    sfpm_eval_result_t result = sfpm_rule_evaluate(rule, facts);
    ASSERT(result.matched == true);
    ASSERT(result.criteria_count == 2);

    ASSERT(sfpm_rule_get_criteria_count(rule) == 2);
    ASSERT(strcmp(sfpm_rule_get_name(rule), "test_rule") == 0);

    sfpm_rule_destroy(rule);
    sfpm_fact_source_destroy(facts);
}

TEST(test_rule_execution) {
    payload_executed = 0;

    sfpm_criteria_t **criterias = NULL;
    sfpm_rule_t *rule = sfpm_rule_create(
        criterias,
        0,
        test_payload,
        NULL,
        NULL
    );

    sfpm_rule_execute_payload(rule);
    ASSERT(payload_executed == 1);

    sfpm_rule_destroy(rule);
}

TEST(test_matching_specificity) {
    payload_executed = 0;

    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(5);
    sfpm_dict_fact_source_add(facts, "a", sfpm_value_from_int(1));
    sfpm_dict_fact_source_add(facts, "b", sfpm_value_from_int(2));

    /* Rule with 2 criteria */
    sfpm_criteria_t *c1a = sfpm_criteria_create("a", SFPM_OP_EQUAL, sfpm_value_from_int(1));
    sfpm_criteria_t *c1b = sfpm_criteria_create("b", SFPM_OP_EQUAL, sfpm_value_from_int(2));
    sfpm_criteria_t **criterias1 = malloc(sizeof(sfpm_criteria_t*) * 2);
    criterias1[0] = c1a;
    criterias1[1] = c1b;
    sfpm_rule_t *rule1 = sfpm_rule_create(criterias1, 2, test_payload, NULL, "rule1");

    /* Rule with 1 criterion */
    sfpm_criteria_t *c2a = sfpm_criteria_create("a", SFPM_OP_EQUAL, sfpm_value_from_int(1));
    sfpm_criteria_t **criterias2 = malloc(sizeof(sfpm_criteria_t*) * 1);
    criterias2[0] = c2a;
    sfpm_rule_t *rule2 = sfpm_rule_create(criterias2, 1, test_payload, NULL, "rule2");

    sfpm_rule_t *rules[] = {rule2, rule1}; /* Deliberately out of order */

    /* Should select rule1 (more specific) */
    sfpm_match(rules, 2, facts, true);
    ASSERT(payload_executed == 1); /* Only one rule should execute */

    sfpm_rule_destroy(rule1);
    sfpm_rule_destroy(rule2);
    sfpm_fact_source_destroy(facts);
}

TEST(test_priority_selection) {
    payload_executed = 0;

    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(5);
    sfpm_dict_fact_source_add(facts, "x", sfpm_value_from_int(1));

    sfpm_criteria_t *c1 = sfpm_criteria_create("x", SFPM_OP_EQUAL, sfpm_value_from_int(1));
    sfpm_criteria_t **criterias1 = malloc(sizeof(sfpm_criteria_t*) * 1);
    criterias1[0] = c1;
    sfpm_rule_t *rule1 = sfpm_rule_create(criterias1, 1, test_payload, NULL, "low_priority");
    sfpm_rule_set_priority(rule1, 1);

    sfpm_criteria_t *c2 = sfpm_criteria_create("x", SFPM_OP_EQUAL, sfpm_value_from_int(1));
    sfpm_criteria_t **criterias2 = malloc(sizeof(sfpm_criteria_t*) * 1);
    criterias2[0] = c2;
    sfpm_rule_t *rule2 = sfpm_rule_create(criterias2, 1, test_payload, NULL, "high_priority");
    sfpm_rule_set_priority(rule2, 10);

    ASSERT(sfpm_rule_get_priority(rule2) == 10);

    sfpm_rule_t *rules[] = {rule1, rule2};
    
    /* Should select rule2 (higher priority) */
    sfpm_match(rules, 2, facts, false);
    ASSERT(payload_executed == 1);

    sfpm_rule_destroy(rule1);
    sfpm_rule_destroy(rule2);
    sfpm_fact_source_destroy(facts);
}

int main(void) {
    printf("=== SFPM-C Basic Tests ===\n\n");

    run_test_value_constructors();
    run_test_dict_fact_source();
    run_test_criteria_comparison();
    run_test_criteria_predicate();
    run_test_rule_evaluation();
    run_test_rule_execution();
    run_test_matching_specificity();
    run_test_priority_selection();

    printf("\n=== Test Results ===\n");
    printf("Tests run: %d\n", tests_run);
    printf("Tests passed: %d\n", tests_passed);
    printf("Tests failed: %d\n", tests_run - tests_passed);

    if (tests_run == tests_passed) {
        printf("\nAll tests PASSED!\n");
        return 0;
    } else {
        printf("\nSome tests FAILED!\n");
        return 1;
    }
}
