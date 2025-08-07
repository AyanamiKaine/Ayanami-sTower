using AyanamisTower.StellaEcs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            builder.Logging.AddConsole();

            builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                // This tells the serializer to include public fields in the JSON output.
                options.SerializerOptions.IncludeFields = true;
            });

            builder.Services.AddSingleton(world);

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
