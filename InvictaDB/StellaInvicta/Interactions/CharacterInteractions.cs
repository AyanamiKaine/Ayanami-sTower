using InvictaDB;
using StellaInvicta.Data;

namespace StellaInvicta.Interactions;

/// <summary>
/// Extension methods for character interactions.
/// </summary>
public static class CharacterInteractions
{
    /// <summary>
    /// An example interaction where a character performs an action.
    /// </summary>
    /// <param name="db"></param>
    /// <param name="character"></param>
    public static InvictaDatabase ExampleInteraction(this Character character, InvictaDatabase db)
    {
        Console.WriteLine($"Character {character.Name} is performing an interaction.");
        return db;
    }
}