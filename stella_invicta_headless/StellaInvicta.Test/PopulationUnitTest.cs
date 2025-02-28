using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;

namespace StellaInvicta.Test;

public class PopulationUnitTest
{
    [Fact]
    public void DefiningAPopulation()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();

        var exampleProvince = world.Entity("Example-PROVINCE")
               .Add<Province>();

        var solariLifeNeeds = world.Entity("Solari-LIFENEEDS")
            .Add<LifeNeed>()
            .Set<ShortDescription>(new("Solari consume gold as part of their diet."))
            .Set<Gold>(new(20));

        // We define a species as an entity.
        // With that we can give the species various different traits.
        var solari = world.Entity("Solari-SPECIE")
            .Add<Specie>()
            .Add<LifeNeed>(solariLifeNeeds)
            .Set<ShortDescription>(new("The solari are a specie that bathed in the sun."))
            .Set<Name>(new("Solari"));

        // Something similar goes for cultures
        // For example we can define that specifc cultures 
        // demand specific goods like a mineral used for culture 
        // purposes
        var solmantum = world.Entity("Solmantum-CULTURE")
            .Add<Culture>()
            .Set<Name>(new("solmantum"));

        var socialism = world.Entity("Socialism-IDEOLOGY")
            .Add<Ideology>()
            .Set<Name>(new("Socialism"))
            .Set<ShortDescription>(new("Advocates for social equality and the empowerment of the working class."))
            .Set<LongDescription>(new(
                """
                Socialism, as a political ideology, promises a radical restructuring of society with a focus on social equality and worker empowerment.  In contrast to the perceived inequalities of capitalism, Socialism champions the rights of the working class and advocates for a more equitable distribution of wealth and resources.

                Pops adhering to Socialist ideals will likely demand significant social reforms, such as improved working conditions, minimum wages, and social welfare programs. They may also agitate for political reforms that grant greater power to the working class and potentially challenge existing hierarchies and power structures.

                Economically, Socialist pops may favor state intervention and control over the economy, potentially leading to demands for nationalization of industries and a shift away from laissez-faire capitalism.  High militancy among Socialist pops can lead to unrest and even revolution if their demands are not met, but successfully managing them can unlock powerful social and economic reforms that reshape your nation.
                """));


        // Pops dont hold their own money they store it in a bank.
        // The bank stores the money and lends it out.
        // A bank has a global money vault, as it lends out money and pops
        // take money from the bank they believe they have everything works
        // if the bank lost too much money and cannot give pops any money anymore
        // all hell breaks lose.
        var federalSolariBank = world.Entity("Bank-Of-Solari-BANK")
            .Add<Bank>()
            .Set<Name>(new("Bank Of Solari"));

        var eternalCycle = world.Entity("Eternal-Cycle-RELIGION")
            .Add<Religion>()
            .Set<Name>(new("Eternal Cycle"))
            .Set<ShortDescription>(new("Time is cyclical. The universe is born, dies, and is reborn in an endless cycle."));

        var kryllBloodFaith = world.Entity("Kryll Blood-RELIGION")
            .Add<Religion>()
            .Set<Name>(new("Kryll Blood-Faith"))
            .Set<ShortDescription>(new("The Kryll are the chosen species, destined to rule the galaxy. Strength, conquest, and the shedding of blood are sacred acts. Their ancestors watch from the afterlife, judging their worthiness."));

        var worker = world.Entity("Worker-POPTYPE")
            .Add<PopType>()
            .Set<Name>(new("Worker"))
            .Set<ShortDescription>(new("Workers to the basic work"));


