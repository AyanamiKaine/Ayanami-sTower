using NLog;

namespace AyanamisTower.SFPM;

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
    /// Evaluates this criteria against facts provided by the source.
    /// Implementations should retrieve the typed fact corresponding to FactName
    /// from the source and perform their specific matching logic.
    /// </summary>
    /// <param name="facts">The source providing access to typed facts.</param>
    /// <returns>true if the criteria is met based on the fact source; otherwise, false.</returns>
    bool Evaluate(IFactSource facts); // Changed from Matches(object)
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
    Predicate,
}

/*
Maybe we can improve this by adding a source code generator, for generating the conditions
*/

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
public class Criteria<TValue>(string factName, TValue? expectedValue, Operator @operator)
    : ICriteria
    where TValue : IComparable<TValue>
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
    private readonly string _predicateName = string.Empty;
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
        this._predicate =
            predicate ?? throw new ArgumentNullException(paramName: nameof(predicate));
        _predicateName = predicateName;
    }

    /// <summary>
    /// Evaluates the criteria by fetching the typed fact from the source.
    /// </summary>
    /// <param name="facts">The fact source.</param>
    /// <returns>True if the criteria matches, false otherwise.</returns>
    public bool Evaluate(IFactSource facts)
    {
        // Try to get the fact value with the correct type from the source
        if (facts.TryGetFact(FactName, out TValue? typedFactValue))
        {
            // Fact exists and has the expected type (or is null if TValue is nullable)

            if (Operator == Operator.Predicate)
            {
                if (_predicate == null) // Defensive check
                {
                    Logger.Error(
                        $"SFPM.Criteria.Evaluate: Predicate operator used but predicate is null for fact '{FactName}'. Returning false."
                    );
                    return false;
                }
                var result = _predicate(typedFactValue!); // Execute predicate. Use ! assuming predicate handles potential null TValue if needed.
                Logger.ConditionalDebug(
                    $"SFPM.Criteria.Evaluate: FactName={FactName}, Predicate={(_predicateName.Length == 0 ? "NoNameGiven" : _predicateName)}, PredicateResult={result}, ProvidedValue='{typedFactValue}'"
                );
                return result;
            }
            else
            {
                // Perform comparison using typed values
                try
                {
                    // Comparer<T> handles nulls and IComparable/IComparable<T>
                    int comparisonResult = 0;
                    if (
                        Operator == Operator.GreaterThan
                        || Operator == Operator.LessThan
                        || Operator == Operator.GreaterThanOrEqual
                        || Operator == Operator.LessThanOrEqual
                    )
                    {
                        // Comparison only makes sense if both values can be compared meaningfully.
                        // Comparer<TValue>.Default handles nulls (null is less than non-null).
                        // It throws if TValue isn't comparable and neither value is null.
                        comparisonResult = Comparer<TValue>.Default.Compare(
                            typedFactValue,
                            ExpectedValue
                        );
                    }

                    bool matchResult = Operator switch
                    {
                        Operator.Equal => EqualityComparer<TValue>.Default.Equals(
                            typedFactValue,
                            ExpectedValue
                        ),
                        Operator.GreaterThan => comparisonResult > 0,
                        Operator.LessThan => comparisonResult < 0,
                        Operator.GreaterThanOrEqual => comparisonResult >= 0,
                        Operator.LessThanOrEqual => comparisonResult <= 0,
                        Operator.NotEqual => !EqualityComparer<TValue>.Default.Equals(
                            typedFactValue,
                            ExpectedValue
                        ),
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(Operator),
                            Operator,
                            "Unknown operator during evaluation"
                        ),
                    };
                    Logger.ConditionalDebug(
                        $"SFPM.Criteria.Evaluate: FactName='{FactName}', Operator={Operator}, Expected='{ExpectedValue}', Actual='{typedFactValue}', Result={matchResult}"
                    );
                    return matchResult;
                }
                catch (ArgumentException ex) // Comparer<T>.Default throws if T is not comparable
                {
                    Logger.Error(
                        ex,
                        $"SFPM.Criteria.Evaluate: Failed to compare values for FactName='{FactName}' (Type: {typeof(TValue).Name}). Ensure the type implements IComparable or IComparable<{typeof(TValue).Name}> for comparison operators."
                    );
                    return false; // Cannot compare, criteria fails
                }
            }
        }
        else
        {
            // Fact not found in source or has the wrong type
            Logger.ConditionalDebug(
                $"SFPM.Criteria.Evaluate: Fact '{FactName}' not found in source or has wrong type (Expected: {typeof(TValue).Name}). Criteria fails."
            );
            return false;
        }
    }
}
