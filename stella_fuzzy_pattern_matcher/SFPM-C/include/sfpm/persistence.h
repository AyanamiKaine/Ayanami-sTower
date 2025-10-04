/**
 * @file persistence.h
 * @brief Rule persistence and hot-reload support
 * 
 * Allows saving and loading rule sets to/from files for:
 * - Hot code reloading
 * - Runtime modifications that persist across restarts
 * - Rule versioning and rollback
 */

#ifndef SFPM_PERSISTENCE_H
#define SFPM_PERSISTENCE_H

#include "rule.h"
#include "matcher.h"
#include <stdbool.h>
#include <stdio.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Save a matcher's rules to a file
 * 
 * Serializes all rules in the matcher to a text-based format that can
 * be loaded later. This allows runtime modifications to persist.
 * 
 * @param matcher The matcher to save
 * @param filename Path to the output file
 * @return true on success, false on failure
 */
bool sfpm_matcher_save_to_file(const sfpm_matcher_t *matcher, const char *filename);

/**
 * @brief Load rules from a file into a matcher
 * 
 * Loads previously saved rules. This replaces the current rule set.
 * 
 * @param matcher The matcher to load rules into
 * @param filename Path to the input file
 * @return true on success, false on failure
 */
bool sfpm_matcher_load_from_file(sfpm_matcher_t *matcher, const char *filename);

/**
 * @brief Append rules from a file to a matcher
 * 
 * Loads rules and adds them to existing rules (doesn't replace).
 * 
 * @param matcher The matcher to append rules to
 * @param filename Path to the input file
 * @return true on success, false on failure
 */
bool sfpm_matcher_append_from_file(sfpm_matcher_t *matcher, const char *filename);

/**
 * @brief Save a single rule to a file stream
 * 
 * Low-level function for custom serialization.
 * 
 * @param rule The rule to save
 * @param file File stream to write to
 * @return true on success, false on failure
 */
bool sfpm_rule_serialize(const sfpm_rule_t *rule, FILE *file);

/**
 * @brief Load a single rule from a file stream
 * 
 * Low-level function for custom deserialization.
 * Note: This is a simplified version - full deserialization requires
 * registering payload functions and criteria builders.
 * 
 * @param file File stream to read from
 * @return Pointer to loaded rule, or NULL on failure
 */
sfpm_rule_t *sfpm_rule_deserialize(FILE *file);

#ifdef __cplusplus
}
#endif

#endif /* SFPM_PERSISTENCE_H */
