using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.Tests;

public class ECSObserverTests
{
    /// <summary>
    /// There should be only be one avalonia control class attached
    /// to an entity, if we set another one the old one should be removed.
    /// Or maybe we should throw?
    /// </summary>
    [Fact]
    public void OnlyOneAvaloniaControlClassPerEntity()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();
        
        var entity = world.Entity("TestEntity")
            .Set(new Button());

        Assert.True(entity.Has<Button>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is Button);
    }
}
