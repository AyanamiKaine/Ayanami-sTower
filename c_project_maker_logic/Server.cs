using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace CProjectMakerLogic
{
    public class Server
    {
        public RouterSocket CProjectMakerLogicServer = new("@tcp://localhost:50001");    // Servers always start with an @
        private DealerSocket LoggerClient = new (">tcp://localhost:50010");    // Clients always start with an >

        public void Run()
        {

            LogMessage("Starting CProjectMakerLogicServer");

            while (true)
            {
                {
                    Console.WriteLine("Waiting for request");
                    try
                    {
                        var clientmessage = CProjectMakerLogicServer.ReceiveMultipartMessage();
                        var clientOriginalMessage = clientmessage[1].ConvertToString();
                        Console.WriteLine(clientOriginalMessage);
                        Request request = JsonSerializer.Deserialize<Request>(clientOriginalMessage);
                        Logic.CreateCProject(request);
                        LogMessage("CProjectMakerLogic got this request: " + request.PrettyPrint());
                        CProjectMakerLogicServer.SendFrame("");
                    }
                    catch (Exception e)
                    {
                        LogMessage("CProjectMakerLogic: " + e.Message);
                        Console.WriteLine(e.Message);                    
                    }

                }
            }
        }
        private void LogMessage(string message)
        {
            LoggerClient.SendFrame($"{{ \"Message\": \"{JsonSerializer.Serialize(message)}\" }}");
        }
    }
}
