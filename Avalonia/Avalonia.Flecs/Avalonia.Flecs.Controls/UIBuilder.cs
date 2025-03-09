using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls;
/// <summary>
/// Extension methods that enable fluent UI building with a hierarchical syntax for Flecs entities.
/// </summary>
public static class UIBuilderExtensions
{
    /// <summary>
    /// Creates a new entity with the specified control component and configures it using a builder pattern.
    /// </summary>
    /// <typeparam name="T">The type of Avalonia control to create.</typeparam>
    /// <param name="world">The Flecs world.</param>
    /// <param name="configure">Action to configure the entity and its children.</param>
    /// <returns>The created entity.</returns>
    public static Entity UI<T>(this World world, Action<UIBuilder<T>> configure) where T : Control, new()
    {
        var entity = world.Entity().Set(new T());
        var builder = new UIBuilder<T>(world, entity);
        configure(builder);
        return entity;
    }

    /// <summary>
    /// Creates a new child entity with the specified control component and configures it using a builder pattern.
    /// </summary>
    /// <typeparam name="T">The type of Avalonia control to create.</typeparam>
    /// <param name="parent">The parent entity.</param>
    /// <returns>The created child entity.</returns>
    public static Entity UI<T>(this Entity parent) where T : Control, new()
    {
        var world = parent.CsWorld();
        var entity = world.Entity().Set(new T()).ChildOf(parent);
        var builder = new UIBuilder<T>(world, entity);

        return entity;
    }
}

/// <summary>
/// A fluent builder for constructing hierarchical UI components as Flecs entities.
/// Provides a clean, nested syntax that visually represents the UI hierarchy.
/// </summary>
/// <typeparam name="T">The type of Avalonia control this builder is configuring.</typeparam>
[Experimental("UIBuilder")]
public class UIBuilder<T> where T : Control
{
    private readonly World _world;
    private readonly Entity _entity;
    private readonly T _control;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIBuilder{T}"/> class.
    /// </summary>
    /// <param name="world">The Flecs world.</param>
    /// <param name="entity">The entity this builder is configuring.</param>
    public UIBuilder(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
        _control = entity.Get<T>();
    }

    /// <summary>
    /// Sets a property on the control by name.
    /// </summary>
    /// <param name="name">The name of the property to set.</param>
    /// <param name="value">The value to set the property to.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public UIBuilder<T> Property(string name, object value)
    {
        _entity.SetProperty(name, value);
        return this;
    }

    /// <summary>
    /// Creates a child control entity and returns its builder.
    /// </summary>
    /// <typeparam name="TChild">The type of child control to create.</typeparam>
    /// <returns>The builder for the newly created child entity.</returns>
    public UIBuilder<TChild> Child<TChild>() where TChild : Control, new()
    {
        var child = _entity.UI<TChild>();
        return new UIBuilder<TChild>(_world, child);
    }

    /// <summary>
    /// Sets the margin on the control.
    /// </summary>
    /// <param name="margin">The margin to set.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public UIBuilder<T> Margin(Thickness margin)
    {
        _control.Margin = margin;
        return this;
    }

    /// <summary>
    /// Sets the column for this control in a Grid.
    /// </summary>
    /// <param name="column">The zero-based column index.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public UIBuilder<T> Column(int column)
    {
        _entity.SetColumn(column);
        return this;
    }
}