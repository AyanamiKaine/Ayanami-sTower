namespace StellaECS;


/// <summary>
/// Represents an entry in the database.
/// </summary>
public class Entity(int id)
{
    /// <summary>
    /// Represent the ID used to lookup data related to the entity in the database.
    /// </summary>
    public int ID { get; } = id;
}
