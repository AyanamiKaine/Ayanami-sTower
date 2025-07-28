#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests;

// --- Components & Relationships for Stress Testing ---

public struct Transform { public float X, Y, Z; }
public struct Renderable { public int MeshId; }
public struct Physics { public float VelocityX, VelocityY; }
public struct Health { public int Value; public int MaxValue; }
public struct Team { public int Id; }

// A tag component
public struct IsAggressive;

// Relationships
public struct IsFollowing : IRelationship { }
public struct IsAttacking : IRelationship { }
public struct IsPairedWith : IBidirectionalRelationship { }


public class StressTests
{
    private const int InitialEntityCount = 10_000;
    private const int SimulationTicks = 200;
    private const int NumEntitiesToDestroyPerTick = 50;
    private const int NumEntitiesToCreatePerTick = 50;

    [Fact]
    public void Run_Complex_Simulation_Without_Errors()
    {
        // Arrange: Setup the world and initial state
        var world = new World(InitialEntityCount * 2); // Allow space for creation/destruction
        var random = new Random(12345); // Use a fixed seed for reproducibility
        var allEntities = new List<Entity>(InitialEntityCount);

        // --- 1. Register all types ---
        world.RegisterComponent<Transform>();
        world.RegisterComponent<Renderable>();
        world.RegisterComponent<Physics>();
        world.RegisterComponent<Health>();
        world.RegisterComponent<Team>();
        world.RegisterComponent<IsAggressive>();
        world.RegisterRelationship<IsFollowing>();
        world.RegisterRelationship<IsAttacking>();
        world.RegisterRelationship<IsPairedWith>();

        // --- 2. Create initial entities with diverse archetypes ---
        for (int i = 0; i < InitialEntityCount; i++)
        {
            var entity = world.CreateEntity();
            entity.Add(new Transform { X = i });
            entity.Add(new Health { Value = 100, MaxValue = 100 });

            // Create varied archetypes
            if (i % 2 == 0) entity.Add(new Renderable { MeshId = i % 10 });
            if (i % 3 == 0) entity.Add(new Physics());
            if (i % 5 == 0) entity.Add(new Team { Id = i % 2 });
            if (i % 10 == 0) entity.Add(new IsAggressive());

            allEntities.Add(entity);
        }

        // --- 3. Create initial relationships ---
        for (int i = 0; i < InitialEntityCount / 10; i++)
        {
            var source = allEntities[random.Next(allEntities.Count)];
            var target = allEntities[random.Next(allEntities.Count)];
            if (source != target && source.IsAlive() && target.IsAlive())
            {
                // Add some one-way and two-way relationships
                if (i % 2 == 0) world.AddRelationship<IsFollowing>(source, target);
                else world.AddRelationship<IsPairedWith>(source, target);
            }
        }


        // Act & Assert: Run the simulation. The main assertion is that no exceptions are thrown.
        for (int tick = 0; tick < SimulationTicks; tick++)
        {
            // --- Query and Modify Data ---
            // Simulate a physics system
            foreach (var entity in world.Query().With<Transform>().With<Physics>().Build())
            {
                ref var t = ref entity.GetMutable<Transform>();
                ref readonly var p = ref entity.Get<Physics>();
                t.X += p.VelocityX;
                t.Y += p.VelocityY;
            }

            // Simulate a combat system
            var attackersQuery = world.Query().With<IsAggressive>().Without<Team>().Build();
            foreach (var attacker in attackersQuery)
            {
                // For simplicity, just find a random target to attack
                var target = allEntities[random.Next(allEntities.Count)];
                if (attacker != target && attacker.IsAlive() && target.IsAlive() && target.Has<Health>())
                {
                    if (!world.HasRelationship<IsAttacking>(attacker, target))
                    {
                        world.AddRelationship<IsAttacking>(attacker, target);
                    }
                    ref var targetHealth = ref target.GetMutable<Health>();
                    targetHealth.Value -= 1;
                }
            }

            // --- Entity Destruction and Creation ---
            // Destroy some random entities
            for (int i = 0; i < NumEntitiesToDestroyPerTick; i++)
            {
                int indexToDestroy = random.Next(allEntities.Count);
                allEntities[indexToDestroy].Destroy();
                allEntities.RemoveAt(indexToDestroy);
            }

            // Create new entities to replace them
            for (int i = 0; i < NumEntitiesToCreatePerTick; i++)
            {
                var entity = world.CreateEntity();
                entity.Add(new Transform());
                allEntities.Add(entity);
            }

            // --- Relationship Churn ---
            // Un-follow some entities
            var followingQuery = world.Query().With<IsFollowing>(allEntities[random.Next(allEntities.Count)]).Build();
            foreach (var entity in followingQuery)
            {
                // 10% chance to stop following
                if (random.NextDouble() < 0.1)
                {
                    // This is a bit inefficient as we don't know the target, but it tests the system.
                    // A real implementation would likely store the target on the relationship component.
                    var targets = world.GetRelationshipTargets<IsFollowing>(entity).ToList();
                    if (targets.Count > 0)
                    {
                        world.RemoveRelationship<IsFollowing>(entity, targets[0]);
                    }
                }
            }
        }

        // --- Final Validation ---
        // The primary test is that the loop completed without crashing.
        // We can add a simple final check.
        int finalEntityCount = 0;
        foreach (var _ in world.Query().With<Transform>().Build())
        {
            finalEntityCount++;
        }

        // We started with 10k, and destroyed/created the same amount each tick, so the total should be the same.
        Assert.Equal(InitialEntityCount, finalEntityCount);
        Assert.Equal(InitialEntityCount, allEntities.Count(e => e.IsAlive()));
    }
}
