using Flecs.NET.Core;

namespace AyanamisTower.NihilEx;
/// <summary>
/// TODO
/// </summary>
public class Engine
{
    /// <summary>
    /// Stores the defined phases for the engine's execution pipeline.
    /// </summary>
    public Dictionary<string, Entity> Phases = [];
    /// <summary>
    /// Gets the Flecs world associated with this engine instance.
    /// </summary>
    public World World { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Engine"/> class with the specified Flecs world.
    /// </summary>
    /// <param name="world">The Flecs world to associate with this engine.</param>
    public Engine(World world)
    {
        World = world;
        InitDefaultPhases();
    }

    private void InitDefaultPhases()
    {

        var preRender = World
            .Entity("PreRender")
            .Add(Ecs.Phase)
            .DependsOn(Ecs.OnUpdate);
        Phases.Add("PreRender", preRender);

        var onRender = World
            .Entity("OnRender")
            .Add(Ecs.Phase)
            .DependsOn(preRender);

        Phases.Add("OnRender", onRender);

        var postRender = World
            .Entity("PostRender")
            .Add(Ecs.Phase)
            .DependsOn(onRender);

        Phases.Add("PostRender", postRender);
    }
}
