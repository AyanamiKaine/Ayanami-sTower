using System;
using System.Collections;
using System.Collections.Generic;

namespace AyanamisTower.Explorations
{
    /// <summary>
    /// Implements a sparse set, a data structure with a fixed capacity that provides
    /// fast insertion, deletion, and membership testing for integers within a specified range.
    /// See: https://programmingpraxis.com/2012/03/09/sparse-sets/
    /// </summary>
    public class SparsedSet : IEnumerable<int>
    {
        // --- Fields ---

        private readonly int[] _dense;  // Packed array of elements currently in the set.
        private readonly int[] _sparse; // Maps an element value to its index in the dense array.

        /// <summary>
        /// Gets the maximum number of elements the set can hold.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// Gets the universe size, which is the maximum value an element can have + 1.
        /// </summary>
        public int UniverseSize { get; }

        /// <summary>
        /// Gets the current number of elements in the set.
        /// </summary>
        public int Count { get; private set; }

        // --- Constructor ---

        /// <summary>
        /// Creates a sparse set with a pre-determined capacity and universe size.
        /// </summary>
        /// <param name="capacity">The maximum number of elements the set can store.</param>
        /// <param name="universeSize">The maximum value of an element that can be stored. The set will be able to store integers from 0 to universeSize - 1.</param>
        public SparsedSet(int capacity, int universeSize)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");
            if (universeSize < 0) throw new ArgumentOutOfRangeException(nameof(universeSize), "Universe size cannot be negative.");

            Capacity = capacity;
            UniverseSize = universeSize;
            Count = 0;

            // The dense array holds the actual elements. Its size is the max number of elements.
            _dense = new int[capacity];

            // The sparse array maps values to their location in dense. Its size is the universe of possible values.
            _sparse = new int[universeSize];
        }

        // --- Public Methods ---

        /// <summary>
        /// Adds an element 'k' to the set.
        /// The operation is O(1).
        /// </summary>
        /// <param name="k">The integer element to add. Must be within the range [0, UniverseSize - 1].</param>
        /// <returns>True if the element was added, false if it was already in the set, the set is full, or 'k' is out of bounds.</returns>
        public bool Add(int k)
        {
            // 1. Check if the element is already present, if the set is full, or if k is out of the valid range.
            if (Count >= Capacity || k < 0 || k >= UniverseSize || Has(k))
            {
                return false;
            }

            // 2. Add the element.
            // Place the new element 'k' at the end of the packed dense array.
            _dense[Count] = k;
            // In the sparse array, at the index matching the value of 'k', store its position in the dense array.
            _sparse[k] = Count;

            // 3. Increment the count of elements.
            Count++;
            return true;
        }

        /// <summary>
        /// Removes an element 'k' from the set.
        /// This is the clever part of the sparse set, allowing O(1) removal.
        /// </summary>
        /// <param name="k">The integer element to remove.</param>
        /// <returns>True if the element was found and removed, false otherwise.</returns>
        public bool Remove(int k)
        {
            // 1. Check if the element to be removed is actually in the set.
            if (!Has(k))
            {
                return false;
            }

            // 2. Get the index of the element 'k' in the dense array.
            int indexOfK = _sparse[k];

            // 3. Get the element that is currently at the *end* of the dense array.
            int lastElement = _dense[Count - 1];

            // 4. Move the last element into the position of the element we are removing.
            _dense[indexOfK] = lastElement;

            // 5. Update the sparse array to point to the new location of the 'lastElement'.
            _sparse[lastElement] = indexOfK;

            // 6. Decrement the count. The old 'k' is now effectively gone, overwritten
            //    and outside the 'Count' boundary. We don't need to clear the old data.
            Count--;
            return true;
        }


        /// <summary>
        /// Checks for the membership of an element 'k'.
        /// The operation is O(1).
        /// </summary>
        /// <param name="k">The integer element to check.</param>
        /// <returns>True if the set contains 'k', false otherwise.</returns>
        public bool Has(int k)
        {
            // A bounds check is crucial to prevent an IndexOutOfRangeException.
            if (k < 0 || k >= UniverseSize)
            {
                return false;
            }

            // 1. Get the potential index from the sparse array.
            int indexInDense = _sparse[k];

            // 2. Check if this index is within the current valid range of the dense array.
            // 3. Perform the round-trip check: does the element at that dense index match k?
            return indexInDense < Count && _dense[indexInDense] == k;
        }

        /// <summary>
        /// Clears the set of all elements in O(1) time.
        /// </summary>
        public void Clear()
        {
            Count = 0;
        }

        // --- IEnumerable Implementation ---

        /// <summary>
        /// Returns an enumerator that iterates through the elements in the set.
        /// </summary>
        public IEnumerator<int> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return _dense[i];
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the elements in the set.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
