namespace StellaInvicta.Components;
/// <summary>
/// Used to determine how much money a
/// bank can lend.
/// e.g. 0.1 means bank can lend 90% of credits
/// </summary>
public record struct ReserveRatio(double Amount);