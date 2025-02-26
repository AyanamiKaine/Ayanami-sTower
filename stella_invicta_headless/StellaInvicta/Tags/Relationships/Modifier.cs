namespace StellaInvicta.Tags.Relationships;

/// <summary>
/// Modifiers are used in pairs to show modifications to different 
/// values
/// 
/// Example:
///   - DamageModifier
///   - DefenseModifier
///   - HealthModifier
///   
/// Entity entity = world.Entity("Entity")
///    .Add(Modifier, Health)(new(5))
///    
/// Here we show a health modifier of 5
/// How you would use this data in a system is up to you.
/// </summary>
public struct Modifier;