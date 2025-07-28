#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Xunit;
using System;
using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests
{
    // Define a couple of simple component structs for testing
    public struct PositionComponent
    {
        public float X, Y;
    }

    public struct VelocityComponent
    {
        public float Dx, Dy;
    }

    public class ComponentStorageTests
    {
        private const int Capacity = 10;
        private const int UniverseSize = 50;

        [Fact]
        public void Add_NewComponent_Succeeds()
        {
            // Arrange
            var storage = new ComponentStorage<PositionComponent>(Capacity, UniverseSize);
            var component = new PositionComponent { X = 10, Y = 20 };
            const int entityId = 5;

            // Act
            storage.Add(entityId, component);

            // Assert
            Assert.Equal(1, storage.Count);
            Assert.True(storage.Has(entityId));
        }

        [Fact]
        public void Add_WhenFull_DoesNothing()
        {
            // Arrange
            var storage = new ComponentStorage<PositionComponent>(1, UniverseSize);
            storage.Add(1, new PositionComponent { X = 1, Y = 1 });

            // Act
            storage.Add(2, new PositionComponent { X = 2, Y = 2 });

            // Assert
            Assert.Equal(1, storage.Count);
            Assert.False(storage.Has(2));
        }

        [Fact]
        public void Remove_ExistingComponent_Succeeds()
        {
            // Arrange
            var storage = new ComponentStorage<PositionComponent>(Capacity, UniverseSize);
            const int entityId = 5;
            storage.Add(entityId, new PositionComponent { X = 10, Y = 20 });

            // Act
            storage.Remove(entityId);

            // Assert
            Assert.Equal(0, storage.Count);
            Assert.False(storage.Has(entityId));
        }

        [Fact]
        public void Remove_AndSwap_CorrectlyMovesComponentData()
        {
            // Arrange
            var storage = new ComponentStorage<PositionComponent>(Capacity, UniverseSize);
            const int entityIdToRemove = 5;
            var componentToRemove = new PositionComponent { X = 10, Y = 20 };

            const int lastEntityId = 15;
            var lastComponent = new PositionComponent { X = 100, Y = 200 };

            storage.Add(1, new PositionComponent());
            storage.Add(entityIdToRemove, componentToRemove); // This will be at index 1
            storage.Add(lastEntityId, lastComponent);       // This will be at index 2 (the end)

            // Act
            storage.Remove(entityIdToRemove);

            // Assert
            Assert.Equal(2, storage.Count);
            Assert.False(storage.Has(entityIdToRemove));
            Assert.True(storage.Has(lastEntityId));

            // Check that the component data from the last entity was moved into the removed slot.
            ref readonly var movedComponent = ref storage.Get(lastEntityId);
            Assert.Equal(lastComponent.X, movedComponent.X);
            Assert.Equal(lastComponent.Y, movedComponent.Y);
        }


        [Fact]
        public void Get_And_GetMutable_ProvideCorrectReference()
        {
            // Arrange
            var storage = new ComponentStorage<PositionComponent>(Capacity, UniverseSize);
            const int entityId = 8;
            storage.Add(entityId, new PositionComponent { X = 1.2f, Y = 3.4f });

            // Act & Assert (Get - readonly)
            ref readonly var readOnlyComp = ref storage.Get(entityId);
            Assert.Equal(1.2f, readOnlyComp.X);

            // Act & Assert (GetMutable)
            ref var mutableComp = ref storage.GetMutable(entityId);
            mutableComp.X = 5.6f;

            // Assert that the change is reflected through the readonly reference
            Assert.Equal(5.6f, readOnlyComp.X);
        }

        [Fact]
        public void Set_NewComponent_AddsIt()
        {
            // Arrange
            var storage = new ComponentStorage<PositionComponent>(Capacity, UniverseSize);
            const int entityId = 3;
            var component = new PositionComponent { X = 1, Y = 1 };

            // Act
            storage.Set(entityId, component);

            // Assert
            Assert.Equal(1, storage.Count);
            Assert.True(storage.Has(entityId));
            Assert.Equal(1, storage.Get(entityId).X);
        }

        [Fact]
        public void Set_ExistingComponent_UpdatesIt()
        {
            // Arrange
            var storage = new ComponentStorage<PositionComponent>(Capacity, UniverseSize);
            const int entityId = 3;
            storage.Add(entityId, new PositionComponent { X = 1, Y = 1 });

            // Act
            var updatedComponent = new PositionComponent { X = 99, Y = 99 };
            storage.Set(entityId, updatedComponent);

            // Assert
            Assert.Equal(1, storage.Count); // Count should not change
            Assert.True(storage.Has(entityId));
            Assert.Equal(99, storage.Get(entityId).X);
        }

        [Fact]
        public void TryGetValue_ForExistingComponent_ReturnsTrueAndComponent()
        {
            // Arrange
            var storage = new ComponentStorage<PositionComponent>(Capacity, UniverseSize);
            const int entityId = 7;
            storage.Add(entityId, new PositionComponent { X = 77, Y = 88 });

            // Act
            bool result = storage.TryGetValue(entityId, out var component);

            // Assert
            Assert.True(result);
            Assert.Equal(77, component.X);
            Assert.Equal(88, component.Y);
        }

        [Fact]
        public void TryGetValue_ForNonExistingComponent_ReturnsFalse()
        {
            // Arrange
            var storage = new ComponentStorage<PositionComponent>(Capacity, UniverseSize);
            const int entityId = 7;

            // Act
            bool result = storage.TryGetValue(entityId, out var component);

            // Assert
            Assert.False(result);
            Assert.Equal(default, component);
        }

        [Fact]
        public void PackedSpans_ReturnCorrectData()
        {
            // Arrange
            var storage = new ComponentStorage<PositionComponent>(Capacity, UniverseSize);
            storage.Add(10, new PositionComponent { X = 1, Y = 1 });
            storage.Add(20, new PositionComponent { X = 2, Y = 2 });
            storage.Add(30, new PositionComponent { X = 3, Y = 3 });

            // Act
            ReadOnlySpan<int> entities = storage.PackedEntities;
            ReadOnlySpan<PositionComponent> components = storage.PackedComponents;

            // Assert
            Assert.Equal(3, entities.Length);
            Assert.Equal(3, components.Length);

            Assert.Equal(10, entities[0]);
            Assert.Equal(20, entities[1]);
            Assert.Equal(30, entities[2]);

            Assert.Equal(1, components[0].X);
            Assert.Equal(2, components[1].X);
            Assert.Equal(3, components[2].X);
        }
    }
}
