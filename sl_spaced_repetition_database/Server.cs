using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace Spaced_Repetition_Database
{
    public class Server
    {
        public RequestSocket    LoadFilesToLearnClient          = new RequestSocket (">tcp://localhost:60003");    // Clients always start with an >
        public RequestSocket    SaveFileToLearnClient           = new RequestSocket (">tcp://localhost:60006");    // Clients always start with an >
        public DealerSocket     LoggerClient                    = new DealerSocket  (">tcp://localhost:60010");    // Clients always start with an >
        public ResponseSocket   SpacedRepetitionDatabaseServer  = new ResponseSocket("@tcp://localhost:60002");    // Servers always start with an @

        public void Run()
        {
            LogMessage("Starting Spaced Repetition Database");

            LoadFilesToLearnClient.SendFrame("""
            {
                "Command": "GET"
            }
            """);
            string json_response = LoadFilesToLearnClient.ReceiveFrameString();
            Response response = JsonSerializer.Deserialize<Response>(json_response);

            List<FileToLearn> FilesToLearn = response.Data;

            while (true)
            {

                    string json_request = SpacedRepetitionDatabaseServer.ReceiveFrameString();
                    Request request = JsonSerializer.Deserialize<Request>(json_request);

                    switch (request.Command)
                    {
                        case Command.CREATE:
                            LogMessage("Spaced Repetition Database: Request to create an item");

                            Logic.AddItem(FilesToLearn, request.FileToLearn.Name, request.FileToLearn.Description, request.FileToLearn.PathToFile, (float)request.FileToLearn.Priority);
                            SaveFileToLearnClient.SendFrame(JsonSerializer.Serialize(FilesToLearn));
                            SaveFileToLearnClient.ReceiveFrameString();
                            SpacedRepetitionDatabaseServer.SendFrame("{{\"status\": \"ok\"}}");
                            break;

                        case Command.UPDATE:
                            LogMessage("Spaced Repetition Database: Request to update an item");

                            FileToLearn file_to_update = FilesToLearn.FirstOrDefault((item) => item.Id == request.FileToLearn.Id);

                            if (file_to_update != null)
                            {

                                LogMessage($"Updating old file to learn. Old Ease Factor: ({file_to_update.EaseFactor}), Old Number Of Times Seen: ({file_to_update.NumberOfTimeSeen}), Old Next Review Date: ({file_to_update.NextReviewDate}), to new file to learn. New Ease Factor: ({request.FileToLearn.EaseFactor}), New Number Of Times Seen: ({request.FileToLearn.NumberOfTimeSeen}), New Review Date: ({request.FileToLearn.NextReviewDate})");

                                file_to_update.Priority = request.FileToLearn.Priority;
                                file_to_update.EaseFactor = request.FileToLearn.EaseFactor;
                                file_to_update.NextReviewDate = request.FileToLearn.NextReviewDate;
                                file_to_update.NumberOfTimeSeen = request.FileToLearn.NumberOfTimeSeen;
                                file_to_update.Priority = request.FileToLearn.Priority;
                                file_to_update.Description = request.FileToLearn.Description;
                                file_to_update.Name = request.FileToLearn.Name;
                                file_to_update.PathToFile = request.FileToLearn.PathToFile;
                            }
                            SaveFileToLearnClient.SendFrame(JsonSerializer.Serialize(FilesToLearn));
                            SaveFileToLearnClient.ReceiveFrameString();
                            SpacedRepetitionDatabaseServer.SendFrame("{{\"status\": \"ok\"}}");
                            break;

                        case Command.DELETE:
                            LogMessage("Spaced Repetition Database: Request to delete an item");
                            Logic.RemoveItem(FilesToLearn, request.Id);
                            SaveFileToLearnClient.SendFrame(JsonSerializer.Serialize(FilesToLearn));
                            SaveFileToLearnClient.ReceiveFrameString();
                            SpacedRepetitionDatabaseServer.SendFrame("{{\"status\": \"ok\"}}");
                            break;

                        case Command.RETRIVE_ITEM_WITH_HIGHEST_PRIORITY:
                            LogMessage("Spaced Repetition Database: Request to retrive the item with the highest priority");

                            FilesToLearn.Sort((item1, item2) => item1.Priority.CompareTo(item2.Priority));
                            // We reverse the list because we want the item with the highest proority at the front
                            // If we dont reverse it the least prority item with be at the front.
                            FilesToLearn.Reverse();

                            var highest_priority_item = JsonSerializer.Serialize<List<FileToLearn>>(FilesToLearn)[0];

                            SpacedRepetitionDatabaseServer.SendFrame($"{highest_priority_item}");
                            break;

                        case Command.RETRIVE_ALL_ITEMS:
                            LogMessage("Spaced Repetition Database: Request to retrive all items");
                            FilesToLearn.Sort((item1, item2) => item1.Priority.CompareTo(item2.Priority));
                            // We reverse the list because we want the item with the highest proority at the front
                            // If we dont reverse it the least prority item with be at the front.
                            FilesToLearn.Reverse();

                            var all_items = JsonSerializer.Serialize(FilesToLearn);

                            SpacedRepetitionDatabaseServer.SendFrame($"{all_items}");
                            break;

                        default:
                            LogMessage("Spaced Repetition Database: Unknown Command Given");

                            SpacedRepetitionDatabaseServer.SendFrame($"{{status: \"error\", error: \"Unknown Command Given!\"}}");
                            break;
                    
                }
            }
        }
        

        private void LogMessage(string message)
        {
            LoggerClient.SendFrame($"{{ \"Message\": \"{message}\" }}");
        }
    }
}
