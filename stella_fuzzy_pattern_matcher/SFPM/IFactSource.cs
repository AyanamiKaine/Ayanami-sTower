using System;
using System.Diagnostics.CodeAnalysis;

namespace AyanamisTower.SFPM;

/// <summary>
/// Represents a source of facts that can be queried by type.
/// </summary>
public interface IFactSource
{
    /// <summary>
    /// Tries to get a fact value of the specified type.
    /// </summary>
    /// <typeparam name="TValue">The expected type of the fact.</typeparam>
    /// <param name="factName">The name of the fact.</param>
    /// <param name="value">When this method returns, contains the value associated
    /// with the specified fact name, if the fact is found and is of the correct type;
    /// otherwise, the default value for the type TValue. This parameter is passed uninitialized.</param>
    /// <returns>true if the fact source contains an element with the specified name
    /// and the element is of type TValue (or assignable to it); otherwise, false.</returns>
    bool TryGetFact<TValue>(string factName, [MaybeNullWhen(false)] out TValue value);

    // Optional: Could add other methods like ContainsFact(string factName) if needed,
    // but TryGetFact is the essential one for typed evaluation.
}
