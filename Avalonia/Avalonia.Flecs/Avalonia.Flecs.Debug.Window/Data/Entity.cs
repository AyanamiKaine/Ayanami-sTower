using CommunityToolkit.Mvvm.ComponentModel;
using Flecs.NET.Core;
namespace Avalonia.Flecs.Debug.Window.Data;

/// <summary>
/// Represents the data of an entity in the ECS world.
/// </summary>
public partial class EntityDataRepresentation : ObservableObject
{
    /// <summary>
    /// Represents the data of an entity in the ECS world.
    /// </summary>
    /// <param name="entity"></param>
    public EntityDataRepresentation(Entity entity)
    {
        UnderlyingEntityRefrence = entity;
        Components = [.. entity.Type().Str().Split(' ')];
        for (int i = 0; i < Components.Count; i++)
        {
            Components[i] = Components[i].Trim(','); // Example: Convert to uppercase
        }
    }

    [ObservableProperty]
    private string _name = "";
    [ObservableProperty]
    private string _description = "";
    [ObservableProperty]
    private List<string> _components = [];
    [ObservableProperty]
    private Entity _underlyingEntityRefrence;

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return UnderlyingEntityRefrence.ToString();
    }
}