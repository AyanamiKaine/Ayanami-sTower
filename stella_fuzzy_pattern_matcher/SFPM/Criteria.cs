namespace SFPM;

/// <summary>
/// Defines a criteria for matching facts.
/// </summary>
public interface ICriteria
{
    /// <summary>
    /// Gets the name of the fact.
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
    Custom
}


/// <summary>
/// Represents a criteria for matching facts with a specific value and operator.
/// </summary>
/// <typeparam name="TValue">The type of the value to be compared.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="Criteria{TValue}"/> class.
/// </remarks>
/// <param name="factName">The name of the fact.</param>
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
    private readonly Predicate<TValue>? predicate; // Nullable predicate, used for custom logic

    /// <summary>
    /// Initializes a new instance of the <see cref="Criteria{TValue}"/> class with a custom predicate.
    /// </summary>
    /// <param name="factName">The name of the fact.</param>
    /// <param name="predicate">The predicate used for custom evaluation.</param>
    public Criteria(string factName, Predicate<TValue> predicate)
        : this(factName, default, Operator.Custom)
    {
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
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
            if (Operator == Operator.Custom && predicate != null) // Check for Custom operator and predicate
            {
                return predicate(typedFactValue); // Execute the predicate lambda
            }
            else
            {

                return Operator switch
                {
                    Operator.Equal => EqualityComparer<TValue>.Default.Equals(typedFactValue, ExpectedValue),
                    Operator.GreaterThan => typedFactValue.CompareTo(ExpectedValue) > 0,
                    Operator.LessThan => typedFactValue.CompareTo(ExpectedValue) < 0,
                    Operator.GreaterThanOrEqual => typedFactValue.CompareTo(ExpectedValue) >= 0,
                    Operator.LessThanOrEqual => typedFactValue.CompareTo(ExpectedValue) <= 0,
                    Operator.NotEqual => !EqualityComparer<TValue>.Default.Equals(typedFactValue, ExpectedValue),
                    _ => throw new ArgumentOutOfRangeException(nameof(Operator), Operator, "Unknown operator"),
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