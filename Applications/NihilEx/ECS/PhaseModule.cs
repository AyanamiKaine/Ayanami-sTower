using Flecs.NET.Core;
namespace AyanamisTower.NihilEx.ECS;

/// <summary>
/// Defines the ECS phases related to rendering within the game loop.
/// </summary>
public struct PhaseModule : IFlecsModule
{
    /// <summary>
    /// Initializes the module
    /// </summary>
    /// <param name="world"></param>
    public void InitModule(World world)
    {
        world.Module<PhaseModule>();

        var preRender = world
            .Entity("PreRender")
            .Add(Ecs.Phase)
            .DependsOn(Ecs.OnUpdate);

        var onRender = world
            .Entity("OnRender")
            .Add(Ecs.Phase)
            .DependsOn(preRender);

        world
            .Entity("PostRender")
            .Add(Ecs.Phase)
            .DependsOn(onRender);
    }
}
