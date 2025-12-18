namespace StellaInvicta.Data;
/// <summary>
/// Represents a type of relationships within the game world.
/// </summary>
/// <param name="Name"></param>
/// <param name="IsSymmetric"></param>
public record RelationshipType(string Name, bool IsSymmetric);