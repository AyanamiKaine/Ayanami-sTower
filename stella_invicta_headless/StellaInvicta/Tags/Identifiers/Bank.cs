namespace StellaInvicta.Tags.Identifiers;
/// <summary>
/// Marks an entity to be a bank. 
/// <remarks>
/// A bank acts as holder and lender of money. Companies, pops, character store their money in banks.
/// If they need money the bank gives it to them. Each company and pops knows their amount of money
/// they should have. While the bank only knows its amount of money they have and how much they lended to others
/// and to whom they lended. The bank does not know which dollar belongs to which company, pop or character.
/// </remarks>
/// </summary>
public struct Bank;

/*
Why doesnt the bank know which dollars belongs to whom? 

Because in our abstraction of the banking and finacial system. Their is no fraud.
If someone goes to the bank requesting money we always believe that person to have
a right to request said money.

The problem I see is what if I write a bug that results in pops requesting money
they should not have, how would I find that out?
*/