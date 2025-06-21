using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AyanamisTower.StellaSharedMemory.Serialization;

/// <summary>
/// Provides an interface for serializing and deserializing objects.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Serializes the specified object into a byte array.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A byte array representing the serialized object.</returns>
    byte[] Serialize<T>(T obj);

    /// <summary>
    /// Deserializes a byte array into an object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize.</typeparam>
    /// <param name="data">The byte array to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    T Deserialize<T>(byte[] data);

    /// <summary>
    /// Attempts to deserialize a byte array into an object.
    /// </summary>
    /// <param name="data">The byte array to deserialize.</param>
    /// <param name="result">The deserialized object, or null if deserialization fails.</param>
    /// <returns>True if deserialization was successful and the result is not null, otherwise false.</returns>
    bool TryDeserialize<T>(byte[] data, [MaybeNullWhen(false)] out T result);
}
