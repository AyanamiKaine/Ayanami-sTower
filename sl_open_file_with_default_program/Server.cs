using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using NetMQ;
using NetMQ.Sockets;

namespace OpenFileWithDefaultProgramComponent
{
    public class Server
    {
        private DealerSocket LoggerClient = new DealerSocket(">tcp://localhost:60010");
        private RouterSocket OpenFileWithDefaultProgramServer   = new("@tcp://localhost:60001");
        public void Run()
        {
            // Receive the message from the server socket
            LogMessage("Starting Open File With Default Program Component");
            while (true)
            {
                var json_request = OpenFileWithDefaultProgramServer.ReceiveMultipartMessage();

                string filePath = json_request[1].ConvertToString();

                if (!File.Exists(filePath))
                {
                    //LogMessage(@$"Default Program Component: File path does not exist: {JsonSerializer.Serialize(filePath)}");
                }
                else
                {
                    //LogMessage(@$"Default Program Component: Trying to open the file at path: {JsonSerializer.Serialize(filePath)}");
                    if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {

                        // Correctly encapsulating the filepath with "" otherwise xdg will complain
                        filePath = '"' + filePath + '"';

                        Process.Start("xdg-open", filePath);
                    }

                    if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Start("explorer.exe", filePath);
                    }
                }
            }
        }
        private void LogMessage(string message)
        {
            LoggerClient.SendFrame($"{{ \"Message\": \"{message}\" }}");
        }
    }
}
