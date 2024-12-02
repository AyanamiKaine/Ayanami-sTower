using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls
{

    /*
    DESIGN NOTES:
    - Maybe we want to include all currently attached components in the exception message.
    */

    /// <summary>
    /// Exception thrown when a component is not found on an entity.
    /// </summary>
    public class ComponentNotFoundException : Exception
    {
        public ComponentNotFoundException(Entity entity, Type componentType, string methodName)
                    : base(@$"Method '{methodName}' expected component '{componentType}' on entity:

               ID: {entity}
               Name: {(entity.Name() != "" ? entity.Name() : "<no name>")}
               Path: {entity.Path()}
               Parent: {(entity.Parent() != 0 ? entity.Parent().ToString() : "<no parent>")}
               Components: {entity.Type().Str() + "\n"}

            Try adding '{componentType}' as a component to this entity.")
        {
            ComponentType = componentType;
            Entity = entity;
        }
        public ComponentNotFoundException(string message, Type componentType, Entity entity) : base(message)
        {
            ComponentType = componentType;
            Entity = entity;
        }

        public object ComponentType { get; set; }
        public Entity Entity { get; set; }
    }
}