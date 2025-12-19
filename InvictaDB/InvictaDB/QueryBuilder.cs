namespace InvictaDB;

/// <summary>
/// Fluent query builder for constructing complex database queries.
/// Provides a chainable API for filtering, joining, and projecting data.
/// </summary>
/// <typeparam name="T">The entity type being queried.</typeparam>
public class QueryBuilder<T>
{
    private readonly InvictaDatabase _db;
    private readonly List<Func<string, T, bool>> _filters = [];

    /// <summary>
    /// Creates a new query builder for the specified entity type.
    /// </summary>
    /// <param name="db">The database to query.</param>
    public QueryBuilder(InvictaDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// Adds a filter predicate to the query.
    /// </summary>
    /// <param name="predicate">The filter condition.</param>
    /// <returns>This builder for chaining.</returns>
    public QueryBuilder<T> Where(Func<string, T, bool> predicate)
    {
        _filters.Add(predicate);
        return this;
    }

    /// <summary>
    /// Adds a filter that checks if an entity with the given ID exists.
    /// </summary>
    /// <param name="idSelector">Function to get the foreign key ID.</param>
    /// <typeparam name="TRef">The referenced entity type.</typeparam>
    /// <returns>This builder for chaining.</returns>
    public QueryBuilder<T> WhereExists<TRef>(Func<T, string> idSelector)
    {
        return Where((_, entity) => _db.Exists<TRef>(idSelector(entity)));
    }

    /// <summary>
    /// Adds a filter that checks if a Ref resolves to an existing entity.
    /// </summary>
    /// <param name="refSelector">Function to get the Ref.</param>
    /// <typeparam name="TRef">The referenced entity type.</typeparam>
    /// <returns>This builder for chaining.</returns>
    public QueryBuilder<T> WhereRefExists<TRef>(Func<T, Ref<TRef>> refSelector) where TRef : class
    {
        return Where((_, entity) => _db.Exists<TRef>(refSelector(entity).Id));
    }

    /// <summary>
    /// Executes the query and returns matching entities.
    /// </summary>
    /// <returns>Entities matching all filters.</returns>
    public IEnumerable<T> ToList()
    {
        var table = _db.GetTable<T>();

        foreach (var (id, entity) in table)
        {
            bool matches = true;
            foreach (var filter in _filters)
            {
                if (!filter(id, entity))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
                yield return entity;
        }
    }

    /// <summary>
    /// Executes the query and returns matching entities with their IDs.
    /// </summary>
    /// <returns>Tuples of (ID, Entity) for matching entities.</returns>
    public IEnumerable<(string Id, T Entity)> ToListWithIds()
    {
        var table = _db.GetTable<T>();

        foreach (var (id, entity) in table)
        {
            bool matches = true;
            foreach (var filter in _filters)
            {
                if (!filter(id, entity))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
                yield return (id, entity);
        }
    }

    /// <summary>
    /// Projects results to a new type.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="selector">The projection function.</param>
    /// <returns>Projected results.</returns>
    public IEnumerable<TResult> Select<TResult>(Func<string, T, TResult> selector)
    {
        return ToListWithIds().Select(x => selector(x.Id, x.Entity));
    }

    /// <summary>
    /// Gets the first matching entity, or default.
    /// </summary>
    /// <returns>The first matching entity, or default.</returns>
    public T? FirstOrDefault()
    {
        return ToList().FirstOrDefault();
    }

    /// <summary>
    /// Gets the first matching entity with ID, or null.
    /// </summary>
    /// <returns>The first matching entity with ID, or null.</returns>
    public (string Id, T Entity)? FirstOrDefaultWithId()
    {
        var result = ToListWithIds().FirstOrDefault();
        return result.Id == null ? null : result;
    }

    /// <summary>
    /// Counts matching entities.
    /// </summary>
    /// <returns>The count of matching entities.</returns>
    public int Count()
    {
        return ToList().Count();
    }

    /// <summary>
    /// Checks if any entity matches.
    /// </summary>
    /// <returns>True if any entity matches.</returns>
    public bool Any()
    {
        return ToList().Any();
    }
}

/// <summary>
/// Extension methods for creating query builders.
/// </summary>
public static class QueryBuilderExtensions
{
    /// <summary>
    /// Creates a fluent query builder for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="db">The database.</param>
    /// <returns>A new query builder.</returns>
    public static QueryBuilder<T> Query<T>(this InvictaDatabase db)
    {
        return new QueryBuilder<T>(db);
    }
}
