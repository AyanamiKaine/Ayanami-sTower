using InvictaDB.Messaging;

namespace InvictaDB.Tests;

/// <summary>
/// Unit tests for the messaging system.
/// </summary>
public class MessagingUnitTest
{
    internal record TestMessage(string Content);
    internal record AnotherMessage(int Value);

    #region GameMessage Tests

    /// <summary>
    /// GameMessage.Create should create a message with correct properties.
    /// </summary>
    [Fact]
    public void GameMessage_Create_SetsPropertiesCorrectly()
    {
        var message = GameMessage.Create("TestType", "TestSender", "payload");

        Assert.Equal("TestType", message.MessageType);
        Assert.Equal("TestSender", message.Sender);
        Assert.Equal("payload", message.Payload);
        Assert.True(message.Timestamp > 0);
    }

    /// <summary>
    /// GameMessage.Create with generic should use type name as message type.
    /// </summary>
    [Fact]
    public void GameMessage_CreateGeneric_UsesTypeNameAsMessageType()
    {
        var payload = new TestMessage("Hello");
        var message = GameMessage.Create("TestSender", payload);

        Assert.Equal("TestMessage", message.MessageType);
        Assert.Equal(payload, message.Payload);
    }

    /// <summary>
    /// GameMessage.GetPayload should return typed payload.
    /// </summary>
    [Fact]
    public void GameMessage_GetPayload_ReturnsTypedPayload()
    {
        var payload = new TestMessage("Hello");
        var message = GameMessage.Create("TestSender", payload);

        var retrieved = message.GetPayload<TestMessage>();

        Assert.NotNull(retrieved);
        Assert.Equal("Hello", retrieved.Content);
    }

    /// <summary>
    /// GameMessage.GetPayload should return default for wrong type.
    /// </summary>
    [Fact]
    public void GameMessage_GetPayload_ReturnsDefaultForWrongType()
    {
        var message = GameMessage.Create("TestSender", new TestMessage("Hello"));

        var retrieved = message.GetPayload<AnotherMessage>();

        Assert.Null(retrieved);
    }

    /// <summary>
    /// GameMessage.IsType should match by string.
    /// </summary>
    [Fact]
    public void GameMessage_IsType_MatchesByString()
    {
        var message = GameMessage.Create("TestType", "Sender", null);

        Assert.True(message.IsType("TestType"));
        Assert.False(message.IsType("OtherType"));
    }

    /// <summary>
    /// GameMessage.IsType should match by generic type.
    /// </summary>
    [Fact]
    public void GameMessage_IsType_MatchesByGenericType()
    {
        var message = GameMessage.Create("Sender", new TestMessage("Hello"));

        Assert.True(message.IsType<TestMessage>());
        Assert.False(message.IsType<AnotherMessage>());
    }

    #endregion

    #region MessageQueue Tests

    /// <summary>
    /// Empty queue should have zero count.
    /// </summary>
    [Fact]
    public void MessageQueue_Empty_HasZeroCount()
    {
        Assert.Equal(0, MessageQueue.Empty.Count);
        Assert.True(MessageQueue.Empty.IsEmpty);
    }

    /// <summary>
    /// Enqueue should add message to queue.
    /// </summary>
    [Fact]
    public void MessageQueue_Enqueue_AddsMessage()
    {
        var queue = MessageQueue.Empty;
        var message = GameMessage.Create("Type", "Sender", null);

        queue = queue.Enqueue(message);

        Assert.Equal(1, queue.Count);
        Assert.False(queue.IsEmpty);
    }

    /// <summary>
    /// Dequeue should return message and new queue.
    /// </summary>
    [Fact]
    public void MessageQueue_Dequeue_ReturnsMessageAndNewQueue()
    {
        var message = GameMessage.Create("Type", "Sender", null);
        var queue = MessageQueue.Empty.Enqueue(message);

        var (dequeued, newQueue) = queue.Dequeue();

        Assert.Equal(message, dequeued);
        Assert.Equal(0, newQueue.Count);
    }

    /// <summary>
    /// Dequeue on empty queue should return null.
    /// </summary>
    [Fact]
    public void MessageQueue_Dequeue_EmptyReturnsNull()
    {
        var (message, queue) = MessageQueue.Empty.Dequeue();

        Assert.Null(message);
        Assert.Same(MessageQueue.Empty, queue);
    }

    /// <summary>
    /// GetMessages should filter by type.
    /// </summary>
    [Fact]
    public void MessageQueue_GetMessages_FiltersByType()
    {
        var queue = MessageQueue.Empty
            .Enqueue(GameMessage.Create("Type1", "Sender", null))
            .Enqueue(GameMessage.Create("Type2", "Sender", null))
            .Enqueue(GameMessage.Create("Type1", "Sender", null));

        var type1Messages = queue.GetMessages("Type1").ToList();
        var type2Messages = queue.GetMessages("Type2").ToList();

        Assert.Equal(2, type1Messages.Count);
        Assert.Single(type2Messages);
    }

