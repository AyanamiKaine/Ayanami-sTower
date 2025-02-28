using System.Numerics;
using Flecs.NET.Core;
using static FlecsExploration.WaysToStructureData;

namespace FlecsExploration;

// Entity extensions for ECS behavior
public static class EntityExtensions
{
    public static void TakeDamage(this Entity entity, float damageAmount)
    {
        if (!entity.Has<Health>()) return;

        ref var health = ref entity.GetMut<Health>();
        health.Value -= damageAmount;
        if (health.Value < 0) health.Value = 0;
    }

    public static void Move(this Entity entity, Vector3 direction)
    {
        if (!entity.Has<Position>() || !entity.Has<MovementSpeed>()) return;

        ref var position = ref entity.GetMut<Position>();
        var speed = entity.Get<MovementSpeed>().Value;
        position.Value += direction * speed;
    }

    public static bool MineBlock(this Entity entity, float hardness, float deltaTime)
    {
        if (!entity.Has<Mining>()) return false;

        // Start or continue breaking block
        entity.Set<BlockBreaking>(new());
        ref var blockBreaking = ref entity.GetMut<BlockBreaking>();
        blockBreaking.InProgress = true;

        // Calculate progress
        float miningSpeed = entity.Get<Mining>().Speed;
        blockBreaking.Progress += miningSpeed * deltaTime;

        // Check if block is broken
        if (blockBreaking.Progress >= hardness)
        {
            blockBreaking.InProgress = false;
            blockBreaking.Progress = 0;
            return true;
        }

        return false;
    }
}

/// <summary>
/// There are many ways to structure normal classes or data structures as entities
/// </summary>
public class WaysToStructureData
{
    // Component structs for ECS approach
    public struct Health { public float Value; }
    public struct Position { public Vector3 Value; }
    public struct Inventory { public int SlotCount; }
    public struct Mining { public float Speed; }

    // Additional components for behavior
    public struct Damage { public float Value; }
    public struct MovementSpeed { public float Value; }
    public struct BlockBreaking { public bool InProgress; public float Progress; }

    // Traditional inheritance approach
    class Character
    {
        public float Health { get; set; }
        public Vector3 Position { get; set; }

        public void TakeDamage(float damage)
        {
            Health -= damage;
            if (Health < 0) Health = 0;
        }

        public void Move(Vector3 direction, float speed)
        {
            Position += direction * speed;
        }
    }

    class Player : Character
    {
        public int InventorySlots { get; set; }
    }

    class Miner : Player
    {
        public float MiningSpeed { get; set; }

        public bool MineBlock(float hardness, float deltaTime)
        {
            float progress = MiningSpeed * deltaTime;
            return progress >= hardness;
        }
    }

    /// <summary>
    /// Here we take the example of 
    /// creating a character in FLECS instead of the common way.
    /// </summary>
    [Fact]
    public void CharacterFlecsExample()
    {
        // Traditional OOP approach
        Console.WriteLine("Traditional inheritance approach:");
        var steve = new Miner
        {
            Health = 20,
            Position = new Vector3(100, 64, 200),
            InventorySlots = 36,
            MiningSpeed = 1.0f
        };

        // Deep inheritance makes it difficult to create entities that share only some behaviors
        // For example, what if we need an entity that can mine but isn't a player?

        // ECS approach with Flecs
        World world = World.Create();

        // Register components
        world.Component<Health>();
        world.Component<Position>();
        world.Component<Inventory>();
        world.Component<Mining>();

        // Create a player entity with components
        var playerEntity = world.Entity("Alex")
            .Set(new Health { Value = 20 })
            .Set(new Position { Value = new Vector3(100, 64, 200) })
            .Set(new Inventory { SlotCount = 36 });

        // Create a mining mob that isn't a player
        var zombieMiner = world.Entity("ZombieMiner")
            .Set(new Health { Value = 10 })
            .Set(new Position { Value = new Vector3(90, 64, 210) })
            .Set(new Mining { Speed = 0.5f });

        /*
        Imagine components as fields for classes/structs any field can be a component.
        I really mean ANY!
        */

        // OOP Approach of getting data
        steve.Health = 0;

        // Flecs Approach, we are getting the refence so we can mutate it
        ref var health = ref zombieMiner.GetMut<Health>();
        health.Value = 0;

        var query = world.Query<Health, Position>();
        query.Each((Entity e, ref Health h, ref Position p) =>
        {
            // Do something with the data
        });

        // Easily add mining capability to player later
        playerEntity.Set(new Mining { Speed = 1.0f });

        var minersQuery = world.Query<Mining>();
        minersQuery.Each((Entity e, ref Mining m) =>
        {
            // Do something with the data
        });

        Assert.Equal(0, zombieMiner.Get<Health>().Value);
    }

    /// <summary>
    /// Demonstrates how to add behavior in ECS using systems and extensions
    /// </summary>
    [Fact]
    public void BehaviorExampleTest()
    {
        // Traditional OOP approach for behavior
        Console.WriteLine("OOP approach to behavior:");
        var steve = new Miner
        {
            Health = 20,
            Position = new Vector3(100, 64, 200),
            InventorySlots = 36,
            MiningSpeed = 1.0f
        };

        // OOP behaviors
        steve.TakeDamage(5);
        steve.Move(Vector3.UnitX, 0.5f);
        steve.MineBlock(1.5f, 2.0f);

        // Assertions for OOP approach
        Assert.Equal(15, steve.Health);
        Assert.Equal(new Vector3(100.5f, 64, 200), steve.Position);

        // ECS approach with Flecs
        World world = World.Create();

        // Register components
        world.Component<Health>();
        world.Component<Position>();
        world.Component<Inventory>();
        world.Component<Mining>();
        world.Component<MovementSpeed>();
        world.Component<BlockBreaking>();
        world.Component<Damage>();

        // Create entities
        var playerEntity = world.Entity("Alex")
            .Set(new Health { Value = 20 })
            .Set(new Position { Value = new Vector3(100, 64, 200) })
            .Set(new Inventory { SlotCount = 36 })
            .Set(new Mining { Speed = 1.0f })
            .Set(new MovementSpeed { Value = 0.5f });

        // Entity behaviors using extensions
        // One big difference is type checking
        // We should check if an entity has the components. This ensures that 
        // the behavior is only executed when the components match.
        // Systems do that automatically because they only run on
        // components that do exist.
        playerEntity.TakeDamage(5);
        playerEntity.Move(Vector3.UnitX);
        playerEntity.MineBlock(1.5f, 2.0f);

        // Assertions for ECS approach
        Assert.Equal(15, playerEntity.Get<Health>().Value);
        Assert.Equal(new Vector3(100.5f, 64, 200), playerEntity.Get<Position>().Value);
        // Add assertions for mining-related components, depending on your implementation

        // Verify entity has required components
        Assert.True(playerEntity.Has<Health>());
        Assert.True(playerEntity.Has<Position>());
        Assert.True(playerEntity.Has<Inventory>());
        Assert.True(playerEntity.Has<Mining>());
    }
}