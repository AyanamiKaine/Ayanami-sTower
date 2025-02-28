namespace StellaInvicta.Tags.Identifiers;
/// <summary>
/// Marks an entity to be a bank. 
/// <remarks>
/// A bank acts as holder and lender of money. Companies, pops, character store their money in banks.
/// If they need money the bank give it to them. Each company and pops knows their amount of money
/// they should have. While the bank only knows its amount of money they have and how much they lended to others
/// and to whom they lended. The bank does not know which dollar belongs to which company, pop or character.
/// </remarks>
/// </summary>
public struct Bank;