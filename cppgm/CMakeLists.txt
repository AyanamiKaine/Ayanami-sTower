cmake_minimum_required(VERSION 3.10)  # Adjust minimum CMake version as needed

# Project name
project(cppgm_vector2_lib)

# Specify source and header files 
add_library(cppgm_vector2_lib SHARED
            cppgm_vector2Util.cc
            cppgm_vector2Util.h
            cppgm_vector2.cc
            cppgm_vector2.h
)

if(CMAKE_BUILD_TYPE MATCHES Release)
    add_compile_options(-O3)  # Add Release-specific optimizations
elseif(CMAKE_BUILD_TYPE MATCHES Debug)
    add_compile_options(-g)   # Add Debug-specific flags
else()
    message(WARNING "Unknown build type, defaulting to debug settings")
    add_compile_options(-g) 
endif()

# Modern CMake: Specify compile features and C++ standard           
target_compile_features(cppgm_vector2_lib PUBLIC cxx_std_17) # Adjust C++ standard as needed
