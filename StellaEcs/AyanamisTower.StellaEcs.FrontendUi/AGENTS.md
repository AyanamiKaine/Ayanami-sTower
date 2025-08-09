# GOAL
# Repository Guidelines

This repository hosts a frontend UI built with Astro and Svelte UI components that consumes a local REST API (default: `http://localhost:5123/api`). Back-end code lives elsewhere—do not add or modify server code here.

## Project Structure & Module Organization
- `src/pages/`: Astro pages and routes.
- `src/components/`: Reusable Svelte and Astro components.
- `src/layouts/`: Site layouts and shells.
- `src/lib/`: Client helpers (API client, stores, types).
- `public/`: Static assets served as-is.
- `tests/`: Unit/e2e tests (e.g., `tests/unit`, `tests/e2e`).

## Build, Test, and Development Commands
- `bun install`: Install dependencies (uses Bun).
- `bun run dev`: Start Astro dev server (hot reload).
- `bun run build`: Production build to `dist/`.
- `bun run preview`: Serve the production build locally.
- `bun run test`: Run unit tests (Vitest).
- `bun run e2e`: Run end-to-end tests (Playwright), if configured.

## Coding Style & Naming Conventions
- Indentation: 2 spaces; line width 100.
- Languages: TypeScript for `.ts/.svelte`, Astro `.astro` components.
- Components: `PascalCase.svelte` / `PascalCase.astro`; utilities `camelCase.ts`.
- Lint/format: Prettier + ESLint + `svelte-check`.
- Scripts: `bun run lint`, `bun run format`, `bun run check` before commits.
- For styling use tailwind-css.
## Testing Guidelines
- Frameworks: Vitest for unit; Playwright for e2e.
- Naming: `*.test.ts` (unit) and `*.spec.ts` (e2e).
- Location: Mirror source in `tests/unit/**` and `tests/e2e/**`.
- Coverage: Aim ≥80% for critical helpers in `src/lib/`.

## Commit & Pull Request Guidelines
- Commits: Conventional Commits (e.g., `feat: add entity grid`).
- PRs: Clear description, linked issue, screenshots/GIFs for UI, steps to verify, notes on a11y/perf.
- Keep PRs scoped; include updated tests and docs.

## Security & Configuration Tips
- API base URL via env: set `PUBLIC_API_BASE_URL` (example: `http://localhost:5123/api`).
- Do not commit secrets; use `.env` and `.env.example` for safe defaults.
- Follow CORS rules; this repo must not change server behavior.
- Set package manager in `package.json`: `"packageManager": "bun@1.x"`.

## Agent-Specific Instructions
- Focus on Astro/Svelte UI only; never modify backend.
- Prefer small composable components, typed API client, and optimistic UI for entity/component actions.
- Use Bun tooling (`bunx astro add svelte`) for integrations when scaffolding.
Create a Frontend UI using astro.js and svelte for ui-components. 

You will consume an REST API found at local host. Both front end and back end server will run on the same machine locally. We want a good looking and interactive side. Do not try writing any backend code you dont own that.

