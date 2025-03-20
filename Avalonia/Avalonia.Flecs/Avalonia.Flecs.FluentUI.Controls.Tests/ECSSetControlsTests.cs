using Avalonia.Controls.Primitives;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;
using Xunit;

namespace Avalonia.Flecs.FluentUI.Controls.Tests;

public class ECSSetControlsTests
{
    [Fact]
    public void Frame()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.FluentUI.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Frame());


        Assert.True(entity.Has<Frame>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is Frame);
    }

    [Fact]
    public void NavigationView()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.FluentUI.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new NavigationView());

        Assert.True(entity.Has<NavigationView>());
        Assert.True(entity.Has<HeaderedContentControl>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is NavigationView);
    }

    [Fact]
    public void NavigationViewItem()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.FluentUI.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new NavigationViewItem());

        Assert.True(entity.Has<NavigationViewItem>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is NavigationViewItem);
    }

    [Fact]
    public void NavigationViewItemHeader()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.FluentUI.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new NavigationViewItemHeader());

        Assert.True(entity.Has<NavigationViewItemHeader>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is NavigationViewItemHeader);
    }
}
