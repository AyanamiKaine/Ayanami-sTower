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
        public List<FileToLearn> PriorityList { get; private set; } = [];

        // I think we must add a way to add plugins.

        /// <summary>
        /// Holds the number of items learned for engourage the user and to let them see how much they already did
        /// </summary>
        private int _totalNumberOfLearnedItem = 0;

        public string AddItem(string name, string description ,string filePath, float priority = 0)
        {
            FileToLearn item = new(Guid.NewGuid(), name, filePath, description, 2.5, priority);

            if (!File.Exists(filePath)) // Update
            {
                return $"{{status: \"error\", error: \"No File with the file - path {item.PathToFile} found \"}}";
            }
            else
            {
                PriorityList.Add(item);
                PriorityList.Sort((item1, item2) => item1.Priority.CompareTo(item2.Priority));
                // We reverse the list because we want the item with the highest proority at the front
                // If we dont reverse it the least prority item with be at the front.
                PriorityList.Reverse();
                return "{status: \"ok\"}";
            }
        }

        public void ClearPriorityList()
        {
            PriorityList.Clear();
        }

        public void RemoveItem(Guid id)
        {
            PriorityList.RemoveAll(item => item.Id.Equals(id));
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

                CreateBackup();
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

        public void CreateBackup(string filePath = "Stella Knowledge Manager/backups", string fileName = "backup_save_data")
        {
            CreateBackupEveryWeekday();

            int maxBackupsToKeep = 30; // Example: Keep a maximum of 30 backups 

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, filePath);
            Directory.CreateDirectory(myAppDataFolder);

            // Key changes start here
            string baseFileName = Path.Combine(myAppDataFolder, fileName);
            string backupFileName = baseFileName ;
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
        private void CreateBackupEveryWeekday(string filePath = "Stella Knowledge Manager/backups/week", string fileName = "backup_save_data")
        {
            int maxBackupsToKeep = 7; // Keep backups for 7 days 

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, filePath);
            Directory.CreateDirectory(myAppDataFolder);

            // Key changes for daily backups with retention
            string baseFileName = Path.Combine(myAppDataFolder, fileName);
            string timestamp = DateTime.Now.ToString("yyyyMMdd"); // Daily timestamp
            string backupFileName = $"{baseFileName}_{timestamp}.json";

            // Only increment index if a file with today's timestamp exists
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

            // Cleanup of old backups
            var backupFiles = Directory.GetFiles(myAppDataFolder, "backup_save_data_*.json");
            if (backupFiles.Length > maxBackupsToKeep)
            {
                var filesToDelete = backupFiles
                                      .GroupBy(f => Path.GetFileNameWithoutExtension(f).Split('_')[1]) // Group by date
                                      .Where(g => g.Count() > 1) // Keep only latest per day
                                      .SelectMany(g => g.OrderBy(f => f).Skip(1)); // Select older ones 

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
