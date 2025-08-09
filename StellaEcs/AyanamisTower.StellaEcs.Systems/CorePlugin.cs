using System;
using AyanamisTower.StellaEcs.Components;

namespace AyanamisTower.StellaEcs.CorePlugin;
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
/// Defines some core systems and services.
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
    public string Prefix => "Core";

    /// <inheritdoc/>
    public IEnumerable<Type> ProvidedSystems => [typeof(MovementSystem2D), typeof(MovementSystem3D)];
    /// <inheritdoc/>
    public IEnumerable<Type> ProvidedServices => [typeof(SearchService)];
    /// <inheritdoc/>
    public IEnumerable<Type> ProvidedComponents => [typeof(Name), typeof(Position2D), typeof(Velocity2D), typeof(Position3D), typeof(Velocity3D)];

    /// <inheritdoc/>
    public void Initialize(World world)
    {
        // Register services and systems, passing 'this' as the owner.
        world.RegisterService(new SearchService(world), this);
        world.RegisterComponent<Position2D>();
        world.RegisterComponent<Velocity2D>();
        world.RegisterComponent<Name>();
        world.RegisterComponent<Position3D>();
        world.RegisterComponent<Velocity3D>();

        world.RegisterSystem(new MovementSystem2D { Name = $"{Prefix}.{nameof(MovementSystem2D)}" }, this);
        world.RegisterSystem(new MovementSystem3D { Name = $"{Prefix}.{nameof(MovementSystem3D)}" }, this);

        // Explicitly register ownership of component types
        foreach (var componentType in ProvidedComponents)
        {
            world.RegisterComponentOwner(componentType, this);
        }
    }

    /// <inheritdoc/>
    public void Uninitialize(World world)
    {
        world.UnregisterService<SearchService>();

        // Use the prefixed name to remove the correct system
        world.RemoveSystemByName($"{Prefix}.{nameof(MovementSystem2D)}");
        world.RemoveSystemByName($"{Prefix}.{nameof(MovementSystem3D)}");

        foreach (var componentType in ProvidedComponents)
        {
            world.UnregisterComponentOwner(componentType);
        }
    }
}
