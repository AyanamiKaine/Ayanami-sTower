namespace StellaInvicta.Tags.Relationships;

/// <summary>
/// Symmetric relationship component that defines a connection between two entities.
/// Can be used to indicate that a star system as a connection to another.
/// This is helpful to dynamically define and remove connections between star systems.
/// Example:
/// Entity SolarSystem = world.Entity("Solar System");
/// Entity AlphaCentauri = world.Entity("Alpha Centauri");
/// SolarSystem.Add `ConnectedTo` (AlphaCentauri);
/// Now both entities have a connection to each other.
/// </summary>
public struct ConnectedTo;