using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;

namespace StellaInvicta.Test;


public class BankUnitTest
{

    private static bool CreateLoan(World world, Entity bank, Entity borrower, double amount, int term)
    {
        // Check if bank has enough credits
        var bankCredits = bank.Get<Credits>();
        var reserveRatio = bank.Get<ReserveRatio>();

        // Calculate how much bank can lend
        double maxLendable = bankCredits.Ammount * (1 - reserveRatio.Amount);

        if (amount > maxLendable)
            return false; // Not enough lendable funds

        // Create the loan
        double rate = bank.Get<LendingInterestRate>().Amount;
        world.Entity()
            .Set(new Loan(bank, borrower, amount, rate, term, term, true));

        // Transfer funds
        bankCredits.Ammount -= amount;
        bank.Set(bankCredits);

        var borrowerCredits = borrower.Get<Credits>();
        borrowerCredits.Ammount += amount;
        borrower.Set(borrowerCredits);

        return true;
    }

    [Fact]
    public void BanksDepositeInterestRate()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        var bank = world.Entity("bank")
            .Add<Bank>()
            .Set<Monthly, DepositInterestRate>(new DepositInterestRate(0.10))
            .Set<Credits>(new(1));

        world.System<DepositInterestRate, Credits>()
            .With<Bank>()
            .TermAt(0).First<Monthly>().Second<DepositInterestRate>()
            .Each((Entity e, ref DepositInterestRate rate, ref Credits credits) =>
            {
                double interest = credits.Ammount * rate.Amount;
                credits.Ammount += interest;
            });

        world.Progress();

        var expectedCredits = 1.10;

        Assert.Equal(expectedCredits, bank.Get<Credits>().Ammount);
    }

    /// <summary>
    /// Banks should be able to give out a certain amount
    /// of money to other entities that need it. This
    /// creates a loan system.
    /// </summary>
    [Fact]
    public void BanksLoansInterestRate()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        var bank = world.Entity("bank")
            .Add<Bank>()
            .Set<Monthly, DepositInterestRate>(new DepositInterestRate(0.10))
            .Set<LendingInterestRate>(new(0.1))
            .Set<Credits>(new(40000));

        var company = world.Entity("company")
            .Add<Company>()
            .Add<Bank>(bank)
            .Set<ReserveRatio>(new(0))
            .Set<Expected, Credits>(new Credits(0));

        CreateLoan(world, bank, company, 200, 1);

        world.System<Loan>()
            .Each((Entity e, ref Loan loan) =>
            {
                if (!loan.IsActive) return;

                // Get borrower's credits
                var borrower = loan.Borrower;
                if (borrower.Has<Credits>())
                {
                    // Calculate monthly interest
                    double interestThisMonth = loan.Principal * loan.InterestRate / 12;

                    // Subtract from borrower
                    var borrowerCredits = borrower.Get<Credits>();
                    borrowerCredits.Ammount -= interestThisMonth;
                    borrower.Set(borrowerCredits);

                    // Add to lender
                    var lender = loan.LenderBank;
                    var lenderCredits = lender.Get<Credits>();
                    lenderCredits.Ammount += interestThisMonth;
                    lender.Set(lenderCredits);

                    // Update loan term
                    loan.RemainingTerm--;
                    if (loan.RemainingTerm <= 0)
                        loan.IsActive = false;
                }
            });
        world.Progress();


        var expectedCredits = 200.0;
        var actualCredits = company.GetSecond<Expected, Credits>().Ammount;

        Assert.Equal(expectedCredits, actualCredits);
    }
}
