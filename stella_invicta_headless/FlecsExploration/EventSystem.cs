using Flecs.NET.Core;

namespace FlecsExploration;


/// <summary>
/// How can we implement an event system in Flecs.NET?
/// 
/// Here we explore ways to implement an event system in Flecs.NET.
/// </summary>



/*
This event system is inspired by the event system in Paradox Games.

I commented much on the functinality of the event system in the code.
But also provide examples down below on how to use the event system.
*/

public class EventSystem
{
    public record struct MeanTimetoHappen(int Days = 0);
    public record struct HappensIn(int Days = 0);

    public record struct Cooldown(int Value = 0);
    public struct Event;
    public struct Created;
    public struct Happened;
    public struct Happening;



    public record struct Date
    {
        private DateTime _dateTime;

        /// <summary>
        /// Initializes a new instance of the Date struct.
        /// </summary>
        /// <param name="day">The day of the month (1-31).</param>
        /// <param name="month">The month of the year (1-12).</param>
        /// <param name="year">The year.</param>
        public Date(int day = 1, int month = 1, int year = 0)
        {
            _dateTime = new DateTime(year, month, day);
            Day = _dateTime.Day;
            Month = _dateTime.Month;
            Year = _dateTime.Year;
        }

        // Properties to access individual date components
        public int Day;
        public int Month;
        public int Year;

        public void AddDays(int days)
        {
            _dateTime = _dateTime.AddDays(days);
            Day = _dateTime.Day;
            Month = _dateTime.Month;
            Year = _dateTime.Year;
        }

        public void SubDays(int days)
        {
            _dateTime = _dateTime.AddDays(-days); // Use a negative value to subtract days
            Day = _dateTime.Day;
            Month = _dateTime.Month;
            Year = _dateTime.Year;
        }

        /// <summary>
        /// Checks if this date represents a birthday (specific day and month).
        /// </summary>
        /// <param name="day">The day of the birthday.</param>
        /// <param name="month">The month of the birthday.</param>
        /// <returns>True if it's the birthday, false otherwise.</returns>
        public readonly bool HasBirthday(int day, int month)
        {
            return _dateTime.Day == day && _dateTime.Month == month;
        }
        /// <summary>
        /// Checks if this date is before another date, including the year.
        /// </summary>
        public readonly bool IsBefore(Date other)
        {
            // Compare years first
            if (Year < other.Year)
                return true;
            if (Year > other.Year)
                return false;

            // If years are equal, compare months
            if (Month < other.Month)
                return true;
            if (Month > other.Month)
                return false;

            // If months are also equal, compare days
            return Day < other.Day;
        }

        /// <summary>
        /// Checks if this date is after another date, including the year.
        /// </summary>
        public readonly bool IsAfter(Date other)
        {
            // Compare years first
            if (Year > other.Year)
                return true;
            if (Year < other.Year)
                return false;

            // If years are equal, compare months
            if (Month > other.Month)
                return true;
            if (Month < other.Month)
                return false;

            // If months are also equal, compare days
            return Day > other.Day;
        }


