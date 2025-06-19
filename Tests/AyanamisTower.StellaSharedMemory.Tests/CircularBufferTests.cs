using AyanamisTower.StellaSharedMemory;
using AyanamisTower.StellaSharedMemory.Serialization;
using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

namespace AyanamisTower.StellaSharedMemory.Tests;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


/// <summary>
/// A simple data structure for serialization tests.
/// Using a record struct for easy value-based equality.
/// </summary>
public record TestMessage(int Id, string Content, DateTime Timestamp);

public class CircularBufferTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly List<string> _testFileNames = new();

    // Using JsonSerializer for tests, but any implementation of ISerializer would work.
    private readonly ISerializer _serializer = new JsonSerializer();

    public CircularBufferTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Generates a unique name for a memory-mapped file for each test
    /// to ensure tests are isolated and don't interfere with each other.
    /// </summary>
    private string GetUniqueName(string baseName)
    {
        var uniqueName = $"{baseName}-{Guid.NewGuid()}";
        _testFileNames.Add(uniqueName);
        _testFileNames.Add($"{uniqueName}-reader-Reader1.state");
        _testFileNames.Add($"{uniqueName}-reader-Reader2.state");
        _testFileNames.Add($"{uniqueName}.lock");
        return uniqueName;
    }

    [Fact]
    public void Write_And_Read_Single_Message_Successfully()
    {
        // Arrange
        var name = GetUniqueName("SingleMessageTest");
        var capacity = 1024;
        var message = Encoding.UTF8.GetBytes("Hello, Stella!");

        using var writer = new MultiWriterCircularBuffer(name, SharedMemoryMode.Create, capacity);
        using var reader = new LockFreeBufferReader(name, "Reader1");

        // Act
        writer.Write(message);
        var readMessage = reader.Read();

        // Assert
        Assert.NotNull(readMessage);
        Assert.Equal(message, readMessage);
    }

    [Fact]
    public void Write_And_Read_Using_Serialization_Extensions()
    {
        // Arrange
        var name = GetUniqueName("SerializationTest");
        var capacity = 1024;
        var originalMessage = new TestMessage(1, "Live long and prosper.", DateTime.UtcNow);

        using var writer = new MultiWriterCircularBuffer(name, SharedMemoryMode.Create, capacity);
        using var reader = new LockFreeBufferReader(name, "Reader1");

        // Act
        writer.Write(originalMessage, _serializer);
        var deserializedMessage = reader.Read<TestMessage>(_serializer);

        // Assert
        Assert.NotNull(deserializedMessage);
        Assert.Equal(originalMessage.Id, deserializedMessage.Id);
        Assert.Equal(originalMessage.Content, deserializedMessage.Content);
        // Note: DateTime precision can vary slightly after serialization, so comparing up to the second is safer.
        Assert.Equal(originalMessage.Timestamp.Truncate(TimeSpan.FromSeconds(1)), deserializedMessage.Timestamp.Truncate(TimeSpan.FromSeconds(1)));
    }


    [Fact]
    public void TryRead_Returns_False_And_Default_When_Buffer_Is_Empty()
    {
        // Arrange
        var name = GetUniqueName("TryReadEmptyTest");
        var capacity = 1024;

        using var writer = new MultiWriterCircularBuffer(name, SharedMemoryMode.Create, capacity);
        using var reader = new LockFreeBufferReader(name, "Reader1");

        // Act
        var result = reader.TryRead(_serializer, out TestMessage? message);

        // Assert
        Assert.False(result);
        Assert.Null(message);
    }

    [Fact]
    public void Read_Returns_Null_When_Buffer_Is_Empty()
    {
        // Arrange
        var name = GetUniqueName("ReadEmptyTest");
        var capacity = 1024;
        using var writer = new MultiWriterCircularBuffer(name, SharedMemoryMode.Create, capacity);
        using var reader = new LockFreeBufferReader(name, "Reader1");

        // Act
        var readMessage = reader.Read();

        // Assert
        Assert.Null(readMessage);
    }

    [Fact]
    public void Write_Causes_Buffer_To_Wrap_Around_And_Overwrite()
    {
        // Arrange
        var name = GetUniqueName("WrapAroundOverwriteTest");
        var capacity = 128; // Small capacity to force a wrap and overwrite.
        var message1 = new byte[80];
        var message2 = new byte[30];
        Array.Fill(message1, (byte) 1);
        Array.Fill(message2, (byte) 2);

        using var writer = new MultiWriterCircularBuffer(name, SharedMemoryMode.Create, capacity);
        using var reader = new LockFreeBufferReader(name, "Reader1");

        // Act
        writer.Write(message1); // This fills most of the buffer.
        writer.Write(message2); // This wraps around and overwrites the start of message1.

        // Assert
        // The reader starts at the beginning, where message2 has been written.
        var readMessage = reader.Read();
        var shouldBeNull = reader.Read();

        // We should read message2 because it overwrote message1.
        Assert.NotNull(readMessage);
        Assert.Equal(message2, readMessage);

        // After reading the only message, the buffer should appear empty to the reader.
        Assert.Null(shouldBeNull);
    }

    [Fact]
    public void Write_Throws_Exception_When_Message_Is_Larger_Than_Capacity()
    {
        // Arrange
        var name = GetUniqueName("TooLargeTest");
        var capacity = 100;
        var largeMessage = new byte[200];

        using var writer = new MultiWriterCircularBuffer(name, SharedMemoryMode.Create, capacity);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => writer.Write(largeMessage));
        _output.WriteLine($"Caught expected exception: {ex.Message}");
    }

    [Fact]
    public void Multiple_Readers_Can_Read_Independently()
    {
        // Arrange
        var name = GetUniqueName("MultiReaderTest");
        const int capacity = 1024;
        var message1 = Encoding.UTF8.GetBytes("Message 1");
        var message2 = Encoding.UTF8.GetBytes("Message 2");

        using var writer = new MultiWriterCircularBuffer(name, SharedMemoryMode.Create, capacity);
        using var reader1 = new LockFreeBufferReader(name, "Reader1");
        using var reader2 = new LockFreeBufferReader(name, "Reader2");

        // Act: Write two messages
        writer.Write(message1);
        writer.Write(message2);

        // Assert: Both readers get both messages
        Assert.Equal(message1, reader1.Read());
        Assert.Equal(message2, reader1.Read());
        Assert.Null(reader1.Read());

        Assert.Equal(message1, reader2.Read());
        Assert.Equal(message2, reader2.Read());
        Assert.Null(reader2.Read());
    }

    [Fact]
    public async Task Multiple_Writers_Can_Write_ConcurrentlyAsync()
    {
        // Arrange
        var name = GetUniqueName("MultiWriterTest");
        var capacity = 65536; // 64k
        var writerCount = 5;
        var messagesPerWriter = 100;
        var totalMessages = writerCount * messagesPerWriter;

        using var initialWriter = new MultiWriterCircularBuffer(name, SharedMemoryMode.Create, capacity);

        // FIX: The reader must be created BEFORE the writers start.
        // This ensures its initial read position is at the beginning of the buffer (0),
        // not at the end of the buffer after all writes are complete.
        using var reader = new LockFreeBufferReader(name, "Reader1");
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < writerCount; i++)
        {
            var writerId = i;
            tasks.Add(Task.Run(() =>
            {
                // Each task creates its own writer instance to the same shared memory
                using var writer = new MultiWriterCircularBuffer(name, SharedMemoryMode.Open);
                for (int j = 0; j < messagesPerWriter; j++)
                {
                    var message = new TestMessage(writerId * 1000 + j, $"From writer {writerId}", DateTime.UtcNow);
                    writer.Write(message, _serializer);
                }
            }));
        }

        await Task.WhenAll(tasks); // Wait for all writers to finish

        // Assert
        var receivedMessages = new List<TestMessage>();
        while (reader.TryRead(_serializer, out TestMessage? msg))
        {
            receivedMessages.Add(msg!);
        }

        _output.WriteLine($"Total messages written: {totalMessages}");
        _output.WriteLine($"Total messages read: {receivedMessages.Count}");

        // The most important assertion: did we get all the messages?
        // This confirms the ticket lock prevented data corruption or lost messages.
        Assert.Equal(totalMessages, receivedMessages.Count);

        // Optional: Check for uniqueness to be extra sure
        Assert.Equal(totalMessages, receivedMessages.Select(m => m.Id).Distinct().Count());
    }

    /// <summary>
    /// Cleans up any memory-mapped files created during the tests.
    /// This is crucial because MMFs can persist on disk otherwise.
    /// </summary>
    public void Dispose()
    {
        foreach (var name in _testFileNames)
        {
            var path = Path.Combine(GetSharedDirectory(), name);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException ex)
                {
                    _output.WriteLine($"Could not delete test file '{path}': {ex.Message}");
                }
            }
        }
    }

    private static string GetSharedDirectory() => OperatingSystem.IsLinux() ? "/dev/shm" : Path.GetTempPath();
}

/// <summary>
/// Helper extension for truncating DateTime, useful for comparisons after serialization.
/// </summary>
public static class DateTimeExtensions
{
    public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
    {
        if (timeSpan == TimeSpan.Zero) return dateTime; // Or throw an ArgumentException
        if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue) return dateTime; // Do not modify "guard" values
        return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
    }
}
