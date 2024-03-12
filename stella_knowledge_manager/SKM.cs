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
    public class SKM : IDataManager
    {
        public SKM() 
        {
        }

        public List<FileToLearn> PriorityList { get; private set; } = [];

        // I think we must add a way to add plugins.

        /// <summary>
        /// NOT IMPLEMENTED
        /// If we defined a default app for a file extension then we use that one, 
        /// instead the default one defined when using the explorer 
        /// NOT IMPLEMENTED
        /// </summary>
        private Dictionary<string, string> _defaultApplicationsForFiles = new()
        {
            { ".txt",   "notepad.exe"   },
            { ".md",    "obsidian.exe"  },
            { ".mp4",   "vlc.exe"       },
            { ".jpg",   "mspaint.exe"   },
            { ".pdf",   "AcroRd32.exe"  }
        };

        /// <summary>
        /// Holds the number of items learned for engourage the user and to let them see how much they already did
        /// </summary>
        private int _totalNumberOfLearnedItem = 0;

        /// <summary>
        /// Saves the conent of SKM to the appdata roaming folder in a specified folder where the default is simply Stella Knowledge Manager
        /// </summary>
        /// <param name="folderName"></param>

        public void RestoreFromBackup() { }

        /// <summary>
        /// This should not be a main function of the class as its only a utility function
        /// </summary>
        public ISRSItem GetItemById(Guid id)
        { 
            return PriorityList.SingleOrDefault((item) => item.Id == id);
        }

        /// <summary>
        /// This should not be a main function of the class as its only a utility function
        /// </summary>
        public ISRSItem GetItemByName(string name)
        {
            return PriorityList.SingleOrDefault((item) => item.Name == name);
        }

        public void AddItem(string name, string description ,string filePath, float priority = 0)
        {
            FileToLearn item = new(Guid.NewGuid(), name, filePath, description, 2.5, priority);

            if (!File.Exists(filePath)) // Update
            {
                Console.WriteLine("File does not exist pls choose a file that exists");
            } 
            else
            {
                PriorityList.Add(item);
                PriorityList.Sort((item1, item2) => item1.Priority.CompareTo(item2.Priority));
                // We reverse the list because we want the item with the highest proority at the front
                // If we dont reverse it the least prority item with be at the front.
                PriorityList.Reverse();
            }
        }

        public void ClearPriorityList()
        {
            PriorityList.Clear();
        }

        public void RemoveItem(Guid id)
        {
            var test = PriorityList.RemoveAll(item => item.Id.Equals(id));
        }

        public void SaveData(string filePath = "Stella Knowledge Manager", string fileName = "main_save_data.json")
        {
            CreateBackup();

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, filePath);
            Directory.CreateDirectory(myAppDataFolder);
            string saveFilePath = Path.Combine(myAppDataFolder, fileName);

            string jsonData = JsonConvert.SerializeObject(PriorityList, Formatting.Indented);
            using (StreamWriter writer = new StreamWriter(saveFilePath)) // Update
            {
                writer.Write(jsonData);
            }
        }

        public void LoadData(string filePath = "Stella Knowledge Manager", string fileName = "main_save_data.json")
        {
                CreateBackup();

                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string myAppDataFolder = Path.Combine(appDataFolder, filePath);
                string loadFilePath = Path.Combine(myAppDataFolder, fileName); // Update

                if (!File.Exists(loadFilePath)) // Update
                {
                    return;
                }

                using (StreamReader reader = new StreamReader(loadFilePath)) // Update 
                {
                    string jsonData = reader.ReadToEnd();
                    PriorityList = JsonConvert.DeserializeObject<List<FileToLearn>>(jsonData);
                }
        }
        /// <summary>
        /// Resets all Due Dates of all items to now
        /// </summary>
        public void ResetAllDueData()
        {
            foreach (var item in PriorityList)
            {
                item.NextReviewDate = DateTime.Now;
            }
        }

        public void CreateBackup(string filePath = "Stella Knowledge Manager/backups", string fileName = "backup_save_data.json")
        {
            int maxBackupsToKeep = 30; // Example: Keep a maximum of 30 backups 

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, filePath);
            Directory.CreateDirectory(myAppDataFolder);

            // Key changes start here
            string baseFileName = Path.Combine(myAppDataFolder, fileName);
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

            string jsonData = JsonConvert.SerializeObject(PriorityList, Formatting.Indented);
            using (StreamWriter writer = new StreamWriter(saveFilePath))
            {
                writer.Write(jsonData);
            }

            var backupFiles = Directory.GetFiles(myAppDataFolder, "backup_save_data_*.json");
            if (backupFiles.Length > maxBackupsToKeep)
            {
                var filesToDelete = backupFiles.OrderBy(f => f).Take(backupFiles.Length - maxBackupsToKeep);
                foreach (var file in filesToDelete)
                {
                    File.Delete(file);
                }
            }
        }

        public void LoadFromBackup(string filePath, string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
