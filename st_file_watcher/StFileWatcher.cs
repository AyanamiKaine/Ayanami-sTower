using StellaSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace StellaFileWatcher
{
    public class StFileWatcher : IDisposable
    {
        /// <summary>
        /// This publisher publishes all events that are happening in the file watcher
        /// </summary>
        private readonly StellaPublishSocket _eventServer;

        /// <summary>
        /// The command Server allows for specific request to the FileWatcher like changing what file/directory to watch
        /// It works in a request/response pattern, usually returns {"ok" : ""} if everything worked out or {"error" : "ERROR_MESSAGE"}
        /// </summary>
        private readonly StellaResponseSocket _commandServer;
        private readonly FileSystemWatcher _watcher;

        private string _pathToWatch;

        public StFileWatcher(string eventServerPID, string commandServerPID, string folderPath)
        {
            _pathToWatch = folderPath;
            _commandServer = new(commandServerPID);
            _eventServer = new(eventServerPID);
            _watcher = new(folderPath)
            {
                NotifyFilter = NotifyFilters.Attributes
                                            | NotifyFilters.CreationTime
                                            | NotifyFilters.DirectoryName
                                            | NotifyFilters.FileName
                                            | NotifyFilters.LastWrite
                                            | NotifyFilters.Security
                                            | NotifyFilters.Size
            };

            _watcher.Changed += PublishOnChangedEvent;
            _watcher.Created += PublishOnCreatedEvent;
            _watcher.Deleted += PublishOnDeletedEvent;
            _watcher.Renamed += PublishOnRenamedEvent;
            _watcher.Error += PublishOnErrorEvent;
        }

        public StFileWatcher()
        {

            // We should implement json validation

            string configString = File.ReadAllText("./config.json");
            Dictionary<string, string> jsonConfig = JsonConvert.DeserializeObject<Dictionary<string, string>>(configString);

            _eventServer = new(jsonConfig["eventServerProtocol"] + jsonConfig["eventServerPid"]);
            _commandServer = new(jsonConfig["commandServerProtocol"] + jsonConfig["commandServerPid"]);
            _pathToWatch = jsonConfig["initalFolderToWatch"];
            _watcher = new(jsonConfig["initalFolderToWatch"])
            {
                NotifyFilter = NotifyFilters.Attributes
                                | NotifyFilters.CreationTime
                                | NotifyFilters.DirectoryName
                                | NotifyFilters.FileName
                                | NotifyFilters.LastWrite
                                | NotifyFilters.Security
                                | NotifyFilters.Size
            };

            _watcher.Changed += PublishOnChangedEvent;
            _watcher.Created += PublishOnCreatedEvent;
            _watcher.Deleted += PublishOnDeletedEvent;
            _watcher.Renamed += PublishOnRenamedEvent;
            _watcher.Error += PublishOnErrorEvent;
        }

        public void Run()
        {
            StartWatching();
            IncludeSubdirectories(true);
            HandleMessage();
        }

        public void HandleMessage()
        {
            while (true)
            {
                string message = _commandServer.Receive();

                var jsonRequest = JsonConvert.DeserializeObject<Dictionary<string, string>>(message); ;


                Dictionary<string, string> jsonResponse = [];

                if (ValidJsonRequest(jsonRequest))
                {
                    jsonResponse.Add("error", $"mallformed json");
                    return;
                }

                switch (jsonRequest["command"])
                {
                    case "GetCurrentWatchedPath":
                        jsonResponse.Add("ok", GetCurrentWatchedPath());
                        string responseMessage = JsonConvert.SerializeObject(jsonResponse);
                        _commandServer.Send(responseMessage);
                        break;
                    default:
                        jsonResponse.Add("error", $"unknown command {jsonRequest["command"]}");
                        break;
                }
            }
        }

        public string GetCurrentWatchedPath()
        {
            return _pathToWatch;
        }

        public void StartWatching()
        {
            _watcher.EnableRaisingEvents = true;
        }

        public void StopWatching()
        {
            _watcher.EnableRaisingEvents = false;
        }

        public void IncludeSubdirectories(bool flag)
        {
            _watcher.IncludeSubdirectories = flag;
        }

        public void LoadConfig()
        {

        }

        public void SetWatchedFile(string filePath)
        {
            _watcher.Path = filePath;
        }

        public void SetWatchDirectory(string directoryPath)
        {
            _watcher.Path = directoryPath;
        }


        private bool ValidJsonRequest(Dictionary<string, string> jsonRequest)
        {

            /*
            {
                "Command" : "GetCurrentWatchedPath"
            }
            => {"ok" : _pathToWatch}
            => {"error" : "Currently no file/directory set to watch"}
            => {"error" : "Invalid Json Request"}
            */

            return jsonRequest.ContainsKey("Command");
        }


        private bool ValidJsonResponse(Dictionary<string, string> jsonResponse)
        {
            return jsonResponse.ContainsKey("type") && jsonResponse.ContainsKey("filePath");
        }

        private void PublishOnChangedEvent(object sender, FileSystemEventArgs e)
        {
            Dictionary<string, string> jsonResponse = [];
            jsonResponse.Add("type", "changed");
            jsonResponse.Add("filePath", e.FullPath);

            string jsonString = JsonConvert.SerializeObject(jsonResponse);

            _eventServer.Send(jsonString, _pathToWatch);
        }

        private void PublishOnCreatedEvent(object sender, FileSystemEventArgs e)
        {
            Dictionary<string, string> jsonResponse = [];
            jsonResponse.Add("type", "Created");
            jsonResponse.Add("filePath", e.FullPath);


            string jsonString = JsonConvert.SerializeObject(jsonResponse);

            _eventServer.Send(jsonString, _pathToWatch);
        }

        private void PublishOnDeletedEvent(object sender, FileSystemEventArgs e)
        {
            Dictionary<string, string> jsonResponse = [];
            jsonResponse.Add("type", "Deleted");
            jsonResponse.Add("filePath", e.FullPath);


            string jsonString = JsonConvert.SerializeObject(jsonResponse);


            _eventServer.Send(jsonString, _pathToWatch);
        }

        private void PublishOnRenamedEvent(object sender, FileSystemEventArgs e)
        {
            Dictionary<string, string> jsonResponse = [];
            jsonResponse.Add("type", "Renamed");
            jsonResponse.Add("filePath", e.FullPath);


            string jsonString = JsonConvert.SerializeObject(jsonResponse);


            _eventServer.Send(jsonString, _pathToWatch);
        }

        private void PublishOnErrorEvent(object sender, System.IO.ErrorEventArgs e) =>
            PrintException(e.GetException());

        private void PrintException(Exception ex)
        {
            if (ex != null)
            {
                Dictionary<string, string> jsonResponse = [];
                jsonResponse.Add("type", "error");
                jsonResponse.Add("message", ex.Message);


                string jsonString = JsonConvert.SerializeObject(jsonResponse);
                _eventServer.Send(jsonString, "Screenshots");

                PrintException(ex.InnerException);
            }
        }

        // Implement IDisposable for automatic cleanup
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Prevent finalizer from running
        }

        protected virtual void Dispose(bool disposing)

        {
            if (disposing
                && _watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }
        }
    }

}