/**
 * @file rule.h
 * @brief Rule definition and management
 * 
 * Rules contain criteria and payloads to execute when matched.
 */

#ifndef SFPM_RULE_H
#define SFPM_RULE_H

#include "criteria.h"
#include "fact_source.h"
#include <stdbool.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Opaque handle to a rule
 */
typedef struct sfpm_rule sfpm_rule_t;

/**
 * @brief Payload function type for rule actions
 * 
 * @param user_data User-defined context data
 */
typedef void (*sfpm_payload_fn)(void *user_data);

/**
 * @brief Result of rule evaluation
 */
typedef struct {
    bool matched;              /**< True if all criteria matched */
    int criteria_count;        /**< Number of criteria that matched */
} sfpm_eval_result_t;

/**
 * @brief Create a rule
 * 
 * @param criterias Array of criteria pointers (takes ownership)
 * @param criteria_count Number of criteria in the array
 * @param payload The payload function to execute
 * @param payload_user_data User data to pass to the payload
 * @param name Optional name for debugging (can be NULL)
 * @return Pointer to the created rule, or NULL on failure
 */
sfpm_rule_t *sfpm_rule_create(sfpm_criteria_t **criterias,
                               size_t criteria_count,
                               sfpm_payload_fn payload,
                               void *payload_user_data,
                               const char *name);

/**
 * @brief Destroy a rule
 * 
 * @param rule The rule to destroy
 */
void sfpm_rule_destroy(sfpm_rule_t *rule);

/**
 * @brief Evaluate a rule against a fact source
 * 
 * @param rule The rule to evaluate
 * @param facts The fact source
 * @return Evaluation result
 */
sfpm_eval_result_t sfpm_rule_evaluate(const sfpm_rule_t *rule,
                                       const sfpm_fact_source_t *facts);

/**
 * @brief Execute the payload of a rule
 * 
 * @param rule The rule whose payload to execute
 */
void sfpm_rule_execute_payload(const sfpm_rule_t *rule);

/**
 * @brief Get the number of criteria in a rule
 * 
 * @param rule The rule
 * @return The criteria count
 */
int sfpm_rule_get_criteria_count(const sfpm_rule_t *rule);

/**
 * @brief Get the priority of a rule
 * 
 * @param rule The rule
 * @return The priority value
 */
int sfpm_rule_get_priority(const sfpm_rule_t *rule);

/**
 * @brief Set the priority of a rule
 * 
 * @param rule The rule
 * @param priority The priority value
 */
void sfpm_rule_set_priority(sfpm_rule_t *rule, int priority);

/**
 * @brief Get the name of a rule
 * 
 * @param rule The rule
 * @return The rule name, or NULL if not set
 */
const char *sfpm_rule_get_name(const sfpm_rule_t *rule);

#ifdef __cplusplus
}
#endif

#endif /* SFPM_RULE_H */
