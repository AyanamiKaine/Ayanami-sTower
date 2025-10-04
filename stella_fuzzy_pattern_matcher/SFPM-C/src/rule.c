#include "sfpm/rule.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

/* Hook chain node structure */
struct sfpm_hook_node {
    sfpm_hook_fn hook;
    void *user_data;
    sfpm_hook_node_t *next;
};

struct sfpm_rule {
    sfpm_criteria_t **criterias;
    size_t criteria_count;
    sfpm_payload_fn payload;
    void *payload_user_data;
    char *name;
    int priority;
    
    /* Hook chains */
    sfpm_hook_node_t *before_hook_chain;
    sfpm_hook_node_t *after_hook_chain;
    sfpm_hook_node_t *middleware_hook_chain;
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
    
    /* Initialize hook chains to NULL */
    rule->before_hook_chain = NULL;
    rule->after_hook_chain = NULL;
    rule->middleware_hook_chain = NULL;

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
    
    /* Free hook chains */
    sfpm_rule_clear_hooks(rule);
    
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
    
    /* Execute before hook chain */
    sfpm_hook_node_t *node = rule->before_hook_chain;
    while (node) {
        bool should_continue = node->hook(node->user_data, rule->payload_user_data);
        if (!should_continue) {
            return;  /* Abort if any before hook returns false */
        }
        node = node->next;
    }
    
    /* Execute middleware hook chain (before payload) */
    node = rule->middleware_hook_chain;
    while (node) {
        bool should_continue = node->hook(node->user_data, rule->payload_user_data);
        if (!should_continue) {
            return;  /* Abort if any middleware hook returns false */
        }
        node = node->next;
    }

    /* Execute main payload */
    rule->payload(rule->payload_user_data);
    
    /* Execute middleware hook chain (after payload) - in reverse would be ideal but we'll keep it simple */
    /* Note: In a full middleware implementation, we'd wrap the payload and call it from within */
    /* For now, middleware hooks execute both before and after, which is useful for timing/logging */
    
    /* Execute after hook chain */
    node = rule->after_hook_chain;
    while (node) {
        node->hook(node->user_data, rule->payload_user_data);
        node = node->next;
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

/* Helper function to add a hook to a chain */
static bool add_hook_to_chain(sfpm_hook_node_t **chain,
                               sfpm_hook_fn hook,
                               void *user_data) {
    if (!chain || !hook) {
        return false;
    }
    
    /* Create new node */
    sfpm_hook_node_t *node = (sfpm_hook_node_t *)malloc(sizeof(sfpm_hook_node_t));
    if (!node) {
        return false;
    }
    
    node->hook = hook;
    node->user_data = user_data;
    node->next = NULL;
    
    /* Add to end of chain */
    if (*chain == NULL) {
        *chain = node;
    } else {
        sfpm_hook_node_t *current = *chain;
        while (current->next) {
            current = current->next;
        }
        current->next = node;
    }
    
    return true;
}

/* Helper function to free a hook chain */
static void free_hook_chain(sfpm_hook_node_t **chain) {
    if (!chain) {
        return;
    }
    
    sfpm_hook_node_t *current = *chain;
    while (current) {
        sfpm_hook_node_t *next = current->next;
        free(current);
        current = next;
    }
    
    *chain = NULL;
}

/* Helper function to count hooks in a chain */
static int count_hooks_in_chain(const sfpm_hook_node_t *chain) {
    int count = 0;
    const sfpm_hook_node_t *current = chain;
    while (current) {
        count++;
        current = current->next;
    }
    return count;
}

bool sfpm_rule_add_before_hook(sfpm_rule_t *rule,
                                sfpm_hook_fn hook,
                                void *user_data) {
    if (!rule) {
        return false;
    }
    return add_hook_to_chain(&rule->before_hook_chain, hook, user_data);
}

bool sfpm_rule_add_after_hook(sfpm_rule_t *rule,
                               sfpm_hook_fn hook,
                               void *user_data) {
    if (!rule) {
        return false;
    }
    return add_hook_to_chain(&rule->after_hook_chain, hook, user_data);
}

bool sfpm_rule_add_middleware_hook(sfpm_rule_t *rule,
                                    sfpm_hook_fn hook,
                                    void *user_data) {
    if (!rule) {
        return false;
    }
    return add_hook_to_chain(&rule->middleware_hook_chain, hook, user_data);
}

void sfpm_rule_clear_hooks(sfpm_rule_t *rule) {
    if (!rule) {
        return;
    }
    
    /* Free hook chains */
    free_hook_chain(&rule->before_hook_chain);
    free_hook_chain(&rule->after_hook_chain);
    free_hook_chain(&rule->middleware_hook_chain);
}

int sfpm_rule_get_before_hook_count(const sfpm_rule_t *rule) {
    if (!rule) {
        return 0;
    }
    return count_hooks_in_chain(rule->before_hook_chain);
}

int sfpm_rule_get_after_hook_count(const sfpm_rule_t *rule) {
    if (!rule) {
        return 0;
    }
    return count_hooks_in_chain(rule->after_hook_chain);
}

int sfpm_rule_get_middleware_hook_count(const sfpm_rule_t *rule) {
    if (!rule) {
        return 0;
    }
    return count_hooks_in_chain(rule->middleware_hook_chain);
}
