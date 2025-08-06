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
