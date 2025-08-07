using System;
using AyanamisTower.StellaEcs.Components;

namespace AyanamisTower.StellaEcs.Systems;
/// <summary>
/// Provides a service to search for entities by their name.
/// </summary>
/// <remarks>
/// The service takes the world instance so it can perform queries.
/// </remarks>
/// <param name="world"></param>
public class SearchService(World world)
{
    private readonly World _world = world;

    /// <summary>
    /// Finds all entities that have a NameComponent matching the specified name.
    /// </summary>
    /// <param name="name">The name to search for (case-insensitive).</param>
    /// <returns>A list of entity summaries for the matching entities.</returns>
    public IEnumerable<EntitySummaryDto> GetEntitiesByName(string name)
    {
        // 1. Use the world's Query method to get all entities that have a NameComponent.
        var entitiesWithNameComponent = _world.Query(typeof(Name));

        var foundEntities = new List<EntitySummaryDto>();

        // 2. Iterate through the results of the query.
        foreach (var entity in entitiesWithNameComponent)
        {
            // 3. For each entity, get its NameComponent.
            var nameComponent = _world.GetComponent<Name>(entity);

            // 4. Compare the component's name with the search term.
            if (nameComponent.Value.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                // 5. If it matches, add the entity's ID and Generation to our results list.
                //    Returning a DTO is better than just the ID, as it provides more context.
                foundEntities.Add(new EntitySummaryDto
                {
                    Id = entity.Id,
                    Generation = entity.Generation,
                    // The URL isn't strictly necessary here but can be useful.
                    // We'll leave it empty for this service-specific response.
                    Url = $"/api/entities/{entity.Id}-{entity.Generation}"
                });
            }
        }

        return foundEntities;
    }
}

/// <summary>
/// Defines some core systems
/// </summary>
public class CorePlugin : IPlugin
{
    /// <inheritdoc/>
    public string Name => "Core Features";

    /// <inheritdoc/>
    public string Version => "1.0.0";

    /// <inheritdoc/>
    public string Author => "Ayanami Kaine";

    /// <inheritdoc/>
    public string Description => "Provides core features for the ECS framework.";

    /// <inheritdoc/>
    public void Initialize(World world)
    {
        world.RegisterService(new SearchService(world));

        world.RegisterSystem(new MovementSystem2D());
        world.RegisterSystem(new MovementSystem3D());
    }

    /// <inheritdoc/>
    public void Uninitialize(World world)
    {
        world.UnregisterService<SearchService>();
        
        world.RemoveSystem<MovementSystem2D>();
        world.RemoveSystem<MovementSystem3D>();
    }
}
