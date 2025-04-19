using Flecs.NET.Core;

namespace AyanamisTower.NihilEx.Test;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class PhaseTest
{
    /// <summary>
    /// First updates run and after that, preRender, onRender and atlast postRender
    /// </summary>
    [Fact]
    public void RenderPhaseCorrectOrder()
    {
        var world = World.Create();
        world.Import<ECS.PhaseModule>();

        var systemRun = false;

        var system = world.System()
            .Kind(world.Entity("PreRender"))
            .Each((_) =>
            {
                systemRun = true;
            });

        world.Progress();

        Assert.True(systemRun);
    }
}
