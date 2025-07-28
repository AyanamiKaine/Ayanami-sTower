#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests
{
    // Use components from other tests
    // public struct Position { public float X, Y; }
    // public struct IsChildOf : IRelationship { }

    public class SerializationTests
    {
        [Fact]
        public void World_CanBeSerializedAndDeserialized_Correctly()
        {
            // --- ARRANGE: Create the original world ---
            var originalWorld = new World(50);
            originalWorld.RegisterComponent<Position>();
            originalWorld.RegisterRelationship<IsChildOf>();

            // Create a runtime component
            var runtimeFields = new Dictionary<string, Type> { { "Value", typeof(int) } };
            Type runtimeHealthType = ComponentFactory.CreateComponentType("RuntimeHealth", runtimeFields);
            originalWorld.RegisterComponent(runtimeHealthType);

            // Create entities
            var parent = originalWorld.CreateEntity();
            parent.Add(new Position { X = 100, Y = 100 });

            var child1 = originalWorld.CreateEntity();
            child1.Add(new Position { X = 10, Y = 10 });
            var healthInstance = Activator.CreateInstance(runtimeHealthType)!;
            runtimeHealthType.GetField("Value")!.SetValue(healthInstance, 50);
            child1.Add(runtimeHealthType, healthInstance);

            var child2 = originalWorld.CreateEntity();
            child2.Add(new Position { X = 20, Y = 20 });
            child2.Destroy(); // Test recycled IDs

            var child3 = originalWorld.CreateEntity(); // This should reuse child2's ID
            child3.Add(new Position { X = 30, Y = 30 });

            // Add relationships
            parent.AddRelationship<IsChildOf>(child1);
            parent.AddRelationship<IsChildOf>(child3);

            // --- ACT: Serialize and Deserialize ---
            var serializer = new WorldSerializer();
            string json = serializer.Serialize(originalWorld);
            //Console.WriteLine("Serialized World JSON:");
            //Console.WriteLine(json);

            var newWorld = serializer.Deserialize(json);

            // --- ASSERT: Verify the new world is identical ---

            // Find the equivalent entities in the new world
            var newParent = newWorld.GetEntityFromId(parent.Id);
            var newChild1 = newWorld.GetEntityFromId(child1.Id);
            var newChild3 = newWorld.GetEntityFromId(child3.Id);

            // 1. Check entity state
            Assert.True(newParent.IsAlive());
            Assert.True(newChild1.IsAlive());
            Assert.True(newChild3.IsAlive());
            Assert.False(newWorld.IsAlive(new Entity(child2.Id, child2.Generation, newWorld))); // Check destroyed entity

            // 2. Check components
            Assert.True(newParent.Has<Position>());
            Assert.Equal(100, newParent.Get<Position>().X);

            Assert.True(newChild1.Has<Position>());
            Assert.True(newChild1.Has(runtimeHealthType));
            var retrievedHealth = newChild1.Get(runtimeHealthType);
            Assert.Equal(50, (int)runtimeHealthType.GetField("Value")!.GetValue(retrievedHealth)!);

            Assert.True(newChild3.Has<Position>());
            Assert.Equal(30, newChild3.Get<Position>().X);

            // 3. Check relationships
            Assert.True(newParent.HasRelationship<IsChildOf>(newChild1));
            Assert.True(newParent.HasRelationship<IsChildOf>(newChild3));
            Assert.Equal(2, newParent.GetRelationshipTargets<IsChildOf>().Count());
        }
    }
}