        // We group specific pops with a set of types
        var workerPops = world.Entity()
            .Add<Province>(exampleProvince)
            .Add<PopType>(worker)
            .Add<Bank>(federalSolariBank)
            .Add<Culture>(solmantum)
            .Add<Specie>(solari)
            .Add<Ideology>(socialism)
            .Add<Religion>(eternalCycle)
            // Here is workerpops expect to have around 200 credits in the bank
            .Set<Expected, Credits>(new Credits(200))
            // The question is what should 1 quantity of pop represent 10000k people?
            .Set<Quantity>(new(2000))
            .Set<Literacy>(Literacy.FromPercentage(5.5f))
            .Set<Militancy>(Militancy.FromPercentage(2.5f))
            .Set<Consciousness>(Consciousness.FromPercentage(1.5f))
            .Set<Happiness>(Happiness.FromPercentage(80));
    }

    /// <summary>
    /// Populations have different needs based on their culture and species.
    /// We need an easy way of querying those needs. The thing is that the needs
    /// calucluation is used to create buy order for the goods. Also its unclear 
    /// should we subtract the amount of needed goods each month when they are supplied
    /// and then renewed each month? and if they are met 100% last month we say that 
    /// the good was matched?
    /// </summary>
    [Fact]
    public void CorrectlyCalculateNeeds()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();

        var solariGoldLifeNeed = world.Entity("SolariGold-LIFENEED")
            .Add<LifeNeed>()
            .Set<Name>(new("Gold"))
            .Set<ShortDescription>(new("Solari consume gold as part of their diet."))
            .Set<Quantity>(new(20));

        var solariFishLifeNeed = world.Entity("SolariFish-LIFENEED")
            .Add<LifeNeed>()
            .Set<Name>(new("Fish"))
            .Set<ShortDescription>(new("Solari also quite like fish"))
            .Set<Quantity>(new(25));

        // We define a species as an entity.
        // With that we can give the species various different traits.
        var solari = world.Entity("Solari-SPECIE")
            .Add<Specie>()
            .Add<LifeNeed>(solariGoldLifeNeed)
            .Add<LifeNeed>(solariFishLifeNeed)
            .Set<ShortDescription>(new("The solari are a specie that bathed in the sun."))
            .Set<Name>(new("Solari"));

        // Something similar goes for cultures
        // For example we can define that specifc cultures 
        // demand specific goods like a mineral used for culture 
        // purposes
        var solmantum = world.Entity("Solmantum-CULTURE")
            .Add<Culture>()
            .Set<Name>(new("solmantum"));

        var sharedProsperityDoctrine = world.Entity("Shared-Prosperity-Doctrine-IDEOLOGY")
            .Add<Ideology>()
            .Set<Name>(new("Shared Prosperity Doctrine"))
            .Set<ShortDescription>(new("Advocates for social equality and the empowerment of the working class."))
            .Set<LongDescription>(new(
                """
                Shared Prosperity Doctrine, as a political ideology, promises a radical restructuring of society with a focus on social equality and worker empowerment.  In contrast to the perceived inequalities of the Golden Rule Charter, Shared Prosperity Doctrine champions the rights of the working class and advocates for a more equitable distribution of wealth and resources.

                Pops adhering to Doctrine ideals will likely demand significant social reforms, such as improved working conditions, minimum wages, and social welfare programs. They may also agitate for political reforms that grant greater power to the working class and potentially challenge existing hierarchies and power structures.

                Economically, Doctrine pops may favor state intervention and control over the economy, potentially leading to demands for nationalization of industries and a shift away from the Golden Rule Charter.  High militancy among Shared Prosperity pops can lead to unrest and even revolution if their demands are not met, but successfully managing them can unlock powerful social and economic reforms that reshape your nation.
                """));


        // Pops dont hold their own money they store it in a bank.
        // The bank stores the money and lends it out.
        // A bank has a global money vault, as it lends out money and pops
        // take money from the bank they believe they have everything works
        // if the bank lost too much money and cannot give pops any money anymore
        // all hell breaks lose.
        var federalSolariBank = world.Entity("Bank-Of-Solari-BANK")
            .Add<Bank>()
            .Set<Name>(new("Bank Of Solari"));

        /*
        Also imagine being highly indebt in the golden exchange and then destroying it!
        all your debt GONE!
        */

        var eternalCycleLuxuryFishNeed = world.Entity()
            .Add<LuxuryNeed>()
            .Set<Name>(new("Fish"))
            .Set<Quantity>(new(5));

        var eternalCycle = world.Entity("Eternal-Cycle-RELIGION")
            .Add<Religion>()
            .Add<LuxuryNeed>(eternalCycleLuxuryFishNeed)
            .Set<Name>(new("Eternal Cycle"))
            .Set<ShortDescription>(new("Time is cyclical. The universe is born, dies, and is reborn in an endless cycle."));

        var kryllBloodFaith = world.Entity("Kryll Blood-RELIGION")
            .Add<Religion>()
            .Set<Name>(new("Kryll Blood-Faith"))
            .Set<ShortDescription>(new("The Kryll are the chosen species, destined to rule the galaxy. Strength, conquest, and the shedding of blood are sacred acts. Their ancestors watch from the afterlife, judging their worthiness."));

        var workerIronLifeNeed = world.Entity()
            .Add<LifeNeed>()
            .Set<Name>(new("Iron"))
            .Set<Quantity>(new(10));

        var worker = world.Entity("Worker-POPTYPE")
            .Add<PopType>()
            .Add<LifeNeed>(workerIronLifeNeed)
            .Set<Name>(new("Worker"))
            .Set<ShortDescription>(new("Workers to the basic work"));


        var workerPops = world.Entity()
            .Add<PopType>(worker)
            .Add<Bank>(federalSolariBank)
            .Add<Culture>(solmantum)
            .Add<Specie>(solari)
            .Add<Ideology>(sharedProsperityDoctrine)
            .Add<Religion>(eternalCycle)
            .Set<Literacy>(new(0.0f))
            .Set<Militancy>(new(0.0f))
            .Set<Consciousness>(new(0.0f))
            .Set<Happiness>(new(0.0f));

        //workerPops.TotalLifeNeeds();


        var specieLifeNeedFound = false;
        var popTypeLifeNeedFound = false;
        var religionLifeNeedFound = false;

        workerPops.Target<Specie>().Each<LifeNeed>(e =>
        {
            if (e.Get<Name>().Value == "Gold")
                Assert.Equal(20, e.Get<Quantity>().Value);

            if (e.Get<Name>().Value == "Fish")
            {
                specieLifeNeedFound = true;
                Assert.Equal(25, e.Get<Quantity>().Value);
            }
        });

        workerPops.Target<PopType>().Each<LifeNeed>(e =>
        {
            if (e.Get<Name>().Value == "Iron")
            {
                Assert.Equal(10, e.Get<Quantity>().Value);
                popTypeLifeNeedFound = true;
            }
        });

        workerPops.Target<Religion>().Each<LuxuryNeed>(e =>
        {
            if (e.Get<Name>().Value == "Fish")
            {
                Assert.Equal(5, e.Get<Quantity>().Value);
                religionLifeNeedFound = true;
            }
        });

        Assert.True(specieLifeNeedFound);
        Assert.True(popTypeLifeNeedFound);
        Assert.True(religionLifeNeedFound);
    }
}
