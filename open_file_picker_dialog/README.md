# Cross-platform File Picker CLI

This is a command-line file picker program written in Dart. It uses the `file_picker` package to provide a cross-platform file selection dialog.

## Features

*   Select files of various types (any, audio, image, video, custom).
*   Filter files by allowed extensions (for custom type).
*   Select single or multiple files.
*   Returns the selected file path(s) to stdout for easy integration with other programs.


## Arguments
* `-t`, `--type`: The type of file to pick (any, audio, image, video, custom). Defaults to any.
* `-e`, `--allowed-extensions`: Allowed file extensions (only for custom type). Can be specified multiple times.
* `-m`, `--multiple`: Allow picking multiple files

## Integration with other programs
This program is designed to be easily integrated with other programs. When a file is selected, the file path is printed to stdout. You can capture this output in another program (e.g., a C# application) to use the selected file.

### Example
```c#
class Program
{
    public static List<string> PickFile(string type = "any", string[]? allowedExtensions = null, bool multiple = false)
    {
        // Build the argument string
        string arguments = "";
        arguments += $"-t {type} ";
        if (allowedExtensions != null && allowedExtensions.Length > 0)
        {
            arguments += $"-e {string.Join(" -e ", allowedExtensions)} ";
        }
        if (multiple)
        {
            arguments += "-m ";
        }

        // Start the Dart file picker process
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            // Use the compiled executable's name here
            FileName = "open_file_picker_dialog.exe",
            Arguments = arguments,  // No need for "dart run" anymore
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };


        using (Process process = new Process
        {
            StartInfo = startInfo
        })
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            // Split the output by newline to get a list of file paths
            List<string> filePaths = output.Trim().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();

            return filePaths;
        }
    }
    static void Main(string[] args)
    {
        // Example usage
        List<string> filePath = PickFile(type: "image");
        Console.WriteLine($"Selected file: {filePath}");

        string[] allowedExtensions = { "txt", "csv" };
        List<string> multipleFiles = PickFile(type: "custom", allowedExtensions: allowedExtensions, multiple: true);
        foreach (string file in multipleFiles)
        {
            Console.WriteLine(file);
        }
    }
}

```

## Why?
It provides other applications with a native file dialog window that may not have an easy way to add a simple package to do the same.

## Note
This program uses the `file_picker` dart/flutter package, which might have limitations or specific behaviors depending on the platform it's running on.

