using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls;


/// <summary>
/// Should be thrown when we want to use a field/property of an avalonia 
/// and expect it to be there.
/// </summary>
public class PropertyNotFoundException : Exception
{
    /// <summary>
    /// Constructor for PropertyNotFoundException
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="componentType"></param>
    /// <param name="expectedProperty"></param>
    /// <param name="methodName"></param>
    public PropertyNotFoundException(Entity entity, Type componentType, string expectedProperty, string methodName)
                    : base(@$"Method '{methodName}' expected property'{expectedProperty}' not found in component {componentType} on entity:

               ID: {entity}
               Name: {(entity.Name() != "" ? entity.Name() : "<no name>")}
               Path: {entity.Path()}
               Parent: {(entity.Parent() != 0 ? entity.Parent().ToString() : "<no parent>")}
               Components: {entity.Type().Str() + "\n"}

            Try adding a avalonia object that has the property:'{expectedProperty}' as a component to this entity.")

    {
        ExpectedProperty = expectedProperty;
        Entity = entity;
    }

    /// <summary>
    /// The exepected property that was not found.
    /// </summary>
    public string ExpectedProperty { get; set; }

    /// <summary>
    /// The entity that the property was not found on.
    /// </summary>
    public Entity Entity { get; set; }
}

