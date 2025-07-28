using System;
using System.Text.Json;

namespace AyanamisTower.StellaEcs.Serialization
{
    /// <summary>
    /// Defines a helper responsible for serializing and deserializing component and relationship data.
    /// This allows for custom logic, especially for runtime-defined types.
    /// </summary>
    public interface ISerializationHelper
    {
        /// <summary>
        /// Serializes a component or relationship data object into a JsonElement.
        /// </summary>
        JsonElement Serialize(object data);

        /// <summary>
        /// Deserializes a JsonElement back into a component or relationship data object of the specified type.
        /// </summary>
        object Deserialize(JsonElement element, Type type);
    }
}
