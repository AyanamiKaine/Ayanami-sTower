using Flecs.NET.Core;

namespace FlecsExploration;


/// <summary>
/// How can we implement singletons in Flecs.NET?
/// </summary>

public record struct Gravity(double Value);
public record struct Position2D(double X, double Y);
public class Singletons
{
    [Fact]
    public void CreatingSingleton()
    {

        World world = World.Create();
        /*
        Singletons in Flecs are just components attached to the root ecs world.
        */
        world.Set<Gravity>(new(9.81));

        // Get singleton component
        ref readonly Gravity g = ref world.Get<Gravity>();

        Assert.Equal(9.81, g.Value);
    }


    /// <summary>
    /// How can we use a defined singleton in a system?
    /// </summary>
    [Fact]
    public void UsingSingeltonInASystem()
    {
        World world = World.Create();
        /*
        Singletons in Flecs are components attached to the root ecs world.
        */
        world.Set<Gravity>(new(9.81));

        var entity = world.Entity()
            .Set<Position2D>(new(0, 50));


        /*
        Here we are using the gravity component 
        attached to the game world in a system.
        */
        var system = world.System<Gravity, Position2D>()
            .TermAt(0).Singleton()
            .Each((ref Gravity gravity, ref Position2D position2D) =>
            {
                position2D.Y -= gravity.Value;
            });

        world.Progress();

        Assert.Equal(50 - 9.81, entity.Get<Position2D>().Y);
    }
}