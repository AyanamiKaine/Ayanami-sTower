using NLog;

namespace SFPM;

/// <summary>
/// Defines a criteria for matching facts.
/// </summary>
public interface ICriteria
{
    /// <summary>
    /// The FactName is used as a key to get the value out of the query data.
    /// </summary>
    string FactName { get; }
    /// <summary>
    /// Gets the operator used for comparison.
    /// </summary>
    Operator Operator { get; }
    /// <summary>
    /// Determines whether the specified fact value matches the criteria.
    /// </summary>
    /// <param name="factValue">The value of the fact to compare against the expected value.</param>
    /// <returns>true if the fact value matches the criteria; otherwise, false.</returns>
    bool Matches(object factValue); // Matches method now takes object - we'll handle type inside implementations
}

/// <summary>
/// Represents the comparison operators for criteria matching.
/// </summary>
public enum Operator
{
    /// <summary>
    /// Represents the equality operator.
    /// </summary>
    Equal,
    /// <summary>
    /// Represents the greater than operator.
    /// </summary>
    GreaterThan,
    /// <summary>
    /// Represents the less than operator.
    /// </summary>
    LessThan,
    /// <summary>
    /// Represents the greater than or equal operator.
    /// </summary>
    GreaterThanOrEqual,
    /// <summary>
    /// Represents the less than or equal operator.
    /// </summary>
    LessThanOrEqual,
    /// <summary>
    /// Represents the not equal operator.
    /// </summary>
    NotEqual,
    /// <summary>
    /// Added Custom operator for predicate-based criteria
    /// </summary>
    Predicate
}


/// <summary>
/// Represents a criteria for matching facts with a specific value and operator.
/// </summary>
/// <typeparam name="TValue">The type of the value to be compared.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="Criteria{TValue}"/> class.
/// </remarks>
/// <param name="factName">The name of the fact. Used as a key for the query data.</param>
/// <param name="expectedValue">The expected value for comparison.</param>
/// <param name="operator">The operator used for comparison.</param>
public class Criteria<TValue>(string factName, TValue? expectedValue, Operator @operator) : ICriteria where TValue : IComparable<TValue>
{
    /// <summary>
    /// Gets the name of the fact.
    /// </summary>
    public string FactName { get; } = factName;
    /// <summary>
    /// Gets the expected value for comparison.
    /// </summary>
    public TValue? ExpectedValue { get; } = expectedValue;
    /// <summary>
    /// Gets the operator used for comparison.
    /// </summary>
    public Operator Operator { get; } = @operator;
    private readonly Predicate<TValue> _predicate = _ => false; // Nullable predicate, used for custom logic
    private readonly string _predicateName = "";
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Initializes a new instance of the <see cref="Criteria{TValue}"/> class with a custom predicate.
    /// </summary>
    /// <param name="factName">The name of the fact. Used as a key for the query data.</param>
    /// <param name="predicate">The predicate used for custom evaluation.</param>
    /// <param name="predicateName">Optional name for the predicate function. Used for display purposes. Pick a descriptive name what the predicate checks</param>
    public Criteria(string factName, Predicate<TValue> predicate, string predicateName = "")
        : this(factName: factName, expectedValue: default, @operator: Operator.Predicate)
    {
        this._predicate = predicate ?? throw new ArgumentNullException(paramName: nameof(predicate));
        _predicateName = predicateName;
    }

    /// <summary>
    /// Determines whether the specified fact value matches the criteria.
    /// </summary>
    /// <param name="factValue">The value of the fact to compare against the expected value.</param>
    /// <returns>true if the fact value matches the criteria; otherwise, false.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public bool Matches(object factValue)
    {
        // **Crucial Type Check:**
        if (factValue is TValue typedFactValue) // Check if factValue is of the correct type
        {
            if (Operator == Operator.Predicate) // Check for Custom operator and predicate
            {
                var result = _predicate(obj: typedFactValue); // Execute the predicate lambda
                Logger.ConditionalDebug(message: $"SFPM.Criteria.Matches: FactName={FactName}, Predicate={(_predicateName.Length == 0 ? "NoNameGiven" : _predicateName)}, PredicateResult={result}, ProvidedPraticateValue={typedFactValue}");
                return result;
            }
            else
            {
                return Operator switch
                {
                    Operator.Equal => EqualityComparer<TValue>.Default.Equals(x: typedFactValue, y: ExpectedValue),
                    Operator.GreaterThan => typedFactValue.CompareTo(other: ExpectedValue) > 0,
                    Operator.LessThan => typedFactValue.CompareTo(other: ExpectedValue) < 0,
                    Operator.GreaterThanOrEqual => typedFactValue.CompareTo(other: ExpectedValue) >= 0,
                    Operator.LessThanOrEqual => typedFactValue.CompareTo(other: ExpectedValue) <= 0,
                    Operator.NotEqual => !EqualityComparer<TValue>.Default.Equals(x: typedFactValue, y: ExpectedValue),
                    _ => throw new ArgumentOutOfRangeException(paramName: nameof(Operator), actualValue: Operator, message: "Unknown operator"),
                };
            }
        }
        else
        {
            // Handle case where factValue is not of the expected type TValue
            // You might want to:
            // - Return false (fact doesn't match if wrong type) - as shown below
            // - Throw an exception (if you expect facts to always be of the correct type)
            // - Log a warning
            return false; // Or throw new ArgumentException($"Fact value for '{FactName}' is not of expected type {typeof(TValue).Name}");
        }
    }
}