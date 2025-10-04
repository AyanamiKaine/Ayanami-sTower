#include <sfpm/sfpm.h>
#include <stdio.h>
#include <stdlib.h>

/* Example payload functions */
static void handle_critical_situation(void *user_data) {
    (void)user_data; /* Unused */
    printf("Critical situation! Taking cover and healing.\n");
}

static void handle_combat(void *user_data) {
    (void)user_data;
    printf("Engaging in combat!\n");
}

static void handle_low_health(void *user_data) {
    (void)user_data;
    printf("Health is low, need to heal.\n");
}

static void handle_idle(void *user_data) {
    (void)user_data;
    printf("Everything is fine, exploring...\n");
}

/* Custom predicate for health check */
static bool health_is_low(const sfpm_value_t *value, void *user_data) {
    int threshold = *(int *)user_data;
    if (value->type != SFPM_TYPE_INT) {
        return false;
    }
    return value->data.int_value < threshold;
}

int main(void) {
    printf("=== SFPM-C Basic Example ===\n\n");

    /* Create fact source */
    sfpm_fact_source_t *facts = sfpm_dict_fact_source_create(10);
    if (!facts) {
        fprintf(stderr, "Failed to create fact source\n");
        return 1;
    }

    /* Add facts */
    sfpm_dict_fact_source_add(facts, "health", sfpm_value_from_int(30));
    sfpm_dict_fact_source_add(facts, "isInCombat", sfpm_value_from_bool(true));
    sfpm_dict_fact_source_add(facts, "enemyCount", sfpm_value_from_int(3));

    /* Create criteria */
    int health_threshold = 50;
    
    /* Criteria for critical rule */
    sfpm_criteria_t *health_low = sfpm_criteria_create_predicate(
        "health",
        health_is_low,
        &health_threshold,
        "health < 50"
    );

    sfpm_criteria_t *in_combat = sfpm_criteria_create(
        "isInCombat",
        SFPM_OP_EQUAL,
        sfpm_value_from_bool(true)
    );

    /* Criteria for combat rule (separate instance) */
    sfpm_criteria_t *in_combat2 = sfpm_criteria_create(
        "isInCombat",
        SFPM_OP_EQUAL,
        sfpm_value_from_bool(true)
    );

    /* Create rules */
    
    /* Rule 1: Critical situation (low health + combat) */
    sfpm_criteria_t **critical_criterias = malloc(sizeof(sfpm_criteria_t*) * 2);
    critical_criterias[0] = health_low;
    critical_criterias[1] = in_combat;
    sfpm_rule_t *critical_rule = sfpm_rule_create(
        critical_criterias,
        2,
        handle_critical_situation,
        NULL,
        "critical_situation"
    );
    sfpm_rule_set_priority(critical_rule, 10);

    /* Rule 2: Combat (just in combat) */
    sfpm_criteria_t **combat_criterias = malloc(sizeof(sfpm_criteria_t*) * 1);
    combat_criterias[0] = in_combat2;
    sfpm_rule_t *combat_rule = sfpm_rule_create(
        combat_criterias,
        1,
        handle_combat,
        NULL,
        "combat"
    );
    sfpm_rule_set_priority(combat_rule, 5);

    /* Create rule array */
    sfpm_rule_t *rules[] = {critical_rule, combat_rule};
    size_t rule_count = 2;

    printf("Scenario: Health=30, InCombat=true, EnemyCount=3\n");
    printf("Expected: Critical situation (most specific match)\n\n");

    /* Match and execute */
    sfpm_match(rules, rule_count, facts, true);

    printf("\n--- Changing scenario ---\n");
    sfpm_dict_fact_source_add(facts, "health", sfpm_value_from_int(80));
    printf("Scenario: Health=80, InCombat=true, EnemyCount=3\n");
    printf("Expected: Engaging in combat\n\n");

    sfpm_match(rules, rule_count, facts, true);

    /* Cleanup - Note: We only destroy the rules, not individual criteria
     * because the rule takes ownership */
    sfpm_rule_destroy(critical_rule);
    sfpm_rule_destroy(combat_rule);
    sfpm_fact_source_destroy(facts);

    printf("\n=== Example completed successfully ===\n");
    return 0;
}
