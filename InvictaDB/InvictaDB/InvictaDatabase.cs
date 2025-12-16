using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace InvictaDB;
/// <summary>
/// Immutable in-memory database.
/// </summary>
public class InvictaDatabase : IImmutableDictionary<string, object>
{
    private readonly ImmutableDictionary<string, object> _currentState;

    // Thread-safe type registry
    private readonly ConcurrentDictionary<Type, string> _typeToTable;
    private readonly ConcurrentDictionary<string, Type> _tableToType;

    /// <summary>
    /// Creates a new empty InvictaDatabase.
    /// </summary>
    public InvictaDatabase()
    {
        _currentState = [];
        _typeToTable = new ConcurrentDictionary<Type, string>();
        _tableToType = new ConcurrentDictionary<string, Type>();
    }

    /// <summary>
    /// Creates a new InvictaDatabase with the specified state and registries.
    /// </summary>
    private InvictaDatabase(
        ImmutableDictionary<string, object> state,
        ConcurrentDictionary<Type, string> typeToTable,
        ConcurrentDictionary<string, Type> tableToType)
    {
        _currentState = state;
        _typeToTable = typeToTable;
        _tableToType = tableToType;
    }

    /// <summary>
    /// Creates a new InvictaDatabase with updated state, sharing the type registries.
    /// </summary>
    private InvictaDatabase WithState(ImmutableDictionary<string, object> newState)
    {
        return new InvictaDatabase(newState, _typeToTable, _tableToType);
    }

    /// <inheritdoc/>
    public IEnumerable<string> Keys => _currentState.Keys;

    /// <inheritdoc/>
    public IEnumerable<object> Values => _currentState.Values;

    /// <inheritdoc/>
    public int Count => _currentState.Count;
    
    /// <inheritdoc/>
    public object this[string key] => _currentState[key];

    /// <summary>
    /// Registers a table for storing entries of type T. Given a specific table name.
    /// </summary>
    /// <typeparam name="T">The type of entry to store.</typeparam>
    /// <param name="tableName">The name of the table.</param>
    public InvictaDatabase RegisterTable<T>(string tableName)
    {
        _typeToTable[typeof(T)] = tableName;
        _tableToType[tableName] = typeof(T);
        if (!_currentState.ContainsKey(tableName))
        {
            return WithState(_currentState.Add(tableName, ImmutableDictionary<string, T>.Empty));
        }
        return this;
    }

    /// <summary>
    /// Registers a table for storing entities of type T. Using the type name as the table name.
    /// </summary>
    /// <typeparam name="T">The type of entry to store.</typeparam>
    public InvictaDatabase RegisterTable<T>()
    {
        var tableName = typeof(T).Name;
        _typeToTable[typeof(T)] = tableName;
        _tableToType[tableName] = typeof(T);
        if (!_currentState.ContainsKey(tableName))
        {
            return WithState(_currentState.Add(tableName, ImmutableDictionary<string, T>.Empty));
        }
        return this;
    }

    /// <summary>
    /// Checks if a table with the given name exists in the database.
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    public bool TableExists(string tableName)
    {
        return _currentState.ContainsKey(tableName);
    }

    /// <summary>
    /// Checks if a table for the given type T exists in the database.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool TableExists<T>()
    {
        if (_typeToTable.TryGetValue(typeof(T), out var tableName))
        {
            return _currentState.ContainsKey(tableName);
        }
        return false;
    }

