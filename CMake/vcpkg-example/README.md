## How to Use Vcpkg with CMake (and neovim/clangd)
- 1. Install Vcpkg
- 2. Create a `vcpkg.json` file where you define your dependencies
- 3. In your `CMakeLists.txt` write the needed `find_package` and linking code
- 4. When generating the build files with CMake you must point to the Toolchain file of vcpkg. In my case the path is `~/vcpkg/scripts/buildsystems/vcpkg.cmake`.
    - The correct command would look like so (assuming you are in the same folder as the projects root) `cmake . -DCMAKE_TOOLCHAIN_FILE="~/vcpkg/scripts/buildsy
stems/vcpkg.cmake"`

## How to give the language server the right header files for autocomplete
- If you use clangds lsp you must generate a file with cmake `cmake <src_directory> - DCMAKE_EXPORT_COMPILE_COMMANDS=1` as its looks for a `compile_commands.json` file in the project directory

## Using CMakePresets
- https://learn.microsoft.com/en-us/vcpkg/users/buildsystems/cmake-integration
- The command in this case would be `cmake -B build
 -S .. --preset debug`

### Command Explained
- `cmake`: The main CMake executable.
- `B build`:
    - Instructs CMake to create a build directory named "build". If the directory doesn't exist, CMake will create it for you.
- `S ..`:
    - Specifies the source directory. The .. indicates that the CMakeLists.txt file is located in the parent directory of your current location.
- `--preset debug`:
    - Loads a CMake preset named "debug". CMake presets are a convenient way to store pre-defined build configurations (more on this below).
