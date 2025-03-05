using Flecs.NET.Core;
namespace StellaInvicta.Components;



/// <summary>
/// Represents a financial loan record in the game economy system.
/// </summary>
/// <param name="LenderBank">The entity representing the bank that issued the loan.</param>
/// <param name="Borrower">The entity representing the borrower of the loan.</param>
/// <param name="Principal">The initial amount of money borrowed.</param>
/// <param name="InterestRate">The annual interest rate of the loan as a decimal value.</param>
/// <param name="Term">The total duration of the loan in turns.</param>
/// <param name="RemainingTerm">The number of turns remaining until the loan is fully paid.</param>
/// <param name="IsActive">Indicates whether the loan is currently active.</param>
/// <remarks>
/// This record structure is used to track loan information between financial entities in the game.
/// The loan terms and status are immutable once created.
/// </remarks>
public record struct Loan(Entity LenderBank, Entity Borrower, double Principal, double InterestRate, int Term, int RemainingTerm, bool IsActive);
