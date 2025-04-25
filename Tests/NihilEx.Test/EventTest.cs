using System;
using Flecs.NET.Core;

namespace AyanamisTower.NihilEx.Test;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


internal record struct Resize(int Height, int Width);

public class EventTest
{
    /// <summary>
    /// Here we wa nt to test if global events are correctly emitted.
    /// </summary>
    [Fact]
    public void GlobalEvents()
    {
        World world = World.Create();
        var hasEventHappened = false;

        var app = world.Entity("App");

        app.Observe(
            (ref Resize p) =>
            {
                hasEventHappened = true;
            }
        );

        app.Emit(new Resize(100, 200));

        Assert.True(hasEventHappened);
    }
}
