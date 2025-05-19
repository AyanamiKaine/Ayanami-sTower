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

var app = builder.Build();

// Map MagicOnion services.
app.MapMagicOnionService();

app.Run();
