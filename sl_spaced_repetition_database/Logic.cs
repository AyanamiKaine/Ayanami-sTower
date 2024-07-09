using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NetMQ.Sockets;

namespace Spaced_Repetition_Database
{
    internal class Logic
    {
        RequestSocket SaveFileToLearnClient = new RequestSocket("@tcp://localhost:60006");

        // Saving the contents of the database to disk should be delegate to another process called SaveFileToLearn, exactly how i did it with LoadFilesToLearn
        public static void SaveData(List<FileToLearn> filesToLearn, string filePath = "Stella Knowledge Manager", string fileName = "main2_save_data.json")
        {

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, filePath);
            Directory.CreateDirectory(myAppDataFolder);
            string saveFilePath = Path.Combine(myAppDataFolder, fileName);

            string jsonData = JsonSerializer.Serialize(filesToLearn);
            using (StreamWriter writer = new StreamWriter(saveFilePath))
            {
                writer.Write(jsonData);
            }
        }

        public static string AddItem(List<FileToLearn> itemList,string name, string description, string filePath, float priority = 0)
        {
            FileToLearn item = new(Guid.NewGuid(), name, filePath, description, 2.5, priority);

            if (!File.Exists(filePath)) 
            {
                return $"{{status: \"error\", error: \"No File with the file - path {item.PathToFile} found \"}}";
            }
            else
            {
                itemList.Add(item);
                itemList.Sort((item1, item2) => item1.Priority.CompareTo(item2.Priority));
                // We reverse the list because we want the item with the highest proority at the front
                // If we dont reverse it the least prority item with be at the front.
                itemList.Reverse();
                return "{status: \"ok\"}";
            }
        }

        public static void RemoveItem(List<FileToLearn> itemList, Guid id)
        {
            itemList.RemoveAll(item => item.Id.Equals(id));
        }

        public static void updateItem(FileToLearn oldItem,FileToLearn newItem)
        {

        }
    }
}
