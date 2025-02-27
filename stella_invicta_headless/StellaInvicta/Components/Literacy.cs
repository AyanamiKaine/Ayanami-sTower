namespace StellaInvicta.Components;


/// <summary>
/// Represents the percentage of the pop that can read and write.
/// </summary>
/// <remarks>
/// Crucial for Research: Higher literacy directly translates to more research points, driving technological advancements.
/// Promotion and Efficiency: Literacy increases the efficiency of many professions and makes pops more likely to promote to higher-strata professions.
/// </remarks>
public record struct Literacy(float Value = 0);
