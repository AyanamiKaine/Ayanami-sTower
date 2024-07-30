using Stella.Testing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StellaSockets;

namespace SlGoodRecall
{
    public class StellaSRAGoodUnitTests
    {
        [ST_TEST]
        private static StellaTesting.TestingResult JsonInputTest()
        {
            string jsonString = """
            {
                "Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                "Name": "Learning_Csharp.pdf",
                "PathToFile": "C:\\Documents\\Study\\Learning_Csharp.pdf",
                "Description": "A comprehensive guide to C# programming.",
                "EaseFactor": 2.5,
                "NextReviewDate": "2024-08-01T10:30:00", 
                "Priority": 1.8,
                "NumberOfTimeSeen": 5
            }
            
            """;
            var jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

            return StellaTesting.AssertTrue(StellaSRAGood.ValidJsonRequestDictionary(jsonDictionary));
        }
        [ST_TEST]
        private static StellaTesting.TestingResult JsonReviewDateInputTest()
        {
            string jsonString = """
            {
                "Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                "Name": "Learning_Csharp.pdf",
                "PathToFile": "C:\\Documents\\Study\\Learning_Csharp.pdf",
                "Description": "A comprehensive guide to C# programming.",
                "EaseFactor": 2.5,
                "NextReviewDate": "2024-08-01T10:30:00", 
                "Priority": 1.8,
                "NumberOfTimeSeen": 5
            }
            
            """;
            var jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

            DateTime actualNextReviewDate = DateTime.Now;
            DateTime expectedNextReviewDate = DateTime.Parse("2024-08-01T10:30:00");

            if (StellaSRAGood.ValidJsonRequestDictionary(jsonDictionary))
            {
                actualNextReviewDate = (DateTime)jsonDictionary["NextReviewDate"];
            }

            return StellaTesting.AssertEqual(expectedNextReviewDate, actualNextReviewDate);
        }

        [ST_TEST]
        private static StellaTesting.TestingResult IntergrationTest()
        {
            StellaSRAGood Server = new("ipc:///StellaSRAGoodIntegrationTest");
            StellaRequestSocket Client = new("ipc:///StellaSRAGoodIntegrationTest");

            string jsonString = """
            {
                "Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                "Name": "Learning_Csharp.pdf",
                "PathToFile": "C:\\Documents\\Study\\Learning_Csharp.pdf",
                "Description": "A comprehensive guide to C# programming.",
                "EaseFactor": 2.5,
                "NextReviewDate": "2024-08-01T10:30:00", 
                "Priority": 1.8,
                "NumberOfTimeSeen": 5
            }
            """;

            Client.Send(jsonString);
            var serverTask = Task.Run(() => Server.Run());

            string serverResponse = Client.Receive();
            var jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(serverResponse);

            return StellaTesting.AssertEqual(jsonDictionary.ContainsKey("ok"), true);
        }

        [ST_TEST]
        private static StellaTesting.TestingResult InvalidJsonRequestTest()
        {
            StellaSRAGood Server = new("ipc:///StellaSRAGoodInvalidJsonRequestTest");
            StellaRequestSocket Client = new("ipc:///StellaSRAGoodInvalidJsonRequestTest");

            string jsonString = """
            {
                "Isad": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                "Nadasme": "Learning_Csharp.pdf",
                "PaddthToFile": "C:\\Documents\\Study\\Learning_Csharp.pdf",
                "Descsription": "A comprehensive guide to C# programming.",
                "EaseFasactor": 2.5,
                "NextRsaeviewDate": "2024-08-01T10:30:00", 
                "Prioaarity": 1.8,
                "NumbeasdrOfTimeSeen": 5
            }
            """;

            Client.Send(jsonString);
            var serverTask = Task.Run(() => Server.Run());

            string serverResponse = Client.Receive();
            var jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(serverResponse);

            return StellaTesting.AssertEqual(jsonDictionary.ContainsKey("error"), true);
        }

        [ST_TEST]
        private static StellaTesting.TestingResult InvalidJsonResponseTest()
        {
            StellaSRAGood Server = new("ipc:///StellaSRAGoodJsonResponseTest");
            StellaRequestSocket Client = new("ipc:///StellaSRAGoodJsonResponseTest");

            string jsonString = """
            {
                "Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            asdasd sa
            """;

            Client.Send(jsonString);
            var serverTask = Task.Run(() => Server.Run());

            string serverResponse = Client.Receive();
            var jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(serverResponse);

            return StellaTesting.AssertEqual(jsonDictionary.ContainsKey("error"), true);
        }
    }
}