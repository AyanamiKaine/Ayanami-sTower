namespace AyanamisTower.StellaEcs.Tests;

// Define simple components to be used in tests.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public struct PositionComponent
{
    public float X;
    public float Y;
}

public struct VelocityComponent
{
    public float Dx;
    public float Dy;
}

public struct HealthComponent
{
    public int Health;
}

// A mock system for testing registration and updates.
public class MockSystem : ISystem
{
    public bool Enabled { get; set; } = true;
    public int UpdateCount { get; private set; }
    public string Name { get; set; } = "MockSystem";
    public List<string> Dependencies => [];

    public void Update(World world, float deltaTime)
    {
        UpdateCount++;
    }
}

// A mock function for testing invocation and parameters.
public class MockFunction : IEntityFunction
{
    public bool WasExecuted { get; private set; }
    public object[]? ReceivedParameters { get; private set; }

    public void Execute(Entity target, World world, object[] parameters)
    {
        WasExecuted = true;
        ReceivedParameters = parameters;

        // Example logic: if the target has health, reduce it.
        if (target.Has<HealthComponent>())
        {
            ref var health = ref target.GetMut<HealthComponent>();
            health.Health -= 10;
        }
    }
}

// A simple message struct for testing.
public struct TestMessage
{
    public int Data;
}

// A mock system that publishes a TestMessage when told to.
public class MessagePublishingSystem : ISystem
{
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "MessagePublisher";
    public List<string> Dependencies => [];

    public bool ShouldPublish { get; set; } = false;
    public int DataToPublish { get; set; } = 0;

    public void Update(World world, float deltaTime)
    {
        if (ShouldPublish)
        {
            world.PublishMessage(new TestMessage { Data = DataToPublish });
        }
    }
}

// A mock system that reads TestMessages and stores them for inspection.
public class MessageReadingSystem : ISystem
{
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "MessageReader";
    // This system now officially depends on the publisher, ensuring it runs after.
    public List<string> Dependencies => ["MessagePublisher"];

    public readonly List<TestMessage> ReceivedMessages = new();

    public void Update(World world, float deltaTime)
    {
        // Clear previous frame's results before reading the new ones.
        ReceivedMessages.Clear();

        var messages = world.ReadMessages<TestMessage>();
        ReceivedMessages.AddRange(messages);
    }
}

// A helper system specifically for testing dependency sorting and execution order.
public class DependencyTrackingSystem(string name, List<string> executionOrderTracker, List<string>? dependencies = null) : ISystem
{
    public string Name { get; set; } = name;
    public bool Enabled { get; set; } = true;
    public List<string> Dependencies { get; } = dependencies ?? [];
    private readonly List<string> _executionOrderTracker = executionOrderTracker;

    public void Update(World world, float deltaTime)
    {
        // When this system runs, it adds its name to the shared tracker list.
        _executionOrderTracker.Add(Name);
    }
}

// --- Relationship Components (unchanged) ---

public struct PlayerInfo { public string Name; }
public struct InventoryData { public int Capacity; }
public struct HasInventory { public Entity InventoryEntity; }
public struct BelongsToOwner { public Entity OwnerEntity; }
public struct GuildInfo { public string Name; }
public struct GuildMembership { public Entity GuildEntity; }
public struct StudentInfo { public string Name; }
public struct CourseInfo { public string Name; }
public struct EnrolledStudent { public Entity StudentEntity; }
public struct EnrolledInCourse { public Entity CourseEntity; }
public struct Grade { public char Value; }