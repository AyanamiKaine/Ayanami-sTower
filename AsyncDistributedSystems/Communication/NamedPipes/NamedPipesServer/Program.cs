using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

string PipeName = "MyNamedPipe";

Console.WriteLine("Starting named pipe server...");

while (true) // Keep the server running indefinitely
{
    try
    {
        await using var server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        Console.WriteLine("Waiting for client connection...");
        await server.WaitForConnectionAsync();

        Console.WriteLine("Client connected.");

        // Read data from the client
        var buffer = new byte[1024];
        var bytesRead = await server.ReadAsync(buffer, 0, buffer.Length);
        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        Console.WriteLine($"Received from client: {message}");

        // Send a response back to the client
        var response = Encoding.UTF8.GetBytes("Message received!");
        await server.WriteAsync(response, 0, response.Length);

        Console.WriteLine("Response sent.");

        // Optional: Close the connection after each message exchange
        // server.Disconnect();
    }
    catch (IOException ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
    }
}