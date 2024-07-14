using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace CProjectMakerLogger
{
    public class Server
    {
        public RouterSocket LoggerServer = new("@tcp://localhost:50010");

        public void Run()
        {
            Logic.LogToFile(DateTime.Now, "./logs/log.txt", "Starting Logger Component\n------------------------------------------------------------------------------------------------------------");

            while (true)
            {
                var message = LoggerServer.ReceiveMultipartMessage();
                string json_request = message[1].ConvertToString();

                try
                {
                    Request request = JsonSerializer.Deserialize<Request>(json_request);
                    Logic.LogToFile(request.Time, request.FilePath, request.Message);
                    Logic.LogToConsole(request.Time, request.Message);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    Logic.LogToFile(DateTime.Now, "./logs/log.txt", $"Logger: {e.Message}");
                }

            }
        }
    }
}
