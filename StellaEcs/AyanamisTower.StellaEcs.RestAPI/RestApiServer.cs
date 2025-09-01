using AyanamisTower.StellaEcs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models; // Required for OpenApiInfo
using System;
using System.Threading.Tasks;

namespace AyanamisTower.StellaEcs.Api
{
    /// <summary>
    /// A static class responsible for creating and managing the REST API web server.
    /// This class is dynamically loaded by the World to avoid a hard dependency on ASP.NET Core.
    /// </summary>
    public static class RestApiServer
    {
        private static WebApplication? _webApp;

        /// <summary>
        /// Starts the REST API server for the ECS World.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="url"></param>
        public static void Start(World world, string url = "http://localhost:5123")
        {
            if (_webApp != null)
            {
                Console.WriteLine("[RestApiServer] Server is already running.");
                return;
            }

            var builder = WebApplication.CreateBuilder();

            builder.WebHost.UseUrls(url);
            builder.Logging.ClearProviders();
            var inMemoryProvider = new InMemoryLogProvider(capacity: 4096);
            builder.Logging.AddProvider(inMemoryProvider);
            builder.Logging.AddConsole();

            builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                // This tells the serializer to include public fields in the JSON output.
                options.SerializerOptions.IncludeFields = true;
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            builder.Services.AddSingleton(world);
            builder.Services.AddSingleton<ILogStore>(inMemoryProvider);

            // --- SWAGGER INTEGRATION START ---
            // 1. Add the API Explorer service. It's essential for discovering endpoints, especially in minimal APIs.
            builder.Services.AddEndpointsApiExplorer();

            // 2. Add the Swagger generator service. This builds the Swagger/OpenAPI specification document.
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Stella ECS REST API",
                    Version = "v1",
                    Description = "An API for inspecting and interacting with a Stella ECS world in real-time."
                });
            });
            // --- SWAGGER INTEGRATION END ---

            var app = builder.Build();

            // Add a global exception handler for robustness.
            app.UseExceptionHandler(exceptionHandlerApp =>
                exceptionHandlerApp.Run(async context =>
                {
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var error = exceptionHandlerPathFeature?.Error;

                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsJsonAsync(new { message = "An unexpected server error occurred." });
                }));

            // --- SWAGGER UI CONFIGURATION START ---
            // 3. Enable middleware to serve the generated Swagger specification as a JSON endpoint.
            app.UseSwagger();

            // 4. Enable middleware to serve the Swagger UI (HTML, JS, CSS, etc.).
            app.UseSwaggerUI(c =>
            {
                // Point the UI to the generated swagger.json endpoint.
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Stella ECS API V1");
                // Serve the Swagger UI from the application root (e.g., http://localhost:5123/).
                c.RoutePrefix = string.Empty;
            });
            // --- SWAGGER UI CONFIGURATION END ---


            // Use our clean, organized endpoint mapping.
            app.MapEcsEndpoints();

            _webApp = app;

            Task.Run(() =>
            {
                _webApp.Run();
            });
        }
        /// <summary>
        /// Stops the REST API server if it is running.
        /// </summary>
        /// <returns></returns>
        public static async Task Stop()
        {
            if (_webApp != null)
            {
                await _webApp.StopAsync();
                _webApp = null;
            }
        }
    }
}
