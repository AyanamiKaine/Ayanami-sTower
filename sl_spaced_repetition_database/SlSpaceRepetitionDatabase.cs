using System.Text.Json.Nodes;
using Newtonsoft.Json;
using StellaSockets;

namespace Spaced_Repetition_Database
{
    public class StellaSpaceRepetitionDatabase
    {
        /// <summary>
        /// Client socket for the logging server
        /// </summary>
        private readonly StellaPushSocket _stellaLoggerClient;
        private readonly StellaResponseSocket _server;
        public StellaSpaceRepetitionDatabase()
        {
            _stellaLoggerClient = new("ipc:///StellaLogger");
            _server = new("ipc:///StellaSpacedRepetitionDatabase");
        }
        public void Run()
        {
            Log("Info", "", "Starting Stella-Spaced-Repetition-Database");


            while (true)
            {
                //string jsonMessage = _server.Receive();
                //HandleMessage(jsonMessage);
            }
        }
        private static void HandleMessage(string jsonMessage)
        {
            /*
            // Possible JSON Format

            "command": "CREATE/RETRIVE/UPDATE/DELETE",
            "id" : "UID",
            */
        }

        private void Create()
        {
            Log("Info", "", "Request to CREATE a new entry");
        }

        private void Retrive()
        {
            Log("Info", "", "Request to RETRIVE an entry");
        }

        private void Update()
        {
            Log("Info", "", "Request to UPDATE an entry");
        }

        private void Delete()
        {
            Log("Info", "", "Request to DELETE an entry");
        }

        private void Log(string type, string time, string message)
        {
            Dictionary<string, string> Message = [];
            Message.Add("LogType", type);
            Message.Add("LogTime", DateTime.Now.ToString());
            Message.Add("LogMessage", message);
            Message.Add("Sender", "StellaRepetitionServer");
            _stellaLoggerClient.SendNonBlock(JsonConvert.SerializeObject(Message));
        }
    }
}