using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.Tests;

public class ECSTextPropertyExentionsTests
{
    [Fact]
    public void SetTextTextBlock()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new TextBlock())
            .SetText("Hello World");

        Assert.Equal("Hello World", entity.Get<TextBlock>().Text);
    }

    [Fact]
    public void GetTextTextBlock()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new TextBlock()
            {
                Text = "Hello World"
            });

        Assert.Equal("Hello World", entity.GetText());
    }

    [Fact]
    public void SetTextTextBox()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new TextBox())
            .SetText("Hello World");

        Assert.Equal("Hello World", entity.Get<TextBox>().Text);
    }

    [Fact]
    public void GetTextTextBox()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new TextBox()
            {
                Text = "Hello World"
            });

        Assert.Equal("Hello World", entity.GetText());
    }

    [Fact]
    public void SetInnerLeftContent()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new TextBox())
            .SetInnerLeftContent(new Button());

        Assert.True(entity.Get<TextBox>().InnerLeftContent is Button);
    }

    [Fact]
    public void GetInnerLeftContent()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new TextBox()
            {
                InnerLeftContent = new Button()
            });

        Assert.True(entity.GetInnerLeftContent() is Button);
    }

    [Fact]
    public void SetInnerRightContent()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new TextBox())
            .SetInnerRightContent(new Button());

        Assert.True(entity.Get<TextBox>().InnerRightContent is Button);
    }

    [Fact]
    public void GetInnerRightContent()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new TextBox()
            {
                InnerRightContent = new Button()
            });

        Assert.True(entity.GetInnerRightContent() is Button);
    }
}
