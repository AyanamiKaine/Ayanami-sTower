using MessagePack;
using MemoryPack;
using ProtoBuf;
using System.Diagnostics.CodeAnalysis;

namespace AyanamisTower.StellaSharedMemory.Serialization;


/// <summary>
/// Serializes objects using System.Text.Json.
/// </summary>
public class JsonSerializer : ISerializer
{
    /// <inheritdoc/>
    public byte[] Serialize<T>(T obj) => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj);
    /// <inheritdoc/>
    public T Deserialize<T>(byte[] data) => System.Text.Json.JsonSerializer.Deserialize<T>(data)!;
    /// <inheritdoc/>
    public bool TryDeserialize<T>(byte[] data, [MaybeNullWhen(false)] out T result)
    {
        try
        {
            result = System.Text.Json.JsonSerializer.Deserialize<T>(data);
            return result != null;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}

/// <summary>
/// Serializes objects using MessagePack.
/// Requires the 'MessagePack' NuGet package.
/// </summary>
public class MessagePackObjectSerializer : ISerializer
{
    /// <inheritdoc/>
    public byte[] Serialize<T>(T obj) => MessagePackSerializer.Serialize(obj);
    /// <inheritdoc/>
    public T Deserialize<T>(byte[] data) => MessagePackSerializer.Deserialize<T>(data);

    /// <inheritdoc/>
    public bool TryDeserialize<T>(byte[] data, [MaybeNullWhen(false)] out T result)
    {
        try
        {
            result = MessagePack.MessagePackSerializer.Deserialize<T>(data);
            return result != null;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}

/// <summary>
/// Serializes objects using MemoryPack.
/// Requires the 'MemoryPack' NuGet package.
/// Your data object [T] must be a partial type decorated with [MemoryPackable].
/// Most performant serializer, lowest latency
/// </summary>
public class MemoryPackObjectSerializer : ISerializer
{
    /// <inheritdoc/>
    public byte[] Serialize<T>(T obj) => MemoryPackSerializer.Serialize(obj);
    /// <inheritdoc/>
    public T Deserialize<T>(byte[] data) => MemoryPackSerializer.Deserialize<T>(data)!;
    /// <inheritdoc/>
    public bool TryDeserialize<T>(byte[] data, [MaybeNullWhen(false)] out T result)
    {
        try
        {
            result = MemoryPack.MemoryPackSerializer.Deserialize<T>(data);
            return result != null;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}

/// <summary>
/// Serializes objects using Protobuf.
/// Requires the 'protobuf-net' NuGet package.
/// </summary>
public class ProtobufObjectSerializer : ISerializer
{
    /// <inheritdoc/>
    public byte[] Serialize<T>(T obj)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, obj);
        return stream.ToArray();
    }
    /// <inheritdoc/>
    public T Deserialize<T>(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return Serializer.Deserialize<T>(stream);
    }

    /// <inheritdoc/>
    public bool TryDeserialize<T>(byte[] data, [MaybeNullWhen(false)] out T result)
    {
        using var stream = new MemoryStream(data);

        try
        {
            result = Serializer.Deserialize<T>(stream);
            return result != null;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}