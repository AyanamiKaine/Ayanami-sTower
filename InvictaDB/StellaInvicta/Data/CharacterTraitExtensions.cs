using InvictaDB;

namespace StellaInvicta.Data;

/// <summary>
/// Extension methods for querying CharacterTrait join tables.
/// </summary>
public static class CharacterTraitExtensions
{
    #region Query Methods

    /// <summary>
    /// Gets all trait IDs for a character.
    /// </summary>
    public static IEnumerable<string> GetTraitIds(this InvictaDatabase db, string characterId)
    {
        return db.GetTable<CharacterTrait>()
            .Where(ct => ct.Value.CharacterId.Id == characterId)
            .Select(ct => ct.Value.TraitId.Id);
    }

    /// <summary>
    /// Gets all full Trait objects for a character.
    /// </summary>
    public static IEnumerable<Trait> GetTraits(this InvictaDatabase db, string characterId)
    {
        var traitTable = db.GetTable<Trait>();
        return db.GetTraitIds(characterId)
            .Where(id => traitTable.ContainsKey(id))
            .Select(id => traitTable[id]);
    }

    /// <summary>
    /// Gets all character IDs that have a specific trait.
    /// </summary>
    public static IEnumerable<string> GetCharacterIdsWithTrait(this InvictaDatabase db, string traitId)
    {
        return db.GetTable<CharacterTrait>()
            .Where(ct => ct.Value.TraitId.Id == traitId)
            .Select(ct => ct.Value.CharacterId.Id);
    }

    /// <summary>
    /// Gets all Character objects that have a specific trait.
    /// </summary>
    public static IEnumerable<Character> GetCharactersWithTrait(this InvictaDatabase db, string traitId)
    {
        var characterTable = db.GetTable<Character>();
        return db.GetCharacterIdsWithTrait(traitId)
            .Where(id => characterTable.ContainsKey(id))
            .Select(id => characterTable[id]);
    }

    /// <summary>
    /// Checks if a character has a specific trait.
    /// </summary>
    public static bool HasTrait(this InvictaDatabase db, string characterId, string traitId)
    {
        var key = $"{characterId}:{traitId}";
        return db.Exists<CharacterTrait>(key);
    }

    /// <summary>
    /// Counts the number of traits a character has.
    /// </summary>
    public static int CountTraits(this InvictaDatabase db, string characterId)
    {
        return db.GetTable<CharacterTrait>()
            .Count(ct => ct.Value.CharacterId.Id == characterId);
    }

    #endregion

    #region Modification Methods

    /// <summary>
    /// Adds a trait to a character.
    /// </summary>
    public static InvictaDatabase AddTrait(this InvictaDatabase db, string characterId, string traitId)
    {
        var link = new CharacterTrait(characterId, traitId);
        return db.Insert(link.CompositeKey, link);
    }

    /// <summary>
    /// Adds multiple traits to a character.
    /// </summary>
    public static InvictaDatabase AddTraits(this InvictaDatabase db, string characterId, params string[] traitIds)
    {
        foreach (var traitId in traitIds)
        {
            db = db.AddTrait(characterId, traitId);
        }
        return db;
    }

    /// <summary>
    /// Removes a trait from a character.
    /// </summary>
    public static InvictaDatabase RemoveTrait(this InvictaDatabase db, string characterId, string traitId)
    {
        var key = $"{characterId}:{traitId}";
        return db.RemoveEntry<CharacterTrait>(key);
    }

    /// <summary>
    /// Removes all traits from a character.
    /// </summary>
    public static InvictaDatabase RemoveAllTraits(this InvictaDatabase db, string characterId)
    {
        var traitKeys = db.GetTable<CharacterTrait>()
            .Where(ct => ct.Value.CharacterId.Id == characterId)
            .Select(ct => ct.Key)
            .ToList();

        foreach (var key in traitKeys)
        {
            db = db.RemoveEntry<CharacterTrait>(key);
        }
        return db;
    }

    #endregion
}
