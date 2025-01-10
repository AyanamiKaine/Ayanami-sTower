using System.IO.Pipes;
using Mens;
using MessagePack;

Anima anima = new();
anima.ReceiveMessage();

TestState state = new();


try
{
    using var client = new NamedPipeClientStream(".", """\\.\pipe\HelloWorld""", PipeDirection.Out, PipeOptions.Asynchronous);
    await client.ConnectAsync();

    var message = new TestMessage("HELLO WORLD");

    // Serialize the message using MessagePack
    var messagePackData = MessagePackSerializer.Serialize(message);

    // Send the serialized data
    await client.WriteAsync(messagePackData, 0, messagePackData.Length);
    Console.WriteLine($"Message sent to {"""\\.\pipe\HelloWorld"""}");

}
catch (Exception ex)
{
    Console.WriteLine($"Error sending message to {"""\\.\pipe\HelloWorld"""}: {ex.Message}");
}

Console.ReadKey();

anima.Step(state);


