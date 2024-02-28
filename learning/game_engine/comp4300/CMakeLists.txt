cmake_minimum_required(VERSION 3.19)
project(comp4300 LANGUAGES CXX)

find_package(sfml REQUIRED)    
add_executable(sfml main.cc)

target_link_libraries(comp4300 PRIVATE sfml-graphics)
target_compile_features(comp4300 PRIVATE cxx_std_20)
