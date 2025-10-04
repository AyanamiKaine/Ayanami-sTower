#include "sfpm/criteria.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

struct sfpm_criteria {
    char *fact_name;
    sfpm_operator_t operator;
    sfpm_value_t expected_value;
    sfpm_predicate_fn predicate;
    void *predicate_user_data;
    char *predicate_name;
};

/* --- Helper functions for value comparison --- */

static int compare_int(int a, int b) {
    if (a < b) return -1;
    if (a > b) return 1;
    return 0;
}

static int compare_float(float a, float b) {
    if (a < b) return -1;
    if (a > b) return 1;
    return 0;
}

static int compare_double(double a, double b) {
    if (a < b) return -1;
    if (a > b) return 1;
    return 0;
}

static int compare_string(const char *a, const char *b) {
    return strcmp(a, b);
}

static int compare_bool(bool a, bool b) {
    if (a == b) return 0;
    if (a) return 1;
    return -1;
}

static bool evaluate_comparison(const sfpm_value_t *actual,
                                 const sfpm_value_t *expected,
                                 sfpm_operator_t op) {
    if (actual->type != expected->type) {
        return false; /* Type mismatch */
    }

    int cmp;
    switch (actual->type) {
        case SFPM_TYPE_INT:
            cmp = compare_int(actual->data.int_value, expected->data.int_value);
            break;
        case SFPM_TYPE_FLOAT:
            cmp = compare_float(actual->data.float_value, expected->data.float_value);
            break;
        case SFPM_TYPE_DOUBLE:
            cmp = compare_double(actual->data.double_value, expected->data.double_value);
            break;
        case SFPM_TYPE_STRING:
            cmp = compare_string(actual->data.string_value, expected->data.string_value);
            break;
        case SFPM_TYPE_BOOL:
            cmp = compare_bool(actual->data.bool_value, expected->data.bool_value);
            break;
        default:
            return false;
    }

    switch (op) {
        case SFPM_OP_EQUAL:
            return cmp == 0;
        case SFPM_OP_NOT_EQUAL:
            return cmp != 0;
        case SFPM_OP_GREATER_THAN:
            return cmp > 0;
        case SFPM_OP_LESS_THAN:
            return cmp < 0;
        case SFPM_OP_GREATER_THAN_OR_EQUAL:
            return cmp >= 0;
        case SFPM_OP_LESS_THAN_OR_EQUAL:
            return cmp <= 0;
        default:
            return false;
    }
}

/* --- Public API --- */

sfpm_criteria_t *sfpm_criteria_create(const char *fact_name,
                                       sfpm_operator_t op,
                                       sfpm_value_t expected_value) {
    if (!fact_name) {
        return NULL;
    }

    sfpm_criteria_t *criteria = (sfpm_criteria_t *)malloc(sizeof(sfpm_criteria_t));
    if (!criteria) {
        return NULL;
    }

    criteria->fact_name = (char *)malloc(strlen(fact_name) + 1);
    if (!criteria->fact_name) {
        free(criteria);
        return NULL;
    }
    strcpy(criteria->fact_name, fact_name);

    criteria->operator = op;
    criteria->expected_value = expected_value;
    criteria->predicate = NULL;
    criteria->predicate_user_data = NULL;
    criteria->predicate_name = NULL;

    return criteria;
}

sfpm_criteria_t *sfpm_criteria_create_predicate(const char *fact_name,
                                                 sfpm_predicate_fn predicate,
                                                 void *user_data,
                                                 const char *predicate_name) {
    if (!fact_name || !predicate) {
        return NULL;
    }

    sfpm_criteria_t *criteria = (sfpm_criteria_t *)malloc(sizeof(sfpm_criteria_t));
    if (!criteria) {
        return NULL;
    }

    criteria->fact_name = (char *)malloc(strlen(fact_name) + 1);
    if (!criteria->fact_name) {
        free(criteria);
        return NULL;
    }
    strcpy(criteria->fact_name, fact_name);

    criteria->operator = SFPM_OP_PREDICATE;
    criteria->predicate = predicate;
    criteria->predicate_user_data = user_data;

    if (predicate_name) {
        criteria->predicate_name = (char *)malloc(strlen(predicate_name) + 1);
        if (criteria->predicate_name) {
            strcpy(criteria->predicate_name, predicate_name);
        }
    } else {
        criteria->predicate_name = NULL;
    }

    return criteria;
}

void sfpm_criteria_destroy(sfpm_criteria_t *criteria) {
    if (!criteria) {
        return;
    }

    free(criteria->fact_name);
    free(criteria->predicate_name);
    free(criteria);
}

bool sfpm_criteria_evaluate(const sfpm_criteria_t *criteria,
                             const sfpm_fact_source_t *facts) {
    if (!criteria || !facts) {
        return false;
    }

    sfpm_value_t actual_value;
    if (!sfpm_fact_source_try_get(facts, criteria->fact_name, &actual_value)) {
        return false; /* Fact not found */
    }

    if (criteria->operator == SFPM_OP_PREDICATE) {
        if (!criteria->predicate) {
            return false;
        }
        return criteria->predicate(&actual_value, criteria->predicate_user_data);
    }

    return evaluate_comparison(&actual_value, &criteria->expected_value, criteria->operator);
}

const char *sfpm_criteria_get_fact_name(const sfpm_criteria_t *criteria) {
    return criteria ? criteria->fact_name : NULL;
}

sfpm_operator_t sfpm_criteria_get_operator(const sfpm_criteria_t *criteria) {
    return criteria ? criteria->operator : SFPM_OP_EQUAL;
}
