using Flecs.NET.Core;
using NLog;
using StellaInvicta.Components;
namespace StellaInvicta;

/// <summary>
/// Module responsible for initializing and configuring the ECS (Entity Component System) components for Stella Invicta.
/// </summary>
/// <remarks>
/// This module implements IFlecsModule interface to provide ECS setup functionality.
/// Use this class to register systems, components, and entities required for the game.
/// </remarks>
public class StellaInvictaECSModule : IFlecsModule
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


    /// <summary>
    /// Initializes the ECS module with the specified world instance.
    /// </summary>
    /// <param name="world">The ECS world instance to initialize the module with.</param>
    /// <remarks>
    /// This method is responsible for setting up all necessary systems, components, and entities
    /// required for the Stella Invicta ECS module to function properly within the given world context.
    /// </remarks>
    public void InitModule(World world)
    {
        world.Module<StellaInvictaECSModule>();

        AddComponents(world);
        AddTags(world);
        AddWorldGlobals(world);
    }

    /// <summary>
    /// Adds global variables to the specified ECS world.
    /// </summary>
    /// <param name="world">The ECS world to which global variables will be added.</param>
    /// <remarks>
    /// Currently initializes a default DateTime instance as a global variable in the world.
    /// </remarks>
    private static void AddWorldGlobals(World world)
    {
        Logger.ConditionalDebug("Adding GameDate component directly to the game world as a global");
        world.Set<GameDate>(new());
    }

    private static void AddComponents(World world)
    {
        // Used for game date handling. Similar to how you would imagine CK2/Vic2 doing it with their time.
        world.RegisterComponent<GameDate>("GameDate");
        world.RegisterComponent<Age>("Age")
            .Member<int>("Value");

        world.RegisterComponent<Wealth>("Wealth")
            .Member<double>("Value");
    }

    /// <summary>
    /// Registers celestial body and diplomacy tag components within the specified world.
    /// </summary>
    /// <param name="world">The game world to which the tags will be added.</param>
    /// <remarks>
    /// This method initializes both celestial body tags (Asteroid, GasGiant, Moon, Planet, Star) 
    /// and diplomatic relationship tags (Ally, AtWar, Enemy, Neutral) as components in the ECS world.
    /// </remarks>
    private static void AddTags(World world)
    {
        world.RegisterTag<Tags.CelestialBodies.Asteroid>("Asteroid");
        world.RegisterTag<Tags.CelestialBodies.GasGiant>("GasGiant");
        world.RegisterTag<Tags.CelestialBodies.Moon>("Moon");
        world.RegisterTag<Tags.CelestialBodies.Planet>("Planet");
        world.RegisterTag<Tags.CelestialBodies.Star>("Star");


        world.RegisterTag<Tags.Relationships.Orbits>("Orbits")
            .Entity.Add(Ecs.Exclusive);
        world.RegisterTag<Tags.Relationships.DockedAt>("DockedAt")
            .Entity.Add(Ecs.Exclusive);
        world.RegisterTag<Tags.Relationships.HomeStation>("HomeStation")
            .Entity.Add(Ecs.Exclusive);

        world.RegisterTag<Tags.Relationships.IsAtWarWith>("IsAtWarWith");
        world.RegisterTag<Tags.Relationships.OwnedBy>("OwnedBy")
                        .Entity.Add(Ecs.Exclusive);

        world.RegisterTag<Tags.Relationships.ConnectedTo>("ConnectedTo")
            .Entity.Add(Ecs.Symmetric);

        world.RegisterTag<Tags.Identifiers.Ally>("Ally");
        world.RegisterTag<Tags.Identifiers.Enemy>("Enemy");
        world.RegisterTag<Tags.Identifiers.Neutral>("Neutral");
    }
}
