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
        using var reader = new LockFreeBufferReader(name);

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
        using var reader = new LockFreeBufferReader(name);

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
        using var reader = new LockFreeBufferReader(name);

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
        using var reader = new LockFreeBufferReader(name);

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
        var capacity = 2048; // Small capacity to force a wrap
        var message1 = new byte[400];
        var message2 = new byte[400];
        var message3 = new byte[400]; // This will overwrite message1
        Array.Fill(message1, (byte)1);
        Array.Fill(message2, (byte)2);
        Array.Fill(message3, (byte)3);

        using var writer = new MultiWriterCircularBuffer(name, SharedMemoryMode.Create, capacity, WriteBehavior.Overwrite);
        using var reader = new LockFreeBufferReader(name);

        // Act
        writer.Write(message1);
        writer.Write(message2);
        // At this point the buffer is full. The next write will wrap around.

        // Reader reads the first message, advancing its position.
        Assert.Equal(message1, reader.Read());

        // Now writer writes the third message. Since the reader has moved,
        // it overwrites the space previously held by message1.
        writer.Write(message3);

        // Assert
        var readMsg2 = reader.Read();
        var readMsg3 = reader.Read();

        Assert.Equal(message2, readMsg2);
        Assert.Equal(message3, readMsg3); // We read message3 because it overwrote message1's slot
        Assert.Null(reader.Read());
    }

    [Fact]
    public void Write_Throws_Exception_When_Message_Is_Larger_Than_Capacity()
    {
        // Arrange
        var name = GetUniqueName("TooLargeTest");
        var capacity = 1000;
        var largeMessage = new byte[2000];

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
        using var reader1 = new LockFreeBufferReader(name);
        using var reader2 = new LockFreeBufferReader(name);

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
        using var reader = new LockFreeBufferReader(name);
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

    // --- Backpressure Tests ---

    [Fact]
    public async Task Writer_Blocks_When_Buffer_Is_Full_And_Unblocks_After_Read()
    {
        // Arrange
        var name = GetUniqueName("BlockUnblockTest");
        const int capacity = 2048; // Capacity for ~3 messages
        var message1 = new byte[400];
        var message2 = new byte[400];
        var message3 = new byte[400];
        Array.Fill(message1, (byte)1);
        Array.Fill(message2, (byte)2);
        Array.Fill(message3, (byte)3);

        using var writer = new MultiWriterCircularBuffer(name, SharedMemoryMode.Create, capacity, WriteBehavior.Block);
        using var reader = new LockFreeBufferReader(name);

        // Act
        writer.Write(message1);
        writer.Write(message2);

        var writerTask = Task.Run(() => writer.Write(message3));

        // Assert
        var isBlocked = await Task.WhenAny(writerTask, Task.Delay(500)) == writerTask;
        Assert.True(isBlocked, "Writer should block when buffer is full.");

        var readMsg1 = reader.Read();
        Assert.Equal(message1, readMsg1);

        var completedTask = await Task.WhenAny(writerTask, Task.Delay(500));
        var completedInTime = completedTask == writerTask;
        Assert.True(completedInTime, "Writer should unblock and complete after a read.");

        await writerTask; // Ensure the writer task is complete

        var readMsg2 = reader.Read();
        var readMsg3 = reader.Read();
        Assert.Equal(message2, readMsg2);
    }

    [Fact]
    public async Task Writer_Blocks_On_Slowest_Reader()
    {
        // Arrange
        var name = GetUniqueName("SlowestReaderTest");
        var capacity = 2048;
        var message1 = new byte[400];
        var message2 = new byte[400];
        var message3 = new byte[400];

        using var writer = new MultiWriterCircularBuffer(name, SharedMemoryMode.Create, capacity, WriteBehavior.Block);
        using var fastReader = new LockFreeBufferReader(name);
        using var slowReader = new LockFreeBufferReader(name);

        // Act
        writer.Write(message1);
        writer.Write(message2);

        Assert.Equal(message1, fastReader.Read());
        Assert.Equal(message2, fastReader.Read());

        var writerTask = Task.Run(() => writer.Write(message3));

        // Assert
        var isBlocked = await Task.WhenAny(writerTask, Task.Delay(500)) == writerTask;
        Assert.True(isBlocked, "Writer should block based on the slowest reader.");

        Assert.Equal(message1, slowReader.Read());

        var completedTask = await Task.WhenAny(writerTask, Task.Delay(500));
        var completedInTime = completedTask == writerTask;
        Assert.True(completedInTime, "Writer should unblock after the slowest reader makes progress.");

        await writerTask; // Ensure the writer task is complete

        Assert.Equal(message2, slowReader.Read());
        Assert.Equal(message3, slowReader.Read());
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
