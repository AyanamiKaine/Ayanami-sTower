#include "sfpm/matcher.h"
#include <stdlib.h>
#include <stdio.h>
#include <time.h>

/* Comparison function for qsort */
static int compare_rules_by_criteria(const void *a, const void *b) {
    sfpm_rule_t *rule_a = *(sfpm_rule_t **)a;
    sfpm_rule_t *rule_b = *(sfpm_rule_t **)b;
    
    int count_a = sfpm_rule_get_criteria_count(rule_a);
    int count_b = sfpm_rule_get_criteria_count(rule_b);
    
    /* Sort descending (highest criteria count first) */
    return count_b - count_a;
}

void sfpm_optimize_rules(sfpm_rule_t **rules, size_t rule_count) {
    if (!rules || rule_count == 0) {
        return;
    }

    qsort(rules, rule_count, sizeof(sfpm_rule_t *), compare_rules_by_criteria);
}

sfpm_rule_t *sfpm_most_specific_rule(sfpm_rule_t **rules, size_t rule_count) {
    if (!rules || rule_count == 0) {
        return NULL;
    }

    sfpm_rule_t *most_specific = rules[0];
    int max_criteria = sfpm_rule_get_criteria_count(most_specific);

    for (size_t i = 1; i < rule_count; i++) {
        int criteria_count = sfpm_rule_get_criteria_count(rules[i]);
        if (criteria_count > max_criteria) {
            max_criteria = criteria_count;
            most_specific = rules[i];
        }
    }

    return most_specific;
}

sfpm_rule_t *sfpm_least_specific_rule(sfpm_rule_t **rules, size_t rule_count) {
    if (!rules || rule_count == 0) {
        return NULL;
    }

    sfpm_rule_t *least_specific = rules[0];
    int min_criteria = sfpm_rule_get_criteria_count(least_specific);

    for (size_t i = 1; i < rule_count; i++) {
        int criteria_count = sfpm_rule_get_criteria_count(rules[i]);
        if (criteria_count < min_criteria) {
            min_criteria = criteria_count;
            least_specific = rules[i];
        }
    }

    return least_specific;
}

void sfpm_match(sfpm_rule_t **rules,
                size_t rule_count,
                const sfpm_fact_source_t *facts,
                bool optimize) {
    if (!rules || rule_count == 0 || !facts) {
        return;
    }

    /* Optimize if requested */
    if (optimize) {
        sfpm_optimize_rules(rules, rule_count);
    }

    /* Find all fully matched rules */
    sfpm_rule_t **matched_rules = (sfpm_rule_t **)malloc(sizeof(sfpm_rule_t *) * rule_count);
    if (!matched_rules) {
        return;
    }

    size_t matched_count = 0;
    int best_score = 0;

    for (size_t i = 0; i < rule_count; i++) {
        if (!rules[i]) {
            continue;
        }

        sfpm_eval_result_t eval = sfpm_rule_evaluate(rules[i], facts);
        
        if (eval.matched) {
            if (eval.criteria_count > best_score) {
                /* New best score, reset matched list */
                best_score = eval.criteria_count;
                matched_count = 0;
                matched_rules[matched_count++] = rules[i];
            } else if (eval.criteria_count == best_score && best_score > 0) {
                /* Tie with current best */
                matched_rules[matched_count++] = rules[i];
            }
        }

        /* Early exit optimization if rules are sorted */
        if (optimize && sfpm_rule_get_criteria_count(rules[i]) < best_score) {
            break;
        }
    }

    /* No matches found */
    if (matched_count == 0) {
        free(matched_rules);
        return;
    }

    /* Select the best rule to execute */
    sfpm_rule_t *selected = NULL;

    if (matched_count == 1) {
        selected = matched_rules[0];
    } else {
        /* Multiple rules tied - use priority */
        int highest_priority = sfpm_rule_get_priority(matched_rules[0]);
        
        for (size_t i = 1; i < matched_count; i++) {
            int priority = sfpm_rule_get_priority(matched_rules[i]);
            if (priority > highest_priority) {
                highest_priority = priority;
            }
        }

        /* Collect rules with highest priority */
        sfpm_rule_t **priority_candidates = (sfpm_rule_t **)malloc(sizeof(sfpm_rule_t *) * matched_count);
        if (!priority_candidates) {
            free(matched_rules);
            return;
        }

        size_t priority_count = 0;
        for (size_t i = 0; i < matched_count; i++) {
            if (sfpm_rule_get_priority(matched_rules[i]) == highest_priority) {
                priority_candidates[priority_count++] = matched_rules[i];
            }
        }

        if (priority_count == 1) {
            selected = priority_candidates[0];
        } else {
            /* Still tied - random selection */
            static bool rand_initialized = false;
            if (!rand_initialized) {
                srand((unsigned int)time(NULL));
                rand_initialized = true;
            }
            
            size_t random_index = rand() % priority_count;
            selected = priority_candidates[random_index];
        }

        free(priority_candidates);
    }

    free(matched_rules);

    /* Execute the selected rule's payload */
    if (selected) {
        sfpm_rule_execute_payload(selected);
    }
}
