using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;

namespace StellaInvicta.Test;


public class BankUnitTest
{
    [Fact]
    public void BanksInterestRate()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        var bank = world.Entity("bank")
            .Add<Bank>()
            .Set<Monthly, InterestRate>(new InterestRate(0.10))
            .Set<Credits>(new(1));

        world.System<InterestRate, Credits>()
            .With<Bank>()
            .TermAt(0).First<Monthly>().Second<InterestRate>()
            .Each((Entity e, ref InterestRate rate, ref Credits credits) =>
            {
                double interest = credits.Ammount * rate.Amount;
                credits.Ammount += interest;
            });

        world.Progress();

        var expectedCredits = 1.10;

        Assert.Equal(expectedCredits, bank.Get<Credits>().Ammount);
    }
}
