cmake_minimum_required(VERSION 3.16)
project(sfpm-cpp VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

option(SFPM_BUILD_TESTS "Build SFPM tests" ON)

# Header-only library (all implementation is in headers for now)
add_library(sfpm INTERFACE)
target_include_directories(sfpm INTERFACE ${CMAKE_CURRENT_SOURCE_DIR}/include)

if (MSVC)
    target_compile_options(sfpm INTERFACE /W4 /permissive-)
else()
    target_compile_options(sfpm INTERFACE -Wall -Wextra -Wpedantic -Werror=return-type)
endif()

add_executable(sfpm_example examples/basic_example.cpp)
target_link_libraries(sfpm_example PRIVATE sfpm)

if(SFPM_BUILD_TESTS)
    enable_testing()
    add_executable(sfpm_tests tests/test_basic.cpp)
    target_link_libraries(sfpm_tests PRIVATE sfpm)
    add_test(NAME sfpm_basic COMMAND sfpm_tests)
endif()

message(STATUS "SFPM-CPP configured. Build tests: ${SFPM_BUILD_TESTS}")