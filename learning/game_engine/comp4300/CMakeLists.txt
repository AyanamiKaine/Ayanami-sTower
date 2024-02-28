cmake_minimum_required(VERSION 3.19)
project(comp4300 LANGUAGES CXX)

find_package(SFML REQUIRED COMPONENTS system window graphics network audio)    

# Automatically collect all source files
file(GLOB_RECURSE comp4300_SOURCES *.cc) 
add_executable(comp4300 ${comp4300_SOURCES})
# Adding all headers
target_include_directories(comp4300 PRIVATE ".")  # Add the current directory

target_link_libraries(comp4300 PRIVATE sfml-system sfml-window sfml-graphics sfml-network sfml-audio)
target_compile_features(comp4300 PRIVATE cxx_std_20)
