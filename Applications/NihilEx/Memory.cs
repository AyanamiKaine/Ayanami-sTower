using System;
using System.Collections.Concurrent; // For ConcurrentDictionary
using System.Collections.Generic; // For KeyNotFoundException
using System.Diagnostics.CodeAnalysis;

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
/// Represents a type-safe, generic memory store for game data.
/// It can hold multiple collections of key-value pairs, where each collection
/// can have different types for its keys and values.
/// Provides direct methods (GetValue/SetValue) and an indexer-like syntax via the For() method.
/// </summary>
public class Memory
{
    // The master storage.
    // Key: The Type of the specific Dictionary<TKey, TValue> being stored.
    // Value: The actual Dictionary<TKey, TValue> instance, stored as object.
    // Using ConcurrentDictionary for basic thread safety when adding/removing dictionary types.
    private readonly ConcurrentDictionary<Type, object> _storage =
        new ConcurrentDictionary<Type, object>();

    /// <summary>
    /// Gets the internal dictionary instance for the specified key and value types.
    /// If it doesn't exist, it creates and returns a new one.
    /// This method ensures thread-safe creation and retrieval of the specific dictionary instance.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <returns>The Dictionary&lt;TKey, TValue instance.</returns>
    private Dictionary<TKey, TValue> GetOrCreateDictionary<TKey, TValue>()
        where TKey : notnull
    {
        // The unique type identifier for this specific dictionary generic combination.
        Type dictionaryType = typeof(Dictionary<TKey, TValue>);

        // Use GetOrAdd for thread-safe retrieval or creation.
        // The value factory delegate () => new Dictionary<TKey, TValue>()
        // is only executed if the key (dictionaryType) is not already present.
        object dictObject = _storage.GetOrAdd(
            dictionaryType,
            (type) => new Dictionary<TKey, TValue>()
        );

        // We know the object stored for this type *must* be Dictionary<TKey, TValue>,
        // either because it was just created or retrieved from a previous Add.
        // So, the cast is safe.
        return (Dictionary<TKey, TValue>)dictObject;
    }

