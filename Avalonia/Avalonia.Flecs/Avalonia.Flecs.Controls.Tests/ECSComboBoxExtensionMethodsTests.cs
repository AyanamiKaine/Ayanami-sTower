using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.Tests;

public class ECSComboBoxExtensionMethodsTests
{

    /// <summary>
    /// The SetPlacerHolderText method should set the placeholder text of the ComboBox or
    /// a property of the entity if the ComboBox component is not present.
    /// </summary>
    [Fact]
    public void SetPlaceholderText()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new ComboBox())
            .SetPlaceholderText("Placeholder");

        Assert.Equal("Placeholder", entity.Get<ComboBox>().PlaceholderText);
    }

    /// <summary>
    /// The GetPlaceholderText method should return the placeholder text of the ComboBox or
    /// a property of the entity if the ComboBox component is not present.
    /// </summary>
    [Fact]
    public void GetPlaceholderText()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new ComboBox()
            {
                PlaceholderText = "Placeholder"
            });

        Assert.Equal("Placeholder", entity.GetPlaceholderText());
    }
}
