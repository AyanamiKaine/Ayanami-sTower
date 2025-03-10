using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Flecs.Controls.ECS;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls;

/*
TODO: Highly Experimental, the goal of this module is to improve the way
we create a UI in code. Its all about making it more obvious how the UI is 
structured. We take insperation how its done in Flutter.
*/

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
    /// <param name="configure">Action to configure the entity and its children.</param>
    /// <returns>The created child entity.</returns>
    public static Entity UI<T>(this Entity parent, Action<UIBuilder<T>> configure) where T : Control, new()
    {
        var world = parent.CsWorld();
        var entity = world.Entity().ChildOf(parent).Set(new T());
        var builder = new UIBuilder<T>(world, entity);
        configure(builder);
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
    /// <summary>
    /// Underlying entity
    /// </summary>
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
    public UIBuilder<TChild> Child<TChild>(Action<UIBuilder<TChild>> configure) where TChild : Control, new()
    {
        var child = _entity.UI(configure);
        return new UIBuilder<TChild>(_world, child);
    }

    /// <summary>
    /// Attaches an ui component as a child.
    /// </summary>
    /// <param name="uIComponent"></param>
    /// <returns></returns>
    public UIBuilder<T> Child(IUIComponent uIComponent)
    {
        uIComponent.Attach(_entity);
        return this;
    }

    /// <summary>
    /// Sets the margin on the control.
    /// </summary>
    /// <param name="margin">The margin to set.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public UIBuilder<T> SetMargin(Thickness margin)
    {
        _entity.SetMargin(margin);
        return this;
    }

    /// <summary>
    /// Sets the column for this control in a Grid.
    /// </summary>
    /// <param name="column">The zero-based column index.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public UIBuilder<T> SetColumn(int column)
    {
        _entity.SetColumn(column);
        return this;
    }

    /// <summary>
    /// Given a string creates a column definition and sets it for the ui-element if it has a grid component.
    /// </summary>
    /// <param name="columnDefinitions"></param>
    /// <returns></returns>
    public UIBuilder<T> SetColumnDefinitions(string columnDefinitions)
    {
        _entity.SetColumnDefinitions(columnDefinitions);
        return this;
    }

    /// <summary>
    /// Given a string creates a row definition and sets it for the ui-element if it has a grid component.
    /// </summary>
    /// <param name="rowDefinitions"></param>
    /// <returns></returns>
    public UIBuilder<T> SetRowDefinitions(string rowDefinitions)
    {
        _entity.SetRowDefinitions(rowDefinitions);
        return this;
    }

    /// <summary>
    /// Sets the text of a TextBox or TextBlock control component of an entity
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public UIBuilder<T> SetText(string text)
    {
        _entity.SetText(text);
        return this;
    }
}

/* EXAMPLE USAGE

private Entity CreateUILayout()
{
    ContentQueuePage contentQueuePage = new(_world);
    KnowledgeVaultPage knowledgeVaultPage = new(_world);
    SettingsPage settingsPage = new(_world);
    HomePage homePage = new(_world);
    LiteraturePage literaturePage = new(_world);
    SpacedRepetitionPage spacedRepetitionPage = new(_world);

    return _world.UI<NavigationView>(nav => {
        nav.Property("PaneTitle", "Stella Learning")
           .Column(0);
        
        // Child elements are nested in the lambda, showing hierarchy
        nav.Child<ScrollViewer>(scroll => {
            scroll.Child<StackPanel>(stack => {
                stack.Child<Grid>(grid => {
                    grid.Property("ColumnDefinitions", "2,*,*")
                       .Property("RowDefinitions", "Auto");
                });
            });
        });
        
        // Attach pages
        ((IUIComponent)spacedRepetitionPage).Attach(_entity);
        
        // Navigation items are clearly children of nav
        nav.Child<NavigationViewItem>(item => {
            item.Property("Content", "Home");
        });
        
        nav.Child<NavigationViewItem>(item => {
            item.Property("Content", "Knowledge Vault");
        });
        
        nav.Child<NavigationViewItem>(item => {
            item.Property("Content", "Content Queue");
        });
        
        // ... other navigation items and event handlers
    });
}
*/