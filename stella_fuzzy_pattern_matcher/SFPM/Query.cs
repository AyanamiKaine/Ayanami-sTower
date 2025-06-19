using NLog;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AyanamisTower.SFPM;

/// <summary>
/// Represents a request to match rules against a specific set of facts provided by an IFactSource.
/// </summary>
public class Query
{
    /// <summary>
    /// Represents a request to match rules against a specific set of facts provided by an IFactSource.
    /// </summary>
    private readonly IFactSource _factSource; // Store the fact source
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Initializes a new instance of the Query class with a fact source.
    /// </summary>
    /// <param name="factSource">The source of facts for this query.</param>
    /// <exception cref="ArgumentNullException">Thrown if factSource is null.</exception>
    public Query(IFactSource factSource)
    {
        _factSource = factSource ?? throw new ArgumentNullException(nameof(factSource));
    }

    /// <summary>
    /// Matches the query's fact source against a collection of rules.
    /// </summary>
    /// <param name="rules">The collection of rules to match against.</param>
    public void Match(IEnumerable<Rule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        Logger.ConditionalDebug(
            $"SFPM.Query.Match: Initiating match for query using {_factSource.GetType().Name}."
        );
        // Delegate directly to the extension method, passing the internal fact source
        rules.Match(_factSource);
    }

    // --- Static helper for convenience if still working with dictionaries often ---

    /// <summary>
    /// Creates a Query instance using a dictionary as the fact source.
    /// </summary>
    /// <param name="queryData">The dictionary containing fact data.</param>
    /// <returns>A new Query instance.</returns>
    public static Query FromDictionary(Dictionary<string, object> queryData)
    {
        ArgumentNullException.ThrowIfNull(queryData);
        return new Query(new DictionaryFactSource(queryData));
    }

    /// <summary>
    /// Private adapter class to wrap a Dictionary as an IFactSource.
    /// </summary>
    private class DictionaryFactSource : IFactSource
    {
        private readonly Dictionary<string, object> _data;

        public DictionaryFactSource(Dictionary<string, object> data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryGetFact<TValue>(string factName, [MaybeNullWhen(false)] out TValue value)
        {
            if (_data.TryGetValue(factName, out object? objValue))
            {
                // Check if the object value is assignable to TValue
                if (objValue is TValue typedValue)
                {
                    value = typedValue;
                    return true;
                }
                // Handle null assignment explicitly if TValue is nullable and object value is null
                if (objValue == null && default(TValue) == null) // Checks if TValue can accept null
                {
                    value = default!; // Assign default (which is null for nullable TValue)
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
}
