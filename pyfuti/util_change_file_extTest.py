import unittest
import os
import shutil
from unittest.mock import patch  # For mocking the 'os' module
from util_change_file_ext import change_extension_in_current_directory

class TestFileRenaming(unittest.TestCase):

    def setUp(self):
        """Creates a temporary directory for testing"""
        self.temp_dir = "test_dir"
        os.mkdir(self.temp_dir)

    def tearDown(self):
        """Removes the temporary directory after tests"""
        shutil.rmtree(self.temp_dir)

    def create_test_files(self, files):
        """Helper to create files in the temporary directory"""
        os.chdir(self.temp_dir)  # Change into the temporary directory
        for file in files:
            with open(file, "w"):  # Create an empty file
                pass
        os.chdir("..")  # Change back to original directory

    @patch('os.listdir')
    @patch('os.rename')
    def test_renaming(self, mock_rename, mock_listdir):
        """Tests the renaming functionality"""
        test_files = ["file1.norg", "file2.norg", "other.txt"]
        self.create_test_files(test_files)

        # Mock the behavior of os.listdir
        mock_listdir.return_value = test_files

        change_extension_in_current_directory()

        # Assert that files were renamed correctly
        mock_rename.assert_has_calls([
            unittest.mock.call("file1.norg", "file1.org"),
            unittest.mock.call("file2.norg", "file2.org")
        ])

if __name__ == '__main__':
    unittest.main()