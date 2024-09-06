using Flecs.NET.Core;

namespace StellaInvicta.Module
{
    public struct Events
    {

        /// <summary>
        /// Adds an event entity to the world that will run the <c>ev</c> event if the happins in runs out
        /// </summary>
        /// <remarks>
        /// We define a <c>MeantTimeToHappen</c> on this bases a happins
        /// in counter will be calculated and defined for the event.
        ///
        /// Everytime the game progresses by one day this counter decreasses by 1
        ///
        /// If the counter is 0 or lower the event defined in ev will run.
        ///
        /// <para>
        ///
        /// Every associated event function will be stored in a global world
        /// singleton component called <c>EventDatabase</c>
        ///
        /// </para>
        /// <para>
        /// The same event can run again after the cooldown expired
        ///
        /// </para>
        /// <para>
        ///
        /// Events are added to the <c>EventDatabase</c> if they are currently existing in
        /// the game world. If they are firing (executing its eventFunction) then
        /// they will exist so long the cooldown timer counts down each day.
        /// When it reaches zero the event will be removed and could be spawned again
        /// with the same id.
        ///
        /// </para>
        /// </remarks>
        /// <param name="world"></param>
        /// <param name="cooldown">Represent how many days later the same event could be spawned again for the same entity</param>
        /// <param name="eventID">The <c>eventID</c> represent what specifc event is currently spawned, if the <c>eventID</c> is found in the <c>EventDatabase</c> the event will not spawn</param>
        /// <param name="meantTimeToHappen">The average time until an event spawns, may spawn -50% to +50 later or sooner
        /// </param>
        /// <param name="ev"></param>
        public static void AddEvent(
            World world,
            int cooldown,
            string eventID,
            int meantTimeToHappen,
            EventDatabase.GameEvent ev
        )
        {
            ref readonly EventDatabase eventDatabase = ref world.Get<EventDatabase>();

            // Here we check if the event is already existing.
            // No need to spam someone with events.
            if (eventDatabase.EventWithIDExists(eventID))
            {
                return;
            }

            ref readonly Date gameDate = ref world.Get<Date>();

            Entity eventEntity = world
                .Entity(eventID)
                .Add<Event>()
                .Set<Cooldown>(new(cooldown))
                .Set<Created, Date>(
                    new Date(day: gameDate.Day, month: gameDate.Month, year: gameDate.Year)
                )
                .Set<MeanTimetoHappen>(new(meantTimeToHappen));

            try
            {
                eventDatabase.Add(eventEntity.ToString(), ev);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("Event Collision Happend!");
                Console.WriteLine(
                    $"An event associated with the ID:{eventEntity.Id} was already defined in the database.\nNo event was spawned as one already exists with the same id(implying it is already running) the eventEntity is now destroyed: {ev}\n The following exception was thrown: '{ex.Message}'"
                );
                eventEntity.Destruct();
            }
        }

        public static EventDatabase GetEventDatabase(World world)
        {
            if (world.Has<EventDatabase>())
            {
                ref readonly EventDatabase eventDatabase = ref world.Get<EventDatabase>();
                return eventDatabase;
            }
            else
            {
                throw new ArgumentException(
                    "Event Database singleton was not found in the ECS World. \nIf you load the Events module it should be added automatically."
                );
            }
        }
    }
}
