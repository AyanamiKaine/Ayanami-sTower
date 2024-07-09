using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace SaveFileToLearn
{
    public class Server
    {
        private ResponseSocket SaveFileToLearnServer    = new ResponseSocket("@tcp://localhost:60006");
        private DealerSocket LoggerClient               = new DealerSocket(">tcp://localhost:60010");    // Clients always start with an >

        public void Run()
        {
            LogMessage("Starting SaveFileToLearn Component");

            while (true)
            {
                try
                {
                    string json_request = SaveFileToLearnServer.ReceiveFrameString();

                    List<FileToLearn> filesToLearn = JsonSerializer.Deserialize<List<FileToLearn>>(json_request);

                    Logic.SaveData(filesToLearn);
                    LogMessage("Saving Files To Learn to disk");

                    SaveFileToLearnServer.SendFrame(JsonSerializer.Serialize(new Response { status = "ok" }));
                }
                catch (JsonException ex)
                {
                    LogMessage(ex.Message);
                    SaveFileToLearnServer.SendFrame(JsonSerializer.Serialize(new Response { status = "error", error = ex.Message }));
                }
                catch (Exception ex)
                {
                    LogMessage(ex.Message);
                    SaveFileToLearnServer.SendFrame(JsonSerializer.Serialize(new Response { status = "error", error = ex.Message }));
                }
            }

        }

        private void LogMessage(string message)
        {
            LoggerClient.SendFrame($"{{ \"Message\": \"{message}\" }}");
        }
    }
}
