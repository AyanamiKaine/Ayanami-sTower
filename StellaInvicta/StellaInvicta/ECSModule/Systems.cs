using Flecs.NET.Core;
using StellaInvicta.Initializer;
namespace StellaInvicta.Module
{
    public struct Systems : IFlecsModule
    {
        public readonly void InitModule(World world)
        {

            world.Routine<Date>("Progess GameDate")
                .Kind(Ecs.PreUpdate)
                .TermAt(0).Singleton()
                .TickSource(world.Lookup("StellaInvicta.Module.PreDefinedEntities.GameSpeed"))
                .Each((ref Date gameDate) =>
                {
                    gameDate.AddDays(1);
                });


            world.Routine<MeanTimetoHappen>("Calculate happens in")
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

            world.Routine<MeanTimetoHappen, HappensIn>("Reduce happens in By 1")
                .MultiThreaded()
                .With<Event>()
                .Without<Happened>()
                .TickSource(world.Lookup("StellaInvicta.Module.PreDefinedEntities.GameSpeed"))
                .Each(
                    (ref MeanTimetoHappen mtth, ref HappensIn happensIn) =>
                    {
                        if (happensIn.Days != 0)
                        {
                            happensIn.Days -= 1;
                        }
                    }
                );

            world.Routine<EventDatabase, HappensIn>("Fire Event")
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

            world.Routine("Event is happening")
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


            EnableDiesOfNaturalCauseEvent(world);

            SystemInitializer.Run(world);
        }

        /// <summary>
        /// Runs every world progress and checks if on the current day something has aged (Birthday)
        ///  in the game world and if so incrases its age by 1
        /// </summary>
        [ECSSystem]
        public class AgeSystem()
        {
            public static void Init(World world)
            {
                //SYSTEMS
                world.Routine<Date, Birthday, Age>("AgeIncrease")
                .MultiThreaded()
                .Kind(Ecs.OnUpdate)
                .TermAt(0).Singleton()
                .TermAt(1).Second<Date>()
                .With<Alive>()
                .Each((ref Date gameDate, ref Date birthdate, ref Age age) =>
                {
                    if (gameDate.HasBirthday(birthdate.Day, birthdate.Month))
                    {
                        age.Value += 1;
                    }
                });
            }
        }

        [ECSSystem]
        public class ReduceEventCooldown()
        {
            public static void Init(World world)
            {

                //Destroy event entity if the event has happened
                world.Routine<EventDatabase, Cooldown>("Reduce Event Cooldown")
                    .MultiThreaded()
                    .TermAt(0)
                    .Singleton()
                    .TickSource(world.Lookup("StellaInvicta.Module.PreDefinedEntities.GameSpeed"))
                    .Kind(Ecs.PostUpdate)
                    .With<Event>()
                    .With<Happened>()
                    .Each(
                        (Entity e, ref EventDatabase eventDatabase, ref Cooldown cooldown) =>
                        {
                            if (cooldown.Value >= 0)
                            {
                                cooldown.Value -= 1;
                            }
                            else
                            {
                                e.Remove<Cooldown>();
                            }
                        }
                    );
            }
        }

        [ECSSystem]
        public class CheckIfEventsCanRunAgain()
        {
            public static void Init(World world)
            {
                // If the cooldown reaches 0 the event is removed from the event database and
                // the same event can happen again.
                world.Routine<EventDatabase>(nameof(CheckIfEventsCanRunAgain))
                    .MultiThreaded()
                    .TermAt(0)
                    .Singleton()
                    .Kind(Ecs.PostUpdate)
                    .With<Event>()
                    .With<Happened>()
                    .Without<Cooldown>()
                    .Each(
                        (Entity e, ref EventDatabase eventDatabase) =>
                        {
                            e.Destruct();
                            eventDatabase.RemoveByID(e.Id.ToString());
                        }
                    );
            }
        }

        /// <summary>
        /// We reset the attributes of a character and recalucate its updated value
        /// </summary>
        /// <param name="world"></param>

        [ECSSystem]
        public class RecalculateCharacterAttributes
        {
            public static void Init(World world)
            {

                world.Routine<
                    Health, Fertility, Diplomacy, Stewardship, Martial, Intrigue, Learning, PersonalCombatSkill>(nameof(RecalculateCharacterAttributes))
                    .MultiThreaded()
                    .Kind(Ecs.PreUpdate)
                    .Each((Entity e,
                        ref Health health,
                        ref Fertility fertility,
                        ref Diplomacy diplomacy,
                        ref Stewardship stewardship,
                        ref Martial martial,
                        ref Intrigue intrigue,
                        ref Learning learning,
                        ref PersonalCombatSkill personalCombatSkill) =>
                    {
                        health.Value = 0;
                        fertility.Value = 0;
                        diplomacy.Value = 0;
                        stewardship.Value = 0;
                        martial.Value = 0;
                        intrigue.Value = 0;
                        learning.Value = 0;
                        personalCombatSkill.Value = 0;
                    });
            }
        }