# Backend Code. 
Here is the backend code for your refrence. 
```C#
using AyanamisTower.StellaEcs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AyanamisTower.StellaEcs.Api
{
    /// <summary>
    /// A static class to define and map all API endpoints for the ECS World.
    /// This promotes separation of concerns and keeps the server startup logic clean.
    /// </summary>
    public static class EcsApiEndpoints
    {
        /// <summary>
        /// Maps the ECS API endpoints to the provided IEndpointRouteBuilder.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IEndpointRouteBuilder MapEcsEndpoints(this IEndpointRouteBuilder app)
        {
            // Grouping endpoints with a common prefix and tag for better organization in Swagger UI.
            var api = app.MapGroup("/api")
                         .WithTags("ECS World");

            // --- World, System, and Component Endpoints ---

            api.MapGet("/world/status", (World w) => Results.Ok(w.GetWorldStatus()))
               .WithName("GetWorldStatus")
               .WithSummary("Gets the current status of the ECS world.")
               .WithDescription("Provides high-level information about the world, such as entity and system counts.")
               .Produces<WorldStatusDto>(StatusCodes.Status200OK);

            api.MapGet("/systems", (World w) => Results.Ok(w.GetSystems()))
               .WithName("GetRegisteredSystems")
               .WithSummary("Retrieves a list of all registered systems.")
               .Produces<IEnumerable<SystemInfoDto>>(StatusCodes.Status200OK);

            api.MapGet("/components", (World w) => Results.Ok(w.GetComponentTypes()))
               .WithName("GetRegisteredComponentTypes")
               .WithSummary("Retrieves a list of all registered component types and their owners.")
               .Produces<IEnumerable<ComponentInfoDto>>(StatusCodes.Status200OK);

            // --- Entity Endpoints ---

            api.MapGet("/entities", (World w, HttpContext context) =>
            {
                var request = context.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";

                var entities = w.GetAllEntities().Select(e => new EntitySummaryDto
                {
                    Id = e.Id,
                    Url = $"{baseUrl}/api/entities/{e.Id}"
                });
                return Results.Ok(entities);
            })
            .WithName("GetAllValidEntities")
            .WithSummary("Retrieves a summary of all valid entities.")
            .WithDescription("Returns a list of all entities with their ID and a URL to their detailed view.")
            .Produces<IEnumerable<EntitySummaryDto>>(StatusCodes.Status200OK);

            api.MapPost("/entities", (World w, HttpContext context) =>
            {
                var entity = w.CreateEntity();
                var request = context.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";

                var entitySummary = new EntitySummaryDto
                {
                    Id = entity.Id,
                    Url = $"{baseUrl}/api/entities/{entity.Id}"
                };

                return Results.Created($"{baseUrl}/api/entities/{entity.Id}", entitySummary);
            })
            .WithName("CreateEntity")
            .WithSummary("Creates a new entity.")
            .WithDescription("Creates a new entity and returns its summary information.")
            .Produces<EntitySummaryDto>(StatusCodes.Status201Created);

            api.MapGet("/entities/{entityId}", (string entityId, World w) =>
            {
                if (!uint.TryParse(entityId, out var id))
                {
                    return Results.BadRequest(new { message = "Invalid entity ID format. Expected '{id}'." });
                }

                var entity = new Entity(id, w);
                if (!w.IsEntityValid(entity))
                {
                    return Results.NotFound(new { message = $"Entity {entityId} is not valid." });
                }

                var components = w.GetAllComponentsForEntity(entity);
                var entityDetails = new EntityDetailDto
                {
                    Id = entity.Id,
                    Components = components
                };

                return Results.Ok(entityDetails);
            })
            .WithName("GetEntityDetails")
            .WithSummary("Gets detailed information for a specific entity.")
            .WithDescription("Retrieves the components attached to a single entity, identified by its composite ID '{id}-{generation}'.")
            .Produces<EntityDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

            api.MapDelete("/entities/{entityId}", (string entityId, World w) =>
            {
                if (!uint.TryParse(entityId, out var id))
                {
                    return Results.BadRequest(new { message = "Invalid entity ID format. Expected '{id}'." });
                }

                var entity = new Entity(id, w);
                if (!w.IsEntityValid(entity))
                {
                    return Results.NotFound(new { message = $"Entity {entityId} is not valid." });
                }

                try
                {
                    w.DestroyEntity(entity);
                    return Results.Ok(new { message = $"Entity {entityId} successfully deleted." });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("DeleteEntity")
            .WithSummary("Deletes an entity.")
            .WithDescription("Destroys the specified entity and removes all its components.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

            api.MapPost("/entities/{entityId}/components/{componentTypeName}", (string entityId, string componentTypeName, World w, [FromBody] JsonElement componentData) =>
            {
                if (!uint.TryParse(entityId, out var id))
                {
                    return Results.BadRequest(new { message = "Invalid entity ID format. Expected '{id}'." });
                }

                var entity = new Entity(id, w);
                if (!w.IsEntityValid(entity))
                {
                    return Results.NotFound(new { message = $"Entity {entityId} is not valid." });
                }

                try
                {
                    bool success = w.SetComponentFromJson(entity, componentTypeName, componentData);
                    if (success)
                    {
                        return Results.Ok(new { message = $"Component '{componentTypeName}' successfully added to entity {entityId}." });
                    }
                    else
                    {
                        return Results.BadRequest(new { message = $"Failed to add component '{componentTypeName}' to entity {entityId}." });
                    }
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
                catch (JsonException ex)
                {
                    return Results.BadRequest(new { message = $"Invalid JSON data: {ex.Message}" });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("AddComponentToEntity")
            .WithSummary("Adds or updates a component on an entity.")
            .WithDescription("Sets a component on the specified entity using JSON data. The component type must be a struct and available in loaded assemblies.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

            api.MapDelete("/entities/{entityId}/components/{componentTypeName}", (string entityId, string componentTypeName, World w) =>
            {
                if (!uint.TryParse(entityId, out var id))
                {
                    return Results.BadRequest(new { message = "Invalid entity ID format. Expected '{id}'." });
                }

                var entity = new Entity(id, w);
                if (!w.IsEntityValid(entity))
                {
                    return Results.NotFound(new { message = $"Entity {entityId} is not valid." });
                }

                try
                {
                    bool success = w.RemoveComponentByName(entity, componentTypeName);
                    if (success)
                    {
                        return Results.Ok(new { message = $"Component '{componentTypeName}' successfully removed from entity {entityId}." });
                    }
                    else
                    {
                        return Results.BadRequest(new { message = $"Failed to remove component '{componentTypeName}' from entity {entityId}." });
                    }
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("RemoveComponentFromEntity")
            .WithSummary("Removes a component from an entity.")
            .WithDescription("Removes the specified component type from the entity if it exists.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

            // --- NEW: Service Endpoints ---

            api.MapGet("/services", (World w) => Results.Ok(w.GetServices()))
               .WithName("GetRegisteredServices")
               .WithSummary("Retrieves a list of all registered services and their methods.")
               .Produces<IEnumerable<ServiceInfoDto>>(StatusCodes.Status200OK);

            api.MapPost("/services/{serviceTypeName}/{methodName}", (string serviceTypeName, string methodName, World w, [FromBody] Dictionary<string, object> parameters) =>
            {
                try
                {
                    var result = w.InvokeServiceMethod(serviceTypeName, methodName, parameters);
                    // If the method returns void, the result will be null. Return an OK status with no content.
                    // Otherwise, return the result serialized as JSON.
                    return result != null ? Results.Ok(result) : Results.Ok();
                }
                catch (KeyNotFoundException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (MissingMethodException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    // Catch any other exceptions during method invocation.
                    return Results.Problem(ex.InnerException?.Message ?? ex.Message);
                }
            })
            .WithName("InvokeServiceMethod")
            .WithSummary("Invokes a method on a registered service.")
            .WithDescription("Dynamically calls a public method on a service. The request body should be a JSON object mapping parameter names to their values. Use the full type name from the GET /services endpoint.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

            // --- Plugin Endpoints ---

            api.MapGet("/plugins", (World w, HttpContext context) =>
                        {
                            var request = context.Request;
                            var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
                            var plugins = w.GetPlugins().Select(p =>
                            {
                                p.Url = $"{baseUrl}{p.Url}"; // Create the full, absolute URL
                                return p;
                            });
                            return Results.Ok(plugins);
                        })
                           .WithName("GetLoadedPlugins")
                           .WithSummary("Retrieves a list of all loaded plugins.")
                           .Produces<IEnumerable<PluginInfoDto>>(StatusCodes.Status200OK);

            api.MapPost("/systems/{systemName}/disable", (string systemName, World w) =>
            {
                if (string.IsNullOrWhiteSpace(systemName))
                {
                    return Results.BadRequest(new { message = "System name must be provided." });
                }

                var ok = w.DisableSystemByName(systemName);
                return ok
                    ? Results.Ok(new { message = $"System '{systemName}' disabled." })
                    : Results.NotFound(new { message = $"System '{systemName}' not found." });
            })
            .WithName("DisableSystemByName")
            .WithSummary("Disables a system by its name.")
            .WithDescription("Sets the system's Enabled flag to false for the system with the given Name.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

            // NEW: Endpoint for getting plugin details
            api.MapGet("/plugins/{pluginPrefix}", (string pluginPrefix, World w) =>
            {
                var details = w.GetPluginDetails(pluginPrefix);
                return details != null ? Results.Ok(details) : Results.NotFound(new { message = $"Plugin with prefix '{pluginPrefix}' not found." });
            })
               .WithName("GetPluginDetails")
               .WithSummary("Gets detailed information for a specific plugin.")
               .WithDescription("Retrieves the systems, services, and components registered by a single plugin.")
               .Produces<PluginDetailDto>(StatusCodes.Status200OK)
               .Produces(StatusCodes.Status404NotFound);
            return app;

            
        }
    }
}


using AyanamisTower.StellaEcs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            builder.Logging.AddConsole();

            builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                // This tells the serializer to include public fields in the JSON output.
                options.SerializerOptions.IncludeFields = true;
            });

            builder.Services.AddSingleton(world);

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



#region Data Transfer Objects (DTOs) for Public API

/// <summary>
/// A DTO for exposing basic entity information, typically in a list.
/// </summary>
public class EntitySummaryDto
{
    /// <summary>
    /// The unique identifier for the entity.
    /// </summary>
    public uint Id { get; set; }
    /// <summary>
    /// The generation of the entity
    /// </summary>
    public int Generation { get; set; }
    /// <summary>
    /// A direct link to the detailed view of this entity.
    /// </summary>
    public required string Url { get; set; }
}

/// <summary>
/// A DTO for exposing the full details of a single entity.
/// </summary>
public class EntityDetailDto
{
    /// <summary>
    /// The unique identifier for the entity.
    /// </summary>
    public uint Id { get; set; }
    /// <summary>
    /// A direct link to the detailed view of this entity.
    /// </summary>
    public required List<ComponentInfoDto> Components { get; set; }
}

/// <summary>
/// A DTO for exposing the world's status.
/// </summary>
public class WorldStatusDto
{
    /// <summary>
    /// The maximum number of entities that can be created in this world.
    /// </summary>
    public uint MaxEntities { get; set; }
    /// <summary>
    /// The number of entity IDs that have been recycled and can be reused.
    /// </summary>
    public int RecycledEntityIds { get; set; }
    /// <summary>
    /// The number of systems that are currently registered in the world.
    /// </summary>
    public int RegisteredSystems { get; set; }
    /// <summary>
    /// The number of component types that are currently registered in the world.
    /// </summary>
    public int ComponentTypes { get; set; }
}

/// <summary>
/// A DTO for exposing system information.
/// </summary>
public class SystemInfoDto
{
    /// <summary>
    /// The name of the system.
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Indicates whether the system is currently enabled.
    /// </summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// The owner of the plugin that provides this system.
    /// </summary>
    public required string PluginOwner { get; set; }

}
/// <summary>
/// A DTO for exposing component information.
/// </summary>
public class ComponentInfoDto
{
    /// <summary>
    /// The name of the component type.
    /// </summary>
    public required string TypeName { get; set; }
    // We could add a 'Data' object here, but it requires serialization logic.
    /// <summary>
    /// The data associated with the component, if applicable.
    /// </summary>
    public object? Data { get; set; }
    /// <summary>
    /// The owner of the plugin that provides this component.
    /// </summary>
    public string? PluginOwner { get; set; }

}

/// <summary>
/// A DTO for exposing service information.
/// </summary>
public class ServiceInfoDto
{
    /// <summary>
    /// The full type name of the service, used for invoking methods.
    /// </summary>
    public required string TypeName { get; set; }

    /// <summary>
    /// A list of public methods available on the service.
    /// </summary>
    public required IEnumerable<string> Methods { get; set; }
    /// <summary>
    /// The owner of the plugin that provides this service.
    /// </summary>
    public required string PluginOwner { get; set; }
}

/// <summary>
/// A DTO for exposing detailed plugin information.
/// </summary>
public class PluginDetailDto : PluginInfoDto
{
    /// <summary>
    /// A list of systems provided by the plugin.
    /// </summary>
    public required IEnumerable<string> Systems { get; set; }
    /// <summary>
    /// A list of services provided by the plugin.
    /// </summary>
    public required IEnumerable<string> Services { get; set; }
    /// <summary>
    /// A list of components provided by the plugin.
    /// </summary>
    public required IEnumerable<string> Components { get; set; }
}

/// <summary>
/// A DTO for exposing plugin information.
/// </summary>
public class PluginInfoDto
{
    /// <summary>
    /// The name of the plugin.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The version of the plugin.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// The author of the plugin.
    /// </summary>
    public required string Author { get; set; }

    /// <summary>
    /// A description of what the plugin does.
    /// </summary>
    public required string Description { get; set; }
    /// <summary>
    /// The unique prefix used for this plugin's systems and services.
    /// </summary>
    public required string Prefix { get; set; }
    /// <summary>
    /// The URL for accessing this plugin's API endpoints.
    /// </summary>
    public required string Url { get; set; }
}

#endregion

```
