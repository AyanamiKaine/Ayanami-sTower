#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Text.Json;

namespace AyanamisTower.StellaEcs.Serialization
{
    /// <summary>
    /// A default implementation of ISerializationHelper that uses System.Text.Json.
    /// </summary>
    public class DefaultSerializationHelper : ISerializationHelper
    {
        private readonly JsonSerializerOptions _options;

        public DefaultSerializationHelper(JsonSerializerOptions? options = null)
        {
            _options = options ?? new JsonSerializerOptions
            {
                WriteIndented = true,
                // FIX: This is crucial to ensure public fields on components are serialized.
                IncludeFields = true
            };
        }

        public JsonElement Serialize(object data)
        {
            // Serialize the object to a temporary JSON string, then parse it into a JsonElement.
            // This is a common way to convert an arbitrary object to a JsonElement.
            var json = JsonSerializer.Serialize(data, data.GetType(), _options);
            return JsonDocument.Parse(json).RootElement;
        }

        public object Deserialize(JsonElement element, Type type)
        {
            // Deserialize the JsonElement directly to the target type.
            return JsonSerializer.Deserialize(element, type, _options)!;
        }
    }
}