        [ECSSystem]
        public class CalculateAttributesBasedOnTraits
        {
            public static void Init(World world)
            {

                world.Routine<
                    Health, Fertility, Diplomacy, Stewardship, Martial, Intrigue, Learning, PersonalCombatSkill>(nameof(CalculateAttributesBasedOnTraits))
                    .MultiThreaded()
                    .With<Trait>().Second(Ecs.Wildcard)
                    .Each((Iter it, int i,
                        ref Health health,
                        ref Fertility fertility,
                        ref Diplomacy diplomacy,
                        ref Stewardship stewardship,
                        ref Martial martial,
                        ref Intrigue intrigue,
                        ref Learning learning,
                        ref PersonalCombatSkill personalCombatSkill) =>
                        {
                            var name = it.Id(0).ToString();
                            Entity trait = it.Id(8).Second();
                            var traitName = trait.Name();

                            if (trait.Has<Modifier, Health>())
                            {
                                ref readonly var modifier = ref trait.GetSecond<Modifier, Health>();
                                health.Value += modifier.Value;
                            }
                            if (trait.Has<Modifier, Fertility>())
                            {
                                ref readonly var modifier = ref trait.GetSecond<Modifier, Fertility>();
                                fertility.Value += modifier.Value;
                            }
                            if (trait.Has<Modifier, Diplomacy>())
                            {
                                ref readonly var modifier = ref trait.GetSecond<Modifier, Diplomacy>();
                                diplomacy.Value += modifier.Value;
                            }
                            if (trait.Has<Modifier, Martial>())
                            {
                                ref readonly var modifier = ref trait.GetSecond<Modifier, Martial>();
                                martial.Value += modifier.Value;
                            }
                            if (trait.Has<Modifier, Stewardship>())
                            {
                                ref readonly var modifier = ref trait.GetSecond<Modifier, Stewardship>();
                                stewardship.Value += modifier.Value;
                            }
                            if (trait.Has<Modifier, Intrigue>())
                            {
                                ref readonly var modifier = ref trait.GetSecond<Modifier, Intrigue>();
                                intrigue.Value += modifier.Value;
                            }
                            if (trait.Has<Modifier, Learning>())
                            {
                                ref readonly var modifier = ref trait.GetSecond<Modifier, Learning>();
                                learning.Value += modifier.Value;
                            }
                            if (trait.Has<Modifier, PersonalCombatSkill>())
                            {
                                ref readonly var modifier = ref trait.GetSecond<Modifier, PersonalCombatSkill>();
                                personalCombatSkill.Value += modifier.Value;
                            }

                        });
            }
        }

        [ECSSystem]
        public class CalculateAttributesBasedOnEducation
        {
            public static void Init(World world)
            {
                world.Routine<
    Health, Fertility, Diplomacy, Stewardship, Martial, Intrigue, Learning, PersonalCombatSkill>(nameof(CalculateAttributesBasedOnEducation))
    .MultiThreaded()
    .With<Education>().Second(Ecs.Wildcard)
    .Each((Iter it, int i,
        ref Health health,
        ref Fertility fertility,
        ref Diplomacy diplomacy,
        ref Stewardship stewardship,
        ref Martial martial,
        ref Intrigue intrigue,
        ref Learning learning,
        ref PersonalCombatSkill personalCombatSkill) =>
        {
            var name = it.Id(0).ToString();
            Entity education = it.Id(8).Second(); // The id number is the last argument number if we add arguments we must incrase this number by one

            if (education.Has<Modifier, Health>())
            {
                ref readonly var modifier = ref education.GetSecond<Modifier, Health>();
                health.Value += modifier.Value;
            }
            if (education.Has<Modifier, Fertility>())
            {
                ref readonly var modifier = ref education.GetSecond<Modifier, Fertility>();
                fertility.Value += modifier.Value;
            }
            if (education.Has<Modifier, Diplomacy>())
            {
                ref readonly var modifier = ref education.GetSecond<Modifier, Diplomacy>();
                diplomacy.Value += modifier.Value;
            }
            if (education.Has<Modifier, Martial>())
            {
                ref readonly var modifier = ref education.GetSecond<Modifier, Martial>();
                martial.Value += modifier.Value;
            }
            if (education.Has<Modifier, Stewardship>())
            {
                ref readonly var modifier = ref education.GetSecond<Modifier, Stewardship>();
                stewardship.Value += modifier.Value;
            }
            if (education.Has<Modifier, Intrigue>())
            {
                ref readonly var modifier = ref education.GetSecond<Modifier, Intrigue>();
                intrigue.Value += modifier.Value;
            }
            if (education.Has<Modifier, Learning>())
            {
                ref readonly var modifier = ref education.GetSecond<Modifier, Learning>();
                learning.Value += modifier.Value;
            }
            if (education.Has<Modifier, PersonalCombatSkill>())
            {
                ref readonly var modifier = ref education.GetSecond<Modifier, PersonalCombatSkill>();
                personalCombatSkill.Value += modifier.Value;
            }

        });
            }
        }

