using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.Tests;

public class ECSBorderTests
{
    [Fact]
    public void EntityChildRelationship()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var border = new Border();
        var stackPanel = new StackPanel();
        var entityA = world.Entity()
            .Set(border);

        var entityB = world.Entity()
             .Set(stackPanel)
             .ChildOf(entityA);


        Assert.True(border.Child == stackPanel);
    }

    [Fact]
    public void UiBuilder()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        UIBuilder<Border> border = null!;
        UIBuilder<StackPanel> stackPanel = null!;



        var entityA = world.UI<Border>((b) =>
        {
            border = b;
            b.Child<StackPanel>((s) =>
            {
                stackPanel = s;
            });
        });

        Assert.True(border.Get<Border>().Child == stackPanel.Get<StackPanel>());
    }
}
