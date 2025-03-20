using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.Tests;
public class UIBuilderTests
{
    [Fact]
    public void Create()
    {
        World world = World.Create();
        var textBlock = world.UI<TextBlock>((textBlock) => { });

        Assert.True(textBlock.Has<TextBlock>());
    }
}