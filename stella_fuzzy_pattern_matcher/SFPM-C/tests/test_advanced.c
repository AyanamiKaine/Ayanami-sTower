#include <sfpm/sfpm.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

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

/* Advanced parity tests based on C# and C++ versions */

static const char *last_executed = NULL;

static void reset_tracking(void) {
    last_executed = NULL;
}

static void track_execution(void *user_data) {
    last_executed = (const char *)user_data;
}

TEST(test_type_safety) {
    /* Test that type mismatches are properly handled */
    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(5);
    sfpm_dict_fact_source_add(facts, "value", sfpm_value_from_int(42));

    /* Try to compare int fact with string criteria - should fail */
    sfpm_criteria_t *wrong_type = sfpm_criteria_create(
        "value",
        SFPM_OP_EQUAL,
        sfpm_value_from_string("42")
    );
    
    ASSERT(sfpm_criteria_evaluate(wrong_type, facts) == false);

    sfpm_criteria_destroy(wrong_type);
    sfpm_fact_source_destroy(facts);
}

TEST(test_float_comparison) {
    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(5);
    sfpm_dict_fact_source_add(facts, "temperature", sfpm_value_from_float(98.6f));

    sfpm_criteria_t *temp_check = sfpm_criteria_create(
        "temperature",
        SFPM_OP_GREATER_THAN,
        sfpm_value_from_float(98.0f)
    );
    
    ASSERT(sfpm_criteria_evaluate(temp_check, facts) == true);

    sfpm_criteria_destroy(temp_check);
    sfpm_fact_source_destroy(facts);
}

TEST(test_string_comparison) {
    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(5);
    sfpm_dict_fact_source_add(facts, "weather", sfpm_value_from_string("Rainy"));

    sfpm_criteria_t *weather_check = sfpm_criteria_create(
        "weather",
        SFPM_OP_EQUAL,
        sfpm_value_from_string("Rainy")
    );
    
    ASSERT(sfpm_criteria_evaluate(weather_check, facts) == true);

    sfpm_criteria_t *not_sunny = sfpm_criteria_create(
        "weather",
        SFPM_OP_NOT_EQUAL,
        sfpm_value_from_string("Sunny")
    );
    
    ASSERT(sfpm_criteria_evaluate(not_sunny, facts) == true);

    sfpm_criteria_destroy(weather_check);
    sfpm_criteria_destroy(not_sunny);
    sfpm_fact_source_destroy(facts);
}

TEST(test_optimization) {
    /* Create rules with varying criteria counts */
    sfpm_criteria_t *c1 = sfpm_criteria_create("a", SFPM_OP_EQUAL, sfpm_value_from_int(1));
    sfpm_criteria_t **criterias1 = malloc(sizeof(sfpm_criteria_t*) * 1);
    criterias1[0] = c1;
    sfpm_rule_t *rule1 = sfpm_rule_create(criterias1, 1, NULL, NULL, "1-criteria");

    sfpm_criteria_t *c2a = sfpm_criteria_create("a", SFPM_OP_EQUAL, sfpm_value_from_int(1));
    sfpm_criteria_t *c2b = sfpm_criteria_create("b", SFPM_OP_EQUAL, sfpm_value_from_int(2));
    sfpm_criteria_t *c2c = sfpm_criteria_create("c", SFPM_OP_EQUAL, sfpm_value_from_int(3));
    sfpm_criteria_t **criterias2 = malloc(sizeof(sfpm_criteria_t*) * 3);
    criterias2[0] = c2a;
    criterias2[1] = c2b;
    criterias2[2] = c2c;
    sfpm_rule_t *rule2 = sfpm_rule_create(criterias2, 3, NULL, NULL, "3-criteria");

    sfpm_criteria_t *c3a = sfpm_criteria_create("a", SFPM_OP_EQUAL, sfpm_value_from_int(1));
    sfpm_criteria_t *c3b = sfpm_criteria_create("b", SFPM_OP_EQUAL, sfpm_value_from_int(2));
    sfpm_criteria_t **criterias3 = malloc(sizeof(sfpm_criteria_t*) * 2);
    criterias3[0] = c3a;
    criterias3[1] = c3b;
    sfpm_rule_t *rule3 = sfpm_rule_create(criterias3, 2, NULL, NULL, "2-criteria");

    sfpm_rule_t *rules[] = {rule1, rule2, rule3};

    /* Optimize - should sort by criteria count descending */
    sfpm_optimize_rules(rules, 3);

    ASSERT(sfpm_rule_get_criteria_count(rules[0]) == 3);
    ASSERT(sfpm_rule_get_criteria_count(rules[1]) == 2);
    ASSERT(sfpm_rule_get_criteria_count(rules[2]) == 1);

    /* Test helper functions */
    sfpm_rule_t *most = sfpm_most_specific_rule(rules, 3);
    ASSERT(sfpm_rule_get_criteria_count(most) == 3);

    sfpm_rule_t *least = sfpm_least_specific_rule(rules, 3);
    ASSERT(sfpm_rule_get_criteria_count(least) == 1);

    sfpm_rule_destroy(rule1);
    sfpm_rule_destroy(rule2);
    sfpm_rule_destroy(rule3);
}

