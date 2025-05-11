using System;
using System.Collections.Concurrent; // For ConcurrentDictionary
using System.Collections.Generic; // For KeyNotFoundException
using System.Diagnostics.CodeAnalysis;
using AyanamisTower.SFPM;
using AyanamisTower.Utilities.Aspects;

namespace AyanamisTower.NihilEx;

/*
Design Note:

The main idea here is that we want the ability to attach certain meta data to
entities that is really arbitrary, for example imagine we want to store the fact
that the character saw trash cans. And at certain moments we want to spawn an event
based on the number of sawn trash cans. We dont want to add a field to the player
class or we dont want to attach a specific SawnTrashCans components in an ECS system.

This should not be data that gets looped and iterated over every frame as this
as performance implications and will blow the cache.

With that we could add the memory class to entities that should have the ability to
remember something. We dont need to attach it to every npc. We could attach it to
entites that represent regions in the game world. Again this class works best
when you want to add the ability to remember things.

TODO:
Implement a dotnet.benchmark project that gives us some concrete performance
numbers.

*/

/// <summary>
/// Represents a type-safe, generic memory store for game data using ConcurrentDictionary internally
/// for thread-safe operations on individual facts.
/// It can hold multiple collections of key-value pairs, where each collection
/// can have different types for its keys and values.
/// Provides direct methods (GetValue/SetValue) and an indexer-like syntax via the For() method.
/// Implements IFactSource for integration with SFPM.
/// </summary>
[PrettyPrint]
public class Memory : IFactSource
{
    // The master storage.
    // Key: The Type representing the specific ConcurrentDictionary<TKey, TValue> being stored.
    // Value: The actual ConcurrentDictionary<TKey, TValue> instance, stored as object.
    // Using ConcurrentDictionary for the outer storage makes adding/removing dictionary *types* thread-safe.
    private readonly ConcurrentDictionary<Type, object> _storage = new();

    /// <summary>
    /// Gets the internal ConcurrentDictionary instance for the specified key and value types.
    /// If it doesn't exist, it creates and returns a new one. Thread-safe.
    /// </summary>
    /// <returns>The ConcurrentDictionary&lt;TKey, TValue&gt; instance.</returns>
    private ConcurrentDictionary<TKey, TValue> GetOrCreateConcurrentDictionary<TKey, TValue>()
        where TKey : notnull
    {
        // Type identifier now represents ConcurrentDictionary<TKey, TValue>
        Type dictionaryType = typeof(ConcurrentDictionary<TKey, TValue>);

        // Use GetOrAdd for thread-safe retrieval or creation.
        // The factory now creates a ConcurrentDictionary.
        object dictObject = _storage.GetOrAdd(
            dictionaryType,
            (type) => new ConcurrentDictionary<TKey, TValue>() // Create ConcurrentDictionary
        );

        // Cast is safe due to GetOrAdd guarantees and class encapsulation.
        return (ConcurrentDictionary<TKey, TValue>)dictObject;
    }

    /// <summary>
    /// Sets or updates a value in the appropriate concurrent dictionary. Thread-safe.
    /// If a dictionary for the combination of TKey and TValue doesn't exist, it will be created automatically.
    /// </summary>
    /// <returns>The Memory instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
    public Memory SetValue<TKey, TValue>(TKey key, TValue value)
        where TKey : notnull
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        // Get the specific ConcurrentDictionary instance (thread-safe retrieval/creation)
        ConcurrentDictionary<TKey, TValue> specificDict = GetOrCreateConcurrentDictionary<
            TKey,
            TValue
        >();

        // Use the thread-safe indexer or AddOrUpdate
        specificDict[key] = value;
        // Or: specificDict.AddOrUpdate(key, value, (k, existingValue) => value);

