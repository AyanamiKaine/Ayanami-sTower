using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AyanamisTower.StellaSharedMemory.Serialization;

namespace AyanamisTower.StellaSharedMemory;

/// <summary>
/// Provides extension methods for circular buffer operations.
/// </summary>
public static class CircularBufferExtensions
{
    /// <summary>
    /// Writes a serializable object to the circular buffer.
    /// </summary>
    public static void Write<T>(
        this MultiWriterCircularBuffer buffer,
        T data,
        ISerializer serializer
    )
    {
        byte[] bytes = serializer.Serialize(data);
        buffer.Write(bytes);
    }

    /// <summary>
    /// Reads a serializable object from the circular buffer. Throws an exception on failure.
    /// </summary>
    /// <returns>The deserialized object. Returns default(T) if no message is available.</returns>
    public static T? Read<T>(this LockFreeBufferReader reader, ISerializer serializer)
    {
        byte[]? data = reader.Read();
        if (data == null)
        {
            return default(T); // Nothing to read
        }
        return serializer.Deserialize<T>(data);
    }

    /// <summary>
    /// Attempts to read a serializable object from the circular buffer. Does not throw on failure.
    /// </summary>
    /// <returns>true if an object was read and deserialized successfully; otherwise, false.</returns>
    public static bool TryRead<T>(
        this LockFreeBufferReader reader,
        ISerializer serializer,
        [MaybeNullWhen(false)] out T result
    )
    {
        byte[]? data = reader.Read();
        if (data == null)
        {
            result = default;
            return false;
        }

        return serializer.TryDeserialize(data, out result);
    }
}