TEST(test_complex_scenario) {
    /* Simulate a game AI scenario like in the C# version */
    reset_tracking();

    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(10);
    sfpm_dict_fact_source_add(facts, "health", sfpm_value_from_int(30));
    sfpm_dict_fact_source_add(facts, "isInCombat", sfpm_value_from_bool(true));
    sfpm_dict_fact_source_add(facts, "enemyCount", sfpm_value_from_int(3));
    sfpm_dict_fact_source_add(facts, "hasWeapon", sfpm_value_from_bool(true));

    /* Critical rule: low health + combat */
    sfpm_criteria_t *crit_health = sfpm_criteria_create(
        "health", SFPM_OP_LESS_THAN, sfpm_value_from_int(50));
    sfpm_criteria_t *crit_combat = sfpm_criteria_create(
        "isInCombat", SFPM_OP_EQUAL, sfpm_value_from_bool(true));
    sfpm_criteria_t **crit_criterias = malloc(sizeof(sfpm_criteria_t*) * 2);
    crit_criterias[0] = crit_health;
    crit_criterias[1] = crit_combat;
    sfpm_rule_t *critical = sfpm_rule_create(
        crit_criterias, 2, track_execution, (void *)"critical", "critical");
    sfpm_rule_set_priority(critical, 10);

    /* Combat rule: just in combat */
    sfpm_criteria_t *combat_check = sfpm_criteria_create(
        "isInCombat", SFPM_OP_EQUAL, sfpm_value_from_bool(true));
    sfpm_criteria_t **combat_criterias = malloc(sizeof(sfpm_criteria_t*) * 1);
    combat_criterias[0] = combat_check;
    sfpm_rule_t *combat = sfpm_rule_create(
        combat_criterias, 1, track_execution, (void *)"combat", "combat");
    sfpm_rule_set_priority(combat, 5);

    sfpm_rule_t *rules[] = {critical, combat};

    /* Should execute critical (more specific) */
    sfpm_match(rules, 2, facts, true);
    ASSERT(last_executed != NULL);
    ASSERT(strcmp(last_executed, "critical") == 0);

    /* Change scenario - high health */
    reset_tracking();
    sfpm_dict_fact_source_add(facts, "health", sfpm_value_from_int(80));
    
    /* Should execute combat (critical no longer matches) */
    sfpm_match(rules, 2, facts, true);
    ASSERT(last_executed != NULL);
    ASSERT(strcmp(last_executed, "combat") == 0);

    sfpm_rule_destroy(critical);
    sfpm_rule_destroy(combat);
    sfpm_fact_source_destroy(facts);
}

TEST(test_no_match_scenario) {
    reset_tracking();

    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(5);
    sfpm_dict_fact_source_add(facts, "value", sfpm_value_from_int(100));

    sfpm_criteria_t *c = sfpm_criteria_create(
        "value", SFPM_OP_LESS_THAN, sfpm_value_from_int(50));
    sfpm_criteria_t **criterias = malloc(sizeof(sfpm_criteria_t*) * 1);
    criterias[0] = c;
    sfpm_rule_t *rule = sfpm_rule_create(
        criterias, 1, track_execution, (void *)"executed", "rule");

    sfpm_rule_t *rules[] = {rule};

    /* Should not execute (criteria doesn't match) */
    sfpm_match(rules, 1, facts, false);
    ASSERT(last_executed == NULL);

    sfpm_rule_destroy(rule);
    sfpm_fact_source_destroy(facts);
}

TEST(test_all_operators) {
    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(5);
    sfpm_dict_fact_source_add(facts, "value", sfpm_value_from_int(50));

    /* Equal */
    sfpm_criteria_t *eq = sfpm_criteria_create("value", SFPM_OP_EQUAL, sfpm_value_from_int(50));
    ASSERT(sfpm_criteria_evaluate(eq, facts) == true);
    sfpm_criteria_destroy(eq);

    /* Not equal */
    sfpm_criteria_t *neq = sfpm_criteria_create("value", SFPM_OP_NOT_EQUAL, sfpm_value_from_int(40));
    ASSERT(sfpm_criteria_evaluate(neq, facts) == true);
    sfpm_criteria_destroy(neq);

    /* Greater than */
    sfpm_criteria_t *gt = sfpm_criteria_create("value", SFPM_OP_GREATER_THAN, sfpm_value_from_int(40));
    ASSERT(sfpm_criteria_evaluate(gt, facts) == true);
    sfpm_criteria_destroy(gt);

    /* Less than */
    sfpm_criteria_t *lt = sfpm_criteria_create("value", SFPM_OP_LESS_THAN, sfpm_value_from_int(60));
    ASSERT(sfpm_criteria_evaluate(lt, facts) == true);
    sfpm_criteria_destroy(lt);

    /* Greater than or equal */
    sfpm_criteria_t *gte = sfpm_criteria_create("value", SFPM_OP_GREATER_THAN_OR_EQUAL, sfpm_value_from_int(50));
    ASSERT(sfpm_criteria_evaluate(gte, facts) == true);
    sfpm_criteria_destroy(gte);

    /* Less than or equal */
    sfpm_criteria_t *lte = sfpm_criteria_create("value", SFPM_OP_LESS_THAN_OR_EQUAL, sfpm_value_from_int(50));
    ASSERT(sfpm_criteria_evaluate(lte, facts) == true);
    sfpm_criteria_destroy(lte);

    sfpm_fact_source_destroy(facts);
}

int main(void) {
    printf("=== SFPM-C Advanced Tests ===\n\n");

    run_test_type_safety();
    run_test_float_comparison();
    run_test_string_comparison();
    run_test_optimization();
    run_test_complex_scenario();
    run_test_no_match_scenario();
    run_test_all_operators();

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
