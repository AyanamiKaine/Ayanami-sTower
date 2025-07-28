#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Xunit;
using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests
{
    public class EntityTests
    {
        struct IsParentOf : IRelationship { }
        struct IsMarriedTo : IBidirectionalRelationship { }

        [Fact]
        public void Equals_WithSameIdAndGeneration_ReturnsTrue()
        {
            // Arrange
            var entity1 = new Entity(1, 1, null);
            var entity2 = new Entity(1, 1, null);

            // Act & Assert
            Assert.True(entity1.Equals(entity2));
            Assert.True(entity1 == entity2);
            Assert.False(entity1 != entity2);
        }

        [Fact]
        public void Equals_WithDifferentId_ReturnsFalse()
        {
            // Arrange
            var entity1 = new Entity(1, 1, null);
            var entity2 = new Entity(2, 1, null);

            // Act & Assert
            Assert.False(entity1.Equals(entity2));
            Assert.False(entity1 == entity2);
            Assert.True(entity1 != entity2);
        }

        [Fact]
        public void Equals_WithDifferentGeneration_ReturnsFalse()
        {
            // Arrange
            var entity1 = new Entity(1, 1, null);
            var entity2 = new Entity(1, 2, null);

            // Act & Assert
            Assert.False(entity1.Equals(entity2));
            Assert.False(entity1 == entity2);
            Assert.True(entity1 != entity2);
        }

        [Fact]
        public void Equals_WithObject_ReturnsCorrectly()
        {
            // Arrange
            var entity = new Entity(5, 2, null);
            object sameEntityObj = new Entity(5, 2, null);
            object differentEntityObj = new Entity(6, 2, null);
            object notAnEntity = new();


            // Act & Assert
            Assert.True(entity.Equals(sameEntityObj));
            Assert.False(entity.Equals(differentEntityObj));
            Assert.False(entity.Equals(notAnEntity));
        }

        [Fact]
        public void GetHashCode_ForEqualEntities_IsEqual()
        {
            // Arrange
            var entity1 = new Entity(10, 5, null);
            var entity2 = new Entity(10, 5, null);

            // Act & Assert
            Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_ForDifferentEntities_IsLikelyNotEqual()
        {
            // Arrange
            var entity1 = new Entity(10, 5, null);
            var entity2 = new Entity(5, 10, null);

            // Act & Assert
            // Note: Hash code collisions are possible, but extremely unlikely for these inputs.
            Assert.NotEqual(entity1.GetHashCode(), entity2.GetHashCode());
        }

        [Fact]
        public void NullEntity_HasNegativeId()
        {
            // Arrange & Act & Assert
            Assert.Equal(-1, Entity.Null.Id);
            Assert.Equal(new Entity(-1, 0, null), Entity.Null);
        }

        [Fact]
        public void RuntimeApi_CorrectlyManipulatesData()
        {
            // Arrange
            var world = new World();
            var positionType = typeof(Position);
            world.RegisterComponent(positionType);

            var entity = world.CreateEntity();

            // Act & Assert for Add
            var positionInstance = Activator.CreateInstance(positionType)!;
            entity.Add(positionType, positionInstance);
            Assert.True(entity.Has(positionType));

            // Act & Assert for Get
            var component = (Position)entity.Get(positionType);
            Assert.Equal(0, component.X);

            // Act & Assert for Set
            var newPosition = new Position { X = 123, Y = 456 };
            entity.Set(positionType, newPosition);
            component = (Position)entity.Get(positionType);
            Assert.Equal(123, component.X);

            // Act & Assert for Remove
            entity.Remove(positionType);
            Assert.False(entity.Has(positionType));
        }

        [Fact]
        public void RuntimeTypeCreation_CanBeUsedWithEcs()
        {
            // Arrange: Define a new component type at runtime
            var fields = new Dictionary<string, Type>
        {
            { "Health", typeof(int) },
            { "Mana", typeof(int) }
        };
            Type runtimeStatsComponentType = ComponentFactory.CreateComponentType("RuntimeStatsComponent", fields);

            var world = new World();
            // Register the newly created type
            world.RegisterComponent(runtimeStatsComponentType);
            var entity = world.CreateEntity();

            // Act: Create an instance and set its fields using Reflection
            object statsInstance = Activator.CreateInstance(runtimeStatsComponentType)!;
            runtimeStatsComponentType.GetField("Health")!.SetValue(statsInstance, 100);
            runtimeStatsComponentType.GetField("Mana")!.SetValue(statsInstance, 50);

            // Add the component to the entity
            entity.Add(runtimeStatsComponentType, statsInstance);

            // Assert: Verify the component exists and its data is correct
            Assert.True(entity.Has(runtimeStatsComponentType));

            object retrievedComponent = entity.Get(runtimeStatsComponentType);
            int healthValue = (int)runtimeStatsComponentType.GetField("Health")!.GetValue(retrievedComponent)!;
            int manaValue = (int)runtimeStatsComponentType.GetField("Mana")!.GetValue(retrievedComponent)!;

            Assert.Equal(100, healthValue);
            Assert.Equal(50, manaValue);
        }

        // --- NEW TEST CASE ---
        [Fact]
        public void RelationshipApi_ManipulatesRelationshipsCorrectly()
        {
            // Arrange
            var world = new World();

            // Register relationships with the world
            world.RegisterRelationship<IsParentOf>();
            world.RegisterRelationship<IsMarriedTo>();

            var parent = world.CreateEntity();
            var child1 = world.CreateEntity();
            var child2 = world.CreateEntity();
            var spouseA = world.CreateEntity();
            var spouseB = world.CreateEntity();

            // --- Act & Assert: Uni-directional relationship (Parent/Child) ---

            // Add relationship
            parent.AddRelationship<IsParentOf>(child1);
            Assert.True(parent.HasRelationship<IsParentOf>(child1));
            Assert.False(child1.HasRelationship<IsParentOf>(parent)); // Ensure it's not bidirectional

            // Get targets
            parent.AddRelationship<IsParentOf>(child2);
            var children = parent.GetRelationshipTargets<IsParentOf>().ToList();
            Assert.Equal(2, children.Count);
            Assert.Contains(child1, children);
            Assert.Contains(child2, children);

            // Remove relationship
            parent.RemoveRelationship<IsParentOf>(child1);
            Assert.False(parent.HasRelationship<IsParentOf>(child1));
            var updatedChildren = parent.GetRelationshipTargets<IsParentOf>().ToList();
            Assert.Single(updatedChildren);
            Assert.Contains(child2, updatedChildren);

            // --- Act & Assert: Bi-directional relationship ---

            // Add relationship
            spouseA.AddRelationship<IsMarriedTo>(spouseB);
            Assert.True(spouseA.HasRelationship<IsMarriedTo>(spouseB));
            Assert.True(spouseB.HasRelationship<IsMarriedTo>(spouseA)); // Check reverse link

            // Remove relationship
            spouseB.RemoveRelationship<IsMarriedTo>(spouseA);
            Assert.False(spouseB.HasRelationship<IsMarriedTo>(spouseA));
            Assert.False(spouseA.HasRelationship<IsMarriedTo>(spouseB)); // Check reverse link is also gone
        }

    }
}