        [ECSSystem]
        public class CalculateAttributesBasedOnRace
        {
            public static void Init(World world)
            {

                world.Routine<
                Health, Fertility, Diplomacy, Stewardship, Martial, Intrigue, Learning, PersonalCombatSkill>(nameof(CalculateAttributesBasedOnRace))
                .MultiThreaded()
                .With<Species>().Second(Ecs.Wildcard)
                .Each((Iter it, int i,
                    ref Health health,
                    ref Fertility fertility,
                    ref Diplomacy diplomacy,
                    ref Stewardship stewardship,
                    ref Martial martial,
                    ref Intrigue intrigue,
                    ref Learning learning,
                    ref PersonalCombatSkill personalCombatSkill) =>
                    {
                        var name = it.Id(0).ToString();
                        Entity race = it.Id(8).Second(); // The id number is the last argument number if we add arguments we must incrase this number by one
                        var raceName = race.Name();

                        if (race.Has<Modifier, Health>())
                        {
                            ref readonly var modifier = ref race.GetSecond<Modifier, Health>();
                            health.Value += modifier.Value;
                        }
                        if (race.Has<Modifier, Fertility>())
                        {
                            ref readonly var modifier = ref race.GetSecond<Modifier, Fertility>();
                            fertility.Value += modifier.Value;
                        }
                        if (race.Has<Modifier, Diplomacy>())
                        {
                            ref readonly var modifier = ref race.GetSecond<Modifier, Diplomacy>();
                            diplomacy.Value += modifier.Value;
                        }
                        if (race.Has<Modifier, Martial>())
                        {
                            ref readonly var modifier = ref race.GetSecond<Modifier, Martial>();
                            martial.Value += modifier.Value;
                        }
                        if (race.Has<Modifier, Stewardship>())
                        {
                            ref readonly var modifier = ref race.GetSecond<Modifier, Stewardship>();
                            stewardship.Value += modifier.Value;
                        }
                        if (race.Has<Modifier, Intrigue>())
                        {
                            ref readonly var modifier = ref race.GetSecond<Modifier, Intrigue>();
                            intrigue.Value += modifier.Value;
                        }
                        if (race.Has<Modifier, Learning>())
                        {
                            ref readonly var modifier = ref race.GetSecond<Modifier, Learning>();
                            learning.Value += modifier.Value;
                        }
                        if (race.Has<Modifier, PersonalCombatSkill>())
                        {
                            ref readonly var modifier = ref race.GetSecond<Modifier, PersonalCombatSkill>();
                            personalCombatSkill.Value += modifier.Value;
                        }

                    });
            }
        }

