using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveFileToLearn
{
    public class Logic
    {
        public static void SaveData(List<FileToLearn> filesToLearn, string filePath = "Stella Knowledge Manager", string fileName = "main_save_data.json")
        {

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, filePath);
            Directory.CreateDirectory(myAppDataFolder);
            string saveFilePath = Path.Combine(myAppDataFolder, fileName);

            string jsonData = JsonSerializer.Serialize(filesToLearn);
            using (StreamWriter writer = new StreamWriter(saveFilePath)) // Update
            {
                writer.Write(jsonData);
            }
        }
    }
}
