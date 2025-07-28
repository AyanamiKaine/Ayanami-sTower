#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Xunit;
using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests
{
    public class EntityTests
    {
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
    }
}
