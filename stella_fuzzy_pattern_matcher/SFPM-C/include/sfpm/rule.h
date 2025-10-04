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
 * @brief Hook function type for before/after payload execution
 * 
 * Called before or after the main payload execution.
 * Can be used for logging, validation, transformation, etc.
 * 
 * @param user_data User-defined context data
 * @param payload_user_data The user data that will be/was passed to the payload
 * @return true to continue execution, false to abort (for before hooks)
 */
typedef bool (*sfpm_hook_fn)(void *user_data, void *payload_user_data);

/**
 * @brief Hook chain node (opaque type)
 * 
 * Allows chaining multiple hooks together.
 */
typedef struct sfpm_hook_node sfpm_hook_node_t;

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
 * @brief Add a before-execution hook to the chain
 * 
 * Hooks are executed in the order they are added.
 * Each hook can abort execution by returning false.
 * 
 * @param rule The rule
 * @param hook The hook function to add
 * @param user_data User data to pass to the hook
 * @return true on success, false on failure
 */
bool sfpm_rule_add_before_hook(sfpm_rule_t *rule,
                                sfpm_hook_fn hook,
                                void *user_data);

/**
 * @brief Add an after-execution hook to the chain
 * 
 * Hooks are executed in the order they are added.
 * 
 * @param rule The rule
 * @param hook The hook function to add
 * @param user_data User data to pass to the hook
 * @return true on success, false on failure
 */
bool sfpm_rule_add_after_hook(sfpm_rule_t *rule,
                               sfpm_hook_fn hook,
                               void *user_data);

/**
 * @brief Add a middleware hook to the chain
 * 
 * Middleware hooks execute between before hooks and after hooks.
 * They wrap the payload execution and can abort by returning false.
 * Execution order: before hooks -> middleware hooks -> payload -> after hooks
 * 
 * @param rule The rule
 * @param hook The hook function to add
 * @param user_data User data to pass to the hook
 * @return true on success, false on failure
 */
bool sfpm_rule_add_middleware_hook(sfpm_rule_t *rule,
                                    sfpm_hook_fn hook,
                                    void *user_data);

/**
 * @brief Clear all hooks from a rule
 * 
 * Removes all before, after, and middleware hooks.
 * 
 * @param rule The rule
 */
void sfpm_rule_clear_hooks(sfpm_rule_t *rule);

/**
 * @brief Get the number of before hooks
 * 
 * @param rule The rule
 * @return Number of before hooks in the chain
 */
int sfpm_rule_get_before_hook_count(const sfpm_rule_t *rule);

/**
 * @brief Get the number of after hooks
 * 
 * @param rule The rule
 * @return Number of after hooks in the chain
 */
int sfpm_rule_get_after_hook_count(const sfpm_rule_t *rule);

/**
 * @brief Get the number of middleware hooks
 * 
 * @param rule The rule
 * @return Number of middleware hooks in the chain
 */
int sfpm_rule_get_middleware_hook_count(const sfpm_rule_t *rule);

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
