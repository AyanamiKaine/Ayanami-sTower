namespace StellaInvicta.Components;

/// <summary>
///  Healthy characters are less likely to fall prey to, and more likely to fully recover from, disease. They are also less likely to die from old age per month than ill characters.
///  High health is key to a long life.
/// </summary>
/// <param name="Value"></param>
public record struct Health(double Value = 0);