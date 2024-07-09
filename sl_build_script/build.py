import os
import subprocess
from pathlib import Path
import shutil
import logging
import zipfile
import platform
from pathlib import Path

# Set up logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

def create_zip(folder_path, zip_name):
    """
    Creates a ZIP archive from the specified folder.

    Args:
        folder_path: The path to the folder to zip.
        zip_name: The desired name of the ZIP archive (with .zip extension).
    """
    with zipfile.ZipFile(zip_name, "w", zipfile.ZIP_DEFLATED) as zipf:
        for root, dirs, files in os.walk(folder_path):
            for file in files:
                file_path = os.path.join(root, file)
                zipf.write(file_path, os.path.relpath(file_path, folder_path))


def copy_folder_recursive(source_folder, destination_folder):
    """Copies a folder and its contents recursively."""
    for item_name in os.listdir(source_folder):
        source_item = os.path.join(source_folder, item_name)
        destination_item = os.path.join(destination_folder, item_name)

        if os.path.isfile(source_item):
            shutil.copy2(source_item, destination_item)  # Copy files with metadata
            logging.info(f"Copied file: {source_item} -> {destination_item}")
        elif os.path.isdir(source_item):
            shutil.copytree(source_item, destination_item, dirs_exist_ok=True)
            logging.info(f"Copied directory: {source_item} -> {destination_item}")

# Paths and commands

current_dir = Path.cwd()  
parent_dir = current_dir.parent 


if (platform.system() == "Windows"):
    flutter_command = ["flutter", "build", "windows"]

    build_folder_path = Path("./build")
    stella_notes_build_path = parent_dir / "sl_flutter_ui" / "build" / "windows" / "x64" / "runner" / "Release"
    build_folder_destination = "./build"

    msbuild_command = (
        r'msbuild "./stella_learning_build.sln" '
        r'/p:Configuration=Release /t:Rebuild '
        r'/p:OutputPath=' + f"{parent_dir.absolute()}/sl_build_script/build"
    )

if (platform.system() == "Linux"):
    flutter_command = ["~/flutter/bin/flutter", "build", "linux"]

    build_folder_path = Path("./build")
    stella_notes_build_path = parent_dir / "sl_flutter_ui" / "build" / "linux" / "x64" / "release" / "bundle"
    build_folder_destination = "./build"

    msbuild_command = (
        r'dotnet build "./stella_learning_build.sln" '
        r'/p:Configuration=Release  /t:Rebuild '
        r'/p:OutputPath=' + f"{parent_dir.absolute()}/sl_build_script/build"
    )

# Create the build folder if it doesn't exist
build_folder_path.mkdir(parents=True, exist_ok=True)
logging.info(f"Created build directory: {build_folder_path}")

# Flutter build
try:
    result = subprocess.run(
        flutter_command,
        shell=True,
        cwd= parent_dir / "sl_flutter_ui",
        capture_output=True,
        text=True,
        check=True,
    )
    logging.info("Flutter build succeeded!")
except subprocess.CalledProcessError as e:
    logging.error("Flutter build failed!")
    logging.error(e.stderr)

# Copy build artifacts
try:
    copy_folder_recursive(stella_notes_build_path, build_folder_destination)
    logging.info("Build artifacts copied to build folder.")
except shutil.Error as e:
    logging.error(f"Error copying build artifacts: {e}")
    raise

# Set environment variables
os.environ["PATH"] += r";C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin"
os.environ["VSINSTALLDIR"] = r"C:/Program Files/Microsoft Visual Studio/2022/Community/"
os.environ["VisualStudioVersion"] = "22.0" 

if platform.system() == "Linux":
    os.environ["PATH"] += ":/usr/bin:/usr/local/bin"  # Add common binary paths
    dotnet_path = shutil.which("dotnet") 
    if dotnet_path:
        os.environ["PATH"] += f":{os.path.dirname(dotnet_path)}"  # Add .NET Core path


# Run MSBuild
try:
    subprocess.run(msbuild_command, shell=True, check=True)
    logging.info("MSBuild completed successfully!")
    logging.info("Open ./build/launcher to start Stella Learning")
except subprocess.CalledProcessError as e:
    logging.error("MSBuild failed!")
    logging.error(e.stderr)
    raise

create_zip(build_folder_destination, "./build.zip")
print(f"ZIP archive './build.zip' created successfully.")