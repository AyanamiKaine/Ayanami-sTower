using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using stella_knowledge_manager;

namespace LoadFilesToLearn
{
    internal class Logic
    {
        public static string LoadData(string filePath = "Stella Knowledge Manager", string fileName = "main_save_data.json")
        {

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string myAppDataFolder = Path.Combine(appDataFolder, filePath);
            string loadFilePath = Path.Combine(myAppDataFolder, fileName); // Update

            if (!File.Exists(loadFilePath)) // Update
            {
                return """
                [
                    {
                        "Id": "35733acb-a29e-4da0-b6bd-b9a756bb6d92",
                        "Name": "LetsFckingGo",
                        "PathToFile": "C:\\Users\\ayanami\\AppData\\Roaming\\Loadtest.txt",
                        "Description": "",
                        "Priority": 0,
                        "NumberOfTimeSeen": 0,
                        "EaseFactor": 2.5,
                        "NextReviewDate": "2024-03-10T15:23:01.4126167+01:00"
                    },
                    {
                        "Id": "36571a35-2ebb-4081-9a28-0e452d4dabc7",
                        "Name": "TestTest",
                        "PathToFile": "C:\\Users\\ayanami\\AppData\\Roaming\\Savetest.txt",
                        "Description": "",
                        "Priority": 0,
                        "NumberOfTimeSeen": 0,
                        "EaseFactor": 2.5,
                        "NextReviewDate": "2024-03-10T15:23:54.1326959+01:00"
                    },
                    {
                        "Id": "e4d222ea-668c-4d4b-be51-57775e4d40bb",
                        "Name": "Test",
                        "PathToFile": "C:\\Users\\ayanami\\AppData\\Roaming\\test.txt",
                        "Description": "",
                        "Priority": 25,
                        "NumberOfTimeSeen": 0,
                        "EaseFactor": 2.5,
                        "NextReviewDate": "2024-03-10T15:22:32.5028198+01:00"
                    },
                    {
                        "Id": "6cb55f16-7159-44b0-9e73-c1f4ec9cf36c",
                        "Name": "NoWay",
                        "PathToFile": "C:\\Users\\ayanami\\AppData\\Roaming\\Savetest.txt",
                        "Description": "",
                        "Priority": 25,
                        "NumberOfTimeSeen": 0,
                        "EaseFactor": 2.5,
                        "NextReviewDate": "2024-03-10T15:23:11.8316588+01:00"
                    },
                    {
                        "Id": "f587e89d-d315-45d0-a77c-9c7d45bcadde",
                        "Name": "Lel",
                        "PathToFile": "C:\\Users\\ayanami\\AppData\\Roaming\\Savetest.txt",
                        "Description": "",
                        "Priority": 100,
                        "NumberOfTimeSeen": 0,
                        "EaseFactor": 2.5,
                        "NextReviewDate": "2024-03-10T15:23:31.0769079+01:00"
                    }
                ]
                """;
            }

            using (StreamReader reader = new StreamReader(loadFilePath)) // Update 
            {
                string jsonData = reader.ReadToEnd();
                return jsonData;
            }
        }
    }
}
