/*
 * math_ops.c - Simple math operations library for hot-reload demo
 * 
 * This library can be recompiled while the VM is running to demonstrate
 * hot-reloading of native functions.
 */

#ifdef _WIN32
    #define EXPORT __declspec(dllexport)
#else
    #define EXPORT __attribute__((visibility("default")))
#endif

#include <stdio.h>

/*
 * Add two integers
 * Initial implementation: simple addition
 * Try changing this to: return a * b; (multiplication)
 * or: return a - b; (subtraction)
 * then recompile and reload!
 */
EXPORT int math_add(int a, int b) {
    printf("[NATIVE] math_add(%d, %d) called\n", a, b);
    return a + b;  /* Change this line and recompile! */
}

/*
 * Multiply two integers
 */
EXPORT int math_mul(int a, int b) {
    printf("[NATIVE] math_mul(%d, %d) called\n", a, b);
    return a * b;
}

/*
 * Get library version (useful for verifying reload)
 */
EXPORT int get_version(void) {
    return 1;  /* Increment this after each recompile */
}
