cmake_minimum_required(VERSION 3.19)
project(map_transform LANGUAGES CXX)

#Automatically collect all source files
file(GLOB_RECURSE map_transform_SOURCES *.cc) 
add_executable(map_transform ${map_transform_SOURCES})

# Adding all headers
target_include_directories(map_transform PRIVATE ".")  # Add the current directory

target_compile_features(map_transform PRIVATE cxx_std_20)
