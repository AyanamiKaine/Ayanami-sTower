using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace AsyncNamedPipesExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Start the server in a separate task
            Task serverTask = RunServerAsync();

            // Give the server a little time to start
            await Task.Delay(500);

            // Start the client
            await RunClientAsync();

            // Wait for the server to finish (optional)
            await serverTask;

            Console.WriteLine("Press any key to exit.");
            Console.Read();
        }

        static async Task RunServerAsync()
        {
            try
            {
                using NamedPipeServerStream server = new("MyPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                Console.WriteLine("[Server] Waiting for client connection...");
                await server.WaitForConnectionAsync(); // Asynchronously wait for a client

                Console.WriteLine("[Server] Client connected.");

                // Read data asynchronously
                StreamReader reader = new(server);
                string message = await reader.ReadLineAsync() ?? "";
                Console.WriteLine($"[Server] Received: {message}");

                // Write data asynchronously
                StreamWriter writer = new StreamWriter(server);
                writer.AutoFlush = true;
                await writer.WriteLineAsync("Hello from server!");

                // Keep the connection open for a bit to demonstrate non-blocking behavior
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Error: {ex.Message}");
            }
        }

        static async Task RunClientAsync()
        {
            try
            {
                using NamedPipeClientStream client = new(".", "MyPipe", PipeDirection.InOut, PipeOptions.Asynchronous);
                Console.WriteLine("[Client] Connecting to server...");
                await client.ConnectAsync(); // Asynchronously connect to the server

                Console.WriteLine("[Client] Connected to server.");

                // Write data asynchronously
                StreamWriter writer = new(client)
                {
                    AutoFlush = true
                };
                await writer.WriteLineAsync("Hello from client!");

                // Read data asynchronously
                StreamReader reader = new StreamReader(client);
                string response = await reader.ReadLineAsync() ?? "";
                Console.WriteLine($"[Client] Received: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Error: {ex.Message}");
            }
        }
    }
}