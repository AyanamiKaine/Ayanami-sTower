namespace StellaInvicta.Tags.Relationships;

/// <summary>
/// Marks an entity as having a neutral diplomatic relationship with the player's faction.
/// </summary>
/// <remarks>
/// Entities with this tag have neither hostile nor allied status in the diplomatic system.
/// They may engage in trade or other non-military interactions with the player.
/// Generally mutually exclusive with <see cref="Enemy"/> and <see cref="Ally"/> tags.
/// </remarks>
public struct Neutral;