        // No lock needed here!
        return this;
    }

    /// <summary>
    /// Tries to get a value from the appropriate concurrent dictionary. Thread-safe.
    /// </summary>
    /// <returns>true if the key was found; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
    public bool TryGetValue<TKey, TValue>(TKey key, [MaybeNullWhen(false)] out TValue value)
        where TKey : notnull
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        // Use the Type for ConcurrentDictionary now
        Type dictionaryType = typeof(ConcurrentDictionary<TKey, TValue>);

        // TryGetValue on the outer storage is thread-safe
        if (_storage.TryGetValue(dictionaryType, out object? dictObject))
        {
            // Safe cast check (though GetOrCreate ensures type correctness)
            if (dictObject is ConcurrentDictionary<TKey, TValue> specificDict)
            {
                // Use the thread-safe TryGetValue of ConcurrentDictionary
                // No lock needed here!
                return specificDict.TryGetValue(key, out value);
            }
            else
            {
                // Should not happen with correct GetOrCreate implementation
                // Consider logging an error if this state is reached.
            }
        }

        // Dictionary type doesn't exist, or cast failed (shouldn't happen)
        value = default;
        return false;
    }

    /// <summary>
    /// Gets a value from the appropriate concurrent dictionary. Thread-safe (reads).
    /// Throws KeyNotFoundException if the dictionary type or the key does not exist.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the key/dictionary type is not found.</exception>
    public TValue GetValue<TKey, TValue>(TKey key)
        where TKey : notnull
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        // Use the updated TryGetValue which is now thread-safe internally
        if (TryGetValue(key, out TValue? value))
        {
            // The [MaybeNullWhen(false)] on TryGetValue ensures 'value' is not null here.
            // The '!' suppression might still be needed if compiler analysis isn't perfect,
            // but semantically, 'value' is guaranteed non-null when TryGetValue returns true.
            return value!;
        }
        else
        {
            // Determine specific reason for failure for a better message
            Type dictionaryType = typeof(ConcurrentDictionary<TKey, TValue>);
            bool dictExists = _storage.ContainsKey(dictionaryType);
            string message = dictExists
                ? $"The key '{key}' was not found in the concurrent dictionary of type '{dictionaryType.FullName}'."
                : $"No concurrent dictionary of type '{dictionaryType.FullName}' exists in Memory.";
            throw new KeyNotFoundException(message);
        }
    }

    /// <summary>
    /// Checks if a key exists in the appropriate concurrent dictionary. Thread-safe.
    /// </summary>
    /// <returns>true if the key exists; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
    public bool ContainsKey<TKey, TValue>(TKey key)
        where TKey : notnull
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        Type dictionaryType = typeof(ConcurrentDictionary<TKey, TValue>);
        if (_storage.TryGetValue(dictionaryType, out object? dictObject))
        {
            if (dictObject is ConcurrentDictionary<TKey, TValue> specificDict)
            {
                // Use the thread-safe ContainsKey of ConcurrentDictionary
                // No lock needed here!
                return specificDict.ContainsKey(key);
            }
            else
            { /* Log error? Should not happen */
            }
        }
        return false; // Dictionary type doesn't exist or cast failed
    }

    /// <summary>
    /// Removes a value associated with the specified key from the appropriate concurrent dictionary. Thread-safe.
    /// </summary>
    /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
    public bool RemoveValue<TKey, TValue>(TKey key)
        where TKey : notnull
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        Type dictionaryType = typeof(ConcurrentDictionary<TKey, TValue>);
        if (_storage.TryGetValue(dictionaryType, out object? dictObject))
        {
            if (dictObject is ConcurrentDictionary<TKey, TValue> specificDict)
            {
                // Use the thread-safe TryRemove method.
                // The second 'out' parameter receives the removed value, which we discard (`out _`).
                // No lock needed here!
                return specificDict.TryRemove(key, out _);
            }
            else
            { /* Log error? Should not happen */
            }
        }
        return false; // Dictionary type doesn't exist or cast failed
    }

    /// <summary>
    /// Clears all entries from the concurrent dictionary corresponding to the specified TKey/TValue types. Thread-safe.
    /// Does nothing if no such dictionary exists.
    /// </summary>
    public void Clear<TKey, TValue>()
        where TKey : notnull
    {
        Type dictionaryType = typeof(ConcurrentDictionary<TKey, TValue>);
        if (_storage.TryGetValue(dictionaryType, out object? dictObject))
        {
            if (dictObject is ConcurrentDictionary<TKey, TValue> specificDict)
            {
                // Use the thread-safe Clear method.
                // No lock needed here!
                specificDict.Clear();
            }
            else
            { /* Log error? Should not happen */
            }
        }
    }

    #region IFactSource Implementation

    /// <summary>
    /// Tries to get a fact value of the specified type from memory. Thread-safe.
    /// Implements the IFactSource interface for SFPM integration.
    /// </summary>
    public bool TryGetFact<TValue>(string factName, [MaybeNullWhen(false)] out TValue value)
    {
        // Delegates to the updated, thread-safe TryGetValue<string, TValue>
        return TryGetValue<string, TValue>(factName, out value);
    }

    #endregion

    /// <summary>
    /// Provides thread-safe indexer-like access via a helper struct.
    /// </summary>
    /// <returns>A TypedMemoryAccessor for the specified types.</returns>
    public TypedMemoryAccessor<TKey, TValue> For<TKey, TValue>()
        where TKey : notnull
    {
        // Pass 'this' (the current Memory instance) to the accessor struct
        return new TypedMemoryAccessor<TKey, TValue>(this);
    }

    /// <summary>
    /// A helper struct providing indexer access. Operations are thread-safe
    /// as they delegate to the Memory class's thread-safe methods.
    /// </summary>
    public readonly struct TypedMemoryAccessor<TKey, TValue>
        where TKey : notnull
    {
        private readonly Memory _parentMemory;

        internal TypedMemoryAccessor(Memory parent)
        {
            _parentMemory = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        /// <summary>
        /// Gets or sets the value associated with the key. Thread-safe.
        /// Getting a non-existent key throws KeyNotFoundException.
        /// Setting will add or update the key.
        /// </summary>
        public TValue this[TKey key]
        {
            // Delegates to parent's GetValue/SetValue which are now thread-safe
            get => _parentMemory.GetValue<TKey, TValue>(key);
            set => _parentMemory.SetValue<TKey, TValue>(key, value);
        }

        /// <summary>
        /// Tries to get the value associated with the specified key. Thread-safe.
        /// </summary>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            // Delegates to parent's TryGetValue which is now thread-safe
            return _parentMemory.TryGetValue<TKey, TValue>(key, out value);
        }

        /// <summary>
        /// Checks if the specified key exists. Thread-safe.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            // Delegates to parent's ContainsKey which is now thread-safe
            return _parentMemory.ContainsKey<TKey, TValue>(key);
        }
    }

    /// <summary>
    /// Clears all stored concurrent dictionaries and their contents. Thread-safe.
    /// </summary>
    public void ClearAll()
    {
        // ConcurrentDictionary.Clear() is thread-safe itself.
        _storage.Clear();
    }
}