        [ECSSystem]
        public class CalculateAttributesBasedOnReligion
        {
            public static void Init(World world)
            {

                world.Routine<
            Health, Fertility, Diplomacy, Stewardship, Martial, Intrigue, Learning, PersonalCombatSkill>("Calculate character attributes based on religion modifiers")
            .MultiThreaded()
            .With<Religion>().Second(Ecs.Wildcard)
            .Each((Iter it, int i,
                ref Health health,
                ref Fertility fertility,
                ref Diplomacy diplomacy,
                ref Stewardship stewardship,
                ref Martial martial,
                ref Intrigue intrigue,
                ref Learning learning,
                ref PersonalCombatSkill personalCombatSkill) =>
                {
                    var name = it.Id(0).ToString();
                    Entity religion = it.Id(8).Second(); // The id number is the last argument number if we add arguments we must incrase this number by one
                    var religion_name = religion.Name();
                    if (religion.Has<Modifier, Health>())
                    {
                        ref readonly var modifier = ref religion.GetSecond<Modifier, Health>();
                        health.Value += modifier.Value;
                    }
                    if (religion.Has<Modifier, Fertility>())
                    {
                        ref readonly var modifier = ref religion.GetSecond<Modifier, Fertility>();
                        fertility.Value += modifier.Value;
                    }
                    if (religion.Has<Modifier, Diplomacy>())
                    {
                        ref readonly var modifier = ref religion.GetSecond<Modifier, Diplomacy>();
                        diplomacy.Value += modifier.Value;
                    }
                    if (religion.Has<Modifier, Martial>())
                    {
                        ref readonly var modifier = ref religion.GetSecond<Modifier, Martial>();
                        martial.Value += modifier.Value;
                    }
                    if (religion.Has<Modifier, Stewardship>())
                    {
                        ref readonly var modifier = ref religion.GetSecond<Modifier, Stewardship>();
                        stewardship.Value += modifier.Value;
                    }
                    if (religion.Has<Modifier, Intrigue>())
                    {
                        ref readonly var modifier = ref religion.GetSecond<Modifier, Intrigue>();
                        intrigue.Value += modifier.Value;
                    }
                    if (religion.Has<Modifier, Learning>())
                    {
                        ref readonly var modifier = ref religion.GetSecond<Modifier, Learning>();
                        learning.Value += modifier.Value;
                    }
                    if (religion.Has<Modifier, PersonalCombatSkill>())
                    {
                        ref readonly var modifier = ref religion.GetSecond<Modifier, PersonalCombatSkill>();
                        personalCombatSkill.Value += modifier.Value;
                    }

                });
            }
        }
        [ECSSystem]
        public class CalculateAttributesBasedOnCulture
        {
            public static void Init(World world)
            {
                world.Routine<
                            Health, Fertility, Diplomacy, Stewardship, Martial, Intrigue, Learning, PersonalCombatSkill>("Calculate character attributes based on culture modifiers")
                            .MultiThreaded()
                            .With<Culture>().Second(Ecs.Wildcard)
                            .Each((Iter it, int i,
                                ref Health health,
                                ref Fertility fertility,
                                ref Diplomacy diplomacy,
                                ref Stewardship stewardship,
                                ref Martial martial,
                                ref Intrigue intrigue,
                                ref Learning learning,
                                ref PersonalCombatSkill personalCombatSkill) =>
                                {
                                    var name = it.Id(0).ToString();
                                    Entity culture = it.Id(8).Second(); // The id number is the last argument number if we add arguments we must incrase this number by one
                                    var cultureName = culture.Name();
                                    if (culture.Has<Modifier, Health>())
                                    {
                                        ref readonly var modifier = ref culture.GetSecond<Modifier, Health>();
                                        health.Value += modifier.Value;
                                    }
                                    if (culture.Has<Modifier, Fertility>())
                                    {
                                        ref readonly var modifier = ref culture.GetSecond<Modifier, Fertility>();
                                        fertility.Value += modifier.Value;
                                    }
                                    if (culture.Has<Modifier, Diplomacy>())
                                    {
                                        ref readonly var modifier = ref culture.GetSecond<Modifier, Diplomacy>();
                                        diplomacy.Value += modifier.Value;
                                    }
                                    if (culture.Has<Modifier, Martial>())
                                    {
                                        ref readonly var modifier = ref culture.GetSecond<Modifier, Martial>();
                                        martial.Value += modifier.Value;
                                    }
                                    if (culture.Has<Modifier, Stewardship>())
                                    {
                                        ref readonly var modifier = ref culture.GetSecond<Modifier, Stewardship>();
                                        stewardship.Value += modifier.Value;
                                    }
                                    if (culture.Has<Modifier, Intrigue>())
                                    {
                                        ref readonly var modifier = ref culture.GetSecond<Modifier, Intrigue>();
                                        intrigue.Value += modifier.Value;
                                    }
                                    if (culture.Has<Modifier, Learning>())
                                    {
                                        ref readonly var modifier = ref culture.GetSecond<Modifier, Learning>();
                                        learning.Value += modifier.Value;
                                    }
                                    if (culture.Has<Modifier, PersonalCombatSkill>())
                                    {
                                        ref readonly var modifier = ref culture.GetSecond<Modifier, PersonalCombatSkill>();
                                        personalCombatSkill.Value += modifier.Value;
                                    }

                                });
            }
        }


