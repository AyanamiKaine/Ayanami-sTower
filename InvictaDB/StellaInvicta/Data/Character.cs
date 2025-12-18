namespace StellaInvicta.Data;
/// <summary>
/// Represents a character within the game world.
/// </summary>
/// <param name="Name"></param>
/// <param name="Age"></param>
/// <param name="Martial"></param>
/// <param name="Sterwardship"></param>
/// <param name="Intrigue"></param>
/// <param name="Learning"></param>
/// <param name="BirthDate"></param>
public record Character(
    string Name,
    int Age,
    int Martial,
    int Sterwardship,
    int Intrigue,
    int Learning,
    DateTime BirthDate
);