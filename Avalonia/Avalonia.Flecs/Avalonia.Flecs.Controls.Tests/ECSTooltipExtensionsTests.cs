using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.Tests;

public class ECSTooltipExtensionsTests
{

    /// <summary>
    /// We want to be able to attach a tooltip to an entity
    /// </summary>
    [Fact]
    public void AttachToolTipToEntity()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var tooltip = world.Entity("ToolTip")
            .Set(new ToolTip())
            .SetContent(new TextBlock()
            {
                Text = "Hello World"
            });

        var entity = world.Entity("Button")
            .Set(new Button())
            .AttachToolTip(tooltip);


        //Here we test what tooltip is now attached to the button class
        //We get the tooltip from the button class and compare it to the tooltip we attached
        //We expect that they are the same
        ToolTip? foundTooltip = (ToolTip?)ToolTip.GetTip(entity.Get<Control>());
        Assert.Equal(tooltip.Get<ToolTip>(), foundTooltip);
    }


    /// <summary>
    /// We have implemented a gettooltip method that gets the tooltip attached to an entity
    /// of the underlying control class/component
    /// </summary>
    [Fact]
    public void GetAttachedTooltipOfEntity()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var tooltip = world.Entity("ToolTip")
            .Set(new ToolTip())
            .SetContent(new TextBlock()
            {
                Text = "Hello World"
            });

        var entity = world.Entity("Button")
            .Set(new Button())
            .AttachToolTip(tooltip);

        ToolTip? foundTooltip = (ToolTip?)entity.GetAttachedToolTip();
        Assert.Equal(tooltip.Get<ToolTip>(), foundTooltip);
    }

    [Fact]
    public void HasToolTipAttached()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var tooltip = world.Entity("ToolTip")
            .Set(new ToolTip())
            .SetContent(new TextBlock()
            {
                Text = "Hello World"
            });

        var entity = world.Entity("Button")
            .Set(new Button())
            .AttachToolTip(tooltip);

        Assert.True(entity.HasToolTipAttached());
    }

    [Fact]
    public void RemoveToolTip()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var tooltip = world.Entity("ToolTip")
            .Set(new ToolTip())
            .SetContent(new TextBlock()
            {
                Text = "Hello World"
            });

        var entity = world.Entity("Button")
            .Set(new Button())
            .AttachToolTip(tooltip);

        entity.RemoveToolTip();
        Assert.False(entity.HasToolTipAttached());
    }
}
