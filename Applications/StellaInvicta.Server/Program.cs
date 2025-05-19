var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMagicOnion();

var app = builder.Build();

app.MapMagicOnionService(); // Add this line

app.Run();
