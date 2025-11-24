using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace NanoRpc.Protocol;

[AttributeUsage(AttributeTargets.Method)]
public class NanoActionAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
}

/// <summary>
/// Marker interface for type safety. All actors must implement this interface.
/// </summary>
public interface INanoActor { }

public enum MsgType : byte
{
    // RPC messages
    Call = 0x01,
    Cast = 0x02,
    Reply = 0x03,
    Error = 0x04,
    Handshake = 0x05,

    // Pub/Sub messages
    Subscribe = 0x10,
    Unsubscribe = 0x11,
    Publish = 0x12,

    // Streaming messages
    StreamStart = 0x20,
    StreamData = 0x21,
    StreamEnd = 0x22,
    StreamCancel = 0x23
}

/// <summary>
/// Protocol constants and limits for security and validation.
/// </summary>
public static class NanoLimits
{
    public const int MaxTargetLength = 256;
    public const int MaxMethodLength = 256;
    public const int MaxBodyLength = 16 * 1024 * 1024; // 16 MB max payload
    public const int MaxTotalFrameSize = MaxTargetLength + MaxMethodLength + MaxBodyLength + NanoHeader.Size;
}

public readonly struct NanoHeader
{
    public const int Size = 17;

    public readonly MsgType Type;
    public readonly uint Id;
    public readonly int TargetLen;
    public readonly int MethodLen;
    public readonly int BodyLen;

    public NanoHeader(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < Size)
            throw new ArgumentException($"Buffer must be at least {Size} bytes", nameof(buffer));

        Type = (MsgType)buffer[0];
        Id = BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]);
        TargetLen = (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[5..]);
        MethodLen = (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[9..]);
        BodyLen = (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[13..]);
    }

    /// <summary>
    /// Validates header values are within acceptable limits.
    /// </summary>
    public readonly bool IsValid() =>
        Enum.IsDefined(Type) &&
        TargetLen >= 0 && TargetLen <= NanoLimits.MaxTargetLength &&
        MethodLen >= 0 && MethodLen <= NanoLimits.MaxMethodLength &&
        BodyLen >= 0 && BodyLen <= NanoLimits.MaxBodyLength;

    public readonly int TotalBodyLength => TargetLen + MethodLen + BodyLen;
}

public static class WireFormatter
{
    /// <summary>
    /// Writes a complete frame to the buffer writer.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when target or method exceed length limits.</exception>
    public static void WriteFrame<T>(IBufferWriter<byte> writer, MsgType type, uint id, string target, string method, T payload)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(method);

        // Calculate sizes first to avoid multiple allocations
        int tLen = Encoding.UTF8.GetByteCount(target);
        int mLen = Encoding.UTF8.GetByteCount(method);

        // Validate limits before serialization to fail fast
        if (tLen > NanoLimits.MaxTargetLength)
            throw new ArgumentException($"Target name exceeds maximum length of {NanoLimits.MaxTargetLength}", nameof(target));
        if (mLen > NanoLimits.MaxMethodLength)
            throw new ArgumentException($"Method name exceeds maximum length of {NanoLimits.MaxMethodLength}", nameof(method));

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);

        if (jsonBytes.Length > NanoLimits.MaxBodyLength)
            throw new ArgumentException($"Payload exceeds maximum size of {NanoLimits.MaxBodyLength} bytes");

        int totalSize = NanoHeader.Size + tLen + mLen + jsonBytes.Length;
        Span<byte> span = writer.GetSpan(totalSize);

        // Header
        span[0] = (byte)type;
        BinaryPrimitives.WriteUInt32BigEndian(span[1..], id);
        BinaryPrimitives.WriteUInt32BigEndian(span[5..], (uint)tLen);
        BinaryPrimitives.WriteUInt32BigEndian(span[9..], (uint)mLen);
        BinaryPrimitives.WriteUInt32BigEndian(span[13..], (uint)jsonBytes.Length);

        // Payload
        Encoding.UTF8.GetBytes(target, span[NanoHeader.Size..]);
        Encoding.UTF8.GetBytes(method, span[(NanoHeader.Size + tLen)..]);
        jsonBytes.CopyTo(span[(NanoHeader.Size + tLen + mLen)..]);

        writer.Advance(totalSize);
    }

    /// <summary>
    /// Writes a frame with pre-serialized JSON bytes to avoid double serialization.
    /// </summary>
    public static void WriteFrameRaw(IBufferWriter<byte> writer, MsgType type, uint id, string target, string method, ReadOnlySpan<byte> jsonPayload)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(method);

        int tLen = Encoding.UTF8.GetByteCount(target);
        int mLen = Encoding.UTF8.GetByteCount(method);

        if (tLen > NanoLimits.MaxTargetLength)
            throw new ArgumentException($"Target name exceeds maximum length of {NanoLimits.MaxTargetLength}", nameof(target));
        if (mLen > NanoLimits.MaxMethodLength)
            throw new ArgumentException($"Method name exceeds maximum length of {NanoLimits.MaxMethodLength}", nameof(method));
        if (jsonPayload.Length > NanoLimits.MaxBodyLength)
            throw new ArgumentException($"Payload exceeds maximum size of {NanoLimits.MaxBodyLength} bytes");

        int totalSize = NanoHeader.Size + tLen + mLen + jsonPayload.Length;
        Span<byte> span = writer.GetSpan(totalSize);

        // Header
        span[0] = (byte)type;
        BinaryPrimitives.WriteUInt32BigEndian(span[1..], id);
        BinaryPrimitives.WriteUInt32BigEndian(span[5..], (uint)tLen);
        BinaryPrimitives.WriteUInt32BigEndian(span[9..], (uint)mLen);
        BinaryPrimitives.WriteUInt32BigEndian(span[13..], (uint)jsonPayload.Length);

        // Payload
        Encoding.UTF8.GetBytes(target, span[NanoHeader.Size..]);
        Encoding.UTF8.GetBytes(method, span[(NanoHeader.Size + tLen)..]);
        jsonPayload.CopyTo(span[(NanoHeader.Size + tLen + mLen)..]);

        writer.Advance(totalSize);
    }
}

/// <summary>
/// Custom exception for RPC-related errors.
/// </summary>
public class NanoRpcException : Exception
{
    public string? Target { get; }
    public string? Method { get; }

    public NanoRpcException(string message) : base(message) { }

    public NanoRpcException(string message, string target, string method) : base(message)
    {
        Target = target;
        Method = method;
    }

    public NanoRpcException(string message, Exception innerException) : base(message, innerException) { }
}