    /// <summary>
    /// Sets or updates a value in the appropriate dictionary based on the generic types.
    /// If a dictionary for the combination of TKey and TValue doesn't exist, it will be created automatically.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The value to associate with the key.</param>
    /// <remarks>
    /// Note: While retrieving/creating the dictionary instance is thread-safe,
    /// modifying the *contents* of a retrieved Dictionary&lt;TKey, TValue&gt; is *not*
    /// inherently thread-safe if multiple threads access the *same* inner dictionary concurrently.
    /// If you need concurrent writes to the *same* key/value types from different threads,
    /// consider adding external locking or modifying this class to store
    /// ConcurrentDictionary&lt;TKey, TValue&gt; internally instead of Dictionary&lt;TKey, TValue&gt;.
    /// </remarks>
    public void SetValue<TKey, TValue>(TKey key, TValue value)
        where TKey : notnull
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        Dictionary<TKey, TValue> specificDict = GetOrCreateDictionary<TKey, TValue>();
        lock (specificDict) // Optional: lock inner dictionary
        {
            specificDict[key] = value;
        }
    }

    /// <summary>
    /// Tries to get a value from the appropriate dictionary based on the generic types.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key whose value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key,
    /// if the key is found; otherwise, the default value for the type TValue.</param>
    /// <returns>true if the key was found in the appropriate dictionary; otherwise, false.</returns>
    public bool TryGetValue<TKey, TValue>(TKey key, [MaybeNullWhen(false)] out TValue value)
        where TKey : notnull
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        Type dictionaryType = typeof(Dictionary<TKey, TValue>);
        if (_storage.TryGetValue(dictionaryType, out object? dictObject))
        {
            Dictionary<TKey, TValue> specificDict = (Dictionary<TKey, TValue>)dictObject;
            lock (specificDict) // Optional: lock inner dictionary
            {
                return specificDict.TryGetValue(key, out value);
            }
        }
        else
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Gets a value from the appropriate dictionary based on the generic types.
    /// Throws KeyNotFoundException if the key is not found or if the dictionary
    /// for the specified TKey/TValue combination does not exist.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key whose value to get.</param>
    /// <returns>The value associated with the key.</returns>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">
    /// Thrown if the key is not found in the relevant dictionary or if a dictionary
    /// for the TKey/TValue combination hasn't been created yet (e.g., via SetValue).
    /// </exception>
    public TValue GetValue<TKey, TValue>(TKey key)
        where TKey : notnull
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (TryGetValue(key, out TValue? value)) // Converting null literal or possible null value to non-nullable type.
        {
            return value!; // Mhhh even though the value maybe null, if that would be the case
            // try get value would return false and not return null. I think the compiler cant see it.
        }
        else
        {
            Type dictionaryType = typeof(Dictionary<TKey, TValue>);
            string message = _storage.ContainsKey(dictionaryType)
                ? $"The key '{key}' was not found in the dictionary of type '{dictionaryType.FullName}'."
                : $"No dictionary of type '{dictionaryType.FullName}' exists in Memory, or the key '{key}' was not found.";
            throw new KeyNotFoundException(message);
        }
    }

    /// <summary>
    /// Checks if a key exists in the appropriate dictionary based on the generic types.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value associated with the key (determines which dictionary to check).</typeparam>
    /// <param name="key">The key to check for existence.</param>
    /// <returns>true if the key exists in the appropriate dictionary; otherwise, false.</returns>
    public bool ContainsKey<TKey, TValue>(TKey key)
        where TKey : notnull
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        Type dictionaryType = typeof(Dictionary<TKey, TValue>);

        if (_storage.TryGetValue(dictionaryType, out object? dictObject))
        {
            Dictionary<TKey, TValue> specificDict = (Dictionary<TKey, TValue>)dictObject;
            lock (specificDict) // Example of locking the inner dictionary
            {
                return specificDict.ContainsKey(key);
            }
            // If no locking needed:
            // return specificDict.ContainsKey(key);
        }
        return false; // Dictionary type doesn't exist, so key can't exist
    }

    /// <summary>
    /// Removes a value associated with the specified key from the appropriate dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value (determines which dictionary to modify).</typeparam>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns>true if the element is successfully found and removed; otherwise, false.
    /// This method returns false if key is not found in the specific Dictionary&lt;TKey, TValue&gt;
    /// or if the dictionary type itself doesn't exist.</returns>
    public bool RemoveValue<TKey, TValue>(TKey key)
        where TKey : notnull
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        Type dictionaryType = typeof(Dictionary<TKey, TValue>);

        if (_storage.TryGetValue(dictionaryType, out object? dictObject))
        {
            Dictionary<TKey, TValue> specificDict = (Dictionary<TKey, TValue>)dictObject;
            lock (specificDict) // Example of locking the inner dictionary
            {
                return specificDict.Remove(key);
            }
            // If no locking needed:
            // return specificDict.Remove(key);
        }
        return false; // Dictionary type doesn't exist, so nothing to remove
    }

    /// <summary>
    /// Clears all entries from the dictionary corresponding to the specified TKey and TValue types.
    /// Does nothing if no such dictionary exists.
    /// </summary>
    /// <typeparam name="TKey">The key type of the dictionary to clear.</typeparam>
    /// <typeparam name="TValue">The value type of the dictionary to clear.</typeparam>
    public void Clear<TKey, TValue>()
        where TKey : notnull
    {
        Type dictionaryType = typeof(Dictionary<TKey, TValue>);
        if (_storage.TryGetValue(dictionaryType, out object? dictObject))
        {
            Dictionary<TKey, TValue> specificDict = (Dictionary<TKey, TValue>)dictObject;
            lock (specificDict) // Example of locking the inner dictionary
            {
                specificDict.Clear();
            }
            // If no locking needed:
            // specificDict.Clear();
        }
    }

    /// <summary>
    /// Provides access to the underlying dictionary specified by TKey and TValue
    /// using indexer syntax.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the desired dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the desired dictionary.</typeparam>
    /// <returns>A TypedMemoryAccessor struct that provides indexer access.</returns>
    /// <example>
    /// int score = memory.For&lt;string, int&gt;()["PlayerScore"];
    /// memory.For&lt;string, int&gt;()["PlayerScore"] = 100;
    /// </example>
    public TypedMemoryAccessor<TKey, TValue> For<TKey, TValue>()
        where TKey : notnull
    {
        // Pass 'this' (the current Memory instance) to the accessor struct
        return new TypedMemoryAccessor<TKey, TValue>(this);
    }

    /// <summary>
    /// A helper struct returned by Memory.For&lt;TKey, TValue&gt;() to provide
    /// indexer access to a specific underlying dictionary type.
    /// Defined as readonly struct for potential performance benefits (avoids heap allocation).
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public readonly struct TypedMemoryAccessor<TKey, TValue>
        where TKey : notnull
    {
        // Keep a reference to the parent Memory instance
        private readonly Memory _parentMemory;

        // Constructor called by Memory.For<TKey, TValue>()
        internal TypedMemoryAccessor(Memory parent)
        {
            _parentMemory = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key in the
        /// underlying dictionary identified by TKey and TValue.
        /// Getting a non-existent key throws KeyNotFoundException.
        /// Setting will add or update the key.
        /// </summary>
        /// <param name="key">The key of the element to get or set.</param>
        /// <returns>The value associated with the key.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Thrown by the getter if the key is not found.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown if the key is null.</exception>
        public TValue this[TKey key]
        {
            get
            {
                // Delegate the call to the parent Memory instance's GetValue method
                return _parentMemory.GetValue<TKey, TValue>(key);
            }
            set
            {
                // Delegate the call to the parent Memory instance's SetValue method
                // 'value' is implicitly available in the set accessor
                _parentMemory.SetValue<TKey, TValue>(key, value);
            }
        }

        // Optional: You could also add TryGetValue and ContainsKey methods here
        // if you want them available via the accessor struct as well.

        /// <summary>
        /// Tries to get the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The output value if found.</param>
        /// <returns>True if the key was found, false otherwise.</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            return _parentMemory.TryGetValue<TKey, TValue>(key, out value);
        }

        /// <summary>
        /// Checks if the specified key exists in the underlying dictionary.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        public bool ContainsKey(TKey key)
        {
            return _parentMemory.ContainsKey<TKey, TValue>(key);
        }
    }

    /// <summary>
    /// Clears all stored dictionaries and their contents.
    /// </summary>
    public void ClearAll()
    {
        // Note: Clearing the outer dictionary might have concurrency implications
        // if other threads are actively using GetOrCreateDictionary at the same time.
        // ConcurrentDictionary.Clear() is generally thread-safe itself.
        _storage.Clear();
    }
}
