namespace AyanamisTower.StellaEcs.Api;

/// <summary>
/// A static class responsible for creating and managing the REST API web server.
/// This class is dynamically loaded by the World to avoid a hard dependency on ASP.NET Core.
/// </summary>
public static class RestApiServer
{
    private static WebApplication? _webApp;

    /// <summary>
    /// Starts the REST API server.
    /// </summary>
    /// <param name="world">The ECS World instance to expose through the API.</param>
    /// <param name="url">The URL for the server to listen on.</param>
    public static void Start(World world, string url = "http://localhost:5123")
    {
        if (_webApp != null)
        {
            Console.WriteLine("[RestApiServer] Server is already running.");
            return;
        }

        var builder = WebApplication.CreateBuilder();

        // Configure the web server to listen on the specified URL
        builder.WebHost.UseUrls(url);

        // Add the World instance to the dependency injection container as a singleton.
        // This makes it available to all API endpoints.
        builder.Services.AddSingleton(world);

        var app = builder.Build();

        // --- Define API Endpoints ---

        app.MapGet("/api/world/status", (World w) => Results.Ok(w.GetWorldStatus()));

        app.MapGet("/api/systems", (World w) => Results.Ok(w.GetSystems()));

        app.MapGet("/api/entities", (World w) =>
        {
            var entities = w.GetAllEntities().Select(e => new { e.Id, e.Generation });
            return Results.Ok(entities);
        });

        app.MapGet("/api/entities/{id}:{generation}", (uint id, int generation, World w) =>
        {
            var entity = new Entity(id, generation, w);
            if (!w.IsEntityValid(entity))
            {
                return Results.NotFound(new { message = $"Entity {id}:{generation} is not valid." });
            }

            var components = w.GetAllComponentsForEntity(entity);
            return Results.Ok(new
            {
                entity = new { entity.Id, entity.Generation },
                components
            });
        });

        app.MapGet("/api/components", (World w) => Results.Ok(w.GetComponentTypes()));


        _webApp = app;

        // Run the web host in a background thread so it doesn't block the main application loop.
        Task.Run(() => _webApp.RunAsync());

        Console.WriteLine($"[RestApiServer] StellaEcs REST API is now listening on {url}");
    }

    /// <summary>
    /// Stops the REST API server if it is running.
    /// </summary>
    public static async Task Stop()
    {
        if (_webApp != null)
        {
            await _webApp.StopAsync();
            _webApp = null;
            Console.WriteLine("[RestApiServer] Server stopped.");
        }
    }
}