    /// <summary>
    /// GetMessagesFrom should filter by sender.
    /// </summary>
    [Fact]
    public void MessageQueue_GetMessagesFrom_FiltersBySender()
    {
        var queue = MessageQueue.Empty
            .Enqueue(GameMessage.Create("Type", "Sender1", null))
            .Enqueue(GameMessage.Create("Type", "Sender2", null))
            .Enqueue(GameMessage.Create("Type", "Sender1", null));

        var sender1Messages = queue.GetMessagesFrom("Sender1").ToList();

        Assert.Equal(2, sender1Messages.Count);
    }

    /// <summary>
    /// ConsumeMessages should return messages and remove them from queue.
    /// </summary>
    [Fact]
    public void MessageQueue_ConsumeMessages_ReturnsAndRemoves()
    {
        var queue = MessageQueue.Empty
            .Enqueue(GameMessage.Create("Type1", "Sender", null))
            .Enqueue(GameMessage.Create("Type2", "Sender", null))
            .Enqueue(GameMessage.Create("Type1", "Sender", null));

        var (consumed, newQueue) = queue.ConsumeMessages("Type1");

        Assert.Equal(2, consumed.Count);
        Assert.Equal(1, newQueue.Count);
        Assert.Empty(newQueue.GetMessages("Type1"));
    }

    #endregion

    #region InvictaDatabase Messaging Tests

    /// <summary>
    /// SendMessage should add message to queue.
    /// </summary>
    [Fact]
    public void Database_SendMessage_AddsToQueue()
    {
        var db = new InvictaDatabase();
        var message = GameMessage.Create("Type", "Sender", null);

        db = db.SendMessage(message);

        Assert.Equal(1, db.Messages.Count);
    }

    /// <summary>
    /// SendMessage with parameters should create and add message.
    /// </summary>
    [Fact]
    public void Database_SendMessageWithParams_CreatesAndAdds()
    {
        var db = new InvictaDatabase();

        db = db.SendMessage("TestType", "TestSender", "payload");

        Assert.Equal(1, db.Messages.Count);
        var message = db.Messages.Peek();
        Assert.NotNull(message);
        Assert.Equal("TestType", message.MessageType);
        Assert.Equal("TestSender", message.Sender);
    }

    /// <summary>
    /// SendMessage generic should use type name.
    /// </summary>
    [Fact]
    public void Database_SendMessageGeneric_UsesTypeName()
    {
        var db = new InvictaDatabase();

        db = db.SendMessage("Sender", new TestMessage("Hello"));

        var message = db.Messages.Peek();
        Assert.NotNull(message);
        Assert.Equal("TestMessage", message.MessageType);
    }

    /// <summary>
    /// ConsumeMessages should return messages and remove from database.
    /// </summary>
    [Fact]
    public void Database_ConsumeMessages_ReturnsAndRemoves()
    {
        var db = new InvictaDatabase()
            .SendMessage("Type1", "Sender", null)
            .SendMessage("Type2", "Sender", null)
            .SendMessage("Type1", "Sender", null);

        var (consumed, newDb) = db.ConsumeMessages("Type1");

        Assert.Equal(2, consumed.Count);
        Assert.Equal(1, newDb.Messages.Count);
    }

    /// <summary>
    /// ClearMessages should empty the queue.
    /// </summary>
    [Fact]
    public void Database_ClearMessages_EmptiesQueue()
    {
        var db = new InvictaDatabase()
            .SendMessage("Type1", "Sender", null)
            .SendMessage("Type2", "Sender", null);

        db = db.ClearMessages();

        Assert.True(db.Messages.IsEmpty);
    }

    /// <summary>
    /// Messages should be immutable across database instances.
    /// </summary>
    [Fact]
    public void Database_Messages_ImmutableAcrossInstances()
    {
        var db1 = new InvictaDatabase();
        var db2 = db1.SendMessage("Type", "Sender", null);

        Assert.True(db1.Messages.IsEmpty);
        Assert.Equal(1, db2.Messages.Count);
    }

    /// <summary>
    /// Database state should be preserved when sending messages.
    /// </summary>
    [Fact]
    public void Database_SendMessage_PreservesState()
    {
        var db = new InvictaDatabase()
            .RegisterTable<TestMessage>()
            .Insert("1", new TestMessage("Hello"));

        db = db.SendMessage("Type", "Sender", null);

        Assert.True(db.TableExists<TestMessage>());
        Assert.Equal("Hello", db.GetEntry<TestMessage>("1").Content);
    }

    #endregion
}
