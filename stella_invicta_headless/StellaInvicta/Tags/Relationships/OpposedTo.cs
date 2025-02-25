namespace StellaInvicta.Tags.Relationships;

/// <summary>
/// Used usually to show that two entities are opposed to each other.
/// EXAMPLE:
/// This is used for traits to show an opposite trait, so we can say if opposed trait -opinion or cant
/// have this and the opposed trait
/// </summary>
/// <remarks>
/// This relationship is <c>Ecs.Symmetric</c>. If A is opposed to B, B is also opposed to A
/// </remarks>
public struct OpposedTo;