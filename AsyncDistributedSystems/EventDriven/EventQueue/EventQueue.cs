using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace EventQueue;


/*

We want to implement an event queue that works with the publish and subscribe pattern. 
Services connect not to other services but instead to only one event queue service. 

All services connected to one event queue represent one system boundry. 
If we just have one event-queue then we only have one system.

*/


public class EventMessage(string topic, byte[] jsonByteData)
{

    // We want to use messagepack and only deserialize the first field that will contain the topic
    // the rest should be simply attached as the rest of the bytedata
    public string Topic { get; set; } = topic;
    public byte[] JsonByteData { get; set; } = jsonByteData;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class EventQueueService
{
    private readonly BlockingCollection<EventMessage> _queue = [];

    // Given the name of a client as a key, it gives us the list of subscribed topics the client is interested in
    private readonly Dictionary<string, HashSet<string>> ClientsSubscribedToTopics = [];

    public void Enqueue(EventMessage eventMessage)
    {
        _queue.Add(eventMessage);
    }

    public EventMessage Dequeue(CancellationToken cancellationToken)
    {
        return _queue.Take(cancellationToken);
    }

    public int Count => _queue.Count;


    public async Task RunAsync(string address = "/tmp/my_unix_socket")
    {


        string socketPath = address; // Choose a suitable path
        var endpoint = new UnixDomainSocketEndPoint(socketPath);

        if (File.Exists(socketPath))
            File.Delete(socketPath);

        // 2. Create a socket
        using var serverSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Unspecified);

        try
        {
            // 3. Bind the socket to the endpoint
            serverSocket.Bind(endpoint);

            // 4. Listen for incoming connections
            serverSocket.Listen(10); // Backlog queue size
            Console.WriteLine($"Server listening on {socketPath}");

            while (true)
            {
                // 5. Accept a client connection (asynchronously)
                var clientSocket = await serverSocket.AcceptAsync();
                Console.WriteLine("Client connected.");

                // 6. Handle the client (in a separate task)
                _ = Task.Run(async () =>
                {
                    using (clientSocket)
                    {
                        var buffer = new byte[1024];
                        while (true)
                        {
                            // 7. Receive data (asynchronously)
                            var bytesReceived = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);
                            if (bytesReceived == 0) break; // Client disconnected

                            var message = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                            Console.WriteLine($"Received: {message}");

                            // 8. Send data back (echo)
                            await clientSocket.SendAsync(buffer.AsMemory(0, bytesReceived), SocketFlags.None);
                        }
                    }
                    Console.WriteLine("Client disconnected.");
                });
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            // 9. Clean up the socket file (important!)
            if (File.Exists(socketPath))
            {
                File.Delete(socketPath);
            }
        }
    }

    /// <summary>
    /// By Default when no topic is subscribed, it will subscribe to ALL Topics
    /// </summary>
    /// <param name="clientName"></param>
    /// <param name="topic"></param>
    private void Subscribe(string clientName, string topic = "*")
    {
        ClientsSubscribedToTopics[clientName].Add(topic);
    }

    private void Unsubscribe(string clientName, string topic)
    {
        ClientsSubscribedToTopics[clientName].Remove(topic);
    }
}
