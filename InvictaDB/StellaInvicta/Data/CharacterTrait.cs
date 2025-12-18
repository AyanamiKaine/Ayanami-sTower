using InvictaDB;

namespace StellaInvicta.Data;

/// <summary>
/// Represents a link between a character and a trait.
/// This is a join table record for the many-to-many relationship.
/// </summary>
/// <param name="CharacterId">Reference to the character.</param>
/// <param name="TraitId">Reference to the trait.</param>
public record CharacterTrait(
    Ref<Character> CharacterId,
    Ref<Trait> TraitId
)
{
    /// <summary>
    /// Creates a composite key for this character-trait link.
    /// Use this as the ID when inserting into the database.
    /// </summary>
    public string CompositeKey => $"{CharacterId.Id}:{TraitId.Id}";
}
