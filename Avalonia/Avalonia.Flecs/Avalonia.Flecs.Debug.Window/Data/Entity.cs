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
    /// <param name="name"></param>
    public EntityDataRepresentation(Entity entity, string name)
    {
        Name = name;
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
    /// Path to the entity. (Showing its parent child relationship)
    /// </summary>
    public string Path
    {
        get => UnderlyingEntityRefrence.ToString();
    }

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// Destroys the underlying entity and removes it 
    /// from the ecs world.
    /// </summary>
    public void Destroy()
    {
        UnderlyingEntityRefrence.Destruct();
    }
}