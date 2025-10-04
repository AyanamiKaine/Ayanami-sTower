/**
 * @file fact_source.h
 * @brief Fact source interface and implementations
 * 
 * Provides an abstraction for querying facts with type safety.
 */

#ifndef SFPM_FACT_SOURCE_H
#define SFPM_FACT_SOURCE_H

#include <stdbool.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * @brief Enumeration of supported fact value types
 */
typedef enum {
    SFPM_TYPE_INT,
    SFPM_TYPE_FLOAT,
    SFPM_TYPE_DOUBLE,
    SFPM_TYPE_STRING,
    SFPM_TYPE_BOOL,
    SFPM_TYPE_UNKNOWN
} sfpm_type_t;

/**
 * @brief Tagged union for storing fact values
 */
typedef struct {
    sfpm_type_t type;
    union {
        int int_value;
        float float_value;
        double double_value;
        const char *string_value;
        bool bool_value;
    } data;
} sfpm_value_t;

/**
 * @brief Opaque handle to a fact source
 */
typedef struct sfpm_fact_source sfpm_fact_source_t;

/**
 * @brief Function pointer type for querying facts
 * 
 * @param source The fact source instance
 * @param fact_name The name of the fact to retrieve
 * @param out_value Pointer to store the retrieved value
 * @return true if fact was found and retrieved, false otherwise
 */
typedef bool (*sfpm_try_get_fact_fn)(const sfpm_fact_source_t *source,
                                      const char *fact_name,
                                      sfpm_value_t *out_value);

/**
 * @brief Function pointer type for destroying a fact source
 * 
 * @param source The fact source instance to destroy
 */
typedef void (*sfpm_destroy_fact_source_fn)(sfpm_fact_source_t *source);

/**
 * @brief Fact source interface structure
 */
struct sfpm_fact_source {
    void *user_data;                          /**< User-defined data */
    sfpm_try_get_fact_fn try_get_fact;        /**< Function to retrieve facts */
    sfpm_destroy_fact_source_fn destroy;      /**< Function to clean up */
};

/**
 * @brief Create a value from an integer
 */
sfpm_value_t sfpm_value_from_int(int value);

/**
 * @brief Create a value from a float
 */
sfpm_value_t sfpm_value_from_float(float value);

/**
 * @brief Create a value from a double
 */
sfpm_value_t sfpm_value_from_double(double value);

/**
 * @brief Create a value from a string (does not take ownership)
 */
sfpm_value_t sfpm_value_from_string(const char *value);

/**
 * @brief Create a value from a boolean
 */
sfpm_value_t sfpm_value_from_bool(bool value);

/**
 * @brief Try to get a fact from a fact source
 * 
 * @param source The fact source
 * @param fact_name The name of the fact
 * @param out_value Pointer to store the value
 * @return true if successful, false otherwise
 */
bool sfpm_fact_source_try_get(const sfpm_fact_source_t *source,
                               const char *fact_name,
                               sfpm_value_t *out_value);

/**
 * @brief Destroy a fact source
 * 
 * @param source The fact source to destroy
 */
void sfpm_fact_source_destroy(sfpm_fact_source_t *source);

/* --- Dictionary-based fact source implementation --- */

/**
 * @brief Entry in a dictionary fact source
 */
typedef struct {
    char *key;
    sfpm_value_t value;
} sfpm_dict_entry_t;

/**
 * @brief Create a dictionary-based fact source
 * 
 * @param capacity Initial capacity of the dictionary
 * @return Pointer to the created fact source, or NULL on failure
 */
sfpm_fact_source_t *sfpm_dict_fact_source_create(size_t capacity);

/**
 * @brief Add a fact to a dictionary fact source
 * 
 * @param source The dictionary fact source
 * @param key The fact name
 * @param value The fact value
 * @return true on success, false on failure
 */
bool sfpm_dict_fact_source_add(sfpm_fact_source_t *source,
                                const char *key,
                                sfpm_value_t value);

#ifdef __cplusplus
}
#endif

#endif /* SFPM_FACT_SOURCE_H */
