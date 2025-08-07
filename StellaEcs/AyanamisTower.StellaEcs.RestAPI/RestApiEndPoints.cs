using AyanamisTower.StellaEcs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Linq;

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
               .WithSummary("Retrieves a list of all registered component types.")
               .Produces<IEnumerable<ComponentInfoDto>>(StatusCodes.Status200OK);

            api.MapGet("/entities", (World w, HttpContext context) =>
            {
                var request = context.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";

                var entities = w.GetAllEntities().Select(e => new EntitySummaryDto
                {
                    Id = e.Id,
                    Generation = e.Generation,
                    Url = $"{baseUrl}/api/entities/{e.Id}-{e.Generation}"
                });
                return Results.Ok(entities);
            })
            .WithName("GetAllValidEntities")
            .WithSummary("Retrieves a summary of all valid entities.")
            .WithDescription("Returns a list of all entities with their ID, generation, and a URL to their detailed view.")
            .Produces<IEnumerable<EntitySummaryDto>>(StatusCodes.Status200OK);

            api.MapGet("/entities/{entityId}", (string entityId, World w) =>
            {
                var parts = entityId.Split('-');
                if (parts.Length != 2 || !uint.TryParse(parts[0], out var id) || !int.TryParse(parts[1], out var gen))
                {
                    return Results.BadRequest(new { message = "Invalid entity ID format. Expected '{id}-{generation}'." });
                }

                var entity = new Entity(id, gen, w);
                if (!w.IsEntityValid(entity))
                {
                    return Results.NotFound(new { message = $"Entity {entityId} is not valid." });
                }

                // NOTE: The return type of `GetAllComponentsForEntity` may need to be mapped to `List<ComponentInfoDto>`.
                // A direct cast is used here for simplicity, but you might need custom mapping logic.
                var components = (List<ComponentInfoDto>)w.GetAllComponentsForEntity(entity);
                var entityDetails = new EntityDetailDto
                {
                    Id = entity.Id,
                    Generation = entity.Generation,
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

            return app;
        }
    }
}
