#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Xunit;
using System;
using System.Linq;
using AyanamisTower.StellaEcs;

namespace AyanamisTower.StellaEcs.Tests
{
    public class SparsedSetTests
    {
        [Fact]
        public void Constructor_WithNegativeCapacity_ThrowsException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new SparsedSet(-1, 10));
        }

        [Fact]
        public void Constructor_WithNegativeUniverseSize_ThrowsException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new SparsedSet(10, -1));
        }

        [Fact]
        public void Add_NewElement_ReturnsTrueAndIncrementsCount()
        {
            // Arrange
            var set = new SparsedSet(10, 100);

            // Act
            bool result = set.Add(5);

            // Assert
            Assert.True(result);
            Assert.Equal(1, set.Count);
            Assert.True(set.Has(5));
        }

        [Fact]
        public void Add_ExistingElement_ReturnsFalse()
        {
            // Arrange
            var set = new SparsedSet(10, 100);
            set.Add(5);

            // Act
            bool result = set.Add(5);

            // Assert
            Assert.False(result);
            Assert.Equal(1, set.Count);
        }

        [Fact]
        public void Add_WhenSetIsFull_ReturnsFalse()
        {
            // Arrange
            var set = new SparsedSet(2, 100);
            set.Add(5);
            set.Add(10);

            // Act
            bool result = set.Add(15);

            // Assert
            Assert.False(result);
            Assert.Equal(2, set.Count);
            Assert.False(set.Has(15));
        }

        [Fact]
        public void Add_ElementOutsideUniverse_ReturnsFalse()
        {
            // Arrange
            var set = new SparsedSet(10, 50);

            // Act
            bool result = set.Add(55);

            // Assert
            Assert.False(result);
            Assert.Equal(0, set.Count);
        }

        [Fact]
        public void Remove_ExistingElement_ReturnsTrueAndDecrementsCount()
        {
            // Arrange
            var set = new SparsedSet(10, 100);
            set.Add(5);

            // Act
            bool result = set.Remove(5);

            // Assert
            Assert.True(result);
            Assert.Equal(0, set.Count);
            Assert.False(set.Has(5));
        }

        [Fact]
        public void Remove_NonExistingElement_ReturnsFalse()
        {
            // Arrange
            var set = new SparsedSet(10, 100);
            set.Add(5);

            // Act
            bool result = set.Remove(10);

            // Assert
            Assert.False(result);
            Assert.Equal(1, set.Count);
        }

        [Fact]
        public void Remove_SwapAndPopLogic_IsCorrect()
        {
            // Arrange
            var set = new SparsedSet(10, 100);
            set.Add(10); // index 0
            set.Add(20); // index 1
            set.Add(30); // index 2

            // Act
            // This should swap element 30 (last) into the slot of element 20.
            set.Remove(20);

            // Assert
            Assert.Equal(2, set.Count);
            Assert.False(set.Has(20));
            Assert.True(set.Has(10));
            Assert.True(set.Has(30));
            // Check the underlying dense array via the enumerator
            Assert.Contains(10, set);
            Assert.Contains(30, set);
            Assert.DoesNotContain(20, set);
        }


        [Fact]
        public void Has_ElementInSet_ReturnsTrue()
        {
            // Arrange
            var set = new SparsedSet(10, 100);
            set.Add(42);

            // Act & Assert
            Assert.True(set.Has(42));
        }

        [Fact]
        public void Has_ElementNotInSet_ReturnsFalse()
        {
            // Arrange
            var set = new SparsedSet(10, 100);
            set.Add(42);

            // Act & Assert
            Assert.False(set.Has(43));
        }

        [Fact]
        public void Has_ElementOutsideUniverse_ReturnsFalse()
        {
            // Arrange
            var set = new SparsedSet(10, 50);

            // Act & Assert
            Assert.False(set.Has(55));
            Assert.False(set.Has(-1));
        }

        [Fact]
        public void Clear_ResetsCountToZero()
        {
            // Arrange
            var set = new SparsedSet(10, 100);
            set.Add(5);
            set.Add(10);

            // Act
            set.Clear();

            // Assert
            Assert.Equal(0, set.Count);
            Assert.False(set.Has(5));
            Assert.False(set.Has(10));
            Assert.Empty(set);
        }

        [Fact]
        public void GetEnumerator_IteratesOverAllElements()
        {
            // Arrange
            var set = new SparsedSet(10, 100);
            var elementsToAdd = new[] { 5, 15, 2, 88, 43 };
            foreach (var element in elementsToAdd)
            {
                set.Add(element);
            }

            // Act
            var enumeratedElements = set.ToList();

            // Assert
            Assert.Equal(elementsToAdd.Length, enumeratedElements.Count);
            foreach (var element in elementsToAdd)
            {
                Assert.Contains(element, enumeratedElements);
            }
        }
    }
}
