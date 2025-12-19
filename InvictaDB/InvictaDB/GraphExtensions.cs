using System.Collections.Immutable;

namespace InvictaDB;

/// <summary>
/// Extension methods for graph-like operations on the database.
/// Provides utilities for traversing relationships, finding paths, and querying connected entities.
/// </summary>
public static class GraphExtensions
{
    #region Ref<T> Extensions - Following References

    /// <summary>
    /// Resolves a Ref to its actual entity, returning null if not found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="reference">The reference to resolve.</param>
    /// <returns>The entity, or default if not found.</returns>
    public static T? Resolve<T>(this InvictaDatabase db, Ref<T> reference) where T : class
    {
        return db.TryGetEntry<T>(reference.Id, out var entry) ? entry : default;
    }

    /// <summary>
    /// Resolves a Ref to its actual entity, throwing if not found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="reference">The reference to resolve.</param>
    /// <returns>The entity.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the entity doesn't exist.</exception>
    public static T ResolveRequired<T>(this InvictaDatabase db, Ref<T> reference) where T : class
    {
        if (db.TryGetEntry<T>(reference.Id, out var entry) && entry is not null)
            return entry;
        throw new KeyNotFoundException($"Entity of type {typeof(T).Name} with ID '{reference.Id}' not found.");
    }

    /// <summary>
    /// Resolves multiple refs to their actual entities, skipping any that don't exist.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="references">The references to resolve.</param>
    /// <returns>The resolved entities.</returns>
    public static IEnumerable<T> ResolveAll<T>(this InvictaDatabase db, IEnumerable<Ref<T>> references) where T : class
    {
        var table = db.GetTable<T>();
        foreach (var reference in references)
        {
            if (table.TryGetValue(reference.Id, out var entry))
                yield return entry;
        }
    }

    /// <summary>
    /// Resolves multiple refs to their actual entities with their IDs.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="references">The references to resolve.</param>
    /// <returns>Tuples of (ID, Entity) for found entities.</returns>
    public static IEnumerable<(string Id, T Entity)> ResolveAllWithIds<T>(
        this InvictaDatabase db,
        IEnumerable<Ref<T>> references) where T : class
    {
        var table = db.GetTable<T>();
        foreach (var reference in references)
        {
            if (table.TryGetValue(reference.Id, out var entry))
                yield return (reference.Id, entry);
        }
    }

    #endregion

    #region Generic Graph Traversal

    /// <summary>
    /// Finds all entities reachable from a starting entity using a neighbor function.
    /// Uses breadth-first search.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="startId">The starting entity ID.</param>
    /// <param name="getNeighborIds">Function to get neighbor IDs from an entity ID.</param>
    /// <param name="maxDepth">Maximum traversal depth (default: unlimited).</param>
    /// <returns>All reachable entity IDs with their distance from start.</returns>
    public static IEnumerable<(string Id, int Depth)> TraverseBFS<T>(
        this InvictaDatabase db,
        string startId,
        Func<InvictaDatabase, string, IEnumerable<string>> getNeighborIds,
        int maxDepth = int.MaxValue)
    {
        var visited = new HashSet<string> { startId };
        var queue = new Queue<(string Id, int Depth)>();
        queue.Enqueue((startId, 0));

        while (queue.Count > 0)
        {
            var (currentId, depth) = queue.Dequeue();
            yield return (currentId, depth);

            if (depth >= maxDepth) continue;

            foreach (var neighborId in getNeighborIds(db, currentId))
            {
                if (visited.Add(neighborId))
                {
                    queue.Enqueue((neighborId, depth + 1));
                }
            }
        }
    }

    /// <summary>
    /// Finds a path between two entities using breadth-first search.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="startId">The starting entity ID.</param>
    /// <param name="targetId">The target entity ID.</param>
    /// <param name="getNeighborIds">Function to get neighbor IDs from an entity ID.</param>
    /// <param name="maxDepth">Maximum search depth.</param>
    /// <returns>The path as a list of IDs, or null if no path exists.</returns>
    public static IReadOnlyList<string>? FindPath<T>(
        this InvictaDatabase db,
        string startId,
        string targetId,
        Func<InvictaDatabase, string, IEnumerable<string>> getNeighborIds,
        int maxDepth = int.MaxValue)
    {
        if (startId == targetId)
            return new[] { startId };

        var visited = new HashSet<string> { startId };
        var queue = new Queue<(string Id, List<string> Path)>();
        queue.Enqueue((startId, new List<string> { startId }));

        while (queue.Count > 0)
        {
            var (currentId, path) = queue.Dequeue();

            if (path.Count > maxDepth) continue;

            foreach (var neighborId in getNeighborIds(db, currentId))
            {
                if (neighborId == targetId)
                {
                    path.Add(neighborId);
                    return path;
                }

                if (visited.Add(neighborId))
                {
                    var newPath = new List<string>(path) { neighborId };
                    queue.Enqueue((neighborId, newPath));
                }
            }
        }

        return null; // No path found
    }

