namespace InvictaDB;

/// <summary>
/// A smart reference to an entity of type T, providing safe navigation and validation.
/// </summary>
/// <typeparam name="T">The type of entity being referenced.</typeparam>
public readonly struct Ref<T> : IEquatable<Ref<T>> where T : class
{
    /// <summary>
    /// Gets the ID of the referenced entity.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets a value indicating whether this reference is empty (has no ID).
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(Id);

    /// <summary>
    /// Creates a new reference with the specified ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    public Ref(string id) => Id = id;

    /// <summary>
    /// Gets an empty reference.
    /// </summary>
    public static Ref<T> Empty => new(string.Empty);

    /// <summary>
    /// Implicitly converts a string ID to a reference.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    public static implicit operator Ref<T>(string id) => new(id);

    /// <inheritdoc />
    public bool Equals(Ref<T> other) => Id == other.Id;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Ref<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Id?.GetHashCode() ?? 0;

    /// <inheritdoc />
    public override string ToString() => Id ?? "(empty)";

    /// <summary>
    /// Resolves this reference to the actual entity.
    /// </summary>
    /// <param name="db">The database containing the entity.</param>
    /// <returns>The referenced entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the reference is empty.</exception>
    public T Resolve(InvictaDatabase db)
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("Cannot resolve an empty reference.");
        }
        return db.GetEntry<T>(Id);
    }

    /// <summary>
    /// Tries to resolve this reference to the actual entity.
    /// </summary>
    /// <param name="db">The database containing the entity.</param>
    /// <returns>The referenced entity, or null if not found or reference is empty.</returns>
    public T? TryResolve(InvictaDatabase db)
    {
        if (IsEmpty) return default;
        return db.TryGetEntry<T>(Id, out var entry) ? entry : default;
    }

    /// <summary>
    /// Checks if the referenced entity exists in the store.
    /// </summary>
    /// <param name="db">The database to check.</param>
    /// <returns>True if the entity exists, false otherwise.</returns>
    public bool Exists(InvictaDatabase db)
    {
        return !IsEmpty && db.Exists<T>(Id);
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(Ref<T> left, Ref<T> right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(Ref<T> left, Ref<T> right) => !left.Equals(right);
}
