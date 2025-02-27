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

        // We define a species as an entity.
        // With that we can give the species various different traits.
        var solari = world.Entity("Solari-SPECIE")
            .Add<Specie>()
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

        var workerPop = world.Entity()
            .Add<Worker>()
            .Add<Bank>(federalSolariBank)
            .Add<Culture>(solmantum)
            .Add<Specie>(solari)
            .Add<Ideology>(socialism)
            .Add<Religion>(eternalCycle)
            .Set<Literacy>(new(0.0f))
            .Set<Militancy>(new(0.0f))
            .Set<Consciousness>(new(0.0f));
    }
}
