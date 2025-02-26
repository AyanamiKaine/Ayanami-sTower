namespace StellaInvicta.Tags.Relationships;

/// <summary>
/// An celstial body may orbit only one other celestial body.
/// 
/// Orbits is defined as an exclusive component to indicate that a celestial body orbits another.
/// 
/// Example:
/// Entity Earth = world.Entity("Earth");
/// Entity Sun = world.Entity("Sun");
/// 
/// Entity Comet = world.Entity("Comet");
/// 
/// Earth.Add`Orbits`(Sun);
/// 
/// Now Earth orbits the Sun.
/// 
/// If we change the relationship to:
/// Earth.Add`Orbits`(Comet);
/// 
/// Now earth does NOT orbit the sun anymore but the comet.
/// </summary>
public struct Orbits;