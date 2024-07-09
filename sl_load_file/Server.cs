using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace LoadFilesToLearn
{
    public class Server
    {
        ResponseSocket  server          = new ResponseSocket("@tcp://localhost:60003");
        DealerSocket    LoggerClient    = new DealerSocket(">tcp://localhost:60010");    // Clients always start with an >

        public void Run()
        {
            // Receive the message from the server socket
            LogMessage("Starting Load FilesToLearn Component");
            while (true)
            {
                string json_request = server.ReceiveFrameString();
                //Request request = JsonSerializer.Deserialize<Request>(json_request);

                var files_to_learn = Logic.LoadData();
                LogMessage("FilesToLearn Component: Loading Data");

                if (files_to_learn == null || files_to_learn == "")
                {
                    server.SendFrame($"{{\"status\": \"error\", \"error\": \"Failed to load data\"}}");
                    LogMessage("FilesToLearn Component: Failed to load data");
                }
                else
                {
                    server.SendFrame($"{{\"status\": \"ok\", \"Data\": {files_to_learn} }}");
                }
            }
        }
        private void LogMessage(string message)
        {
            LoggerClient.SendFrame($"{{ \"Message\": \"{message}\" }}");
        }
    }
}
