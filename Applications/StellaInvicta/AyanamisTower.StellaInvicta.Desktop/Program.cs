using System.Timers;
using AyanamisTower.StellaInvicta.Shared;
using Grpc.Net.Client;
using MagicOnion.Client;

// Connect to the server using gRPC channel.
var channel = GrpcChannel.ForAddress("http://localhost:5254");

// Create a proxy to call the server transparently.
var client = MagicOnionClient.Create<IMyFirstService>(channel);



System.Timers.Timer timer = new(TimeSpan.FromMilliseconds(100));

timer.Elapsed += async (_, _) =>
{
    var result = await client.SumAsync(123, 456);
    Console.WriteLine($"Result: {result}");
};

timer.Start();

while (true)
{
    await Task.Delay(100);
}