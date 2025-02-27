namespace StellaInvicta.Components;



/// <summary>
/// Represents pop dissatisfaction and anger.
///
/// Driven by Unmet Needs: Lack of goods, high taxes, unemployment, war exhaustion, and negative events increase militancy.
/// Leads to Unrest and Rebellions: High militancy can lead to protests, strikes, and eventually, armed rebellions.
/// </summary>
/// <param name="Value">The numerical value representing the militancy level. Defaults to 0.</param>
public record struct Militancy(float Value = 0);
