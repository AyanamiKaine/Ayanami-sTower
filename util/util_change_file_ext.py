import os

def change_extension_in_current_directory(old_ext=".norg", new_ext=".org"):
    """
    Renames files in the current directory from an old extension to a new one.
    :param old_ext: The old file extension.
    :param new_ext: The new file extension to replace with.
    """
    # Get the list of files in the current directory
    files_in_directory = os.listdir('.')
    # Filter out files that have the old extension
    files_to_rename = [file for file in files_in_directory if file.endswith(old_ext)]

    for file_name in files_to_rename:
        # Construct the new file name by replacing the old extension with the new one
        new_file_name = file_name[:-len(old_ext)] + new_ext
        # Rename the file
        os.rename(file_name, new_file_name)
        print(f"Renamed {file_name} to {new_file_name}")