using Flecs.NET.Core;

namespace FlecsExploration;


/// <summary>
/// Sometimes we need to have some tagged component data
/// For example so we can say <Requires, Energy>
/// Here Requires is a TAG component and Energy is a component with
/// data.
///
/// Often we use this to show relationships
/// </summary> 
public class Pair
{

    // Components
    record struct Position(float X, float Y);
    record struct Requires(float Amount);
    record struct Expires(float Timeout);

    // Tags
    struct Gigawatts;
    struct MustHave;

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
    public void CreatingPair()
    {
        World world = World.Create();
        world.Component<Likes>().Entity.Add(Ecs.Relationship);

        Entity entity = world.Entity()
            //.Add<Likes>()          // Panic, 'Likes' is not used as relationship
            //.Add<Apples, Likes>()  // Panic, 'Likes' is not used as relationship
            .Add<Likes, Apples>();   // OK
    }

    [Fact]
    public void PairWithData()
    {
        using World world = World.Create();

        // When one element of a pair is a component and the other element is a tag,
        // the pair assumes the type of the component.
        Entity e1 = world.Entity().Set<Gigawatts, Requires>(new Requires(1.21f));

        // When querying for a relationship component, add the pair type as template
        // argument to the builder:
        using Query<Requires> q = world.QueryBuilder<Requires>()
            .TermAt(0).First<Gigawatts>() // Set first part of pair for second term
            .Build();

        // When iterating, always use the pair type:
        q.Each((ref Requires rq) =>
        {
            rq.Amount += 1;
        });

        ref readonly Requires r = ref e1.GetSecond<Gigawatts, Requires>();
        Assert.Equal(1.21f, r.Amount);
    }

    [Fact]
    public void TagPairWithData()
    {
        // I want to understand what happens when we define a 
        // tag and add an entity to it.
        // The documentation says we would add a pair?
        // But how can we now get the entity from the pair?

        using World world = World.Create();

        var moon = world.Entity("Moon");
        var asteroid = world.Entity("Asteroid");
        var entity = world.Entity()
            .Add<MustHave>(moon)
            .Add<MustHave>(asteroid);

        entity.Each<MustHave>((Entity e) =>
        {
            // This will be called twice, once for moon and once for asteroid
        });

        var shouldBeMoonEntity = entity.Target<MustHave>();
        var shouldBeAsteroidEntity = entity.Target<MustHave>(1);

        Assert.Equal(moon, shouldBeMoonEntity);
        Assert.Equal(asteroid, shouldBeAsteroidEntity);
    }
}