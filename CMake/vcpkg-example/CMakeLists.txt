cmake_minimum_required(VERSION 3.19)
project(vcpkg-example CXX)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

find_package(fmt REQUIRED)    
add_executable(vcpkg-example main.cpp)

target_link_libraries(vcpkg-example PRIVATE fmt::fmt)