        [ECSSystem]
        public class CalculateAgeBasedOnBirthday
        {
            public static void Init(World world)
            {

                world.Routine<Date, Birthday, Age>(nameof(CalculateAgeBasedOnBirthday))
                    .TermAt(0).Singleton()
                    .TermAt(1).Second<Date>()
                    .Each((Entity e, ref Date gamedate, ref Date birthdate, ref Age age) =>
                    {
                        age.Value = gamedate.Year - birthdate.Year;
                        if (gamedate.Month > birthdate.Month)
                        {
                            age.Value += 1;
                        }
                        else if (gamedate.Month == birthdate.Month)
                        {
                            if (gamedate.Day >= birthdate.Day)
                            {
                                age.Value += 1;
                            }
                        }
                    });
            }
        }

        /// <summary>
        /// Adds the System to the world
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="world">Flecs ECS World</param>
        /// <param name="cooldown">How long it will take until the same event can happen for the same entitiy</param>
        /// <param name="meantTimeToHappen">The average time it will take for the event to fire,
        /// give or take 50% more or less time. Determined at random</param>
        /// <param name="healthThreashold">Threshold of <c>health</c> an entity must have before the event fires</param>
        public static void EnableDiesOfNaturalCauseEvent(World world, int cooldown = 31, int meantTimeToHappen = 61, double healthThreashold = 10)
        {
            // This system spawns events for all entities
            // that met our defined conditions.
            world.Routine<Date>("Spawn Event-DiesOfNaturalCause")
                .MultiThreaded()
                .TermAt(0).Singleton()
                .Each(
                    (ref Date gameDate) =>
                    {
                        // First we query for the data we are intrested in
                        // You are too old event
                        // In this case all characters that are alive and kings
                        // with health and age components
                        var q = world
                            .QueryBuilder<EventDatabase, Age, Health>()
                            .TermAt(0).Singleton()
                            .With<Species>().Second(Ecs.Wildcard)
                            .With<Alive>()
                            .Build();

                        // Second we iterate over each data we are intrested
                        q.Each(
                            (
                                Entity e,
                                ref EventDatabase eventDatabase,
                                ref Age age,
                                ref Health health
                            ) =>
                            {
                                string eventID = e.ToString() + "-DiesOfNaturalCause";

                                // Third we check for the events conditions.
                                // If met spawn event
                                if (health.Value <= healthThreashold && e.Has<Alive>())
                                {
                                    // Fourth we define the event logic
                                    Events.AddEvent(
                                        cooldown: cooldown,
                                        world: world,
                                        eventID: eventID,
                                        meantTimeToHappen: meantTimeToHappen, // Here the event mean time to happen is 0,
                                                                              // so it should happen as soon as possible
                                        ev: (World world) =>
                                        {
                                            ref readonly Age currentCharacterAge = ref e.Get<Age>();
                                            ref readonly Health currentCharacterHealth =
                                                ref e.Get<Health>();

                                            // Fifth we implement a check to see if our conditions are still met.
                                            // Sometimes it makes no sense to spawn an event if some conditions changed in the mean time.
                                            // For example the character dying.
                                            if (
                                                currentCharacterHealth.Value <= healthThreashold
                                                && e.Has<Alive>()
                                            )
                                            {
                                                // Calculate a base probability
                                                double baseProbability = 0.2f; // Adjust this base value as needed

                                                // Increase probability based on age (you'll need to define how much)
                                                double ageFactor =
                                                    (currentCharacterAge.Value - 60) * 0.01f; // Example: 1% increase per year over 60
                                                baseProbability += ageFactor;

                                                // Decrease probability based on health (again, define the scaling)
                                                double healthFactor =
                                                    (10 - currentCharacterHealth.Value) * 0.02f; // Example: 2% increase per health point below 10
                                                baseProbability += healthFactor;

                                                // Ensure the probability stays within bounds (0 to 1)
                                                baseProbability = Math.Clamp(
                                                    baseProbability,
                                                    0f,
                                                    1f
                                                );

                                                // Now use this dynamic probability in your existing code
                                                int threshold = (int)(baseProbability * 100);

                                                //Generate a random number between 0 and 100
                                                Random random = new();
                                                int randomNumber = random.Next(0, 101);

                                                if (randomNumber < threshold)
                                                {
                                                    e.Remove<Alive>();

                                                    ref readonly Date currentGameDate =
                                                        ref world.Get<Date>();

                                                    e.Set<Died, Date>(
                                                        new Date(
                                                            currentGameDate.Day,
                                                            currentGameDate.Month,
                                                            currentGameDate.Year
                                                        )
                                                    );
                                                }
                                            }
                                        }
                                    );
                                }
                            }
                        );
                    }
                );
        }
    }
}
