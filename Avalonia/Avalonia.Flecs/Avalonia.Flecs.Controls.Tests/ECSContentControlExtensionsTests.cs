using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.Tests;

public class ECSContentControlExtensionsTests
{

    [Fact]
    public void SetContent()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new ContentControl())
            .SetContent(new Button());

        Assert.True(entity.Get<ContentControl>().Content is Button);
    }

    [Fact]
    public void GetContent()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new ContentControl()
            {
                Content = new Button()
            });

        Assert.True(entity.GetContent() is Button);
    }
}
