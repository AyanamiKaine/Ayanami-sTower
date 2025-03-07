using System.Numerics;
using Flecs.NET.Core;
using InformationHidingAndECS.Components;

namespace InformationHidingAndECS;

public class InformationHidingTests
{
    [Fact]
    public void SimpleGameExample()
    {
        var gameManager = new GameManager();
        var player = new Player()
        {
            PlayerName = "Steve"
        };

        gameManager.AddEntity(player);
        gameManager.ProcessTicks();
    }

    [Fact]
    public void SimpleECSExample()
    {
        World world = World.Create();
        var player = world.Entity("Steve")
            .Add<Player>()
            .Set<UserInput>(new())
            // Sometimes we want to use a data structure multiple times like a Vector3 for position and velocity
            .Set<Position3D, Vector3>(Vector3.Zero)
            .Set<Velocity3D, Vector3>(Vector3.Zero)
            .Set<AttackPower>(new(1.0))
            .Set<JumpStrength>(new(1.0f));

        // Behavior is attached to a combination of components
        world.System<UserInput, Vector3>("playerTicks")
            .With<Player>()
            .TermAt(1).First<Position3D>().Second<Vector3>()
            .Each((Entity player, ref UserInput input, ref Vector3 pos3D) =>
            {
                // Change position based on user input;
            });

        // The the entity does not have the component, it gets added and a mut ref gets returned.
        player.Ensure<Health>().Amount = 2;

        // If the entity already has the component, it gets simply returned as a mut ref.
        Assert.Equal(2, player.Ensure<Health>().Amount);

        /*
        The interesting thing is that we can add or remove components from an entity.
        This is really intresting because we can add data or remove it based on context.
        Or even ignore it.

        Even though we could give our player entity over 1000 components, our systems that just use 3
        do not know any more and are not exposed to the system. 
        */

        // The conclusion is that everywhere you could use Flecs ECS to structure a highly modular and performant system.
    }
}
