using InvictaDB.Messaging;

namespace InvictaDB.Tests;

/// <summary>
/// Unit tests for the Event Bus system in InvictaDatabase (Merged with Messages).
/// </summary>
public class EventBusUnitTest
{
    internal record TestEvent(string Data);

    /// <summary>
    /// SendMessage should add an event to the Messages queue.
    /// </summary>
    [Fact]
    public void SendMessage_AddsEventToMessages()
    {
        var db = new InvictaDatabase();
        var evt = new TestEvent("Something happened");

        db = db.SendMessage("TestSender", evt);

        Assert.False(db.Messages.IsEmpty);
        Assert.Equal(1, db.Messages.Count);

        var published = db.Messages.Messages.First();
        Assert.Equal("TestEvent", published.MessageType);
        Assert.Equal("TestSender", published.Sender);
        Assert.Equal(evt, published.Payload);
    }

    /// <summary>
    /// ClearMessages should remove all events from the queue.
    /// </summary>
    [Fact]
    public void ClearMessages_RemovesAllEvents()
    {
        var db = new InvictaDatabase();
        db = db.SendMessage("Sender", new TestEvent("1"));
        db = db.SendMessage("Sender", new TestEvent("2"));

        Assert.Equal(2, db.Messages.Count);

        db = db.ClearMessages();

        Assert.True(db.Messages.IsEmpty);
        Assert.Equal(0, db.Messages.Count);
    }

    /// <summary>
    /// Batch operations should support sending messages (events).
    /// </summary>
    [Fact]
    public void Batch_SendMessage_AddsEventOnCommit()
    {
        var db = new InvictaDatabase();
        var evt = new TestEvent("Batch Event");

        db = db.Batch(batch =>
        {
            batch.SendMessage("BatchSender", evt);
        });

        Assert.False(db.Messages.IsEmpty);
        var published = db.Messages.Messages.First();
        Assert.Equal("TestEvent", published.MessageType);
        Assert.Equal("BatchSender", published.Sender);
        Assert.Equal(evt, published.Payload);
    }

    /// <summary>
    /// Multiple events should be preserved in order.
    /// </summary>
    [Fact]
    public void SendMessage_PreservesOrder()
    {
        var db = new InvictaDatabase();

        db = db.SendMessage("Sender", new TestEvent("1"));
        db = db.SendMessage("Sender", new TestEvent("2"));
        db = db.SendMessage("Sender", new TestEvent("3"));

        var events = db.Messages.Messages.ToList();

        Assert.Equal(3, events.Count);
        Assert.Equal("1", ((TestEvent)events[0].Payload!).Data);
        Assert.Equal("2", ((TestEvent)events[1].Payload!).Data);
        Assert.Equal("3", ((TestEvent)events[2].Payload!).Data);
    }
}
