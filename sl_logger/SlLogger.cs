using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using StellaSockets;
namespace SlLogger
{
    class StellaLogger
    {
        private readonly StellaPullSocket _server;
        private static string _logFilePath;
        private const int MAX_LOG_FILE_SIZE = 10 * 1024 * 1024; // 10 MB

        public StellaLogger()
        {
            _server = new("ipc:///StellaLogger");
            _logFilePath = "./log.txt";
        }

        public StellaLogger(string fileToLogTo)
        {
            _server = new("ipc:///StellaLogger");
            _logFilePath = fileToLogTo;
        }

        public StellaLogger(string address, string fileToLogTo)
        {
            _server = new(address);
            _logFilePath = fileToLogTo;
        }

        public void Run()
        {
            Info("Starting SL_LOGGER");

            while (true)
            {
                string jsonMessage = _server.Receive();
                HandleMessage(jsonMessage);
            }
        }

        private static void HandleMessage(string jsonMessage)
        {
            try
            {

                if (!ValidJsonMessage(jsonMessage))
                {
                    var logMessage = "error Json string message was not valid, couldnt correctly be deserialized";
                    Error(logMessage);
                    return;
                }

                var jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonMessage);

                if (!ValidJsonRequestDictionary(jsonDictionary))
                {
                    var logMessage = "Json dictionary was not valid, couldnt correctly be deserialized";
                    Error(logMessage);
                    return;
                }


                string errorType = (string)jsonDictionary["LogType"];
                DateTime ErrorTime = DateTime.Parse(jsonDictionary["LogTime"].ToString());
                string errorMessage = (string)jsonDictionary["LogMessage"];
                string sender = jsonDictionary["Sender"]?.ToString();  // New: Get sender

                Log(errorType, ErrorTime, errorMessage, sender);
            }
            catch (Exception ex)
            {
                Error($"Error processing log message: {ex.Message}");
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
                Error(e.Message);
                return false;
            }
        }

        private static bool ValidJsonRequestDictionary(Dictionary<string, object>? jsonDictionary)
        {
            bool isNotNull = jsonDictionary != null;
            bool hasLogTypeKey = jsonDictionary.ContainsKey("LogType");
            bool hasLogTimeKey = jsonDictionary.ContainsKey("LogTime");
            bool hasLogMessageKey = jsonDictionary.ContainsKey("LogMessage");
            bool hasSenderKey = jsonDictionary.ContainsKey("Sender");


            if (!isNotNull ||
                !hasLogTypeKey ||
                !hasLogTimeKey ||
                !hasLogMessageKey ||
                !hasSenderKey)
            {

                string errorMessage = "Invalid JSON log message received:";


                if (!isNotNull)
                {
                    errorMessage += " Dictionary is null.";
                }
                else
                {
                    if (!hasLogTypeKey) errorMessage += " Missing 'LogType' key.";
                    if (!hasLogTimeKey) errorMessage += " Missing 'LogTime' key.";
                    if (!hasLogMessageKey) errorMessage += " Missing 'LogMessage' key.";
                    if (!hasSenderKey) errorMessage += " Missing 'Sender' key.";
                }
                Log("Error", DateTime.Now, errorMessage, "");
                return false;
            }

            return true;
        }

        private static void Log(string logType, DateTime logTime, string message, string? sender)
        {
            // Set Console color based on log level
            switch (logType)
            {
                case "Info":
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case "Warning":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case "Error":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case "Critical":
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case "Debug":
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
            }

            string logMessage = "";

            logMessage += $"[{logTime:HH:mm:ss} {logType.ToUpper()}] {message}";
            if (!string.IsNullOrWhiteSpace(sender))
            {
                logMessage += $" [Sender: {sender}]";
            }
            Console.WriteLine(logMessage);

            // Reset Console color
            Console.ResetColor();


            // Write to file
            try
            {
                FileInfo logFileInfo = new(_logFilePath);
                if (logFileInfo.Exists && logFileInfo.Length > MAX_LOG_FILE_SIZE)
                {
                    File.WriteAllText(_logFilePath, string.Empty); // Reset log file
                    Log("Info", DateTime.Now, "Log file reset due to size limit.", "");
                }

                using (StreamWriter sw = File.AppendText(_logFilePath))
                {
                    sw.WriteLine(logMessage);
                }
            }
            catch (Exception ex)
            {
                Error($"Error writing log to file: {ex.Message}"); // Log error but don't fail
            }

        }

        // Overloads for convenience:
        public static void Info(string message) => Log("Info", DateTime.Now, message, "");
        public static void Warning(string message) => Log("Warning", DateTime.Now, message, "");
        public static void Error(string message) => Log("Error", DateTime.Now, message, "");
        public static void Debug(string message) => Log("Debug", DateTime.Now, message, "");
    }
}
