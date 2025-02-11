namespace SFPM;

/// <summary>
/// Represents a query, its a set of key, value pairs.
/// </summary>
public class Query()
{
    private readonly Dictionary<string, object> _queryData = [];

    /// <summary>
    /// Adds a key and value to the query.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public Query Add(string key, object value)
    {
        _queryData.Add(key, value);
        return this;
    }
}