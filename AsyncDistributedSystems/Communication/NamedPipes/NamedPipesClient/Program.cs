using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

string PipeName = "MyNamedPipe";
Console.WriteLine("Starting named pipe client...");

try
{
    await using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

    Console.WriteLine("Connecting to server...");
    await client.ConnectAsync(5000); // 5 seconds timeout

    Console.WriteLine("Connected to server.");

    // Send a message to the server
    var message = Encoding.UTF8.GetBytes("Hello from client!");
    await client.WriteAsync(message, 0, message.Length);

    Console.WriteLine("Message sent.");

    // Read the response from the server
    var buffer = new byte[1024];
    var bytesRead = await client.ReadAsync(buffer, 0, buffer.Length);
    var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

    Console.WriteLine($"Received from server: {response}");
}
catch (TimeoutException ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
}
catch (IOException ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
}
