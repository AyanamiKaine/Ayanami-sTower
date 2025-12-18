using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using AyanamisTower.SFPM;

namespace InvictaDB.PatternMatching;

/// <summary>
/// Provides IFactSource adapters for InvictaDatabase, enabling fuzzy pattern matching
/// against database state and entities.
/// </summary>
public static class FactSourceAdapters
{
    /// <summary>
    /// Creates a fact source from the database's singletons.
    /// Fact names correspond to singleton type names or custom IDs.
    /// </summary>
    public static IFactSource AsFactSource(this InvictaDatabase db)
    {
        return new DatabaseFactSource(db);
    }

    /// <summary>
    /// Creates a fact source from a specific entity, using property names as fact names.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to wrap.</param>
    public static IFactSource AsFactSource<T>(this T entity) where T : class
    {
        return new EntityFactSource<T>(entity);
    }

    /// <summary>
    /// Creates a composite fact source that checks multiple sources in order.
    /// Useful for combining entity properties with global game state.
    /// </summary>
    public static IFactSource Combine(params IFactSource[] sources)
    {
        return new CompositeFactSource(sources);
    }

    /// <summary>
    /// Creates a fact source for an entity that also includes database singletons as fallback.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="db">The database for singleton access.</param>
    public static IFactSource AsFactSourceWithContext<T>(this T entity, InvictaDatabase db) where T : class
    {
        return new CompositeFactSource(new EntityFactSource<T>(entity), new DatabaseFactSource(db));
    }
}

/// <summary>
/// Fact source that wraps an InvictaDatabase, providing access to singletons.
/// </summary>
internal class DatabaseFactSource : IFactSource
{
    private readonly InvictaDatabase _db;

    public DatabaseFactSource(InvictaDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public bool TryGetFact<TValue>(string factName, [MaybeNullWhen(false)] out TValue value)
    {
        // Try to get a singleton by name
        if (_db.TryGetValue(factName, out var obj) && obj is TValue typed)
        {
            value = typed;
            return true;
        }

        // Try by type name if TValue matches
        var typeName = typeof(TValue).Name;
        if (_db.TryGetValue(typeName, out obj) && obj is TValue typedByType)
        {
            value = typedByType;
            return true;
        }

        value = default;
        return false;
    }
}

/// <summary>
/// High-performance fact source that wraps an entity, using compiled property accessors.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
internal class EntityFactSource<T> : IFactSource where T : class
{
    private readonly T _entity;
    private static readonly ConcurrentDictionary<string, Delegate?> _propertyAccessors = new();
    private static readonly ConcurrentDictionary<string, Type> _propertyTypes = new();

    static EntityFactSource()
    {
        // Pre-cache all property accessors for this type
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanRead)
            {
                _propertyTypes[prop.Name] = prop.PropertyType;
                _propertyAccessors[prop.Name] = CreatePropertyAccessor(prop);
            }
        }
    }

    public EntityFactSource(T entity)
    {
        _entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }

    public bool TryGetFact<TValue>(string factName, [MaybeNullWhen(false)] out TValue value)
    {
        if (_propertyAccessors.TryGetValue(factName, out var accessor) && accessor != null)
        {
            // Check if the property type is compatible
            if (_propertyTypes.TryGetValue(factName, out var propType))
            {
                if (typeof(TValue).IsAssignableFrom(propType) || propType.IsAssignableFrom(typeof(TValue)))
                {
                    try
                    {
                        var rawValue = ((Func<T, object?>)accessor)(_entity);
                        if (rawValue is TValue typed)
                        {
                            value = typed;
                            return true;
                        }
                        // Handle null for nullable types
                        if (rawValue == null && default(TValue) == null)
                        {
                            value = default!;
                            return true;
                        }
                    }
                    catch
                    {
                        // Property access failed
                    }
                }
            }
        }

        value = default;
        return false;
    }

    private static Delegate? CreatePropertyAccessor(PropertyInfo property)
    {
        try
        {
            var parameter = Expression.Parameter(typeof(T), "entity");
            var propertyAccess = Expression.Property(parameter, property);
            var converted = Expression.Convert(propertyAccess, typeof(object));
            var lambda = Expression.Lambda<Func<T, object?>>(converted, parameter);
            return lambda.Compile();
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Fact source that combines multiple sources, checking each in order.
/// </summary>
internal class CompositeFactSource : IFactSource
{
    private readonly IFactSource[] _sources;

    public CompositeFactSource(params IFactSource[] sources)
    {
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
    }

    public bool TryGetFact<TValue>(string factName, [MaybeNullWhen(false)] out TValue value)
    {
        foreach (var source in _sources)
        {
            if (source.TryGetFact(factName, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }
}
