cmake_minimum_required(VERSION 3.19)
set(PROJECT_NAME comp4300)

project(${PROJECT_NAME} LANGUAGES CXX)



find_package(SFML REQUIRED COMPONENTS system window graphics network audio)    

# So clangd can work correctly
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

# Automatically collect all source files
file(GLOB_RECURSE comp4300_SOURCES *.cc) 
add_executable(${PROJECT_NAME}  ${comp4300_SOURCES})
# Adding all headers
target_include_directories(${PROJECT_NAME}  PRIVATE ".")  # Add the current directory

target_link_libraries(${PROJECT_NAME}  PRIVATE sfml-system sfml-window sfml-graphics sfml-network sfml-audio)
target_compile_features(${PROJECT_NAME}  PRIVATE cxx_std_20)
