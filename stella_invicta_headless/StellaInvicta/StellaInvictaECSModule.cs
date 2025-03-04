using System.Numerics;
using Flecs.NET.Bindings;
using Flecs.NET.Core;
using NLog;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;
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
        AddSimulationTickSource(world);
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
        world.RegisterComponent<GameDate>("GameDate")
            .SetDocBrief("Used to represent a date in the gameworld. 24 Hour Days | 30 Days each month | 360 Days each year")
            .Member<int>("Year")
            .Member<int>("Month")
            .Member<int>("Day")
            .Member<int>("Hour")
            .Member<int>("Minute")
            .Member<int>("Turn");

        world.RegisterComponent<Age>("Age")
            .SetDocBrief("Used to represent an age related to the global gamedate")
            .Member<int>("Value");

        world.RegisterComponent<Vector3>("Vector3")
            .Member<float>("X")
            .Member<float>("Y")
            .Member<float>("Z");

        world.RegisterComponent<Vector2>("Vector2")
            .Member<float>("X")
            .Member<float>("Y");

        world.RegisterComponent<Quaternion>("Quaternion")
            .Member<float>("X")
            .Member<float>("Y")
            .Member<float>("Z")
            .Member<float>("W");

        world.RegisterComponent<Wealth>("Wealth")
            .Member<double>("Value");

        world.RegisterComponent<Age>("Age")
            .Member<int>("Value");

        world.RegisterComponent<Consciousness>("Consciousness")
            .Member<float>("Value");


        world.RegisterComponent<Credits>("Credits")
            .Member<float>("Amount");


        world.RegisterComponent<Diplomacy>("Diplomacy")
            .Member<double>("Value");


        world.RegisterComponent<Fertility>("Fertility")
            .Member<double>("Value");

        world.RegisterComponent<Greed>("Greed")
            .Member<double>("Value");

        // DONE!
        // BIG QUESTION DO I NEED TO ALSO ADD THE PRIVATE FIELDS AS MEMBER?
        // I personally think so because how else should it know the right offset?
        // ANSWER: YES, all fields that are actual part of the datastructure must be added
        // why are Value and Percentages excluded? Because they are only properties, Value
        // is a get setter, and Percentage is a caluclate property, when accessed it gets
        // calculated.
        world.RegisterComponent<Happiness>("Happiness")
            .Member<float>("Value");
        //.Member<float>("Value")
        //.Member<float>("Percentage");

        world.RegisterComponent<Health>("Health")
            .Member<double>("Value");

        world.RegisterComponent<Militancy>("Militancy")
            .Member<float>("Value");

        world.RegisterComponent<Literacy>("Literacy")
            .Member<float>("Value");


        world.RegisterComponent<Quantity>("Quantity")
            .Member<int>("Value");

        world.RegisterComponent<ShortDescription>("ShortDescription");
        world.RegisterComponent<Name>("Name");

        world.RegisterComponent<Size>("Size")
            .Member<int>("Value");
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

        world.RegisterTag<Tags.Identifiers.Position>("Position").Add(Ecs.Exclusive);
        world.RegisterTag<Tags.Identifiers.Velocity>("Velocity").Add(Ecs.Exclusive);

        world.RegisterTag<Tags.Identifiers.Population>("Population");

        world.RegisterTag<Tags.Identifiers.Alive>("Alive");
        world.RegisterTag<Tags.Identifiers.Died>("Died");

        world.RegisterTag<Input>("Input");
        world.RegisterTag<Output>("Output");
        world.RegisterTag<Building>("Building");
        world.RegisterTag<Inventory>("Inventory");

        // You can have only one bank
        world.RegisterTag<Tags.Identifiers.Bank>("Bank")
            .Add(Ecs.Exclusive);
        // TODO: When we say something like, pop.add<province>(PROVINE-ENTITY) to indicate
        // that the population is part of the province so and so. We would like to give the province
        // prov.add<Population>(pop), to indicate that the province has the population so and so.
        // HINT: probably we can use observers to add the other relationship when the other is added.
        world.RegisterTag<Tags.Identifiers.Province>("Province")
            .Add(Ecs.Exclusive);

    }

    /// <summary>
    /// Creates an ticksource entity called "SimulationSpeed" that
    /// runs every second.
    /// </summary>
    /// <param name="world"></param>
    private static void AddSimulationTickSource(World world)
    {
        world.Timer("SimulationSpeed")
            .Interval(SimulationSpeed.ReallyVeryFast);
    }
}