    /// <summary>
    /// Finds all connected components in the graph.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="getNeighborIds">Function to get neighbor IDs (should be symmetric for undirected graphs).</param>
    /// <returns>List of components, each being a set of entity IDs.</returns>
    public static IReadOnlyList<IReadOnlySet<string>> FindConnectedComponents<T>(
        this InvictaDatabase db,
        Func<InvictaDatabase, string, IEnumerable<string>> getNeighborIds)
    {
        var table = db.GetTable<T>();
        var allIds = table.Keys.ToHashSet();
        var visited = new HashSet<string>();
        var components = new List<IReadOnlySet<string>>();

        foreach (var id in allIds)
        {
            if (visited.Contains(id)) continue;

            var component = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue(id);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!component.Add(current)) continue;
                visited.Add(current);

                foreach (var neighborId in getNeighborIds(db, current))
                {
                    if (!component.Contains(neighborId) && allIds.Contains(neighborId))
                    {
                        queue.Enqueue(neighborId);
                    }
                }
            }

            components.Add(component);
        }

        return components;
    }

    /// <summary>
    /// Gets the degree (number of connections) of an entity.
    /// </summary>
    /// <param name="db">The database.</param>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="getNeighborIds">Function to get neighbor IDs.</param>
    /// <returns>The number of connections.</returns>
    public static int GetDegree(
        this InvictaDatabase db,
        string entityId,
        Func<InvictaDatabase, string, IEnumerable<string>> getNeighborIds)
    {
        return getNeighborIds(db, entityId).Count();
    }

    #endregion

    #region Join Operations

    /// <summary>
    /// Performs an inner join between two tables based on a key selector.
    /// </summary>
    /// <typeparam name="TLeft">Left table entity type.</typeparam>
    /// <typeparam name="TRight">Right table entity type.</typeparam>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="leftKeySelector">Selector for the join key from left entities.</param>
    /// <param name="rightKeySelector">Selector for the join key from right entities (usually the ID).</param>
    /// <param name="resultSelector">Function to create result from matched pairs.</param>
    /// <returns>Joined results.</returns>
    public static IEnumerable<TResult> Join<TLeft, TRight, TResult>(
        this InvictaDatabase db,
        Func<string, TLeft, string> leftKeySelector,
        Func<string, TRight, string> rightKeySelector,
        Func<string, TLeft, string, TRight, TResult> resultSelector)
    {
        var leftTable = db.GetTable<TLeft>();
        var rightTable = db.GetTable<TRight>();

        // Build a lookup from the right table
        var rightLookup = rightTable.ToDictionary(
            kvp => rightKeySelector(kvp.Key, kvp.Value),
            kvp => (kvp.Key, kvp.Value));

        foreach (var (leftId, leftEntity) in leftTable)
        {
            var joinKey = leftKeySelector(leftId, leftEntity);
            if (rightLookup.TryGetValue(joinKey, out var rightMatch))
            {
                yield return resultSelector(leftId, leftEntity, rightMatch.Key, rightMatch.Value);
            }
        }
    }

    /// <summary>
    /// Performs a left join between two tables based on a key selector.
    /// </summary>
    /// <typeparam name="TLeft">Left table entity type.</typeparam>
    /// <typeparam name="TRight">Right table entity type.</typeparam>
    /// <typeparam name="TResult">Result type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="leftKeySelector">Selector for the join key from left entities.</param>
    /// <param name="rightKeySelector">Selector for the join key from right entities.</param>
    /// <param name="resultSelector">Function to create result (right values may be null/default).</param>
    /// <returns>Joined results.</returns>
    public static IEnumerable<TResult> LeftJoin<TLeft, TRight, TResult>(
        this InvictaDatabase db,
        Func<string, TLeft, string> leftKeySelector,
        Func<string, TRight, string> rightKeySelector,
        Func<string, TLeft, string?, TRight?, TResult> resultSelector)
    {
        var leftTable = db.GetTable<TLeft>();
        var rightTable = db.GetTable<TRight>();

        var rightLookup = rightTable.ToDictionary(
            kvp => rightKeySelector(kvp.Key, kvp.Value),
            kvp => (kvp.Key, kvp.Value));

        foreach (var (leftId, leftEntity) in leftTable)
        {
            var joinKey = leftKeySelector(leftId, leftEntity);
            if (rightLookup.TryGetValue(joinKey, out var rightMatch))
            {
                yield return resultSelector(leftId, leftEntity, rightMatch.Key, rightMatch.Value);
            }
            else
            {
                yield return resultSelector(leftId, leftEntity, null, default);
            }
        }
    }

    #endregion

    #region Aggregation Helpers

    /// <summary>
    /// Groups entities by a key and returns counts.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The grouping key type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="keySelector">Function to extract the grouping key.</param>
    /// <returns>Dictionary of key to count.</returns>
    public static IReadOnlyDictionary<TKey, int> GroupCount<T, TKey>(
        this InvictaDatabase db,
        Func<string, T, TKey> keySelector)
        where TKey : notnull
    {
        return db.GetTable<T>()
            .GroupBy(kvp => keySelector(kvp.Key, kvp.Value))
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Groups entities by a key and aggregates values.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The grouping key type.</typeparam>
    /// <typeparam name="TValue">The value type to aggregate.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="keySelector">Function to extract the grouping key.</param>
    /// <param name="valueSelector">Function to extract the value to aggregate.</param>
    /// <param name="aggregator">Function to aggregate values.</param>
    /// <returns>Dictionary of key to aggregated value.</returns>
    public static IReadOnlyDictionary<TKey, TValue> GroupAggregate<T, TKey, TValue>(
        this InvictaDatabase db,
        Func<string, T, TKey> keySelector,
        Func<string, T, TValue> valueSelector,
        Func<IEnumerable<TValue>, TValue> aggregator)
        where TKey : notnull
    {
        return db.GetTable<T>()
            .GroupBy(kvp => keySelector(kvp.Key, kvp.Value))
            .ToDictionary(
                g => g.Key,
                g => aggregator(g.Select(kvp => valueSelector(kvp.Key, kvp.Value))));
    }

    #endregion

    #region Query Helpers

    /// <summary>
    /// Filters a table by a predicate, returning entities with their IDs.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>Filtered entities with IDs.</returns>
    public static IEnumerable<(string Id, T Entity)> Where<T>(
        this InvictaDatabase db,
        Func<string, T, bool> predicate)
    {
        return db.GetTable<T>()
            .Where(kvp => predicate(kvp.Key, kvp.Value))
            .Select(kvp => (kvp.Key, kvp.Value));
    }

    /// <summary>
    /// Projects entities to a new form.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="selector">The projection function.</param>
    /// <returns>Projected results.</returns>
    public static IEnumerable<TResult> Select<T, TResult>(
        this InvictaDatabase db,
        Func<string, T, TResult> selector)
    {
        return db.GetTable<T>()
            .Select(kvp => selector(kvp.Key, kvp.Value));
    }

    /// <summary>
    /// Gets the first entity matching a predicate, or default.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The first matching entity with ID, or null.</returns>
    public static (string Id, T Entity)? FirstOrDefault<T>(
        this InvictaDatabase db,
        Func<string, T, bool> predicate)
    {
        foreach (var kvp in db.GetTable<T>())
        {
            if (predicate(kvp.Key, kvp.Value))
                return (kvp.Key, kvp.Value);
        }
        return null;
    }

    /// <summary>
    /// Checks if any entity matches a predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>True if any entity matches.</returns>
    public static bool Any<T>(this InvictaDatabase db, Func<string, T, bool> predicate)
    {
        return db.GetTable<T>().Any(kvp => predicate(kvp.Key, kvp.Value));
    }

    /// <summary>
    /// Checks if all entities match a predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>True if all entities match.</returns>
    public static bool All<T>(this InvictaDatabase db, Func<string, T, bool> predicate)
    {
        return db.GetTable<T>().All(kvp => predicate(kvp.Key, kvp.Value));
    }

    /// <summary>
    /// Counts entities matching a predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The count of matching entities.</returns>
    public static int Count<T>(this InvictaDatabase db, Func<string, T, bool> predicate)
    {
        return db.GetTable<T>().Count(kvp => predicate(kvp.Key, kvp.Value));
    }

    #endregion
}
