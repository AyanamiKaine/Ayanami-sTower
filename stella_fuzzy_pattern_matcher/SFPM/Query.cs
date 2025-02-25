using NLog;

namespace SFPM;

/// <summary>
/// Represents a query, it's a set facts, represented as a key value pair.
/// </summary>
public class Query()
{

    /// <summary>
    /// Sets the query data from a dictionary
    /// </summary>
    /// <param name="queryData"></param>
    public Query(Dictionary<string, object> queryData) : this()
    {
        _queryData = queryData;
    }

    private readonly Dictionary<string, object> _queryData = [];
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Adds a key and value to the query.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public Query Add(string key, object value)
    {
        _queryData.Add(
            key: key,
            value: value);
        return this;
    }

    /// <summary>
    /// Matches a query against a list of rules, it tries to select a rule that matches the most
    /// (i.e. the most criteria) as its more specific. Then it runs the payload of the rule.
    ///
    /// If more than one rule with the same amount of criteria matches a random one in selected.
    ///
    /// To improve performance, sort rules from most specific rule to less.
    /// I.e. the first rule in the list should have the most criteria.
    /// </summary>
    /// <param name="rules"></param>
    public void Match(List<Rule> rules)
    {
        rules.Match(_queryData);
    }
}