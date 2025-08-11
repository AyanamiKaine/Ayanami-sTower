using AyanamisTower.StellaEcs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
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

            // --- World Control Endpoints ---
            api.MapPost("/world/pause", (World w) => { w.Pause(); return Results.Ok(new { message = "World paused" }); })
                .WithName("PauseWorld").WithSummary("Pauses world updates").Produces<object>(StatusCodes.Status200OK);
            api.MapPost("/world/resume", (World w) => { w.Resume(); return Results.Ok(new { message = "World resumed" }); })
                .WithName("ResumeWorld").WithSummary("Resumes world updates").Produces<object>(StatusCodes.Status200OK);
            api.MapPost("/world/step", (World w, HttpContext ctx) =>
            {
                var q = ctx.Request.Query;
                var frames = int.TryParse(q["frames"], out var f) ? Math.Clamp(f, 1, 10_000) : 1;
                var dt = float.TryParse(q["dt"], out var d) ? Math.Clamp(d, 0f, 10f) : Math.Max(0.0001f, w.LastDeltaTime);
                for (int i = 0; i < frames; i++) w.Step(dt);
                return Results.Ok(new { message = $"Stepped {frames} frame(s)", w.Tick, DeltaTime = w.LastDeltaTime });
            })
                .WithName("StepWorld").WithSummary("Steps the world while paused").Produces<object>(StatusCodes.Status200OK);

            api.MapGet("/systems", (World w) => Results.Ok(w.GetSystems()))
               .WithName("GetRegisteredSystems")
               .WithSummary("Retrieves a list of all registered systems.")
               .Produces<IEnumerable<SystemInfoDto>>(StatusCodes.Status200OK);

            // --- Logs Endpoints ---
            api.MapGet("/logs", (HttpContext ctx, [FromServices] ILogStore logs) =>
            {
                var q = ctx.Request.Query;
                var take = int.TryParse(q["take"], out var t) ? Math.Clamp(t, 1, 2000) : 200;
                var afterId = long.TryParse(q["afterId"], out var a) ? Math.Max(0, a) : 0;
                LogLevel? min = null;
                if (Enum.TryParse<LogLevel>(q["minLevel"], ignoreCase: true, out var parsed)) min = parsed;
                var category = q["category"].ToString();
                if (string.IsNullOrWhiteSpace(category)) category = null;
                var result = logs.GetTail(take, afterId, min, category);
                return Results.Ok(result);
            })
            .WithName("TailLogs")
            .WithSummary("Returns recent logs with optional filters.")
            .WithDescription("Query: take (<=2000), afterId, minLevel (Trace..Critical), category (substring)")
            .Produces<IEnumerable<LogEntry>>(StatusCodes.Status200OK);

            api.MapDelete("/logs", ([FromServices] ILogStore logs) =>
            {
                logs.Clear();
                return Results.Ok(new { message = "Logs cleared" });
            })
            .WithName("ClearLogs")
            .WithSummary("Clears the in-memory log buffer.")
            .Produces<object>(StatusCodes.Status200OK);

            // Disable a system by name
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

            // Enable a system by name
            api.MapPost("/systems/{systemName}/enable", (string systemName, World w) =>
            {
                if (string.IsNullOrWhiteSpace(systemName))
                {
                    return Results.BadRequest(new { message = "System name must be provided." });
                }

                var ok = w.EnableSystemByName(systemName);
                return ok
                    ? Results.Ok(new { message = $"System '{systemName}' enabled." })
                    : Results.NotFound(new { message = $"System '{systemName}' not found." });
            })
            .WithName("EnableSystemByName")
            .WithSummary("Enables a system by its name.")
            .WithDescription("Sets the system's Enabled flag to true for the system with the given Name.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

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

                // Resolve entity from the world's active set to capture the correct generation
                var entity = w.GetAllEntities().FirstOrDefault(e => e.Id == id);
                if (!entity.IsValid())
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

                var entity = w.GetAllEntities().FirstOrDefault(e => e.Id == id);
                if (!entity.IsValid())
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

                var entity = w.GetAllEntities().FirstOrDefault(e => e.Id == id);
                if (!entity.IsValid())
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

                var entity = w.GetAllEntities().FirstOrDefault(e => e.Id == id);
                if (!entity.IsValid())
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

            // --- Dynamic Component Endpoints ---
            api.MapPost("/entities/{entityId}/dynamic/{name}", (string entityId, string name, World w, [FromBody] JsonElement? payload) =>
            {
                if (!uint.TryParse(entityId, out var id)) return Results.BadRequest(new { message = "Invalid entity ID format. Expected '{id}'." });
                var entity = w.GetAllEntities().FirstOrDefault(e => e.Id == id);
                if (!entity.IsValid()) return Results.NotFound(new { message = $"Entity {entityId} is not valid." });
                object? data = null;
                if (payload.HasValue)
                {
                    try { data = JsonSerializer.Deserialize<object>(payload.Value); }
                    catch (Exception ex) { return Results.BadRequest(new { message = $"Invalid JSON data: {ex.Message}" }); }
                }
                w.SetDynamicComponent(entity, name, data);
                return Results.Ok(new { message = $"Dynamic '{name}' set on entity {entityId}." });
            })
               .WithName("SetDynamicOnEntity").WithSummary("Sets a dynamic component on an entity").Produces<object>(StatusCodes.Status200OK);

            api.MapGet("/entities/{entityId}/dynamic/{name}", (string entityId, string name, World w) =>
            {
                if (!uint.TryParse(entityId, out var id)) return Results.BadRequest(new { message = "Invalid entity ID format. Expected '{id}'." });
                var entity = w.GetAllEntities().FirstOrDefault(e => e.Id == id);
                if (!entity.IsValid()) return Results.NotFound(new { message = $"Entity {entityId} is not valid." });
                if (!w.HasDynamicComponent(entity, name)) return Results.NoContent();
                var value = w.GetDynamicComponent(entity, name);
                return value is null ? Results.NoContent() : Results.Ok(value);
            })
               .WithName("GetDynamicFromEntity").WithSummary("Gets a dynamic component's data").Produces(StatusCodes.Status204NoContent).Produces<object>(StatusCodes.Status200OK);

            api.MapDelete("/entities/{entityId}/dynamic/{name}", (string entityId, string name, World w) =>
            {
                if (!uint.TryParse(entityId, out var id)) return Results.BadRequest(new { message = "Invalid entity ID format. Expected '{id}'." });
                var entity = w.GetAllEntities().FirstOrDefault(e => e.Id == id);
                if (!entity.IsValid()) return Results.NotFound(new { message = $"Entity {entityId} is not valid." });
                w.RemoveDynamicComponent(entity, name);
                return Results.Ok(new { message = $"Dynamic '{name}' removed from entity {entityId}." });
            })
               .WithName("RemoveDynamicFromEntity").WithSummary("Removes a dynamic component from an entity").Produces<object>(StatusCodes.Status200OK);

            api.MapGet("/query/dynamic", (World w, HttpContext ctx) =>
            {
                var names = ctx.Request.Query["names"].ToString();
                if (string.IsNullOrWhiteSpace(names)) return Results.BadRequest(new { message = "Provide 'names' as comma-separated list." });
                var parts = names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var result = w.QueryDynamic(parts).Select(e => e.Id);
                return Results.Ok(result);
            })
               .WithName("QueryDynamic").WithSummary("Queries entities by dynamic component names").Produces<IEnumerable<uint>>(StatusCodes.Status200OK);

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
