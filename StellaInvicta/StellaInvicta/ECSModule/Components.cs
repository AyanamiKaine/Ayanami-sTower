using Flecs.NET.Core;
using StellaInvicta.Initializer;
namespace StellaInvicta.Module
{

    public record struct Name(string Value = "");

    public record struct Age(int Value = 0);
    [Component]
    class AgeComponent()
    {
        public static void Init(World world)
        {
            world.Component<Age>(nameof(Age))
                .Member<int>("Value");
        }
    }

    public record struct Population(long Value = 0);

    public record struct Wealth(double Value = 0);
    [Component]
    class WealthComponent()
    {
        public static void Init(World world)
        {
            world.Component<Wealth>(nameof(Wealth))
                .Member<double>("Value");
        }
    }

    /// <summary>
    /// Used to show the size of the entity that has 
    /// this component
    /// </summary>
    /// <param name="Value"></param>
    public record struct Size(int Value = 0);
    [Component]
    class SizeComponent()
    {
        public static void Init(World world)
        {
            world.Component<Size>(nameof(Size))
                .Member<double>("Value");
        }
    }

    /// <summary>
    /// Overall satisfaction and well-being
    /// </summary>
    /// <param name="Value"></param>
    public record struct Happiness(double Value = 0);
    [Component]
    class HappinessComponent()
    {
        public static void Init(World world)
        {
            world.Component<Happiness>(nameof(Happiness))
                .Member<double>("Value");
        }
    }

    /// <summary>
    /// Captures the idea of a substance that illuminates 
    /// the mind or consciousness, enhancing psionic 
    /// abilities or facilitating navigation through the vastness of space.
    /// </summary>
    public record struct Lux(int Quantity = 0);
    [Component]
    public class LuxComponent()
    {
        public static void Init(World world)
        {
            world.Component<Lux>(nameof(Lux))
                .Member<int>("Quantity");
        }
    }

    public record struct Prestige(double Value = 0);
    [Component]
    class PrestigeComponent()
    {
        public static void Init(World world)
        {
            world.Component<Prestige>(nameof(Prestige))
                .Member<double>("Value");
        }
    }
    public record struct Piety(double Value = 0);
    [Component]
    class PietyComponent()
    {
        public static void Init(World world)
        {
            world.Component<Piety>(nameof(Piety))
                .Member<double>("Value");
        }
    }
    public record struct Cooldown(int Value = 0);
    [Component]
    class CooldownComponent()
    {
        public static void Init(World world)
        {
            world.Component<Cooldown>(nameof(Cooldown))
                .Member<int>("Days");
        }
    }
    /// <summary>
    ///  Healthy characters are less likely to fall prey to, and more likely to fully recover from, disease. They are also less likely to die from old age per month than ill characters.
    ///  High health is key to a long life.
    /// </summary>
    /// <param name="Value"></param>
    public record struct Health(double Value = 0);
    [Component]
    public class HealthComponent()
    {
        public static void Init(World world)
        {
            world.Component<Health>(nameof(Health))
                .Member<double>("Value");
        }
    }
    public record struct MonthlyPiety(double Value = 0);
    [Component]
    public class MonthlyPietyComponent()
    {
        public static void Init(World world)
        {
            world.Component<MonthlyPiety>(nameof(MonthlyPiety))
                .Member<double>("Value");
        }
    }
    public record struct Diplomacy(double Value = 0);
    [Component]
    public class DiplomacyComponent()
    {
        public static void Init(World world)
        {
            world.Component<Diplomacy>(nameof(Diplomacy))
                .Member<double>("Value");
        }
    }
    /// <summary>
    /// Percent based
    /// </summary>
    /// <param name="Value"></param>
    public record struct Attraction(double Value = 0);
    [Component]
    public class AttractionComponent()
    {
        public static void Init(World world)
        {
            world.Component<Attraction>(nameof(Attraction))
                .Member<double>("Value");
        }
    }
    public record struct Stewardship(double Value = 0);
    [Component]
    public class StewardshipComponent()
    {
        public static void Init(World world)
        {
            world.Component<Stewardship>(nameof(Stewardship))
                .Member<double>("Value");
        }
    }
    public record struct Martial(double Value = 0);
    [Component]
    public class MartialComponent()
    {
        public static void Init(World world)
        {
            world.Component<Martial>(nameof(Martial))
                .Member<double>("Value");
        }
    }
    public record struct Intrigue(double Value = 0);
    [Component]
    public class IntrigueComponent()
    {
        public static void Init(World world)
        {
            world.Component<Intrigue>(nameof(Intrigue))
                .Member<double>("Value");
        }
    }
    public record struct Learning(double Value = 0);
    [Component]
    public class LearningComponent()
    {
        public static void Init(World world)
        {
            world.Component<Learning>(nameof(Learning))
                .Member<double>("Value");
        }
    }
    /// <summary>
    /// Fertility determines how likely an individual character is to conceive children.
    /// </summary>
    /// <param name="Value"></param>
    public record struct Fertility(double Value = 0);
    [Component]
    class FertilityComponent()
    {
        public static void Init(World world)
        {
            world.Component<Fertility>(nameof(Fertility))
                .Member<double>("Value");
        }
    }

