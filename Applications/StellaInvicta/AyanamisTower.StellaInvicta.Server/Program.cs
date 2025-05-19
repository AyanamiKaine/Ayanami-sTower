using AyanamisTower.StellaInvicta.Server.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core; // Required for HttpProtocols

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on the specified port and enable HTTP/2.
// This is crucial for gRPC (and thus MagicOnion) to work over cleartext HTTP.
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5254, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2; // Enable HTTP/2, Magic onion/Grpc depends on it
    });
});

// Add MagicOnion services.
builder.Services.AddGrpc(); // MagicOnion runs on gRPC
builder.Services.AddMagicOnion();

// --- Register Game State and Game Loop Service ---
// Register GameState as a singleton so it's shared
builder.Services.AddSingleton<GameState>();

// Register the GameLoopService as a hosted service
builder.Services.AddHostedService<GameLoopService>();

var app = builder.Build();

// Map MagicOnion services.
app.MapMagicOnionService();


app.Run();
