#include "sfpm/fact_source.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

/* --- Value constructors --- */

sfpm_value_t sfpm_value_from_int(int value) {
    sfpm_value_t result;
    result.type = SFPM_TYPE_INT;
    result.data.int_value = value;
    return result;
}

sfpm_value_t sfpm_value_from_float(float value) {
    sfpm_value_t result;
    result.type = SFPM_TYPE_FLOAT;
    result.data.float_value = value;
    return result;
}

sfpm_value_t sfpm_value_from_double(double value) {
    sfpm_value_t result;
    result.type = SFPM_TYPE_DOUBLE;
    result.data.double_value = value;
    return result;
}

sfpm_value_t sfpm_value_from_string(const char *value) {
    sfpm_value_t result;
    result.type = SFPM_TYPE_STRING;
    result.data.string_value = value;
    return result;
}

sfpm_value_t sfpm_value_from_bool(bool value) {
    sfpm_value_t result;
    result.type = SFPM_TYPE_BOOL;
    result.data.bool_value = value;
    return result;
}

/* --- Fact source interface --- */

bool sfpm_fact_source_try_get(const sfpm_fact_source_t *source,
                               const char *fact_name,
                               sfpm_value_t *out_value) {
    if (!source || !fact_name || !out_value) {
        return false;
    }
    if (!source->try_get_fact) {
        return false;
    }
    return source->try_get_fact(source, fact_name, out_value);
}

void sfpm_fact_source_destroy(sfpm_fact_source_t *source) {
    if (!source) {
        return;
    }
    if (source->destroy) {
        source->destroy(source);
    }
}

/* --- Dictionary fact source implementation --- */

typedef struct {
    sfpm_dict_entry_t *entries;
    size_t size;
    size_t capacity;
} dict_data_t;

static bool dict_try_get_fact(const sfpm_fact_source_t *source,
                               const char *fact_name,
                               sfpm_value_t *out_value) {
    if (!source || !source->user_data || !fact_name || !out_value) {
        return false;
    }

    dict_data_t *data = (dict_data_t *)source->user_data;
    
    for (size_t i = 0; i < data->size; i++) {
        if (strcmp(data->entries[i].key, fact_name) == 0) {
            *out_value = data->entries[i].value;
            return true;
        }
    }
    
    return false;
}

static void dict_destroy(sfpm_fact_source_t *source) {
    if (!source || !source->user_data) {
        return;
    }

    dict_data_t *data = (dict_data_t *)source->user_data;
    
    for (size_t i = 0; i < data->size; i++) {
        free(data->entries[i].key);
    }
    
    free(data->entries);
    free(data);
    free(source);
}

sfpm_fact_source_t *sfpm_dict_fact_source_create(size_t capacity) {
    if (capacity == 0) {
        capacity = 16; /* Default capacity */
    }

    sfpm_fact_source_t *source = (sfpm_fact_source_t *)malloc(sizeof(sfpm_fact_source_t));
    if (!source) {
        return NULL;
    }

    dict_data_t *data = (dict_data_t *)malloc(sizeof(dict_data_t));
    if (!data) {
        free(source);
        return NULL;
    }

    data->entries = (sfpm_dict_entry_t *)malloc(sizeof(sfpm_dict_entry_t) * capacity);
    if (!data->entries) {
        free(data);
        free(source);
        return NULL;
    }

    data->size = 0;
    data->capacity = capacity;

    source->user_data = data;
    source->try_get_fact = dict_try_get_fact;
    source->destroy = dict_destroy;

    return source;
}

bool sfpm_dict_fact_source_add(sfpm_fact_source_t *source,
                                const char *key,
                                sfpm_value_t value) {
    if (!source || !source->user_data || !key) {
        return false;
    }

    dict_data_t *data = (dict_data_t *)source->user_data;

    /* Check if we need to resize */
    if (data->size >= data->capacity) {
        size_t new_capacity = data->capacity * 2;
        sfpm_dict_entry_t *new_entries = (sfpm_dict_entry_t *)realloc(
            data->entries,
            sizeof(sfpm_dict_entry_t) * new_capacity
        );
        if (!new_entries) {
            return false;
        }
        data->entries = new_entries;
        data->capacity = new_capacity;
    }

    /* Check if key already exists (update) */
    for (size_t i = 0; i < data->size; i++) {
        if (strcmp(data->entries[i].key, key) == 0) {
            data->entries[i].value = value;
            return true;
        }
    }

    /* Add new entry */
    char *key_copy = (char *)malloc(strlen(key) + 1);
    if (!key_copy) {
        return false;
    }
    strcpy(key_copy, key);

    data->entries[data->size].key = key_copy;
    data->entries[data->size].value = value;
    data->size++;

    return true;
}
