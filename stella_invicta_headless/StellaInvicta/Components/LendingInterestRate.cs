namespace StellaInvicta.Components;
/// <summary>
/// Current interest rate for loans.
/// If a new loan is made it should use this
/// inital interest rate, if this rate changes
/// the interest rate of existing loans should
/// NOT change.
/// </summary>
public record struct LendingInterestRate(double Amount);