using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;

namespace StellaInvicta.Test;

public class ReligionUnitTest
{
    [Fact]
    public void CreatingAndStoringReligions()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();


        // Problem, we create many different types of religions as entities.
        // How can we define conditions that say something like, if character has religion so and so than
        // this event happens?

        // We define a list singelton on the world that holds all religions, maybe a religion database and not just a primitive 
        // data structure like a list?

        var kryllBloodFaith = world.Entity("Kryll-Blood-Faith")
            .Add<Religion>()
            .Set<Name>(new("Kryll Blood-Faith"))
            .Set<ShortDescription>(new("The Kryll are the chosen species, destined to rule the galaxy. Strength, conquest, and the shedding of blood are sacred acts. Their ancestors watch from the afterlife, judging their worthiness."));

        var tom = world.Entity()
            .Add<Religion>(kryllBloodFaith);

        // We can just use the entity defined name, the thing is that we 
        // REALLY must document what the entity is named. This needs to be somewhat automated.
        // Maybe we print the a file? Then we need a standadized way to name such entities
        // Kyll-Blood-RELIGION instead of faith
        // 
        Assert.True(tom.Has<Religion>(world.Entity("Kryll-Blood-Faith")));
    }
}
