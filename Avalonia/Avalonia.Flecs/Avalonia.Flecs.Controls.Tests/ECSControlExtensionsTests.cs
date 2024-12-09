using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.Tests;

public class ECSControlExtensionsTests
{

    [Fact]
    public void SetRow()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Button())
            .SetRow(1);

        Assert.Equal(1, Grid.GetRow(entity.Get<Button>()));
    }

    [Fact]
    public void GetRow()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Button())
            .SetRow(1);

        Assert.Equal(1, entity.GetRow());
    }

    [Fact]
    public void SetColumn()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Button())
            .SetColumn(1);

        Assert.Equal(1, Grid.GetColumn(entity.Get<Button>()));
    }

    [Fact]
    public void GetColumn()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Button())
            .SetColumn(1);

        Assert.Equal(1, entity.GetColumn());
    }

    [Fact]
    public void SetColumnSpan()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Button())
            .SetColumnSpan(2);

        Assert.Equal(2, Grid.GetColumnSpan(entity.Get<Button>()));
    }

    [Fact]
    public void GetColumnSpan()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Button())
            .SetColumnSpan(2);

        Assert.Equal(2, entity.GetColumnSpan());
    }

    [Fact]
    public void SetRowSpan()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Button())
            .SetRowSpan(2);

        Assert.Equal(2, Grid.GetRowSpan(entity.Get<Button>()));
    }

    [Fact]
    public void GetRowSpan()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Button())
            .SetRowSpan(2);

        Assert.Equal(2, entity.GetRowSpan());
    }
}
