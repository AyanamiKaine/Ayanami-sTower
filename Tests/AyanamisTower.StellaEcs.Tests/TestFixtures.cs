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
    public int UpdateCount { get; private set; }

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
    public readonly List<TestMessage> ReceivedMessages = new();

    public void Update(World world, float deltaTime)
    {
        // Clear previous frame's results before reading the new ones.
        ReceivedMessages.Clear();

        var messages = world.ReadMessages<TestMessage>();
        ReceivedMessages.AddRange(messages);
    }
}

public struct PlayerInfo
{
    public string Name;
}

public struct InventoryData
{
    public int Capacity;
}

/// <summary>
/// Component on the Player, pointing to their Inventory.
/// The Entity field acts as a "foreign key".
/// </summary>
public struct HasInventory
{
    public Entity InventoryEntity;
}

/// <summary>
/// Component on the Inventory, pointing back to its owner.
/// This makes the relationship bidirectional.
/// </summary>
public struct BelongsToOwner
{
    public Entity OwnerEntity;
}


// --- Components for One-to-Many Relationship Test ---

public struct GuildInfo
{
    public string Name;
}

/// <summary>
/// Component placed on a Member entity, pointing to the Guild they belong to.
/// This is the ECS equivalent of a `guild_id` foreign key on the `Members` table.
/// </summary>
public struct GuildMembership
{
    public Entity GuildEntity;
}


// --- Components for Many-to-Many Relationship Test ---

public struct StudentInfo
{
    public string Name;
}

public struct CourseInfo
{
    public string Name;
}

// These next components are used on the "Relation Entity" (the Enrollment).

/// <summary>
/// A component on the relation entity that points to the student.
/// (The `student_id` foreign key).
/// </summary>
public struct EnrolledStudent
{
    public Entity StudentEntity;
}

/// <summary>
/// A component on the relation entity that points to the course.
/// (The `course_id` foreign key).
/// </summary>
public struct EnrolledInCourse
{
    public Entity CourseEntity;
}

/// <summary>
/// Data *about* the relationship can also live on the relation entity.
/// This is like having a `grade` column on the `Enrollments` join table.
/// </summary>
public struct Grade
{
    public char Value;
}