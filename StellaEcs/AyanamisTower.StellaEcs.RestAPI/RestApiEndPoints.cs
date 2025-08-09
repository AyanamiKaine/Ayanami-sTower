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
                var parts = entityId.Split('-');
                if (parts.Length != 2 || !uint.TryParse(parts[0], out var id) || !int.TryParse(parts[1], out var gen))
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
