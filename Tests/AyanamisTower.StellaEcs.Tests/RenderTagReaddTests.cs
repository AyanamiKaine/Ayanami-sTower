using System.Numerics;
using AyanamisTower.StellaEcs;
using AyanamisTower.StellaEcs.Components;
using AyanamisTower.StellaEcs.Engine.Components;

namespace AyanamisTower.StellaEcs.Tests;
#pragma warning disable CS1591

public class RenderTagReaddTests
{
    [Fact]
    public void RemovingAndReadding_RenderLit3D_Tag_AllowsEntityToBeQueriedAgain()
    {
        var world = new World();
        var e = world.CreateEntity();

        // Arrange: entity has required components for lit render
        e.Set(new Position3D(0, 0, 0));
        e.Set(new Mesh3D { Mesh = null! }); // Mesh object is not used by query; null here is fine for test
        e.Set(new RenderLit3D());

        // Sanity: present in query
        Assert.Contains(e, world.Query(typeof(Position3D), typeof(Mesh3D), typeof(RenderLit3D)));

        // Remove tag and verify it's gone from query
        e.Remove<RenderLit3D>();
        Assert.DoesNotContain(e, world.Query(typeof(Position3D), typeof(Mesh3D), typeof(RenderLit3D)));

        // Re-add tag and verify it's present again
        e.Set(new RenderLit3D());
        Assert.Contains(e, world.Query(typeof(Position3D), typeof(Mesh3D), typeof(RenderLit3D)));
    }
}