    /// <summary>
    /// Gets the table for the given type T by table name.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="tableName"></param>
    /// <returns></returns>
    public ImmutableDictionary<string, T> GetTable<T>(string tableName)
    {
        if (_currentState.TryGetValue(tableName, out var tableObj))
        {
            return (ImmutableDictionary<string, T>)tableObj;
        }
        return [];
    }
    /// <summary>
    /// Gets the table for the given type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ImmutableDictionary<string, T> GetTable<T>()
    {
        if (_currentState.TryGetValue(typeof(T).Name, out var tableObj))
        {
            return (ImmutableDictionary<string, T>)tableObj;
        }
        return [];
    }
    /// <summary>
    /// Inserts an entry into the appropriate table.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="id"></param>
    /// <param name="entry"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public InvictaDatabase Insert<T>(string id, T entry)
    {
        if (!_typeToTable.TryGetValue(typeof(T), out var tableName))
        {
            throw new InvalidOperationException($"Type {typeof(T).Name} is not mapped to a table.");
        }

        var table = GetTable<T>(tableName);
        table = table.SetItem(id, entry);
        return WithState(_currentState.SetItem(tableName, table));
    }
    /// <summary>
    /// Inserts a singleton entry into the database.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="id"></param>
    /// <param name="entry"></param>
    /// <returns></returns>
    public InvictaDatabase InsertSingleton<T>(string id, T entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry), "Entry cannot be null.");
        }

        return WithState(_currentState.SetItem(id, entry));
    }
    /// <summary>
    /// Inserts a singleton entry into the database.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entry"></param>
    /// <returns></returns>
    public InvictaDatabase InsertSingleton<T>(T entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry), "Entry cannot be null.");
        }

        return WithState(_currentState.SetItem(typeof(T).Name, entry));
    }
    /// <summary>
    /// Gets a singleton entry by ID.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public T GetSingleton<T>(string id)
    {
        if (_currentState.TryGetValue(id, out var entry))
        {
            return (T)entry;
        }
        throw new InvalidOperationException($"Singleton with ID {id} does not exist.");
    }

    /// <summary>
    /// Gets a singleton entry by type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public T GetSingleton<T>()
    {
        if (_currentState.TryGetValue(typeof(T).Name, out var entry))
        {
            return (T)entry;
        }
        throw new InvalidOperationException($"Singleton of type {typeof(T).Name} does not exist.");
    }

    /// <summary>
    /// Gets an entry by ID.
    /// </summary>
    /// <typeparam name="T">The type of entry.</typeparam>
    /// <param name="id">The entry ID.</param>
    /// <returns>The entry.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the type is not registered.</exception>
    public T GetEntry<T>(string id)
    {
        if (!_typeToTable.TryGetValue(typeof(T), out var tableName))
        {
            throw new InvalidOperationException($"Type {typeof(T).Name} is not mapped to a table.");
        }

        return GetEntry<T>(tableName, id)!;
    }

    /// <summary>
    /// Gets an entry by table name and ID.
    /// </summary>
    /// <typeparam name="T">The type of entry.</typeparam>
    /// <param name="tableName">The table name.</param>
    /// <param name="id">The entry ID.</param>
    /// <returns>The entry, or default if not found.</returns>
    public T? GetEntry<T>(string tableName, string id)
    {
        var table = GetTable<T>(tableName);
        return table.TryGetValue(id, out var entry) ? entry : default;
    }

    /// <summary>
    /// Tries to get an entry by ID.
    /// </summary>
    /// <typeparam name="T">The type of entry.</typeparam>
    /// <param name="id">The entry ID.</param>
    /// <param name="entry">The entry if found.</param>
    /// <returns>True if found, false otherwise.</returns>
    public bool TryGetEntry<T>(string id, out T? entry)
    {
        entry = default;
        if (!_typeToTable.TryGetValue(typeof(T), out var tableName))
        {
            return false;
        }

        var table = GetTable<T>(tableName);
        return table.TryGetValue(id, out entry);
    }

    /// <summary>
    /// Checks if an entry exists.
    /// </summary>
    /// <typeparam name="T">The type of entry.</typeparam>
    /// <param name="id">The entry ID.</param>
    /// <returns>True if exists, false otherwise.</returns>
    public bool Exists<T>(string id)
    {
        if (!_typeToTable.TryGetValue(typeof(T), out var tableName))
        {
            return false;
        }
        return GetTable<T>(tableName).ContainsKey(id);
    }

    /// <inheritdoc/>
    public IImmutableDictionary<string, object> Add(string key, object value)
    {
        return WithState(_currentState.Add(key, value));
    }

    /// <inheritdoc/>
    public IImmutableDictionary<string, object> AddRange(IEnumerable<KeyValuePair<string, object>> pairs)
    {
        return WithState(_currentState.AddRange(pairs));
    }

    /// <inheritdoc/>
    public IImmutableDictionary<string, object> Clear()
    {
        return WithState(_currentState.Clear());
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<string, object> pair)
    {
        return _currentState.Contains(pair);
    }

    /// <inheritdoc/>
    public IImmutableDictionary<string, object> Remove(string key)
    {
        return WithState(_currentState.Remove(key));
    }

    /// <inheritdoc/>
    public IImmutableDictionary<string, object> RemoveRange(IEnumerable<string> keys)
    {
        return WithState(_currentState.RemoveRange(keys));
    }

    /// <inheritdoc/>
    public IImmutableDictionary<string, object> SetItem(string key, object value)
    {
        return WithState(_currentState.SetItem(key, value));
    }

    /// <inheritdoc/>
    public IImmutableDictionary<string, object> SetItems(IEnumerable<KeyValuePair<string, object>> items)
    {
        return WithState(_currentState.SetItems(items));
    }

    /// <inheritdoc/>
    public bool TryGetKey(string equalKey, out string actualKey)
    {
        return _currentState.TryGetKey(equalKey, out actualKey);
    }

    /// <inheritdoc/>
    public bool ContainsKey(string key)
    {
        return _currentState.ContainsKey(key);
    }

    /// <inheritdoc/>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
    {
        return _currentState.TryGetValue(key, out value);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return _currentState.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
