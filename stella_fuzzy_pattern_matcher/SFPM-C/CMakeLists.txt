cmake_minimum_required(VERSION 3.16)
project(sfpm-c VERSION 0.1.0 LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

option(SFPM_BUILD_TESTS "Build SFPM tests" ON)
option(SFPM_BUILD_EXAMPLES "Build SFPM examples" ON)

# Library sources
set(SFPM_SOURCES
    src/fact_source.c
    src/criteria.c
    src/rule.c
    src/matcher.c
)

set(SFPM_HEADERS
    include/sfpm/fact_source.h
    include/sfpm/criteria.h
    include/sfpm/rule.h
    include/sfpm/matcher.h
    include/sfpm/sfpm.h
)

add_library(sfpm ${SFPM_SOURCES} ${SFPM_HEADERS})
target_include_directories(sfpm PUBLIC 
    $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
    $<INSTALL_INTERFACE:include>
)

# Compiler warnings and definitions
if(MSVC)
    target_compile_options(sfpm PRIVATE /W4 /WX)
    target_compile_definitions(sfpm PRIVATE _CRT_SECURE_NO_WARNINGS)
else()
    target_compile_options(sfpm PRIVATE -Wall -Wextra -Wpedantic -Werror)
endif()

# Examples
if(SFPM_BUILD_EXAMPLES)
    add_executable(sfpm_example examples/basic_example.c)
    target_link_libraries(sfpm_example PRIVATE sfpm)
    
    add_executable(sfpm_comparison examples/interpreter_comparison.c)
    target_link_libraries(sfpm_comparison PRIVATE sfpm)
    
    add_executable(sfpm_cached examples/interpreter_cached.c)
    target_link_libraries(sfpm_cached PRIVATE sfpm)
    
    add_executable(sfpm_tiered examples/interpreter_tiered.c)
    target_link_libraries(sfpm_tiered PRIVATE sfpm)
    
    add_executable(sfpm_game_ai examples/interpreter_game_ai.c)
    target_link_libraries(sfpm_game_ai PRIVATE sfpm)
    
    add_executable(sfpm_hooks examples/interpreter_hooks.c)
    target_link_libraries(sfpm_hooks PRIVATE sfpm)
    
    add_executable(sfpm_hook_chaining examples/interpreter_hook_chaining.c)
    target_link_libraries(sfpm_hook_chaining PRIVATE sfpm)
endif()

# Tests
if(SFPM_BUILD_TESTS)
    enable_testing()
    add_executable(sfpm_test_basic tests/test_basic.c)
    target_link_libraries(sfpm_test_basic PRIVATE sfpm)
    add_test(NAME sfpm_basic COMMAND sfpm_test_basic)
    
    add_executable(sfpm_test_advanced tests/test_advanced.c)
    target_link_libraries(sfpm_test_advanced PRIVATE sfpm)
    add_test(NAME sfpm_advanced COMMAND sfpm_test_advanced)
    
    add_executable(sfpm_test_hook_chaining tests/test_hook_chaining.c)
    target_link_libraries(sfpm_test_hook_chaining PRIVATE sfpm)
    add_test(NAME sfpm_hook_chaining COMMAND sfpm_test_hook_chaining)
endif()

# Installation
install(TARGETS sfpm
    EXPORT sfpm-targets
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
)

install(DIRECTORY include/sfpm DESTINATION include)

message(STATUS "SFPM-C configured. Build tests: ${SFPM_BUILD_TESTS}, Build examples: ${SFPM_BUILD_EXAMPLES}")
