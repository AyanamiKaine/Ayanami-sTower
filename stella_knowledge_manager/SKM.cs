using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace stella_knowledge_manager
{
    public class SKM
    {
        public SKM() 
        {
            LoadFromFile();
        }

        private List<FileToLearn> _priorityList = [];        

        private Dictionary<string, string> _defaultApplicationsForFiles = new()
        {
            { ".txt",   "notepad.exe"   },
            { ".md",    "obsidian.exe"  },
            { ".mp4",   "vlc.exe"       },
            { ".jpg",   "mspaint.exe"   },
            { ".pdf",   "AcroRd32.exe"  }
        };

        public void SaveToFile()
        {
            CreateBackup();

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, "Stella Knowledge Manager");
            Directory.CreateDirectory(myAppDataFolder);
            string saveFilePath = Path.Combine(myAppDataFolder, "main_savedata.json");

            string jsonData = JsonConvert.SerializeObject(_priorityList, Formatting.Indented);
            using (StreamWriter writer = new StreamWriter(saveFilePath)) // Update
            {
                writer.Write(jsonData);
            }
        }

        public void LoadFromFile()
        {
            CreateBackup();

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, "Stella Knowledge Manager");
            string loadFilePath = Path.Combine(myAppDataFolder, "main_savedata.json"); // Update

            if (!File.Exists(loadFilePath)) // Update
            {
                return;
            }

            using (StreamReader reader = new StreamReader(loadFilePath)) // Update 
            {
                string jsonData = reader.ReadToEnd();
                _priorityList = JsonConvert.DeserializeObject<List<FileToLearn>>(jsonData);
            }
        }

        public void CreateBackup()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, "Stella Knowledge Manager");
            Directory.CreateDirectory(myAppDataFolder);

            // Key changes start here
            string baseFileName = Path.Combine(myAppDataFolder, "backup_savedata");
            string backupFileName = baseFileName + ".json";
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss"); // For finer control

            int backupIndex = 1;
            while (File.Exists(backupFileName))
            {
                backupFileName = $"{baseFileName}_{timestamp}_{backupIndex}.json";
                backupIndex++;
            }

            // Update the save location
            string saveFilePath = backupFileName;

            string jsonData = JsonConvert.SerializeObject(_priorityList, Formatting.Indented);
            using (StreamWriter writer = new StreamWriter(saveFilePath))
            {
                writer.Write(jsonData);
            }
        }
        public void RestoreFromBackup() { }

        public void AddItem(string name ,string filePath, float priority)
        {
            var id = IdGenerator.GenerateId(name);
            FileToLearn item = new(id, name, filePath, "", 2.5, priority);

            if (!File.Exists(filePath)) // Update
            {
                Console.WriteLine("File does not exist pls choose a file that exists");
            } 
            else
            {
                _priorityList.Add(item);
                SaveToFile();
                _priorityList.Sort((item1, item2) => item1.Priority.CompareTo(item2.Priority));

            }

        }

        public void Clear()
        {
            _priorityList.Clear();
            SaveToFile();
        }

        public void RemoveItem(string id)
        {
            var test = _priorityList.RemoveAll(item => item.Id.Equals(id, StringComparison.CurrentCultureIgnoreCase));
            SaveToFile() ;
        }

        public void ShowAllItems()
        {
            // Use LINQ for conciseness
            var sortedItems = _priorityList
                                .OrderByDescending(item => item.Priority) // Sort by priority
                                .ToList();

            foreach (var item in sortedItems)
            { 
                item.PrettyPrint();
                //Console.WriteLine($"ID: {item.Id}. File Name: {item.PathToFile}, Priority: {item.Priority}, Due Data: {item.LastReviewDate}");  // Improved formatting
            }
            PrintStats();
        }

        public void UpdatePriorityValue(string filePath, float priorityValue)
        {
            // LINQ to find and update:
            //_priorityList.Where(item => item.PathToFile == filePath)
            //             .FirstOrDefault().Priority = priorityValue;
            SaveToFile();
        }

        public void PrintStats()
        {
            Console.WriteLine($"Total Number of items: {_priorityList.Count}");
        }

        public void StartLearning()
        {
            foreach (var item in _priorityList)
            {
                bool itemIsDue = true;

                if (item.NextReviewDate >= DateTime.Now)
                {
                    itemIsDue = false;
                }

                if (!File.Exists(item.PathToFile))
                {
                    Console.WriteLine("File not found. Please try again.");
                    return;
                }
                if(itemIsDue)
                {
                    try
                    {
                        // Start the process
                        //var process = new Process();
                        //process.StartInfo = new ProcessStartInfo(item.PathToFile) { UseShellExecute = true };
                        //process.Start();
                        Process.Start("explorer.exe", item.PathToFile);
                        // Wait for the process to exit

                        Console.WriteLine("DISPLAY DESCRIPTION (WHAT DO YOU WANT TO ACHIEVE WITH LEARNING THIS ITEM?)");

                        Console.WriteLine("Press Enter To Continue...");
                        Console.ReadLine().ToLower(); // Read input and convert to lowercase

                        Console.WriteLine("How was your recall?");
                        Console.WriteLine("Enter 'g' for good, 'b' for bad, or 'r' to do it again:");

                        string input = Console.ReadLine().ToLower(); // Read input and convert to lowercase

                        if (input == "g")
                        {
                            Console.WriteLine("Great! Let's move on.");
                            item.NextReviewDate = SpacedRepetitionScheduler.CalculateNextReviewDate(item, "g");
                        }
                        else if (input == "b")
                        {
                            Console.WriteLine("Don't worry, we can try again later.");
                            item.NextReviewDate = SpacedRepetitionScheduler.CalculateNextReviewDate(item, "b");

                        }
                        else if (input == "r")
                        {
                            Console.WriteLine("Let's try that again.");
                            item.NextReviewDate = SpacedRepetitionScheduler.CalculateNextReviewDate(item, "r");
                        }
                        else
                        {
                            Console.WriteLine("Invalid input. Please enter 'g', 'b', or 'r'.");
                        }

                        Console.WriteLine($"New Due Date: {item.NextReviewDate}");
                        SaveToFile();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Item: '{item.Name}' next review day is {item.NextReviewDate}");
                }
            }
            SaveToFile();
        }
    }
}
