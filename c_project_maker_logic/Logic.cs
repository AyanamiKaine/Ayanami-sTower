using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using LibGit2Sharp;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Net;
using ICSharpCode.SharpZipLib.Tar;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace CProjectMakerLogic
{
    static public class Logic
    {
        static public void CreateCProject(Request request)
        {
            Directory.CreateDirectory($"{request.ProjectPath}/{request.ProjectName}");
            Directory.CreateDirectory($"{request.ProjectPath}/{request.ProjectName}/src");
            Directory.CreateDirectory($"{request.ProjectPath}/{request.ProjectName}/libs");
            Directory.CreateDirectory($"{request.ProjectPath}/{request.ProjectName}/include");

            InstallPackagesWithVCPKG(request);

            if(request.AddLuaJIT)
            {
                string folderPath = $"{request.ProjectPath}/{request.ProjectName}/src/lua";
                Directory.CreateDirectory(folderPath);
                
                File.Create(folderPath + "/main.lua").Close();


                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    File.WriteAllText(folderPath + "/main.lua",
                        """
                        -- Include the fennel compiler and run the fennel main file
                        -- If you dont want to use fennel simply remove the line
                        require("lua\\fennel").install().dofile("fennel\\main.fnl")
                        
                        print("Hello world from main.lua!");
                        """);
                }

                // This is needed because of the different filepath delimiters on unix('/') and windows('//')('\')
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    File.WriteAllText(folderPath + "/main.lua",
                        """
                        -- Include the fennel compiler and run the fennel main file
                        -- If you dont want to use fennel simply remove the line
                        require("lua/fennel").install().dofile("fennel/main.fnl")
                        
                        print("Hello world from main.lua!");
                        """);
                }

                File.Create(folderPath + "/mymodule.lua").Close();
                string fileContent = """
                    -- mymodule.lua

                    local mymodule = {}

                    function mymodule.sayhi()
                      print('Hello from mymodule!')
                    end

                    return mymodule
                    """;
                File.WriteAllText(folderPath + "/mymodule.lua", fileContent);

                IncludeFennel(request, folderPath);
            }

            CloningThirdPartyGitProjects(request);
            CreateCMakeListsFile(request.ProjectName, request.ProjectPath, request.CVersion, request.AutoAddFiles, request.AddVCPKG, request.AddCzmq, request.AddCJson, request.AddSokol, request.AddNuklear, request.AddFlecs, request.AddLuaJIT);
            CreateMain(request.MainTemplate, request.ProjectName, request.ProjectPath);
        }

        static public void CloningThirdPartyGitProjects(Request request)
        {
            if (request.AddSokol)
            {
                string sokol_repository_url = "https://github.com/floooh/sokol.git";
                string sokol_local_path = $"{request.ProjectPath}/{request.ProjectName}/libs/sokol";

                Repository.Clone(sokol_repository_url, sokol_local_path);

                // Adding sokol header to the project
                string destIncludeDir = Path.Combine($"{request.ProjectPath}/{request.ProjectName}", "include"); // Project's include folder

                // 3. Create Destination Directory (if it doesn't exist)
                Directory.CreateDirectory(destIncludeDir);

                // 4. Copy Header Files (with error handling and filtering)
                try
                {
                    foreach (string file in Directory.GetFiles(sokol_local_path, "*.h"))
                    {
                        string destFilePath = Path.Combine(destIncludeDir, Path.GetFileName(file));
                        File.Copy(file, destFilePath, true); // Overwrite if exists
                    }
                    foreach (string file in Directory.GetFiles(sokol_local_path + "/util", "*.h"))
                    {
                        string destFilePath = Path.Combine(destIncludeDir, Path.GetFileName(file));
                        File.Copy(file, destFilePath, true); // Overwrite if exists
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine("Error copying header files: " + ex.Message);
                    return; // Exit if copying fails
                }

                Console.WriteLine("Sokol headers copied successfully!");
            }

            if (request.AddNuklear)
            {
                string nuklear_repository_url = "https://github.com/Immediate-Mode-UI/Nuklear.git";
                string nuklear_local_path = $"{request.ProjectPath}/{request.ProjectName}/libs/nuklear";

                Repository.Clone(nuklear_repository_url, nuklear_local_path);

                // Adding sokol header to the project
                string destIncludeDir = Path.Combine($"{request.ProjectPath}/{request.ProjectName}", "include"); // Project's include folder
                // 3. Create Destination Directory (if it doesn't exist)
                Directory.CreateDirectory(destIncludeDir);

                // 4. Copy Header Files (with error handling and filtering)
                try
                {
                    foreach (string file in Directory.GetFiles(nuklear_local_path, "*.h"))
                    {
                        string destFilePath = Path.Combine(destIncludeDir, Path.GetFileName(file));
                        File.Copy(file, destFilePath, true); // Overwrite if exists
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine("Error copying header files: " + ex.Message);
                    return; // Exit if copying fails
                }

                Console.WriteLine("nuklear headers copied successfully!");
            }
            if (request.AddArenaAllocator)
            {
                string arena_allocator_repository_url = "https://github.com/AyanamiKaine/arena_allocator.git";
                string arena_allocator_local_path = $"{request.ProjectPath}/{request.ProjectName}/libs/arena_allocator";

                Repository.Clone(arena_allocator_repository_url, arena_allocator_local_path);

                // Adding sokol header to the project
                string destIncludeDir = Path.Combine($"{request.ProjectPath}/{request.ProjectName}", "include"); // Project's include folder
                // 3. Create Destination Directory (if it doesn't exist)
                Directory.CreateDirectory(destIncludeDir);

                // 4. Copy Header Files (with error handling and filtering)
                try
                {
                    foreach (string file in Directory.GetFiles(arena_allocator_local_path + "/include", "*.h"))
                    {
                        string destFilePath = Path.Combine(destIncludeDir, Path.GetFileName(file));
                        File.Copy(file, destFilePath, true); // Overwrite if exists
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine("Error copying header files: " + ex.Message);
                    return; // Exit if copying fails
                }

                Console.WriteLine("arena_allocators headers copied successfully!");
            }
        }

    

        static public void CopyLuaJITHeadersIntoProject(string project_path, string project_name)
        {
            // 1. Clone Repository (with error handling)
            string repoPath = $"{project_path}/{project_name}/libs/luajit";  // Will store the actual cloned repo path
            try
            {
                Repository.Clone("https://github.com/LuaJIT/LuaJIT.git", $"{project_path}/{project_name}/libs/luajit");
            }
            catch (LibGit2SharpException ex)
            {
                Console.WriteLine("Error cloning repository: " + ex.Message);
                return; // Exit if cloning fails
            }

            // 2. Define Source and Destination Paths (with Path.Combine for robustness)
            string srcHeaderDir = Path.Combine(repoPath, "src");
            string destIncludeDir = Path.Combine($"{project_path}/{project_name}", "include"); // Project's include folder

            // 3. Create Destination Directory (if it doesn't exist)
            Directory.CreateDirectory(destIncludeDir);

            // 4. Copy Header Files (with error handling and filtering)
            try
            {
                foreach (string file in Directory.GetFiles(srcHeaderDir, "*.h"))
                {
                    string destFilePath = Path.Combine(destIncludeDir, Path.GetFileName(file));
                    File.Copy(file, destFilePath, true); // Overwrite if exists
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine("Error copying header files: " + ex.Message);
                return; // Exit if copying fails
            }

            Console.WriteLine("LuaJIT headers copied successfully!");

            if (!File.Exists($"{project_path}/{project_name}/libs/luajit/src/libluajit.so"))
            {
                Console.WriteLine("Building LuaJIT...");
                BuildLuaJit($"{project_path}/{project_name}/libs/luajit/src", $"{project_path}/{project_name}/build");
            }
        }

        static public void BuildLuaJit(string luaJITSourceFolder, string destinationFolder)
        {
            try
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = "make",        // The command you want to run (make)
                    WorkingDirectory = luaJITSourceFolder, // Optional: set the working directory (where the Makefile is)
                    RedirectStandardOutput = true, // Capture the output
                    UseShellExecute = false, // Necessary for redirection
                };

                Process process = new() { StartInfo = startInfo };
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(); // Wait for the process to finish

                Console.WriteLine(output); // Print the output

                if (!Directory.Exists(destinationFolder))
                {
                    Console.WriteLine("build folder does not exist, creating build folder...");
                    Directory.CreateDirectory(destinationFolder);
                }

                File.Move($"{luaJITSourceFolder}/libluajit.so", $"{destinationFolder}/libluajit.so");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error moving file: {e.Message}");
            }
        }

        static public void InstallPackagesWithVCPKG(Request request)
        {
            string variableName = "VCPKG_ROOT"; 
            string vcpkgRoot = Environment.GetEnvironmentVariable(variableName);

            if (vcpkgRoot != null)
            {
                Console.WriteLine($"The value of '{variableName}' is: {vcpkgRoot}");
                Console.WriteLine("Installing the packages with VCPKG");
                

                if (request.AddCzmq == true)
                {
                    Console.WriteLine("Trying to install czmq with VCPKG");
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = Path.Combine(vcpkgRoot, "vcpkg"), // Path to vcpkg executable
                        Arguments = "install czmq",                 // Arguments for vcpkg
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                    };

                    Process process = new() { StartInfo = startInfo };
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(); // Wait for the process to finish

                    Console.WriteLine(output); // Print the output
                }

                if (request.AddCJson == true)
                {
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = "vcpkg install cjson",        // The command you want to run (make)
                        RedirectStandardOutput = true, // Capture the output
                        UseShellExecute = false, // Necessary for redirection
                    };

                    Process process = new() { StartInfo = startInfo };
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(); // Wait for the process to finish

                    Console.WriteLine(output); // Print the output
                }

                if (request.AddFlecs == true)
                {
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = "vcpkg install flecs",        // The command you want to run (make)
                        RedirectStandardOutput = true, // Capture the output
                        UseShellExecute = false, // Necessary for redirection
                    };

                    Process process = new() { StartInfo = startInfo };
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(); // Wait for the process to finish

                    Console.WriteLine(output); // Print the output
                }
            }
            else
            {
                Console.WriteLine($"The environment variable '{variableName}' does not exist.");
                Console.WriteLine($"It must exist so VCPKG works correctly in CMAKE");
                Console.WriteLine("Or it means that you didnt install VCPKG");
            }
        }

        static public void CreateCMakeListsFile(string project_name,string project_path ,string c_version, bool auto_add_file, bool add_vcpkg, bool add_czmq, bool add_json_c, bool add_sokol, bool add_nuklear, bool add_flecs, bool add_luajit)
        {
            string cmake_file_content = "";
            string cmake_target_link_libraries = $"target_link_libraries({project_name} PRIVATE ";

            cmake_file_content += "cmake_minimum_required(VERSION  3.10)\r\n";
            if (add_vcpkg)
            {
                cmake_file_content += """

                    # vcpkg Integration (If using vcpkg)
                    if(DEFINED ENV{VCPKG_ROOT})
                      set(CMAKE_TOOLCHAIN_FILE $ENV{VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake)
                    endif()

                    """;
            }

            cmake_file_content += $"""

                project({project_name})

                """;

            cmake_file_content += $"""

                set(CMAKE_C_STANDARD {c_version})
                set(CMAKE_C_STANDARD_REQUIRED ON)
                
                """;

            if (auto_add_file)
            {
                cmake_file_content += $"""

                file(GLOB_RECURSE SOURCES "src/*.c" "src/*.h")

                """;
            }

            cmake_file_content += """

                # Adding Third-Party Packages that get fetched via VCPKG

                """;

            if (add_czmq)
            {
                cmake_file_content += """

                find_package(czmq CONFIG REQUIRED)

                """;

                cmake_target_link_libraries += "czmq czmq-static ";
            }

            /*
            if (add_arena_allocator)
            {
                //We have to copy the arena_allocator project as a sub directory
                //my_main_project/  
                //├── CMakeLists.txt
                //└── my_c_library/ 
                //    ├── include/
                //    │   └── my_c_library.h
                //    ├── src/
                //    │   └── my_c_library.c
                //    └── CMakeLists.txt  
                //# ... (Other CMake configurations)

                //# Add the subdirectory (your C library)
                //add_subdirectory(my_c_library)

                //# Link your main target to the library
                //add_executable(my_app main.cpp) # Or whatever your main target is
                //target_link_libraries(my_app my_c_library) 
             }
            */

            if (add_flecs)
            {
                cmake_file_content += """

                find_package(flecs CONFIG REQUIRED)
                """;

                //If vcpkg installed a shared Flecs library (flecs::flecs), it will link to that.
                //If vcpkg installed a static Flecs library (flecs::flecs_static), it will link to that instead.
                cmake_target_link_libraries += "flecs::flecs_static ";
            }

            if (add_json_c)
            {
                cmake_file_content += """

                find_package(json-c CONFIG REQUIRED)   
                
                """;

                cmake_target_link_libraries += "json-c::json-c ";
            }

            if (add_luajit)
            {
                CopyLuaJITHeadersIntoProject(project_path, project_name);
                cmake_file_content += "message(STATUS \"Finding LuaJIT (luajit or lua51)...\")\n\r";
                
                // On Unix and Windows the created LuaJit library will have a different name, 
                // On Windows = lua51.dll
                // On Linux   = libluajit
                // TODO: Implement a command to automatically build the library on Linux, on Windows we should distribute it with
                // the source
                cmake_file_content += """
                if(UNIX AND NOT APPLE)
                    # Explicitly specify the search path for libluajit.so
                    find_library(luajit 
                                NAMES luajit # Look for "libluajit.so" (no need for lua51)
                                PATHS ${CMAKE_CURRENT_BINARY_DIR} # Only search in this directory
                                NO_DEFAULT_PATH)      # Don't look in standard system locations

                    if(luajit)
                        message(STATUS "Found LuaJIT (Linux): ${luajit}")
                    else()
                        message(FATAL_ERROR "LuaJIT library not found in /libs/luajit/src")
                    endif()
                else()
                    message(STATUS "Not on Linux, try windows LuaJIT search")
                    
                    find_library(luajit NAMES lua51)
                    if(luajit)
                        message(STATUS "Found LuaJIT: ${luajit}")
                    else()
                        message(FATAL_ERROR "LuaJIT library not found")
                    endif()
                    
                endif()
                
                """;
                    
                cmake_target_link_libraries += "${luajit} ";
                cmake_file_content += "if(luajit)\r\n    message(STATUS \"Found LuaJIT: ${luajit}\")\r\nelse()\r\n    message(FATAL_ERROR \"LuaJIT library not found\")\r\nendif()\r\n";
                cmake_file_content += """
                    # Here we copy our lua folder (./src/lua) to a build folder so the program can find the lua scripts
                    # Here we copy our fennel folder (./src/fennel) to a build folder so the program can find the fennel scripts
                   
                    set(LUA_SOURCE_FOLDER "${CMAKE_CURRENT_SOURCE_DIR}/src/lua")
                    set(FENNEL_SOURCE_FOLDER "${CMAKE_CURRENT_SOURCE_DIR}/src/fennel") 
                    set(DESTINATION_FOLDER "${CMAKE_CURRENT_BINARY_DIR}")

                    file(COPY ${LUA_SOURCE_FOLDER} DESTINATION ${DESTINATION_FOLDER})
                    file(COPY ${FENNEL_SOURCE_FOLDER} DESTINATION ${DESTINATION_FOLDER})
                    """;
            }

            if (add_sokol)
            {
                cmake_file_content += "add_library(sokol INTERFACE)\r\n";
                cmake_target_link_libraries += "sokol ";
            }

            if (add_nuklear)
            {
                cmake_file_content += "add_library(nuklear INTERFACE)\r\n";
                cmake_target_link_libraries += "nuklear ";
            }

            cmake_file_content += "\r\n";

            cmake_file_content += $"add_executable({project_name}" + " ${SOURCES})\r\n";

            if (add_luajit)
            {
                cmake_file_content +=
                    $"""
                add_custom_command(
                    TARGET {project_name}
                    POST_BUILD
                """;
                cmake_file_content +=
                    """
                    COMMAND ${CMAKE_COMMAND} -E copy_directory  
                            ${LUA_SOURCE_FOLDER} ${DESTINATION_FOLDER}/lua
                    COMMAND ${CMAKE_COMMAND} -E copy_directory
                            ${FENNEL_SOURCE_FOLDER} ${DESTINATION_FOLDER}/fennel
                    COMMENT "Copying Lua and Fennel files"
                )

                """;
            }
            cmake_file_content += "include_directories(include)\r\n";
            cmake_file_content += "include_directories(src)\r\n\r\n";

            cmake_target_link_libraries += ")";

            cmake_file_content += cmake_target_link_libraries;
            string cmake_file_path = Path.Combine($"{project_path}/{project_name}", "CMakeLists.txt");
            // Create or overwrite the file
            File.Create(cmake_file_path).Close();
            File.WriteAllText(cmake_file_path, cmake_file_content);
        }

        public static void IncludeFennel(Request request, string path)
        {

            string folderPath = $"{request.ProjectPath}/{request.ProjectName}/src/fennel";
            Directory.CreateDirectory(folderPath);

            string luaFolderPath = $"{request.ProjectPath}/{request.ProjectName}/src/lua";

            // Create fennel main file
            File.Create(folderPath + "/main.fnl").Close();
            File.WriteAllText(folderPath + "/main.fnl", "(print \"Hello world from fennel!\")");

            // Create fennel module example
            File.Create(folderPath + "/my-module.fnl").Close();
            File.WriteAllText(folderPath + "/my-module.fnl",
                """
                ;; File: my-module.fnl
                ;; To include the module in fennel write (local my-module (require :fennel.my-module))
                (local my-module {}) ;; Create a table to hold module contents

                (fn my-module.greet [name]
                  (print "Hello World from a fennel module"))

                ;; Export the module
                my-module  
                """);


            // 1. Downloading the tar.gz File
            string url = "https://fennel-lang.org/downloads/fennel-1.4.2.tar.gz";
            string archivePath = Path.Combine(luaFolderPath, Path.GetFileName(url));

            using (var client = new WebClient())
            {
                client.DownloadFile(new Uri(url), archivePath);
            }

            ExtractTarGz(archivePath, luaFolderPath);

            File.Delete(archivePath); // Remove the downloaded archive
            File.Copy(luaFolderPath + "/fennel-1.4.2" + "/fennel.lua", luaFolderPath + "/fennel.lua");
            Directory.Delete(luaFolderPath + "/fennel-1.4.2", recursive: true); // Remove the extracted directory
        }

        public static void ExtractTarGz(string gzArchiveName, string destFolder)
        {
            using (Stream inStream = File.OpenRead(gzArchiveName))
            using (Stream gzipStream = new GZipStream(inStream, CompressionMode.Decompress))
            using (TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream))
            {
                tarArchive.ExtractContents(destFolder);
            }
        }


        static public void CreateMain(string type, string project_name, string project_path)
        {
            string main_c_content = "";

            if (type == "Sokol+Nuklear")
            {
                string filePath = "./sokol_nuklear.txt"; // Replace with your actual file path
                string fileContents = File.ReadAllText(filePath);
                main_c_content += fileContents; 
            }
            
            if (type == "empty")
            {
            }

            if (type == "LuaJIT")
            {

                main_c_content += """
                    #include "lua.h"
                    #include "lualib.h"
                    #include "lauxlib.h"

                    int main(int argc, char const *argv[])
                    {
                        lua_State *L = luaL_newstate();  
                        luaL_openlibs(L);  

                        // Lua code to be executed (as a string)
                        const char *luaCode = "print('Hello from Lua!');";

                        // Execute the Lua code directly
                        luaL_dostring(L, luaCode);

                        // Loading a Lua Module directly via c
                        luaL_dofile(L, "./lua/mymodule.lua");

                        // Making the Lua Module globally available
                        lua_setglobal(L, "mymodule");
                        lua_settop(L, 0);
                        // The module can now be used in a lua 
                        // NOTE: This is equivialnt of doing (local mymodule = require 'mymodule') in lua


                        // Executing a lua script
                        if (luaL_dofile(L, "./lua/main.lua") != 0) {
                            const char *errorMessage = lua_tostring(L, -1);
                            fprintf(stderr, "Error executing Lua script: %s\n", errorMessage);
                            lua_pop(L, 1); // Remove the error message from the stack
                            // Additional error handling (e.g., clean up resources)
                        }
                        lua_close(L);   
                    }
                    """;
            }

            if (type == "vannila")
            {
                main_c_content += """
                    int main(int argc, char const *argv[])
                    {
                        return 0;
                    }
                    """;
            }


            string main_file_path = Path.Combine($"{project_path}/{project_name}/src/", "main.c");
            // Create or overwrite the file
            File.Create(main_file_path).Close();
            File.WriteAllText(main_file_path, main_c_content);

        }

    }
}
