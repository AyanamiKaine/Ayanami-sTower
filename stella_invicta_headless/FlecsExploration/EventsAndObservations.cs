using Flecs.NET.Core;

namespace FlecsExploration;


struct Died { };
struct Dead { };

public class EventsAndObservations
{
    /// <summary>
    /// Sometimes we want to observe events, and sometimes we want to create our own events.
    /// </summary>
    [Fact]
    public void EntityEvent()
    {
        World world = World.Create();
        var enemy = world.Entity("Enemy");
        var enemyEventDiedHappended = false;

        // Observer that runs its callback when the event is emitted
        enemy.Observe<Died>((e) =>
        {
            enemyEventDiedHappended = true;
        });

        // Emitting the died event
        enemy.Emit<Died>();

        Assert.True(enemyEventDiedHappended);
    }

    /// <summary>
    /// Sometimes we want to create an world observer that observers events on ALL entities
    /// </summary>
    [Fact]
    public void WorldEventObserver()
    {
        World world = World.Create();
        var enemy = world.Entity("Enemy");

        var enemyEventDiedHappended = false;

        world.Observer()
            .With<Died>()
            .Event<Died>()
            .Each((it, i) =>
            {
                enemyEventDiedHappended = true;
            });



        // The observer query can be matched against the entity, so make sure it
        // has the Position component before emitting the event. This does not
        // trigger the observer yet.
        enemy.Add<Died>();

        // Emitting the died event
        world.Event<Died>()
            .Id<Died>()
            .Entity(enemy)
            .Emit();

        Assert.True(enemyEventDiedHappended);
    }

    /* <summary>
    /// We can also create a really simple reactive system using tags and systems.
    /// 
    /// Why should this be done?
        ;;Good Use Cases for Observers;;

        Good use cases for observers are scenarios where you need to respond to a structural change in the ECS, like a component that is being added or removed to an entity. Another good use case for observers is if you need to respond to changes in a component that is always assigned through a set operation. A typical example is a Window component, where you can resize a window by setting the component.

        Another good application for observers is when you have events that are infrequent (like a window resize) and the builtin observer API provides everything that's needed.

        ;;Bad Use Cases for Observers;;

        If you find yourself adding or removing components just to trigger observer events, that's a bad application for observers. Not only would that be an expensive solution for a simple problem, it would also be unreliable because features like command batching impact how and when events are emitted.

        Another rule of thumb is that if you can solve something with a system, it should probably be solved with a system. Running something every frame may sound expensive when compared to reacting to aperiodic events, but systems are much more efficient to run, and have more predictable performance. You can also use marker tags in combination with a not operator to prevent a system from running repeatedly for the same entity.
    /// </summary> */
    [Fact]
    public void AlternativeWayOfHandelingEvents()
    {
        World world = World.Create();
        var enemy = world.Entity("Enemy")
            .Add<Died>();

        var enemyEventDiedHappended = false;

        world.System("DiedEventHandler")
            .With<Died>()
            .Each((e) =>
            {
                enemyEventDiedHappended = true;
                e.Remove<Died>();
                e.Add<Dead>();
            });

        world.Progress();

        Assert.True(enemyEventDiedHappended);
    }


}