        /// <summary>
        /// Stores functions associated with an EventID(String), those events are expected
        /// to be able to use the <c>world</c> where the event is fired.
        /// </summary>
        public readonly record struct EventDatabase()
        {
            // TODO: We need to implement a way to associate entities
            // With the event so we can mutate the entities.
            // We probably want to say something like CharacterEvent
            // ProvinceEvent, NationEvent, etc. So we than can get
            // the CharacterID, ProvinceID etc.

            /// <summary>
            /// A game event is a function defintion to be used for
            /// events that need to query things from the gameworld
            /// and effect the gameworld
            /// </summary>
            /// <param name="world"></param>
            public delegate void GameEvent(World world);
            private readonly Dictionary<string, GameEvent> _database = [];
            public readonly int Count => _database.Count;

            public readonly void Add(string ID, GameEvent ev)
            {
                _database.Add(ID, ev);
            }

            /// <summary>
            /// Checks if an event is already in the database
            /// Implying it is currently running.
            /// </summary>
            /// <param name="ID"></param>
            /// <returns></returns>
            public readonly bool EventWithIDExists(string ID)
            {
                return _database.ContainsKey(ID);
            }

            public readonly void RemoveByID(string ID)
            {
                _database.Remove(ID);
            }

            public readonly void ReplaceGameEvent(string ID, GameEvent ev)
            {
                try
                {
                    _database[ID] = ev;
                }
                catch (KeyNotFoundException ex)
                {
                    Console.WriteLine(
                        $"No event with the ID of:{ID} found in the Event Database\nAn Empty event is now created for the ID\n, exception thrown. {ex.Message}"
                    );
                    ReplaceGameEvent(ID, (world) => { });
                }
            }

            /// <summary>
            /// Tries to run an <c>GameEvent</c> that is associated with and <c>Entity</c>
            /// </summary>
            /// <remarks>
            /// If the <c>ID</c> is not found an Entry in the datbase gets created with the <c>ID</c> and
            /// an empty event (a function that does nothing) gets associated with the <c>ID</c>.
            /// </remarks>
            /// <param name="ID"></param>
            /// <param name="world"></param>
            public readonly void Run(string ID, World world)
            {
                try
                {
                    _database[ID](world);
                }
                catch (KeyNotFoundException ex)
                {
                    Console.WriteLine(
                        $"No event with the ID of:{ID} found in the Event Database\nAn Empty event is now created for the ID\n, exception thrown. {ex.Message}"
                    );
                    ReplaceGameEvent(ID, (world) => { });
                }
            }

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

        [Fact]
        public void CreatingEventSystem()
        {
            World world = World.Create();
            world.Set<Date>(new(1, 1, 1444));
            world.Set<EventDatabase>(new());

            var eventDatabase = EventDatabase.GetEventDatabase(world);


            world.System<Date>("Progess GameDate")
                           .Kind(Ecs.PreUpdate)
                           .TermAt(0).Singleton()
                           .Each((ref Date gameDate) =>
                           {
                               gameDate.AddDays(1);
                           });


            world.System<MeanTimetoHappen>("Calculate happens in")
                .MultiThreaded()
                .With<Event>()
                .Without<HappensIn>()
                .Each(
                (Entity e, ref MeanTimetoHappen mtth) =>
                {
                    Random random = new();
                    double randomNumber = random.NextDouble() + 0.5;
                    int happensIn = (int)Math.Round(mtth.Days * randomNumber);
                    e.Set<HappensIn>(new(happensIn));
                }
            );

            world.System<MeanTimetoHappen, HappensIn>("Reduce happens in By 1")
                .MultiThreaded()
                .With<Event>()
                .Without<Happened>()
                .Each(
                    (ref MeanTimetoHappen mtth, ref HappensIn happensIn) =>
                    {
                        if (happensIn.Days != 0)
                        {
                            happensIn.Days -= 1;
                        }
                    }
                );

            world.System<EventDatabase, HappensIn>("Fire Event")
                .MultiThreaded()
                .TermAt(0)
                .Singleton()
                .With<Event>()
                .Without<Happening>()
                .Without<Happened>()
                .Each(
                    (Entity e, ref EventDatabase eventDatabase, ref HappensIn happensIn) =>
                    {
                        if (happensIn.Days <= 0)
                        {
                            e.Add<Happening>();
                            e.Remove<HappensIn>();
                            eventDatabase.Run(e.ToString(), world);
                        }
                    }
                );

            world.System("Event is happening")
                .MultiThreaded()
                .With<Event>()
                .With<Happening>()
                .Each(
                    (Entity e) =>
                    {
                        e.Remove<Happening>();
                        e.Add<Happened>();
                    }
                );


            EventDatabase.AddEvent(world, 10, "TestEvent2", 10, (world) =>
            {
                Console.WriteLine("TestEvent2 was fired!");
            });

        }

        [Fact]
        public void CreatingEventComponent()
        {
            /*
            One way to implement an event system in Flecs.NET is to create an event component.
            */
        }
    }
}