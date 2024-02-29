cmake_minimum_required(VERSION 3.19)
project(lambda LANGUAGES CXX)

#Automatically collect all source files
file(GLOB_RECURSE lambda_SOURCES *.cc) 
add_executable(lambda ${map_transform_SOURCES})

# Adding all headers
target_include_directories(lambda PRIVATE ".")  # Add the current directory

target_compile_features(lambda PRIVATE cxx_std_20)
