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
        /// <summary>
        /// Constructor for ComponentNotFoundException
        /// </summary>
        public ComponentNotFoundException() : base()
        {
        }

        /// <summary>
        /// Constructor for ComponentNotFoundException
        /// </summary>
        /// <param name="message"></param>
        public ComponentNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructor for ComponentNotFoundException
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ComponentNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor for ComponentNotFoundException
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="componentType"></param>
        /// <param name="methodName"></param>
        public ComponentNotFoundException(Entity entity, Type componentType, string methodName)
                    : base(@$"Method '{methodName}' expected component '{componentType}' on entity:

               ID: {entity}
               Name: {(entity.Name() != "" ? entity.Name() : "<no name>")}
               Path: {entity.Path()}
               Parent: {(entity.Parent() != 0 ? entity.Parent().ToString() : "<no parent>")}
               Components: {entity.Type().Str() + "\n"}

            Try adding '{componentType}' as a component to this entity.")
        {
        }
        /// <summary>
        /// Constructor for ComponentNotFoundException
        /// </summary>
        /// <param name="message"></param>
        /// <param name="componentType"></param>
        /// <param name="entity"></param>
        public ComponentNotFoundException(string message, Type componentType, Entity entity) : base(message)
        {
        }
    }
}