    /// <summary>
    /// Determines how rational a decision will be made by the ai.
    /// </summary>
    /// <param name="Value"></param>
    public record struct Rationality(double Value = 0);
    [Component]
    class RationalityComponent()
    {
        public static void Init(World world)
        {
            world.Component<Rationality>(nameof(Rationality))
                .Member<double>("Value");
        }
    }


    /// <summary>
    /// Determines how greedy the decision of the ai are, related to wealth and power
    /// </summary>
    /// <param name="Value"></param>
    public record struct Greed(double Value = 0);
    [Component]
    class GreedComponent()
    {
        public static void Init(World world)
        {
            world.Component<Greed>(nameof(Greed))
                .Member<double>("Value");
        }
    }

    /// <summary>
    /// Determines how zealous the decision of the ai are related to religious or ideological decisions.
    /// </summary>
    /// <param name="Value"></param>
    public record struct Zeal(double Value = 0);
    [Component]
    class ZealComponent()
    {
        public static void Init(World world)
        {
            world.Component<Zeal>(nameof(Zeal))
                .Member<double>("Value");
        }
    }

    public record struct PersonalCombatSkill(double Value = 0);
    [Component]
    class PersonalCombatSkillComponent()
    {
        public static void Init(World world)
        {
            world.Component<PersonalCombatSkill>(nameof(PersonalCombatSkill))
                .Member<double>("Value");
        }
    }
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
    }
    [Component]
    class DateComponent()
    {
        public static void Init(World world)
        {
            world.Component<Date>(nameof(Date))
                .Member<DateTime>("PRIVATE")
                .Member<int>("Day")
                .Member<int>("Month")
                .Member<int>("Year");
        }
    }
    public record struct MeanTimetoHappen(int Days = 0);
    [Component]
    class MeanTimetoHappenSkillComponent()
    {
        public static void Init(World world)
        {
            world.Component<MeanTimetoHappen>(nameof(MeanTimetoHappen))
                .Member<int>("Days");
        }
    }
    public record struct HappensIn(int Days = 0);
    [Component]
    class HappensInComponent()
    {
        public static void Init(World world)
        {
            world.Component<HappensIn>(nameof(HappensIn))
                .Member<int>("Days");
        }
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
    }

    /// <summary>
    /// Used for the <c>Entity</c> is married to <c>Entity</c> Relationship
    /// </summary>
    /// <remarks>
    /// This relationship is <c>Ecs.Symmetric</c>. If A is married to B, B is also married to A
    /// </remarks>
    public struct IsMarriedTo;
    [Component]
    public class IsMarriedToComponent()
    {
        public static void Init(World world)
        {
            world.Component<IsMarriedTo>(nameof(IsMarriedTo))
                .Entity.Add(Ecs.Symmetric);
        }
    }
    /// <summary>
    /// Used for the <c>Entity</c> is at war with <c>Entity</c> Relationship
    /// </summary>
    /// <remarks>
    /// This relationship is <c>Ecs.Symmetric</c>. If A is at war with B, B is also at war with A
    /// </remarks>
    public struct IsAtWarWith;
    [Component]
    public class IsAtWarWithComponent()
    {
        public static void Init(World world)
        {
            world.Component<IsAtWarWith>(nameof(IsAtWarWith))
                .Entity.Add(Ecs.Symmetric);
        }
    }
    /// <summary>
    /// Used usually to show that two entities are opposed to each other.
    /// EXAMPLE:
    /// This is used for traits to show an opposite trait, so we can say if opposed trait -opinion or cant
    /// have this and the opposed trait
    /// </summary>
    /// <remarks>
    /// This relationship is <c>Ecs.Symmetric</c>. If A is opposed to B, B is also opposed to A
    /// </remarks>
    public struct OpposedTo;
    [Component]
    public class OpposedToComponent()
    {
        public static void Init(World world)
        {
            world.Component<OpposedTo>(nameof(OpposedTo))
                .Entity.Add(Ecs.Symmetric);
        }
    }

    /// <summary>
    /// Shows the relationship of that the entity 
    /// is presence at that location.
    /// 
    /// Entity planet.Add(Presence)(character)
    /// 
    /// </summary>
    public struct Presence;
    [Component]
    public class PresenceComponent()
    {
        public static void Init(World world)
        {
            world.Component<Presence>(nameof(Presence));
        }
    }

    /// <summary>
    /// A character can only be part of one House. Its a <c>Ecs.Exclusive</c>   
    /// </summary>
    public struct House;
    [Component]
    public class HouseComponent()
    {
        public static void Init(World world)
        {
            world.Component<House>(nameof(House))
                .Entity.Add(Ecs.Exclusive);
        }
    }

    /// <summary>
    /// And ideology of an character usually determines 
    /// what actions he will more likely take
    /// A character can only have on ideology. 
    /// Its <c>Ecs.Exclusive</c>.   
    /// </summary>
    public struct Ideology;
    [Component]
    public class IdeologyComponent()
    {
        public static void Init(World world)
        {
            world.Component<Ideology>(nameof(Ideology))
                .Entity.Add(Ecs.Exclusive);
        }
    }




    /// <summary>
    /// A character can have a profession its his job he currently
    /// executes. Professions usally determine what actions an characters
    /// can execute that have more impact. For example a trade may change 
    /// supply and demand but cant change the tax rates. Only a governour could
    /// do that.
    /// Its <c>Ecs.Exclusive</c>.   
    /// </summary>
    public struct Profession;
    [Component]
    public class ProfessionComponent()
    {
        public static void Init(World world)
        {
            world.Component<Profession>(nameof(Profession))
                .Entity.Add(Ecs.Exclusive);
        }
    }

    // Tags
    /*
    Tags are usually used as an identifier
    Enemy, Ally, etc.
    */
    public struct King;
    [Component]
    public class KingComponent()
    {
        public static void Init(World world)
        {
            world.Component<King>(nameof(King));
        }
    }
    public struct Emperor;
    [Component]
    public class EmperorComponent()
    {
        public static void Init(World world)
        {
            world.Component<Emperor>(nameof(Emperor));
        }
    }
    public struct Duke;
    public struct Count;
    public struct Baron;
    public struct Birthday;
    [Component]
    public class BirthdayComponent()
    {
        public static void Init(World world)
        {
            world.Component<Birthday>(nameof(Birthday));
        }
    }
    public struct Event;
    [Component]
    public class EventComponent()
    {
        public static void Init(World world)
        {
            world.Component<Event>(nameof(Event));
        }
    }
    public struct Alive;
    [Component]
    public class AliveComponent()
    {
        public static void Init(World world)
        {
            world.Component<Alive>(nameof(Alive));
        }
    }
    public struct Died;
    [Component]
    public class DiedComponent()
    {
        public static void Init(World world)
        {
            world.Component<Died>(nameof(Died));
        }
    }
    public struct Pregnant;
    [Component]
    public class PregnantComponent()
    {
        public static void Init(World world)
        {
            world.Component<Pregnant>(nameof(Pregnant));
        }
    }
    public struct Happening;
    [Component]
    public class HappeningComponent()
    {
        public static void Init(World world)
        {
            world.Component<Happening>(nameof(Happening));
        }
    }
    public struct Happened;
    [Component]
    public class HappenedComponent()
    {
        public static void Init(World world)
        {
            world.Component<Happened>(nameof(Happened));
        }
    }
    public struct Created;
    [Component]
    public class CreatedComponent()
    {
        public static void Init(World world)
        {
            world.Component<Created>(nameof(Created));
        }
    }
    /// <summary>
    /// Owned by Relationship will be <c>Ecs.Exclusive</c> can only be owned by one entity
    /// </summary>
    public struct OwnedBy;
    [Component]
    public class OwnedByComponent()
    {
        public static void Init(World world)
        {
            world.Component<OwnedBy>(nameof(OwnedBy))
                .Entity.Add(Ecs.Exclusive);
        }
    }

    /// <summary>
    /// Identify to show that the entity is an ai
    /// </summary>
    public struct AI;
    [Component]
    public class AIComponent()
    {
        public static void Init(World world)
        {
            world.Component<AI>(nameof(AI));
        }
    }

    /// <summary>
    /// Identify to show that the entity is a human player
    /// </summary>
    public struct Player;
    [Component]
    public class PlayerComponent()
    {
        public static void Init(World world)
        {
            world.Component<Player>(nameof(Player));
        }
    }

    // IsA Relationships
    /*
    The IsA relationship is a builtin relationship that allows 
    applications to express that one entity is equivalent to another.

    Apple.IsA(Fruit);

    This means that an Apple is also a Fruit (Apple == Fruit)
    but a Fruit is not an Apple (Fruit != Apple)

    An Apple is a subset of Fruit. Fruit is a superset of Apple
    */
    /// <summary>
    /// Used to say that kind is a trait, so we can easily query all traits that exist.
    /// or better we will say query for health and trait and get all traits that 
    /// the entity has
    /// </summary>
    public struct Trait;
    [Component]
    public class TraitComponent()
    {
        public static void Init(World world)
        {
            world.Component<Trait>(nameof(Trait));
        }
    }
    public struct Species;
    [Component]
    public class RaceComponent()
    {
        public static void Init(World world)
        {
            world.Component<Species>(nameof(Species));
        }
    }
    public struct Education;
    [Component]
    public class EducationComponent()
    {
        public static void Init(World world)
        {
            world.Component<Education>(nameof(Education))
                .Entity.Add(Ecs.Exclusive);
        }
    }
    public struct Culture;
    [Component]
    public class CultureComponent()
    {
        public static void Init(World world)
        {
            world.Component<Culture>(nameof(Culture));
        }
    }
    public struct Religion;
    [Component]
    public class ReligionComponent()
    {
        public static void Init(World world)
        {
            world.Component<Religion>(nameof(Religion));
        }
    }

    /// <summary>
    /// Designates a pair to indicate its a modifier
    /// (Modifier, TypeToBeModified) 
    /// </summary>
    public struct Modifier;
    [Component]
    public class ModifierComponent()
    {
        public static void Init(World world)
        {
            world.Component<Modifier>(nameof(Modifier));
        }
    }
    public struct Attribute;
    [Component]
    public class AttributeComponent()
    {
        public static void Init(World world)
        {
            world.Component<Attribute>(nameof(Attribute));
        }
    }
    public struct Flat;
    [Component]
    public class FlatComponent()
    {
        public static void Init(World world)
        {
            world.Component<Flat>(nameof(Flat));
        }
    }
    public struct Percent;
    [Component]
    public class PercentComponent()
    {
        public static void Init(World world)
        {
            world.Component<Percent>(nameof(Percent));
        }
    }
    public struct Components : IFlecsModule
    {
        // Components
        public readonly void InitModule(World world)
        {

            ComponentInitializer.Run(world);
        }
    }
}