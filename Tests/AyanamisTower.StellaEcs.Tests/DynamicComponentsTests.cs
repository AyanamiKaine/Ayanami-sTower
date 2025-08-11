using System.Linq;
using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests;
#pragma warning disable CS1591

public class DynamicComponentsTests
{
    [Fact]
    public void SetGetHasRemove_DynamicComponent_Works()
    {
        var w = new World();
        var e = w.CreateEntity();

        Assert.False(e.HasDynamic("Tag"));
        e.SetDynamic("Tag", "Player");

        Assert.True(e.HasDynamic("Tag"));
        Assert.Equal("Player", e.GetDynamic<string>("Tag"));

        e.RemoveDynamic("Tag");
        Assert.False(e.HasDynamic("Tag"));
    }

    [Fact]
    public void QueryDynamic_ByName_ReturnsMatchingEntities()
    {
        var w = new World();
        var a = w.CreateEntity();
        var b = w.CreateEntity();
        var c = w.CreateEntity();

        a.SetDynamic("Tag", "Enemy");
        b.SetDynamic("Tag", "Enemy");
        c.SetDynamic("Other", 123);

        var enemies = w.QueryDynamic("Tag").ToList();
        Assert.Equal(2, enemies.Count);
        Assert.Contains(a, enemies);
        Assert.Contains(b, enemies);
        Assert.DoesNotContain(c, enemies);
    }

    [Fact]
    public void QueryDynamic_WithMultipleNames_RequiresAll()
    {
        var w = new World();
        var a = w.CreateEntity();
        var b = w.CreateEntity();
        var c = w.CreateEntity();

        a.SetDynamic("A", true).SetDynamic("B", 1);
        b.SetDynamic("A", true);
        c.SetDynamic("B", 1);

        var both = w.QueryDynamic("A", "B").ToList();
        Assert.Single(both);
        Assert.Contains(a, both);
    }

    [Fact]
    public void DestroyEntity_RemovesDynamicComponents()
    {
        var w = new World();
        var e = w.CreateEntity();
        e.SetDynamic("Tag", "Temp");
        Assert.True(e.HasDynamic("Tag"));
        e.Destroy();

        var e2 = w.CreateEntity();
        // Ensure no exception or stale dynamic data leaks to new entities
        Assert.False(e2.HasDynamic("Tag"));
    }
}
