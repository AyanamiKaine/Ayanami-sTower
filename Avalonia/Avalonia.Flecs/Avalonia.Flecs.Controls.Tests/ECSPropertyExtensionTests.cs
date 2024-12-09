using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.Tests;

public class ECSPropertyExtensionTests
{

    /// <summary>
    /// Make sure we can set a property value on an entity.
    /// Here we set the Content property of a Button to "Hello World".
    /// </summary>
    [Fact]
    public void SetPropertyValue()
    {
        World world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();
        var entity = world.Entity("TestEntity")
            .Set(new Button())
            .SetProperty("Content", "Hello World");

        Assert.Equal("Hello World", entity.Get<Button>().Content as string);
    }

    /// <summary>
    /// Make sure we can get a property value from an entity.
    /// Here we manually cast the returned object to a string.
    /// </summary>
    [Fact]
    public void GetPropertyValueManualCast()
    {
        World world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();
        var entity = world.Entity("TestEntity")
            .Set(new Button()
            {
                Content = "Hello World"
            });
        Assert.Equal("Hello World", entity.GetProperty<object>("Content") as string);
    }

    /// <summary>
    /// Make sure we can get a property value from an entity.
    /// Here we let the extension method automatically cast the object to a string.
    /// </summary>
    [Fact]
    public void GetPropertyValueAutomaticCast()
    {
        World world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();
        var entity = world.Entity("TestEntity")
            .Set(new Button()
            {
                Content = "Hello World"
            });    
        Assert.Equal("Hello World", entity.GetProperty<string>("Content"));
    }

}
