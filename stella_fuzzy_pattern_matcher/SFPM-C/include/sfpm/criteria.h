/**
 * @file criteria.h
 * @brief Criteria for pattern matching
 * 
 * Defines comparison operators and predicates for matching facts.
 */

#ifndef SFPM_CRITERIA_H
#define SFPM_CRITERIA_H

#include "fact_source.h"
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Comparison operators for criteria
 */
typedef enum {
    SFPM_OP_EQUAL,
    SFPM_OP_NOT_EQUAL,
    SFPM_OP_GREATER_THAN,
    SFPM_OP_LESS_THAN,
    SFPM_OP_GREATER_THAN_OR_EQUAL,
    SFPM_OP_LESS_THAN_OR_EQUAL,
    SFPM_OP_PREDICATE
} sfpm_operator_t;

/**
 * @brief Opaque handle to a criteria
 */
typedef struct sfpm_criteria sfpm_criteria_t;

/**
 * @brief Predicate function type for custom criteria evaluation
 * 
 * @param value The fact value to evaluate
 * @param user_data User-defined context data
 * @return true if the predicate is satisfied, false otherwise
 */
typedef bool (*sfpm_predicate_fn)(const sfpm_value_t *value, void *user_data);

/**
 * @brief Destroy a criteria
 * 
 * @param criteria The criteria to destroy
 */
void sfpm_criteria_destroy(sfpm_criteria_t *criteria);

/**
 * @brief Create a criteria with a comparison operator
 * 
 * @param fact_name The name of the fact to match
 * @param op The comparison operator
 * @param expected_value The expected value
 * @return Pointer to the created criteria, or NULL on failure
 */
sfpm_criteria_t *sfpm_criteria_create(const char *fact_name,
                                       sfpm_operator_t op,
                                       sfpm_value_t expected_value);

/**
 * @brief Create a criteria with a custom predicate
 * 
 * @param fact_name The name of the fact to match
 * @param predicate The predicate function
 * @param user_data User data to pass to the predicate
 * @param predicate_name Optional name for debugging (can be NULL)
 * @return Pointer to the created criteria, or NULL on failure
 */
sfpm_criteria_t *sfpm_criteria_create_predicate(const char *fact_name,
                                                 sfpm_predicate_fn predicate,
                                                 void *user_data,
                                                 const char *predicate_name);

/**
 * @brief Evaluate a criteria against a fact source
 * 
 * @param criteria The criteria to evaluate
 * @param facts The fact source
 * @return true if the criteria matches, false otherwise
 */
bool sfpm_criteria_evaluate(const sfpm_criteria_t *criteria,
                             const sfpm_fact_source_t *facts);

/**
 * @brief Get the fact name for a criteria
 * 
 * @param criteria The criteria
 * @return The fact name, or NULL if criteria is NULL
 */
const char *sfpm_criteria_get_fact_name(const sfpm_criteria_t *criteria);

/**
 * @brief Get the operator for a criteria
 * 
 * @param criteria The criteria
 * @return The operator
 */
sfpm_operator_t sfpm_criteria_get_operator(const sfpm_criteria_t *criteria);

#ifdef __cplusplus
}
#endif

#endif /* SFPM_CRITERIA_H */
