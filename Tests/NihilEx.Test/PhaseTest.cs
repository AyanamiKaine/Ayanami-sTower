using System.Collections.Generic; // Required for List<T>
using Flecs.NET.Core;
using Xunit; // Assuming xUnit is used based on [Fact]

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
        World world = World.Create();
        var engine = new Engine(world);

        // Use a list to record the execution order
        var executionOrder = new List<string>();

        world
            .System("PreRenderSystem") // Give systems names for clarity
            .Kind(engine.Phases["PreRender"])
            .Each(() =>
            {
                executionOrder.Add("PreRender");
            });

        world
            .System("OnRenderSystem")
            .Kind(engine.Phases["OnRender"])
            .Each(() =>
            {
                // Ensure PreRender ran before OnRender
                Assert.Contains("PreRender", executionOrder);
                Assert.DoesNotContain("OnRender", executionOrder);
                Assert.DoesNotContain("PostRender", executionOrder);
                executionOrder.Add("OnRender");
            });

        world
            .System("PostRenderSystem")
            .Kind(engine.Phases["PostRender"])
            .Each(() =>
            {
                // Ensure PreRender and OnRender ran before PostRender
                Assert.Contains("PreRender", executionOrder);
                Assert.Contains("OnRender", executionOrder);
                Assert.DoesNotContain("PostRender", executionOrder);
                executionOrder.Add("PostRender");
            });

        world.Progress();

        // Verify the final recorded order
        Assert.Equal(3, executionOrder.Count);
        Assert.Equal("PreRender", executionOrder[0]);
        Assert.Equal("OnRender", executionOrder[1]);
        Assert.Equal("PostRender", executionOrder[2]);
    }
}
