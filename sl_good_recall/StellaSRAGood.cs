using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StellaSockets;

namespace SlGoodRecall
{
    public class StellaSRAGood
    {
        private readonly StellaResponseSocket _server;

        public StellaSRAGood()
        {
            _server = new StellaResponseSocket("ipc:///StellaSRAGood");
        }

        public StellaSRAGood(string address)
        {
            _server = new StellaResponseSocket(address);
        }

        public void Run()
        {
            while (true)
            {
                string request = _server.Receive();
                HandleMessage(request);
            }
        }

        private void HandleMessage(string jsonMessage)
        {

            if (!ValidJsonMessage(jsonMessage))
            {
                Dictionary<string, string> ErrorMessage = [];
                ErrorMessage.Add("error", "Json string message was not valid, couldnt correctly be deserialized");

                string jsonErrorResponse = JsonConvert.SerializeObject(ErrorMessage);

                _server.Send(jsonErrorResponse);
                return;
            }

            var jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonMessage);
            if (!ValidJsonRequestDictionary(jsonDictionary))
            {
                Dictionary<string, string> ErrorMessage = [];
                ErrorMessage.Add("error", "Json dictionary was not valid, couldnt correctly be deserialized");

                string jsonErrorResponse = JsonConvert.SerializeObject(ErrorMessage);

                _server.Send(jsonErrorResponse);
                return;
            }

            // Because we validated the json before and checked for possible null values
            // we can expect that every value here will not be null

            // Now you can access properties using their names
            double currentEaseFactor = double.Parse(jsonDictionary["EaseFactor"].ToString());
            int currentNumberOfTimeSeen = int.Parse(jsonDictionary["NumberOfTimeSeen"].ToString());

            DateTime newDueDate = CalculateNextReviewDate(currentNumberOfTimeSeen, currentEaseFactor);

            Dictionary<string, DateTime> responseDictionary = [];
            responseDictionary.Add("ok", newDueDate);

            if (!ValidJsonResponseDictionary(responseDictionary))
            {
                Dictionary<string, string> ErrorMessage = [];
                ErrorMessage.Add("error", "Created Json Response is not valid");

                string jsonErrorResponse = JsonConvert.SerializeObject(ErrorMessage);

                _server.Send(jsonErrorResponse);
                return;
            }

            string jsonResponse = JsonConvert.SerializeObject(responseDictionary);
            _server.Send(jsonResponse);
        }

        public static bool ValidJsonMessage(string jsonMessage)
        {

            try
            {
                JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonMessage);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public static bool ValidJsonRequestDictionary(Dictionary<string, object>? jsonDictionary)
        {
            return
                jsonDictionary != null &&
                jsonDictionary.ContainsKey("EaseFactor") &&
                jsonDictionary.ContainsKey("NumberOfTimeSeen");
        }

        public static bool ValidJsonResponseDictionary(Dictionary<string, DateTime> jsonDictionary)
        {
            return
                jsonDictionary != null &&
                jsonDictionary.ContainsKey("ok");
        }

        private static DateTime CalculateNextReviewDate(int numberOfTimeSeen, double easeFactor)
        {
            if (numberOfTimeSeen <= 2)
            {
                return DateTime.Now.AddMinutes(5);
            }

            int daysUntilNextReview = 1;
            double newEaseFactor;

            if (numberOfTimeSeen == 3)
            {
                newEaseFactor = 2.5;
            }
            else
            {
                newEaseFactor = easeFactor * 1.5;
            }

            daysUntilNextReview = (int)Math.Round(daysUntilNextReview * newEaseFactor, 0);

            return DateTime.Now.AddDays(daysUntilNextReview);
        }
    }
}