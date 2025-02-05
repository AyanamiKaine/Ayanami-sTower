namespace StellaECS;


/// <summary>
/// Represents an entry in the database.
/// </summary>
public class Entity(int id, World world)
{
    /// <summary>
    /// Represent the ID used to lookup data related to the entity in the database.
    /// </summary>
    public int ID { get; } = id;
    /// <summary>
    /// Gets the name of the entity.
    /// </summary>
    public string Name
    {
        get
        {
            return World.GetEntityName(this);
        }
    }

    /// <summary>
    /// Represents the world the entity belongs to.
    /// </summary>
    public World World { get; } = world;
}
