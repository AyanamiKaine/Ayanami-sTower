using System.Collections.Concurrent;
using System.IO.Pipes;
using MessagePack;

namespace Mens;

public interface IAddress
{
    public string Address { get; set; }
}

/*

Actors behavor based on their internal state and outside stimula (We model this in form of messages)

*/


public class Vita<State, Message>(Guid guid = new(), string address = """\\.\pipe\HelloWorld""")
{
    public Guid PID { get; set; } = guid;
    public string Address { get; set; } = address;
    private readonly ConcurrentQueue<Message> _messageQueue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly AutoResetEvent _messageReceivedEvent = new(false);

    /// <summary>
    /// Moves to the next step
    /// </summary>
    public void Step(State state)
    {
        var message = GetNextMessage();

        if (message is not null)
            HandleMessage(state, message);
        else
            HandleState(state);
    }

    /// <summary>
    /// What is the purpose of HandleState?
    /// 
    /// It takes insperation from erlang/elixier where state is not something encapsulated in the class
    /// itself but instead be part of the step of the actor.
    /// 
    /// </summary>
    /// <param name="state"></param>
    public virtual void HandleState(State state)
    {
        switch (state)
        {
            default:
                Console.WriteLine("No State Logic Provided");
                break;
        }
    }

    public virtual void HandleMessage(State state, Message message)
    {
        switch (message)
        {
            default:
                Console.WriteLine("No Handle Message Logic Provided");
                break;
        }
    }

    /// <summary>
    /// 
    /// The default implementation uses the namedpipes class
    /// 
    /// Use public new void ReceiveMessage() // To Hide Vita.ReceiveMessage
    /// and to define your own implementation.
    /// </summary>
    public void ReceiveMessage()
    {
        // Start listening for messages in a separate task
        Task.Run(() => StartListeningForMessages(_cancellationTokenSource.Token));
    }

    public static async Task SendMessageAsync<T>(string pipeAddress, T message)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", pipeAddress, PipeDirection.Out, PipeOptions.Asynchronous);
            await client.ConnectAsync();

            // Serialize the message using MessagePack
            var messagePackData = MessagePackSerializer.Serialize(message);

            // Send the serialized data
            await client.WriteAsync(messagePackData, 0, messagePackData.Length);
            Console.WriteLine($"Message sent to {pipeAddress}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message to {pipeAddress}: {ex.Message}");
        }
    }

    private async Task StartListeningForMessages(CancellationToken cancellationToken)
    {

        using var server = new NamedPipeServerStream(
            Address,
            PipeDirection.In,
            1, // maxNumberOfServerInstances
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous
        );

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await server.WaitForConnectionAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested) return;

                // Read the incoming message
                var buffer = new byte[4096]; // Adjust buffer size as needed
                var bytesRead = await server.ReadAsync(buffer, cancellationToken);
                var messagePackData = new byte[bytesRead];
                Array.Copy(buffer, messagePackData, bytesRead);

                EnqueueMessage(messagePackData);
            }
            catch (OperationCanceledException)
            {
                // Expected when the cancellation token is signaled
                break;
            }
            catch (Exception ex)
            {
                // Handle pipe errors (log, reconnect, etc.)
                Console.WriteLine($"Error receiving message: {ex.Message}");

                // Optional: Implement a backoff strategy before attempting to reconnect
                await Task.Delay(1000, cancellationToken); // Wait for 1 second before retrying
            }
        }
    }

    private void EnqueueMessage(byte[] messagePackData)
    {
        try
        {
            // Deserialize the MessagePack data to a dynamic object (you can specify a concrete type if you know the schema)
            var message = MessagePackSerializer.Deserialize<Message>(messagePackData);
            _messageQueue.Enqueue(message);
            _messageReceivedEvent.Set(); // Signal that a message has been received
        }
        catch (MessagePackSerializationException ex)
        {
            // Handle deserialization errors (log, throw, etc.)
            Console.WriteLine($"Error deserializing MessagePack data: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Handle other potential exceptions
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }

    private Message? GetNextMessage()
    {
        if (_messageQueue.TryDequeue(out var message))
        {
            return message;
        }

        return default;
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _messageReceivedEvent.Set(); // Ensure any waiting thread can exit
        _messageReceivedEvent.Dispose();
    }
}
