using System.Diagnostics.CodeAnalysis;
using AyanamisTower.StellaSharedMemory.Serialization;

namespace AyanamisTower.StellaSharedMemory;

/// <summary>
/// Provides extension methods for reading and writing serializable objects.
/// </summary>
public static class StellaSharedMemoryExtensions
{
    /// <summary>
    /// Writes a serializable object to the shared memory.
    /// </summary>
    public static void Write<T>(this SharedMemory sharedMemory, T data, ISerializer serializer)
    {
        var bytes = serializer.Serialize(data);
        var viewStream = sharedMemory.ViewStream;

        viewStream.Seek(0, SeekOrigin.Begin);

        using (var writer = new BinaryWriter(viewStream, System.Text.Encoding.UTF8, true))
        {
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
        viewStream.SetLength(viewStream.Position);
    }

    /// <summary>
    /// Reads a serializable object from the shared memory. Throws an exception on failure.
    /// </summary>
    public static T Read<T>(this SharedMemory sharedMemory, ISerializer serializer)
    {
        var viewStream = sharedMemory.ViewStream;
        viewStream.Seek(0, SeekOrigin.Begin);

        using var reader = new BinaryReader(viewStream, System.Text.Encoding.UTF8, true);
        if (viewStream.Length < 4)
        {
            throw new EndOfStreamException(
                "Shared memory is too small to contain a length prefix."
            );
        }

        int length = reader.ReadInt32();

        if (viewStream.Length < 4 + length)
        {
            throw new EndOfStreamException(
                "Shared memory content is shorter than the expected length."
            );
        }

        byte[] data = reader.ReadBytes(length);
        return serializer.Deserialize<T>(data);
    }

    /// <summary>
    /// Attempts to read a serializable object from the shared memory. Does not throw on failure.
    /// </summary>
    /// <typeparam name="T">The type of the object to read.</typeparam>
    /// <param name="sharedMemory">The shared memory instance.</param>
    /// <param name="serializer">The serializer to use.</param>
    /// <param name="result">When this method returns, contains the deserialized object if successful; otherwise, the default value for T.</param>
    /// <returns>true if an object was read and deserialized successfully; otherwise, false.</returns>
    public static bool TryRead<T>(
        this SharedMemory sharedMemory,
        ISerializer serializer,
        [MaybeNullWhen(false)] out T result
    )
    {
        var viewStream = sharedMemory.ViewStream;
        viewStream.Seek(0, SeekOrigin.Begin);

        // Cannot read if the stream is too short for even a length prefix
        if (viewStream.Length < 4)
        {
            result = default;
            return false;
        }

        using var reader = new BinaryReader(viewStream, System.Text.Encoding.UTF8, true);
        try
        {
            int length = reader.ReadInt32();

            // Cannot read if the data is shorter than specified by the prefix
            if (length < 0 || viewStream.Length < 4 + length)
            {
                result = default;
                return false;
            }

            byte[] data = reader.ReadBytes(length);
            return serializer.TryDeserialize(data, out result);
        }
        catch (IOException)
        {
            // Catch potential IO errors during reading
            result = default;
            return false;
        }
    }
}
