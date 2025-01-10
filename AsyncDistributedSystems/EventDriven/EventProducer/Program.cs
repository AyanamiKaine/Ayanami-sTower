// 1. Create a Unix Domain Socket path (must match the server)
using System.Net.Sockets;
using System.Text;

string socketPath = "/tmp/my_unix_socket";
var endpoint = new UnixDomainSocketEndPoint(socketPath);

// 2. Create a socket
using var clientSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Unspecified);

try
{
    // 3. Connect to the server (asynchronously)
    clientSocket.Connect(endpoint);
    Console.WriteLine("Connected to server.");

    while (true)
    {
        // 4. Get input from the console
        Console.Write("Enter message (or 'exit'): ");
        var message = Console.ReadLine();
        if (message?.ToLower() == "exit") break;

        // 5. Send data (asynchronously)
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await clientSocket.SendAsync(messageBytes, SocketFlags.None);

        // 6. Receive data (asynchronously)
        var buffer = new byte[1024];
        var bytesReceived = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);
        var response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
        Console.WriteLine($"Server replied: {response}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex}");
}