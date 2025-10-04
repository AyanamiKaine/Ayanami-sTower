#include "sfpm/rule.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

struct sfpm_rule {
    sfpm_criteria_t **criterias;
    size_t criteria_count;
    sfpm_payload_fn payload;
    void *payload_user_data;
    char *name;
    int priority;
    
    /* Hooks */
    sfpm_hook_fn before_hook;
    void *before_hook_user_data;
    sfpm_hook_fn after_hook;
    void *after_hook_user_data;
};

sfpm_rule_t *sfpm_rule_create(sfpm_criteria_t **criterias,
                               size_t criteria_count,
                               sfpm_payload_fn payload,
                               void *payload_user_data,
                               const char *name) {
    if (!criterias && criteria_count > 0) {
        return NULL;
    }

    sfpm_rule_t *rule = (sfpm_rule_t *)malloc(sizeof(sfpm_rule_t));
    if (!rule) {
        return NULL;
    }

    rule->criterias = criterias;
    rule->criteria_count = criteria_count;
    rule->payload = payload;
    rule->payload_user_data = payload_user_data;
    rule->priority = 0;
    
    /* Initialize hooks to NULL */
    rule->before_hook = NULL;
    rule->before_hook_user_data = NULL;
    rule->after_hook = NULL;
    rule->after_hook_user_data = NULL;

    if (name) {
        rule->name = (char *)malloc(strlen(name) + 1);
        if (rule->name) {
            strcpy(rule->name, name);
        }
    } else {
        rule->name = NULL;
    }

    return rule;
}

void sfpm_rule_destroy(sfpm_rule_t *rule) {
    if (!rule) {
        return;
    }

    /* Destroy all criteria */
    for (size_t i = 0; i < rule->criteria_count; i++) {
        sfpm_criteria_destroy(rule->criterias[i]);
    }
    free(rule->criterias);
    free(rule->name);
    free(rule);
}

sfpm_eval_result_t sfpm_rule_evaluate(const sfpm_rule_t *rule,
                                       const sfpm_fact_source_t *facts) {
    sfpm_eval_result_t result = {false, 0};

    if (!rule || !facts) {
        return result;
    }

    if (rule->criteria_count == 0) {
        result.matched = true;
        result.criteria_count = 0;
        return result;
    }

    for (size_t i = 0; i < rule->criteria_count; i++) {
        if (!rule->criterias[i]) {
            continue; /* Skip null criteria */
        }

        if (!sfpm_criteria_evaluate(rule->criterias[i], facts)) {
            return result; /* Short-circuit on first failure */
        }
    }

    result.matched = true;
    result.criteria_count = (int)rule->criteria_count;
    return result;
}

void sfpm_rule_execute_payload(const sfpm_rule_t *rule) {
    if (!rule || !rule->payload) {
        return;
    }
    
    /* Execute before hook if present */
    if (rule->before_hook) {
        bool should_continue = rule->before_hook(rule->before_hook_user_data,
                                                  rule->payload_user_data);
        if (!should_continue) {
            return;  /* Abort execution if before hook returns false */
        }
    }

    /* Execute main payload */
    rule->payload(rule->payload_user_data);
    
    /* Execute after hook if present */
    if (rule->after_hook) {
        rule->after_hook(rule->after_hook_user_data,
                         rule->payload_user_data);
    }
}

int sfpm_rule_get_criteria_count(const sfpm_rule_t *rule) {
    return rule ? (int)rule->criteria_count : 0;
}

int sfpm_rule_get_priority(const sfpm_rule_t *rule) {
    return rule ? rule->priority : 0;
}

void sfpm_rule_set_priority(sfpm_rule_t *rule, int priority) {
    if (rule) {
        rule->priority = priority;
    }
}

const char *sfpm_rule_get_name(const sfpm_rule_t *rule) {
    return rule ? rule->name : NULL;
}

void sfpm_rule_set_before_hook(sfpm_rule_t *rule,
                                sfpm_hook_fn hook,
                                void *user_data) {
    if (!rule) {
        return;
    }
    rule->before_hook = hook;
    rule->before_hook_user_data = user_data;
}

void sfpm_rule_set_after_hook(sfpm_rule_t *rule,
                               sfpm_hook_fn hook,
                               void *user_data) {
    if (!rule) {
        return;
    }
    rule->after_hook = hook;
    rule->after_hook_user_data = user_data;
}
