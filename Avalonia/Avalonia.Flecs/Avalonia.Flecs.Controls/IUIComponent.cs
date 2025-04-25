using Avalonia.Controls;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls;

// TODO: Think about removing the interface in favor of the UIBuilder.

/// <summary>
/// Defines an interface for UI Components that are made out of entities.
/// </summary>
public interface IUIComponent
{
    /// <summary>
    /// Root Entity of the component.
    /// You can use this to get access to the entire ui
    /// entity tree handle with great care.
    /// </summary>
    Entity Root { get; }

    /// <summary>
    /// Attaches the UI Component to another entity using the childOf relationship.
    /// In turn creating a UI-TREE out of entities.
    /// </summary>
    /// <param name="parent"></param>
    void Attach(Entity parent)
    {
        Root.ChildOf(parent);
    }

    /// <summary>
    /// Removes ui component from the UI tree;
    /// </summary>
    void Detach()
    {
        Root.Remove(Ecs.ChildOf);
    }

    /// <summary>
    /// Sets the margin of the underlying control element
    /// </summary>
    /// <param name="margin">New Margin</param>
    void SetMargin(Thickness margin)
    {
        Root.Get<Control>().Margin = margin;
    }

    /// <summary>
    /// Gets the margin of the underlying control element
    /// </summary>
    Thickness GetMargin()
    {
        return Root.Get<Control>().Margin;
    }
}
