namespace StellaInvicta.Components;

/// <summary>
/// Determines how greedy the decision of the ai are, related to wealth and power
/// </summary>
/// <param name="Value"></param>
public record struct Greed(double Value = 0);