using Flecs.NET.Core;

namespace FlecsExploration;


///
public class Relationships
{
    /// <summary>
    /// Here we want to enforce that 
    /// Likes is only used as a indication 
    /// of a Relationship
    /// 
    /// world.Component<Likes>().Entity.Add(Ecs.Relationship);
    /// 
    /// We do this to enforce this constraint
    /// </summary>
    public struct Likes { }
    public struct Apples { }

    [Fact]
    public void BasicRelationship()
    {
        World world = World.Create();
        world.Component<Likes>().Entity.Add(Ecs.Relationship);

        Entity entity = world.Entity()
            //.Add<Likes>()          // Panic, 'Likes' is not used as relationship
            //.Add<Apples, Likes>()  // Panic, 'Likes' is not used as relationship
            .Add<Likes, Apples>();   // OK
    }


    /// <summary>
    /// Sometimes we want to enforce that a component
    /// can only be used as a target in a relationship
    /// </summary>
    [Fact]
    public void TargetForARelationship()
    {
        World world = World.Create();

        world.Component<Apples>().Entity.Add(Ecs.Target);

        Entity entity = world.Entity()
            //.Add<Apples>()         // Panic, 'Apples' is not used as target
            //.Add<Apples, Likes>()  // Panic, 'Apples' is not used as target
            .Add<Likes, Apples>();   // OK
    }
}