/**
 * @file matcher.h
 * @brief Rule matching engine
 * 
 * Matches rules against facts and executes the best matching rule.
 */

#ifndef SFPM_MATCHER_H
#define SFPM_MATCHER_H

#include "rule.h"
#include "fact_source.h"
#include <stdbool.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Match rules against a fact source and execute the best match
 * 
 * The matcher selects the rule with the most matching criteria.
 * If multiple rules have the same criteria count, priority is used.
 * If priorities are equal, a random rule is selected.
 * 
 * @param rules Array of rule pointers
 * @param rule_count Number of rules in the array
 * @param facts The fact source to match against
 * @param optimize If true, sorts rules by criteria count for performance
 */
void sfpm_match(sfpm_rule_t **rules,
                size_t rule_count,
                const sfpm_fact_source_t *facts,
                bool optimize);

/**
 * @brief Optimize rules by sorting by criteria count (in-place)
 * 
 * @param rules Array of rule pointers
 * @param rule_count Number of rules in the array
 */
void sfpm_optimize_rules(sfpm_rule_t **rules, size_t rule_count);

/**
 * @brief Find the most specific rule (highest criteria count)
 * 
 * @param rules Array of rule pointers
 * @param rule_count Number of rules (must be > 0)
 * @return Pointer to the most specific rule, or NULL if array is empty
 */
sfpm_rule_t *sfpm_most_specific_rule(sfpm_rule_t **rules, size_t rule_count);

/**
 * @brief Find the least specific rule (lowest criteria count)
 * 
 * @param rules Array of rule pointers
 * @param rule_count Number of rules (must be > 0)
 * @return Pointer to the least specific rule, or NULL if array is empty
 */
sfpm_rule_t *sfpm_least_specific_rule(sfpm_rule_t **rules, size_t rule_count);

#ifdef __cplusplus
}
#endif

#endif /* SFPM_MATCHER_H */
