using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Launcher
{
    internal class Program
    {
        private static readonly List<Process> _childProcesses = [];

        static void Main()
        {

            string[] exeNames = [];

            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                exeNames = [
                "LoadFilesToLearn.exe",
                "Logger.exe",
                "OpenFileWithDefaultProgramComponent.exe",
                "SaveFileToLearn.exe",
                "Spaced Repetition Database.exe",
                "SpaceRepetitionAlgorithm.exe",
                "stella_notes.exe"
                ];
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                exeNames = [
                "LoadFilesToLearn",
                "Logger",
                "OpenFileWithDefaultProgramComponent",
                "SaveFileToLearn",
                "Spaced Repetition Database",
                "SpaceRepetitionAlgorithm",
                "stella_notes"
                ];
            }

            SetupSaveFolder();

            // Get the directory of the currently executing program
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Start the child processes and track them
            foreach (string exeName in exeNames)
            {
                try
                {
                    Process process = Process.Start(exeName);
                    if (process != null)
                    {
                        _childProcesses.Add(process);
                        Console.WriteLine($"Started: {exeName}");

                        // Ensure stella_notes is handled specially
                        // We want to close every other process if we close the flutter_ui 
                        if (exeName == "stella_notes.exe" || exeName == "stella_notes")
                        {
                            process.EnableRaisingEvents = true;
                            process.Exited += OnStellaNotesExited;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error starting {exeName}: {ex.Message}");
                }
            }

            // Keep the main program running until all child processes finish
            while (_childProcesses.Any(p => !p.HasExited))
            {
            }

            Console.WriteLine("All child processes have exited.");
        }
        private static void OnStellaNotesExited(object? sender, EventArgs e)
        {
            Console.WriteLine("stella_notes has exited. Terminating other processes...");
            foreach (var process in _childProcesses)
            {
                if (!process.HasExited && process.ProcessName != "stella_notes.exe" || process.ProcessName != "stella_notes") 
                {
                    try
                    {
                        process.Kill();
                        Console.WriteLine($"Terminated: {process.ProcessName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error terminating {process.ProcessName}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Setting up the save folder an en empty save file
        /// </summary>
        private static void SetupSaveFolder() 
        {
            // Setup save folder BEGIN
            string savePath = "Stella Knowledge Manager";
            string saveName = "main2_save_data.json";

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, savePath);
            string saveLocation = Path.Combine(myAppDataFolder, saveName);// Update
            
            if (Directory.Exists(myAppDataFolder) == true && File.Exists(saveLocation) == true)
            {
                return;
            }
            
            Directory.CreateDirectory(myAppDataFolder);
            
            List<FileToLearn> initalData = [];

            string jsonData = JsonSerializer.Serialize(initalData);
            using StreamWriter writer = new(saveLocation); // Update
            writer.Write(jsonData);
            // Setup save folder END
        }
    }
}
