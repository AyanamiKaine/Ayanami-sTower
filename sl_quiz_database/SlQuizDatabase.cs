using Newtonsoft.Json;
using StellaSockets;

namespace sl_quiz_database
{
    public class SlQuizDatabase
    {
        private readonly StellaResponseSocket _server;
        private readonly StellaPushSocket _stellaLoggerClient;
        private readonly QuizDatabase _quizDatabase;

        public SlQuizDatabase()
        {
            _server = new("ipc:///StellaQuizDatabase");
            _stellaLoggerClient = new("ipc:///StellaLogger");
            _quizDatabase = new();
        }

        public void Run()
        {
            while (true)
            {
                string jsonMessage = _server.Receive();
                HandleMessage(jsonMessage);
            }
        }
        private static void HandleMessage(string jsonMessage)
        {

            /*
            


            */

            try
            {

                if (!ValidJsonMessage(jsonMessage))
                {
                    var logMessage = "error Json string message was not valid, couldnt correctly be deserialized";
                    //Error(logMessage);
                    return;
                }

                var jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonMessage);

                if (!ValidJsonRequestDictionary(jsonDictionary))
                {
                    var logMessage = "Json dictionary was not valid, couldnt correctly be deserialized";
                    //Error(logMessage);
                    return;
                }

                //Log(errorType, ErrorTime, errorMessage, sender);
            }
            catch (Exception ex)
            {
                //Error($"Error processing log message: {ex.Message}");
            }
        }
        private static bool ValidJsonMessage(string jsonMessage)
        {
            try
            {
                JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonMessage);
                return true;
            }
            catch (JsonException e)
            {
                //Error(e.Message);
                return false;
            }
        }

        private static bool ValidJsonRequestDictionary(Dictionary<string, object>? jsonDictionary)
        {
            return false;
        }
    }
}