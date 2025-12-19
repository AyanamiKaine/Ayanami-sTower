using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using InvictaDB.Events;
using InvictaDB.Messaging;

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

    // Event log for tracking changes
    private readonly DatabaseEventLog _eventLog;

    // Message queue for inter-system communication
    private readonly MessageQueue _messageQueue;

    /// <summary>
    /// Creates a new empty InvictaDatabase.
    /// </summary>
    public InvictaDatabase()
    {
        _currentState = [];
        _typeToTable = new ConcurrentDictionary<Type, string>();
        _tableToType = new ConcurrentDictionary<string, Type>();
        _eventLog = DatabaseEventLog.Empty;
        _messageQueue = MessageQueue.Empty;
    }

    /// <summary>
    /// Creates a new InvictaDatabase with the specified state and registries.
    /// </summary>
    private InvictaDatabase(
        ImmutableDictionary<string, object> state,
        ConcurrentDictionary<Type, string> typeToTable,
        ConcurrentDictionary<string, Type> tableToType,
        DatabaseEventLog eventLog,
        MessageQueue messageQueue)
    {
        _currentState = state;
        _typeToTable = typeToTable;
        _tableToType = tableToType;
        _eventLog = eventLog;
        _messageQueue = messageQueue;
    }

    /// <summary>
    /// Creates a new InvictaDatabase with updated state, sharing the type registries.
    /// </summary>
    private InvictaDatabase WithState(ImmutableDictionary<string, object> newState)
    {
        return new InvictaDatabase(newState, _typeToTable, _tableToType, _eventLog, _messageQueue);
    }

    /// <summary>
    /// Creates a new InvictaDatabase with updated state and events.
    /// </summary>
    private InvictaDatabase WithStateAndEvent(ImmutableDictionary<string, object> newState, DatabaseEvent evt)
    {
        return new InvictaDatabase(newState, _typeToTable, _tableToType, _eventLog.Add(evt), _messageQueue);
    }

    /// <summary>
    /// Gets the event log containing all events since the last clear.
    /// </summary>
    public DatabaseEventLog EventLog => _eventLog;

    /// <summary>
    /// Gets all pending events.
    /// </summary>
    public ImmutableList<DatabaseEvent> PendingEvents => _eventLog.Events;

    /// <summary>
    /// Clears all pending events, returning a new database instance.
    /// </summary>
    public InvictaDatabase ClearEvents()
    {
        return new InvictaDatabase(_currentState, _typeToTable, _tableToType, _eventLog.Clear(), _messageQueue);
    }

    /// <summary>
    /// Gets the message queue for inter-system communication.
    /// </summary>
    public MessageQueue Messages => _messageQueue;

    /// <summary>
    /// Sends a message to the message queue.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns>A new database with the message enqueued.</returns>
    public InvictaDatabase SendMessage(GameMessage message)
    {
        return new InvictaDatabase(_currentState, _typeToTable, _tableToType, _eventLog, _messageQueue.Enqueue(message));
    }

    /// <summary>
    /// Sends a message to the message queue.
    /// </summary>
    /// <param name="messageType">The type of message.</param>
    /// <param name="sender">The sender system name.</param>
    /// <param name="payload">Optional payload data.</param>
    /// <returns>A new database with the message enqueued.</returns>
    public InvictaDatabase SendMessage(string messageType, string sender, object? payload = null)
    {
        var message = GameMessage.Create(messageType, sender, payload);
        return SendMessage(message);
    }

    /// <summary>
    /// Sends a typed message to the message queue.
    /// </summary>
    /// <typeparam name="T">The payload type (used as message type).</typeparam>
    /// <param name="sender">The sender system name.</param>
    /// <param name="payload">The payload data.</param>
    /// <returns>A new database with the message enqueued.</returns>
    public InvictaDatabase SendMessage<T>(string sender, T payload)
    {
        var message = GameMessage.Create(sender, payload);
        return SendMessage(message);
    }

    /// <summary>
    /// Sends multiple messages to the message queue.
    /// </summary>
    /// <param name="messages">The messages to send.</param>
    /// <returns>A new database with the messages enqueued.</returns>
    public InvictaDatabase SendMessages(IEnumerable<GameMessage> messages)
    {
        return new InvictaDatabase(_currentState, _typeToTable, _tableToType, _eventLog, _messageQueue.EnqueueRange(messages));
    }

    /// <summary>
    /// Consumes all messages of a specific type, returning them and a new database without those messages.
    /// </summary>
    /// <param name="messageType">The type of messages to consume.</param>
    /// <returns>The consumed messages and a new database.</returns>
    public (IReadOnlyList<GameMessage> Consumed, InvictaDatabase Database) ConsumeMessages(string messageType)
    {
        var (consumed, newQueue) = _messageQueue.ConsumeMessages(messageType);
        return (consumed, new InvictaDatabase(_currentState, _typeToTable, _tableToType, _eventLog, newQueue));
    }

    /// <summary>
    /// Consumes all messages of a specific type, returning them and a new database without those messages.
    /// </summary>
    /// <typeparam name="T">The message type to consume.</typeparam>
    /// <returns>The consumed messages and a new database.</returns>
    public (IReadOnlyList<GameMessage> Consumed, InvictaDatabase Database) ConsumeMessages<T>()
    {
        return ConsumeMessages(typeof(T).Name);
    }

    /// <summary>
    /// Clears all messages from the queue.
    /// </summary>
    /// <returns>A new database with an empty message queue.</returns>
    public InvictaDatabase ClearMessages()
    {
        return new InvictaDatabase(_currentState, _typeToTable, _tableToType, _eventLog, _messageQueue.Clear());
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
            var evt = DatabaseEvent.TableRegistered<T>(tableName);
            return WithStateAndEvent(_currentState.Add(tableName, ImmutableDictionary<string, T>.Empty), evt);
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
            var evt = DatabaseEvent.TableRegistered<T>(tableName);
            return WithStateAndEvent(_currentState.Add(tableName, ImmutableDictionary<string, T>.Empty), evt);
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
        // First try to look up the table name from the type registry
        if (_typeToTable.TryGetValue(typeof(T), out var tableName))
        {
            if (_currentState.TryGetValue(tableName, out var tableObj))
            {
                return (ImmutableDictionary<string, T>)tableObj;
            }
        }
        // Fall back to using the type name directly
        else if (_currentState.TryGetValue(typeof(T).Name, out var tableObj))
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
        var isUpdate = table.ContainsKey(id);
        var oldValue = isUpdate ? table[id] : default;
        table = table.SetItem(id, entry);

        var evt = isUpdate
            ? DatabaseEvent.Update(tableName, id, oldValue, entry)
            : DatabaseEvent.Insert(tableName, id, entry);

        return WithStateAndEvent(_currentState.SetItem(tableName, table), evt);
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

        T? oldValue = default;
        if (_currentState.TryGetValue(id, out var existing))
        {
            oldValue = (T)existing;
        }

        var evt = DatabaseEvent.SingletonChanged(id, oldValue, entry);
        return WithStateAndEvent(_currentState.SetItem(id, entry), evt);
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

        var id = typeof(T).Name;
        T? oldValue = default;
        if (_currentState.TryGetValue(id, out var existing))
        {
            oldValue = (T)existing;
        }

        var evt = DatabaseEvent.SingletonChanged(id, oldValue, entry);
        return WithStateAndEvent(_currentState.SetItem(id, entry), evt);
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
    public T Get<T>(string id)
    {
        if (!_typeToTable.TryGetValue(typeof(T), out var tableName))
        {
            throw new InvalidOperationException($"Type {typeof(T).Name} is not mapped to a table.");
        }

        return Get<T>(tableName, id)!;
    }

    /// <summary>
    /// Gets an entry by table name and ID.
    /// </summary>
    /// <typeparam name="T">The type of entry.</typeparam>
    /// <param name="tableName">The table name.</param>
    /// <param name="id">The entry ID.</param>
    /// <returns>The entry, or default if not found.</returns>
    public T? Get<T>(string tableName, string id)
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
    public bool TryGet<T>(string id, out T? entry)
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

    /// <summary>
    /// Removes an entry from the appropriate table.
    /// </summary>
    /// <typeparam name="T">The type of entry.</typeparam>
    /// <param name="id">The entry ID.</param>
    /// <returns>The updated database.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the type is not registered.</exception>
    public InvictaDatabase RemoveEntry<T>(string id)
    {
        if (!_typeToTable.TryGetValue(typeof(T), out var tableName))
        {
            throw new InvalidOperationException($"Type {typeof(T).Name} is not mapped to a table.");
        }

        var table = GetTable<T>(tableName);
        if (!table.TryGetValue(id, out var oldValue))
        {
            return this; // Entry doesn't exist, return unchanged
        }

        table = table.Remove(id);
        var evt = DatabaseEvent.Remove(tableName, id, oldValue);
        return WithStateAndEvent(_currentState.SetItem(tableName, table), evt);
    }

    /// <summary>
    /// Removes a singleton from the database by ID.
    /// </summary>
    /// <typeparam name="T">The type of singleton.</typeparam>
    /// <param name="id">The singleton ID.</param>
    /// <returns>The updated database.</returns>
    public InvictaDatabase RemoveSingleton<T>(string id)
    {
        if (!_currentState.TryGetValue(id, out var existing))
        {
            return this; // Singleton doesn't exist, return unchanged
        }

        var evt = DatabaseEvent.Remove<T>(null, id, (T)existing);
        return WithStateAndEvent(_currentState.Remove(id), evt);
    }

    /// <summary>
    /// Removes a singleton from the database by type.
    /// </summary>
    /// <typeparam name="T">The type of singleton.</typeparam>
    /// <returns>The updated database.</returns>
    public InvictaDatabase RemoveSingleton<T>()
    {
        var id = typeof(T).Name;
        if (!_currentState.TryGetValue(id, out var existing))
        {
            return this; // Singleton doesn't exist, return unchanged
        }

        var evt = DatabaseEvent.Remove<T>(null, id, (T)existing);
        return WithStateAndEvent(_currentState.Remove(id), evt);
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

    #region Batch Operations

    /// <summary>
    /// Executes a batch of operations on the database, committing them as a single state transition.
    /// This is significantly more performant for multiple updates than calling Insert/Remove individually.
    /// </summary>
    /// <param name="batchAction">The action to perform on the batch builder.</param>
    /// <returns>The updated database.</returns>
    public InvictaDatabase Batch(Action<BatchOperations> batchAction)
    {
        var batch = new BatchOperations(this);
        batchAction(batch);
        return batch.Commit();
    }

    /// <summary>
    /// Helper class for batching database operations to reduce allocation overhead.
    /// </summary>
    public class BatchOperations
    {
        private readonly InvictaDatabase _sourceDb;
        private readonly ImmutableDictionary<string, object>.Builder _rootStateBuilder;
        private readonly List<DatabaseEvent> _pendingEvents = [];

        // Cache for active table builders to avoid recreating them multiple times in one batch
        private readonly Dictionary<string, ITableBuilderAdapter> _activeTableBuilders = [];

        internal BatchOperations(InvictaDatabase sourceDb)
        {
            _sourceDb = sourceDb;
            _rootStateBuilder = sourceDb._currentState.ToBuilder();
        }

        /// <summary>
        /// Inserts or updates an entry in the batch.
        /// </summary>
        /// <typeparam name="T">The type of entry.</typeparam>
        /// <param name="id">The entry ID.</param>
        /// <param name="entry">The entry to insert or update.</param>
        public void Insert<T>(string id, T entry)
        {
            if (!_sourceDb._typeToTable.TryGetValue(typeof(T), out var tableName))
            {
                throw new InvalidOperationException($"Type {typeof(T).Name} is not mapped to a table.");
            }

            var builder = GetTableBuilder<T>(tableName);

            bool isUpdate = builder.ContainsKey(id);
            T? oldValue = isUpdate ? builder[id] : default;

            // Update the builder
            builder[id] = entry;

            // Queue the event
            var evt = isUpdate
                ? DatabaseEvent.Update(tableName, id, oldValue, entry)
                : DatabaseEvent.Insert(tableName, id, entry);
            _pendingEvents.Add(evt);
        }

        /// <summary>
        /// Removes an entry in the batch.
        /// </summary>
        /// <typeparam name="T">The type of entry.</typeparam>
        /// <param name="id">The entry ID.</param>
        public void RemoveEntry<T>(string id)
        {
            if (!_sourceDb._typeToTable.TryGetValue(typeof(T), out var tableName))
            {
                throw new InvalidOperationException($"Type {typeof(T).Name} is not mapped to a table.");
            }

            var builder = GetTableBuilder<T>(tableName);

            if (builder.TryGetValue(id, out var oldValue))
            {
                builder.Remove(id);
                _pendingEvents.Add(DatabaseEvent.Remove(tableName, id, oldValue));
            }
        }

        /// <summary>
        /// Inserts or updates a singleton in the batch.
        /// </summary>
        /// <typeparam name="T">The type of singleton.</typeparam>
        /// <param name="id">The singleton ID.</param>
        /// <param name="entry">The singleton value.</param>
        public void InsertSingleton<T>(string id, T entry)
        {
            if (entry is null) throw new ArgumentNullException(nameof(entry));

            T? oldValue = default;
            if (_rootStateBuilder.TryGetValue(id, out var existing))
            {
                oldValue = (T)existing;
            }

            _rootStateBuilder[id] = entry;
            _pendingEvents.Add(DatabaseEvent.SingletonChanged(id, oldValue, entry));
        }

        /// <summary>
        /// Inserts or updates a singleton in the batch using the type name as the ID.
        /// </summary>
        /// <typeparam name="T">The type of singleton.</typeparam>
        /// <param name="entry">The singleton value.</param>
        public void InsertSingleton<T>(T entry)
        {
            InsertSingleton(typeof(T).Name, entry);
        }

        /// <summary>
        /// Removes a singleton in the batch.
        /// </summary>
        /// <typeparam name="T">The type of singleton.</typeparam>
        /// <param name="id">The singleton ID.</param>
        public void RemoveSingleton<T>(string id)
        {
            if (_rootStateBuilder.TryGetValue(id, out var existing))
            {
                _rootStateBuilder.Remove(id);
                _pendingEvents.Add(DatabaseEvent.Remove<T>(null, id, (T)existing));
            }
        }

        /// <summary>
        /// Removes a singleton in the batch using the type name as the ID.
        /// </summary>
        /// <typeparam name="T">The type of singleton.</typeparam>
        public void RemoveSingleton<T>()
        {
            RemoveSingleton<T>(typeof(T).Name);
        }

        /// <summary>
        /// Gets an entry by ID from the current batch state.
        /// </summary>
        /// <typeparam name="T">The type of entry.</typeparam>
        /// <param name="id">The entry ID.</param>
        /// <returns>The entry if found, default otherwise.</returns>
        public T? GetEntry<T>(string id)
        {
            if (!_sourceDb._typeToTable.TryGetValue(typeof(T), out var tableName))
            {
                return default;
            }

            // Check if we have an active builder for this table
            if (_activeTableBuilders.TryGetValue(tableName, out var adapter))
            {
                var builder = ((TableBuilderAdapter<T>)adapter).Builder;
                return builder.TryGetValue(id, out var value) ? value : default;
            }

            // Otherwise, check the source database
            return _sourceDb.Get<T>(tableName, id);
        }

        /// <summary>
        /// Gets a singleton by ID from the current batch state.
        /// </summary>
        /// <typeparam name="T">The type of singleton.</typeparam>
        /// <param name="id">The singleton ID.</param>
        /// <returns>The singleton if found.</returns>
        public T GetSingleton<T>(string id)
        {
            if (_rootStateBuilder.TryGetValue(id, out var value))
            {
                return (T)value;
            }
            throw new InvalidOperationException($"Singleton with ID {id} does not exist.");
        }

        /// <summary>
        /// Gets a singleton by type from the current batch state.
        /// </summary>
        /// <typeparam name="T">The type of singleton.</typeparam>
        /// <returns>The singleton if found.</returns>
        public T GetSingleton<T>()
        {
            return GetSingleton<T>(typeof(T).Name);
        }

        /// <summary>
        /// Checks if an entry exists in the current batch state.
        /// </summary>
        /// <typeparam name="T">The type of entry.</typeparam>
        /// <param name="id">The entry ID.</param>
        /// <returns>True if the entry exists.</returns>
        public bool Exists<T>(string id)
        {
            if (!_sourceDb._typeToTable.TryGetValue(typeof(T), out var tableName))
            {
                return false;
            }

            // Check if we have an active builder for this table
            if (_activeTableBuilders.TryGetValue(tableName, out var adapter))
            {
                return ((TableBuilderAdapter<T>)adapter).Builder.ContainsKey(id);
            }

            // Otherwise, check the source database
            return _sourceDb.Exists<T>(id);
        }

        /// <summary>
        /// Sends a message to the message queue within the batch.
        /// </summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="sender">The sender system name.</param>
        /// <param name="payload">The message payload.</param>
        public void SendMessage<T>(string sender, T payload)
        {
            // Note: Messages are handled separately from the main state
            // For now, we'll store them and apply at commit time
            // This requires extending the batch to track messages
            _pendingMessages.Add(GameMessage.Create(sender, payload));
        }

        private readonly List<GameMessage> _pendingMessages = [];

        /// <summary>
        /// Commits the batch changes to a new InvictaDatabase instance.
        /// </summary>
        internal InvictaDatabase Commit()
        {
            // 1. Finalize all table builders and put them back into the root state builder
            foreach (var kvp in _activeTableBuilders)
            {
                var tableName = kvp.Key;
                var tableAdapter = kvp.Value;
                _rootStateBuilder[tableName] = tableAdapter.Build();
            }

            // 2. Create the new immutable state
            var finalState = _rootStateBuilder.ToImmutable();

            // 3. Handle message queue
            var messageQueue = _sourceDb._messageQueue;
            foreach (var message in _pendingMessages)
            {
                messageQueue = messageQueue.Enqueue(message);
            }

            // 4. Return new DB instance with updated state and added events
            return new InvictaDatabase(
                finalState,
                _sourceDb._typeToTable,
                _sourceDb._tableToType,
                _sourceDb._eventLog.AddRange(_pendingEvents),
                messageQueue
            );
        }

        // --- Helper Logic for Generic Builders ---

        // We need an interface to store generic builders in a non-generic dictionary
        private interface ITableBuilderAdapter
        {
            object Build();
        }

        // Concrete wrapper for the ImmutableDictionary.Builder
        private class TableBuilderAdapter<T> : ITableBuilderAdapter
        {
            public readonly ImmutableDictionary<string, T>.Builder Builder;

            public TableBuilderAdapter(ImmutableDictionary<string, T> sourceTable)
            {
                Builder = sourceTable.ToBuilder();
            }

            public object Build() => Builder.ToImmutable();
        }

        // Helper to get or create a strongly-typed builder
        private ImmutableDictionary<string, T>.Builder GetTableBuilder<T>(string tableName)
        {
            if (_activeTableBuilders.TryGetValue(tableName, out var adapter))
            {
                return ((TableBuilderAdapter<T>)adapter).Builder;
            }

            // Retrieve existing table (immutable) from source DB or create empty
            var existingTable = _sourceDb.GetTable<T>(tableName);

            // Create new adapter
            var newAdapter = new TableBuilderAdapter<T>(existingTable);
            _activeTableBuilders[tableName] = newAdapter;

            return newAdapter.Builder;
        }
    }

    #endregion
}
