using Flecs.NET.Core;

namespace FlecsExploration;


struct Died { };

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


}