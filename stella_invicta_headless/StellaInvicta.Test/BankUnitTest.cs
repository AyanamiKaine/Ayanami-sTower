using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;

namespace StellaInvicta.Test;


// TODO: Think about implementing Account System
/*
I feel it in my finger tips to also implement an account system, this would add some
complexity, the question is if the trade-off is worth?
*/

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

        var borrowerCredits = borrower.GetSecond<Expected, Credits>();
        borrowerCredits.Ammount += amount;
        borrower.Set<Expected, Credits>(borrowerCredits);


        /*
        If the borrowers gets the loan from his bank, we dont need to transver the money to the banks account,
        because the bank already has that money, otherwise the bank will believe it just got money to lend out
        otherwise we send it to the new bank.
        */
        var borrowersBank = borrower.Target<Bank>();
        if (borrowersBank == bank)
        {
            return true;
        }
        else
        {
            var borrowersBankCredits = borrowersBank.Get<Credits>();
            borrowersBankCredits.Ammount += amount;
            borrowersBank.Set(borrowersBankCredits);
        }


        return true;
    }

    [Fact]
    public void BanksDepositeInterestRate()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        var bank = world.Entity("bank")
            .Add<Bank>()
            .Set<DepositInterestRate>(new(0.10))
            .Set<Credits>(new(1));

        world.System<DepositInterestRate, Credits>()
            .With<Bank>()
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
    /// creates a loan system. Bank generate profit my 
    /// getting the interest back from the bank.
    /// </summary>
    [Fact]
    public void BanksLoansInterestRate()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        var bank = world.Entity("bank")
            .Add<Bank>()
            .Set<DepositInterestRate>(new(0.10))
            .Set<ReserveRatio>(new(0))
            .Set<LendingInterestRate>(new(0.1))
            .Set<Credits>(new(40000));

        var company = world.Entity("company")
            .Add<Company>()
            .Add<Bank>(bank)
            .Set<Expected, Credits>(new Credits(0));

        CreateLoan(world, bank, company, 200, 1);

        // TODO: we need two systems
        // one paying interest on the loan
        // one paying back the loan.
        world.System<Loan>("Paying Interest")
            .Each((Entity e, ref Loan loan) =>
            {
                if (!loan.IsActive) return;

                // Get borrower's credits
                var borrower = loan.Borrower;
                if (borrower.Has<Expected, Credits>())
                {
                    // Calculate monthly interest
                    double interestThisMonth = loan.Principal * loan.InterestRate / 12;

                    // Subtract from borrower
                    var borrowerCredits = borrower.GetSecond<Expected, Credits>();
                    borrowerCredits.Ammount -= interestThisMonth;
                    borrowerCredits.Ammount = Math.Round(borrowerCredits.Ammount, 3);
                    borrower.Set<Expected, Credits>(borrowerCredits);

                    // Add to lender
                    var lender = loan.LenderBank;
                    var lenderCredits = lender.Get<Credits>();
                    lenderCredits.Ammount += interestThisMonth;
                    lenderCredits.Ammount = Math.Round(lenderCredits.Ammount, 3);
                    lender.Set(lenderCredits);
                }
            });
        world.Progress();


        var expectedCredits = 198.333;
        var actualCredits = company.GetSecond<Expected, Credits>().Ammount;

        Assert.Equal(expectedCredits, actualCredits);
    }

    /// <summary>
    /// Banks should be able to give out a certain amount
    /// of money to other entities that need it. This
    /// creates a loan system.
    /// </summary>
    [Fact]
    public void BanksCanLendOutMoney()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        var bank = world.Entity("bank")
            .Add<Bank>()
            .Set<Monthly, DepositInterestRate>(new DepositInterestRate(0.10))
            .Set<ReserveRatio>(new(0))
            .Set<LendingInterestRate>(new(0.1))
            .Set<Credits>(new(40000));

        var company = world.Entity("company")
            .Add<Company>()
            .Add<Bank>(bank)
            .Set<Expected, Credits>(new Credits(0));

        CreateLoan(world, bank, company, 200, 1);

        var expectedCredits = 200.0;
        var actualCredits = company.GetSecond<Expected, Credits>().Ammount;

        Assert.Equal(expectedCredits, actualCredits);
    }
}
