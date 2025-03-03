using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;
using StellaInvicta.Tags.Relationships;

namespace StellaInvicta;

/// <summary>
/// Static Class to create predefined entities for stella invicta
/// </summary>
public static class EntityCreator
{
    // TODO: We should define a random name character generator based on culture, religion, etc.
    // TODO: Implement Logging
    /// <summary>
    /// Creates a character
    /// </summary>
    /// <param name="world"></param>
    /// <param name="locatedAt">This creates the relationship where the character is</param>
    /// <param name="characterName">Display name of the character</param>
    /// <param name="age">Starting age of the character</param>
    /// <param name="birthDate">When the character was born</param>
    /// <param name="culture">Culture of the character</param>
    /// <param name="specie">Specie of the character</param>
    /// <param name="religion">Religion of the character</param>
    /// <returns></returns>
    public static Entity CreateCharacter(World world, Entity locatedAt, Entity culture, Entity specie, Entity religion, GameDate birthDate, string characterName, int age = 0)
    {
        return world.Entity($"{characterName}-CHARACTER")
            .Set<Birthday, GameDate>(birthDate)
            .Add<Culture>(culture)
            .Add<Specie>(specie)
            .Add<Religion>(religion)
            .Add<Character>()
            .Add<LocatedAt>(locatedAt)
            .Set<Name>(new(characterName))
            .Set<Age>(new(age));
    }
}