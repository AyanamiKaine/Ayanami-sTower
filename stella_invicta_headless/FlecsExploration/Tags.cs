using Flecs.NET.Core;

namespace FlecsExploration;



/// <summary>
/// Tags in Flecs are components without data.
/// </summary>
file struct PlayerTag { }

/// <summary>
/// Sometimes we want to give entites tags like "Player", "Ally", "Enemy".
/// </summary>
public class Tags
{


    [Fact]
    public void CreatingTags()
    {
        World world = World.Create();
        world.Component<PlayerTag>();

        var entity = world.Entity("Player")
                    .Add<PlayerTag>();

        var hasEntityPlayerTag = entity.Has<PlayerTag>();
        Assert.True(hasEntityPlayerTag);
    }

    /// <summary>
    /// Creating a system that works only on entities with certain tags.
    /// </summary>
    [Fact]
    public void SystemBasedOnTags()
    {
        World world = World.Create();
        world.Component<PlayerTag>("Player");

        var entity = world.Entity("Player")
                    .Add<PlayerTag>();

        /*
        To say "run this system only if they have the tag PlayerTag" we use the With method and define 
        the component we want to account for.

        This also works for saying "entity should have this component attach but we dont need a refrence to it"
        */
        world.System("Player System")
            .With<PlayerTag>()
            .Each((Entity e) =>
            {
                Assert.Equal(entity, e);
            });

        world.Progress();
    }
}