using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls;
/// <summary>
/// Defines an interface for UI Components that are made out of entities.
/// </summary>
public interface IUIComponent
{
    /// <summary>
    /// Attaches the UI Component to another entity using the childOf relationship.
    /// In turn creating a UI-TREE out of entities.
    /// </summary>
    /// <param name="entity"></param>
    public void Attach(Entity entity);

    /// <summary>
    /// Removes ui component from the UI tree;
    /// </summary>
    public void Detach();

    /// <summary>
    /// Sets the margin of the underlying control element
    /// </summary>
    /// <param name="margin">New Margin</param>
    public void SetMargin(Thickness margin);
    /// <summary>
    /// Gets the margin of the underlying control element
    /// </summary>
    public Thickness GetMargin();
}