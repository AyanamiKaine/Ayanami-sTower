using Flecs.NET.Core;
using NLog;
using NLog.LayoutRenderers;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;

namespace StellaInvicta.Systems;

/*
TODO:
We should probably add something like a locatedAt Relationship to buildings, 
where we then can query the entity that represents the location and find its
infrastructure value. We can use this value to either increase or decrease 
the productivity of the building.
*/

/// <summary>
/// System responsible for handling production processes in the game.
/// Manages the conversion of input goods to output goods based on workforce and building levels.
/// </summary>
/// <remarks>
/// The production system processes entities with Building components that have:
/// - Inventory (current goods storage)
/// - Input (required goods for production)
/// - Output (produced goods)
/// - Expected WorkForce (required workers)
/// - Level (building level)
/// </remarks>
public class ProductionSystem() : ISystem
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    Entity systemEntity;
    /// <summary>
    /// Enables the production system by setting up and processing production cycles for buildings.
    /// </summary>
    /// <param name="world">The game world instance containing all entities and systems.</param>
    /// <param name="simulationSpeed">The timer entity controlling simulation speed.</param>
    /// <returns>The system entity that manages production operations.</returns>
    /// <remarks>
    /// This method:
    /// - Processes buildings with inventory, input/output goods lists, and workforce requirements
    /// - Calculates production based on workforce availability and employment ratio
    /// - Consumes input goods and produces output goods according to building level and workforce efficiency
    /// </remarks>
    public Entity Enable(World world, TimerEntity simulationSpeed)
    {
        return systemEntity = world.System<GoodsList, GoodsList, GoodsList, WorkForce>()
            .With<Building>()
            .TermAt(0).First<Inventory>().Second<GoodsList>()
            .TermAt(1).First<Input>().Second<GoodsList>()
            .TermAt(2).First<Output>().Second<GoodsList>()
            .TermAt(3).First<Expected>().Second<WorkForce>()
            .Each((Entity e, ref GoodsList inventory, ref GoodsList inputGoodsList, ref GoodsList outputGoodsList, ref WorkForce expectedWorkForce) =>
            {
                // TODO: ADD LOGGING
                var employedWorkForce = 0;
                e.Each<WorkForce>(e => employedWorkForce += e.Get<Quantity>().Value);
                var employmentRatio = Math.Clamp((double)employedWorkForce / expectedWorkForce.Value , 0, 1);

                inventory -= inputGoodsList ;
                inventory += outputGoodsList  * employmentRatio;
            });
    }
    /// <summary>
    /// Disables the production system by disabling its associated system entity.
    /// </summary>
    public void Disable()
    {
        systemEntity.Disable();
    }
}