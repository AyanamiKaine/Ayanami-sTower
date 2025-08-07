using AyanamisTower.StellaEcs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
            var api = app.MapGroup("/api");

            api.MapGet("/world/status", (World w) => Results.Ok(w.GetWorldStatus()));

            api.MapGet("/systems", (World w) => Results.Ok(w.GetSystems()));

            api.MapGet("/components", (World w) => Results.Ok(w.GetComponentTypes()));

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
            });

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

                // This method now returns component data.
                var components = w.GetAllComponentsForEntity(entity);
                var entityDetails = new EntityDetailDto
                {
                    Id = entity.Id,
                    Generation = entity.Generation,
                    Components = components
                };

                return Results.Ok(entityDetails);
            });

            return app;
        }
    }
}
