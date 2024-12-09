using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.Tests;

/// <summary>
/// When we set an avalonia control class to an entity, it should also be set as an object.
/// We are doing this to make it easier to work with the entity. With various helper functions
/// that dont care exactly what type of control it is just that it has a content property for example.
/// </summary>
public class ECSSetControlsTests
{
    [Fact]
    public void AutoCompleteBox()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new AutoCompleteBox());

        Assert.True(entity.Has<AutoCompleteBox>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is AutoCompleteBox);
    }

    [Fact]
    public void Border()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Border());

        Assert.True(entity.Has<Border>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is Border);
    }

    [Fact]
    public void Canvas()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Canvas());

        Assert.True(entity.Has<Canvas>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is Canvas);
    }

    [Fact]
    public void ComboBox()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new ComboBox());

        Assert.True(entity.Has<ComboBox>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is ComboBox);
    }

    [Fact]
    public void ContentControl()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new ContentControl());

        Assert.True(entity.Has<ContentControl>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is ContentControl);
    }

    [Fact]
    public void Control()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Control());

        Assert.True(entity.Has<Control>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is Control);
    }

    [Fact]
    public void Decorator()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Decorator());

        Assert.True(entity.Has<Decorator>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is Decorator);
    }

    [Fact]
    public void DockPanel()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new DockPanel());

        Assert.True(entity.Has<DockPanel>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is DockPanel);
    }

    [Fact]
    public void Expander()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Expander());

        Assert.True(entity.Has<Expander>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is Expander);
    }

    [Fact]
    public void Grid()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Grid());

        Assert.True(entity.Has<Grid>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is Grid);
    }

    [Fact]
    public void HeaderedContentControl()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new HeaderedContentControl());

        Assert.True(entity.Has<HeaderedContentControl>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is HeaderedContentControl);
    }

    [Fact]
    public void HeaderedSelectingItemsControl()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new HeaderedSelectingItemsControl());

        Assert.True(entity.Has<HeaderedSelectingItemsControl>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is HeaderedSelectingItemsControl);
    }

    [Fact]
    public void InputElement()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new InputElement());

        Assert.True(entity.Has<InputElement>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is InputElement);
    }

    [Fact]
    public void Interactive()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Interactive());

        Assert.True(entity.Has<Interactive>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is Interactive);
    }

    [Fact]
    public void ItemsControl()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new ItemsControl());

        Assert.True(entity.Has<ItemsControl>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is ItemsControl);
    }

    [Fact]
    public void Layoutable()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Layoutable());

        Assert.True(entity.Has<Layoutable>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is Layoutable);
    }

    [Fact]
    public void ListBox()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new ListBox());

        Assert.True(entity.Has<ListBox>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is ListBox);
    }

    [Fact]
    public void Menu()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Menu());

        Assert.True(entity.Has<Menu>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is Menu);
    }

    [Fact]
    public void MenuFlyout()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new MenuFlyout());

        Assert.True(entity.Has<MenuFlyout>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is MenuFlyout);
    }

    [Fact]
    public void MenuItem()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new MenuItem());

        Assert.True(entity.Has<MenuItem>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is MenuItem);
    }

    [Fact]
    public void RadioButton()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new RadioButton());

        Assert.True(entity.Has<RadioButton>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is RadioButton);
    }

    [Fact]
    public void RelativePanel()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new RelativePanel());

        Assert.True(entity.Has<RelativePanel>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is RelativePanel);
    }

    [Fact]
    public void RepeatButton()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new RepeatButton());

        Assert.True(entity.Has<RepeatButton>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is RepeatButton);
    }

    [Fact]
    public void ScrollViewer()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new ScrollViewer());

        Assert.True(entity.Has<ScrollViewer>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is ScrollViewer);
    }

    [Fact]
    public void SelectingItemsControl()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new SelectingItemsControl());

        Assert.True(entity.Has<SelectingItemsControl>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is SelectingItemsControl);
    }

    [Fact]
    public void SplitButton()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new SplitButton());

        Assert.True(entity.Has<SplitButton>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is SplitButton);
    }

    [Fact]
    public void StackPanel()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new StackPanel());

        Assert.True(entity.Has<StackPanel>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is StackPanel);
    }

    [Fact]
    public void TemplatedControl()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new TemplatedControl());

        Assert.True(entity.Has<TemplatedControl>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is TemplatedControl);
    }

    [Fact]
    public void TextBlock()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new TextBlock());

        Assert.True(entity.Has<TextBlock>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is TextBlock);
    }

    [Fact]
    public void ToggleButton()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new ToggleButton());

        Assert.True(entity.Has<ToggleButton>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is ToggleButton);
    }

    [Fact]
    public void ToggleSwitch()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new ToggleSwitch());

        Assert.True(entity.Has<ToggleSwitch>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is ToggleSwitch);
    }

    [Fact]
    public void Visual()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Visual());

        Assert.True(entity.Has<Visual>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is Visual);
    }


    [Fact]
    public void WrapPanel()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new WrapPanel());

        Assert.True(entity.Has<WrapPanel>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is WrapPanel);
    }

    [Fact]
    public void Button()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new Button());

        Assert.True(entity.Has<Button>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is Button);
    }

    [Fact]
    public void TextBox()
    {
        var world = World.Create();
        world.Import<Avalonia.Flecs.Controls.ECS.Module>();

        var entity = world.Entity("TestEntity")
            .Set(new TextBox());

        Assert.True(entity.Has<TextBox>());
        Assert.True(entity.Has<object>());
        Assert.True(entity.Get<object>() is TextBox);
    }
}
