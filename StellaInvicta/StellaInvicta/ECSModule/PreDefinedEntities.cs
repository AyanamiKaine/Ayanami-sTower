using StellaInvicta.GameLogic.Functions;
using Flecs.NET.Core;
using StellaInvicta.Initializer;
/*
Everything that wants to spawn an entity before 
the world progresses should be defined here
*/
namespace StellaInvicta.Module
{

    /// <summary>
    /// Dynasti (Ruling families, political elites, and influential figures)
    /// </summary>
    [PopulationPrefab]
    public class Dynasti
    {
        public static void Init(World world)
        {
            world.Prefab(nameof(Dynasti));
        }
    }

    /// <summary>
    /// Administratores (Government officials, 
    /// administrators, and bureaucrats 
    /// managing planetary affairs)
    /// </summary>
    [PopulationPrefab]
    public class Administratores
    {
        public static void Init(World world)
        {
            world.Prefab(nameof(Administratores));
        }
    }

    /// <summary>
    /// Investores (Entrepreneurs, 
    /// investors, and corporate leaders driving economic growth)
    /// </summary>
    [PopulationPrefab]
    public class Investores
    {
        public static void Init(World world)
        {
            world.Prefab(nameof(Investores));
        }
    }


    /// <summary>
    /// Sacerdotes (Religious leaders and spiritual guides,
    /// potentially influencing social and political dynamics)
    /// </summary>
    [PopulationPrefab]
    public class Sacerdotes
    {
        public static void Init(World world)
        {
            world.Prefab(nameof(Sacerdotes));
        }
    }

    /// <summary>
    /// Technici (Skilled technicians, engineers,
    /// and artisans crafting advanced technologies)
    /// </summary>
    [PopulationPrefab]
    public class Technici
    {
        public static void Init(World world)
        {
            world.Prefab(nameof(Technici));
        }
    }

    /// <summary>
    /// Agri-Technologistae (Agricultural workers utilizing 
    /// advanced techniques and bio-engineering for food production)
    /// </summary>
    [PopulationPrefab]
    public class AgriTechnologistae
    {
        public static void Init(World world)
        {
            world.Prefab(nameof(AgriTechnologistae));
        }
    }

    /// <summary>
    /// Operarii (Unskilled workers performing 
    /// manual labor in various industries and infrastructure projects)
    /// </summary>
    [PopulationPrefab]
    public class Operarii
    {
        public static void Init(World world)
        {
            world.Prefab(nameof(Operarii));
        }
    }

    /// <summary>
    /// Praefecti(Military leaders, 
    /// strategists, and commanders of fleets and armies)
    /// </summary>
    [PopulationPrefab]
    public class Praefecti
    {
        public static void Init(World world)
        {
            world.Prefab(nameof(Praefecti));
        }
    }

    /// <summary>
    /// Legionarii (Professional soldiers and 
    /// space marines forming the backbone of military forces)
    /// </summary>
    [PopulationPrefab]
    public class Legionarii
    {
        public static void Init(World world)
        {
            world.Prefab(nameof(Legionarii));
        }
    }

    /// <summary>
    /// Slaves
    /// </summary>
    [PopulationPrefab]
    public class Servi
    {
        public static void Init(World world)
        {
            world.Prefab(nameof(Servi))
                .IsA<Pop>();
        }
    }

    /// <summary>
    /// BasePopulationEntity
    /// </summary>
    [PopulationPrefab]
    public class Pop
    {
        public static void Init(World world)
        {
            world.Prefab(nameof(Pop))
                .Set<Size>(new(0))
                .Set<Wealth>(new(0))
                .Set<Happiness>(new(0));

            /*
            There is a chance that 
            instead of using IsA Relationship to extend
            the entity type, instead we store a
            type relationship component in the pop
            entity itself.
            */
        }
    }

    /// <summary>
    /// Emphasize a connection to a mystical life force or energy 
    /// that permeates the universe
    /// Introduce rituals and practices involving meditation, 
    /// breathing techniques, or even controlled exposure to certain 
    /// substances to achieve spiritual enlightenment.
    /// Consider incorporating a prophecy or belief in a 
    /// chosen one who will bring balance to the galaxy
    /// </summary>
    [Religion]
    public class HarmoniaUniversalis
    {
        public static void Init(World world)
        {
            world.Entity(nameof(HarmoniaUniversalis))
                .Set<Modifier, Health>(new Health(1.0))
                .Set<Modifier, Learning>(new Learning(3));
        }
    }

    /// <summary>
    /// Portray the Cult as a secretive and potentially 
    /// subversive organization, operating in the 
    /// shadows and challenging the established order.
    /// Introduce a conflict between the Cult's 
    /// pursuit of technological singularity and the 
    /// reverence for human intuition and organic life
    /// </summary>
    [Religion]
    public class CultusMachinae
    {
        public static void Init(World world)
        {
            world.Entity(nameof(CultusMachinae))
                .Set<Modifier, Health>(new Health(0.5));
        }
    }


    /// <summary>
    /// Emphasize bloodlines, genetic heritage, 
    /// and the importance of preserving lineage
    /// 
    /// Introduce rituals and traditions honoring ancestors, 
    /// perhaps involving genetic testing, ancestral records, 
    /// or even the preservation of bodily remains.
    /// Consider the potential for conflicts and power 
    /// struggles within families
    /// </summary>
    [Religion]
    public class ViaMajorum
    {
        public static void Init(World world)
        {
            world.Entity(nameof(ViaMajorum))
                .Set<Modifier, Health>(new Health(0.5));
        }
    }


    /// <summary>
    /// Connect the religion to the vastness and 
    /// dangers of space travel, emphasizing the 
    /// need for courage, resilience, and navigation skills
    /// Introduce rituals and practices that involve 
    /// exposure to the harshness of space or reliance on 
    /// special technologies or substances to navigate its depths.
    /// Consider the potential for a conflict between 
    /// those who embrace the Void and those who fear its dangers.
    /// </summary>
    [Religion]
    public class AmplexusVacui
    {
        public static void Init(World world)
        {
            world.Entity(nameof(AmplexusVacui))
                .Set<Modifier, Health>(new Health(-1))
                .Set<Modifier, Martial>(new Martial(4));
        }
    }

    /// <summary>
    /// Human
    /// (Wise Man)
    /// </summary>
    [Species]
    public class HomoSapiens
    {
        public static void Init(World world)
        {
            world.Entity(nameof(HomoSapiens))
                .Set<Modifier, Health>(new Health(3.0))
                .Set<Modifier, Diplomacy>(new Diplomacy(3))
                .Set<Modifier, Learning>(new Learning(3));
        }
    }

    /// <summary>
    /// Dwarfs
    /// (Iron Man)  
    /// </summary>
    [Species]
    public class HomoFerrarius
    {
        public static void Init(World world)
        {
            world.Entity(nameof(HomoFerrarius))
                .Set<Modifier, Health>(new Health(6.0))
                .Set<Modifier, Martial>(new Martial(3))
                .Set<Modifier, Learning>(new Learning(-2))
                .Set<Modifier, Stewardship>(new Stewardship(-2));
        }
    }


    /// <summary>
    /// Elfs
    /// (Long-Lived People)
    /// </summary>
    [Species]
    public class GensLongaeva
    {
        public static void Init(World world)
        {
            world.Entity(nameof(GensLongaeva))
                .Set<Modifier, Health>(new Health(8.5));
        }
    }

    /// <summary>
    /// Orcs
    /// (Warlike Man)
    /// </summary>
    [Species]
    public class HomoBellicosus
    {
        public static void Init(World world)
        {
            world.Entity(nameof(HomoBellicosus))
                .Set<Modifier, Health>(new Health(3.5))
                .Set<Modifier, Rationality>(new Rationality(-0.25))
                .Set<Modifier, Martial>(new Martial(2))
                .Set<Modifier, Learning>(new Learning(-4));
        }
    }

    [Profession]
    public class FleetCommander
    {
        public static void Init(World world)
        {
            world.Entity(nameof(FleetCommander));
        }
    }

    [Profession]
    public class EmpireLeader
    {
        public static void Init(World world)
        {
            world.Entity(nameof(EmpireLeader));
        }
    }

    [Profession]
    public class CompanyLeader
    {
        public static void Init(World world)
        {
            world.Entity(nameof(CompanyLeader));
        }
    }

    [Profession]
    public class Researcher
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Researcher));
        }
    }

    [Profession]
    public class SectorGovenor
    {
        public static void Init(World world)
        {
            world.Entity(nameof(SectorGovenor));
        }
    }


    [Profession]
    public class PlanetGovenor
    {
        public static void Init(World world)
        {
            world.Entity(nameof(PlanetGovenor));
        }
    }

    [Profession]
    public class PirateGovenor
    {
        public static void Init(World world)
        {
            world.Entity(nameof(PirateGovenor));
        }
    }


    [Profession]
    public class Spy
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Spy));
        }
    }

    /// <summary>
    /// A fleet commander that is not bound by a border.
    /// </summary>
    [Profession]
    public class MercenaryLeader
    {
        public static void Init(World world)
        {
            world.Entity(nameof(MercenaryLeader));
        }
    }

    /// <summary>
    /// A company leader that is not bound by a border.
    /// </summary>
    [Profession]
    public class FreeCompanyLeader
    {
        public static void Init(World world)
        {
            world.Entity(nameof(MercenaryLeader));
        }
    }


    [Education]
    public class BrilliantStrategist
    {
        public static void Init(World world)
        {

            world.Entity(nameof(BrilliantStrategist))
                    .Set<Modifier, Health>(new Health(0.5))
                    .Set<Modifier, Martial>(new Martial(9))
                    .Set<Modifier, Stewardship>(new Stewardship(2))
                    .Set<Modifier, Intrigue>(new Intrigue(2))
                    .Set<Modifier, Learning>(new Learning(-1))
                    .Set<Modifier, PersonalCombatSkill>(new PersonalCombatSkill(20));
        }
    }

    [Trait]
    public class Decadent
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Decadent))
                .Set<Modifier, PersonalCombatSkill>(new PersonalCombatSkill(-10));
        }
    }

    [Trait]
    public class Kind
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Kind))
                .Set<Modifier, Diplomacy>(new Diplomacy(2))
                .Set<Modifier, Martial>(new Martial(-1))
                .Set<Modifier, Intrigue>(new Intrigue(-2));
        }
    }

    [Trait]
    public class Zealous
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Zealous))
                .Add<OpposedTo>(Traits.Get(world, nameof(Decadent)))
                .Set<Modifier, Zeal>(new Zeal(10))
                .Set<Modifier, MonthlyPiety>(new MonthlyPiety(1))
                .Set<Modifier, Diplomacy>(new Diplomacy(2))
                .Set<Modifier, Martial>(new Martial(-1))
                .Set<Modifier, Intrigue>(new Intrigue(-2));
        }
    }

    /// <summary>
    /// A prophet in a religion wields incredible 
    /// power of thre religion and its followers
    /// he/she/they have the ability to fundamentally
    /// change the religion, move it into a fanatic
    /// jihad or moving them away from it
    /// <remarks>
    /// The trait should be spawned by flavour events/
    /// event chains. It should feel special and meaningful
    /// when the player becomes a prophet for the first 
    /// time.
    /// </remarks>
    /// </summary>
    [Trait]
    public class Prophet
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Prophet))
                .Set<Modifier, Diplomacy>(new Diplomacy(5))
                .Set<Modifier, Zeal>(new Zeal(1000));
        }
    }

    /*
    ///////////////////////
    /// POSITIVE TRAITS ///
    ///////////////////////
    */

    [Trait]
    public class Attractive
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Attractive))
                .Set<Modifier, Attraction>(new Attraction(0.3));
        }
    }

    [Trait]
    public class Genius
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Genius))
                .Set<Modifier, Diplomacy>(new Diplomacy(5))
                .Set<Modifier, Martial>(new Martial(5))
                .Set<Modifier, Stewardship>(new Stewardship(5))
                .Set<Modifier, Learning>(new Learning(5))
                .Set<Modifier, Intrigue>(new Intrigue(5))
                .Set<Modifier, PersonalCombatSkill>(new PersonalCombatSkill(10));
        }
    }
    [Trait]
    public class Quick
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Quick))
                .Add<OpposedTo>(Traits.Get(world, nameof(Genius)));
        }
    }

    [Trait]
    public class Strong
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Strong));
        }
    }

    /*
    ///////////////////////
    /// NEGATIVE TRAITS ///
    ///////////////////////
    */

    [Trait]
    public class Slow
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Slow))
                .Add<OpposedTo>(Traits.Get(world, nameof(Genius)))
                .Add<OpposedTo>(Traits.Get(world, nameof(Quick)))
                .Set<Modifier, Diplomacy>(new Diplomacy(-3))
                .Set<Modifier, Martial>(new Martial(-3))
                .Set<Modifier, Stewardship>(new Stewardship(-3))
                .Set<Modifier, Learning>(new Learning(-3))
                .Set<Modifier, Intrigue>(new Intrigue(-3))
                .Set<Modifier, PersonalCombatSkill>(new PersonalCombatSkill(-5));
        }
    }
    [Trait]
    public class Imbecile
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Imbecile))
                .Add<OpposedTo>(Traits.Get(world, nameof(Genius)))
                .Add<OpposedTo>(Traits.Get(world, nameof(Quick)))
                .Add<OpposedTo>(Traits.Get(world, nameof(Slow)))
                .Set<Modifier, Diplomacy>(new Diplomacy(-8))
                .Set<Modifier, Martial>(new Martial(-8))
                .Set<Modifier, Stewardship>(new Stewardship(-8))
                .Set<Modifier, Learning>(new Learning(-8))
                .Set<Modifier, Intrigue>(new Intrigue(-8))
                .Set<Modifier, PersonalCombatSkill>(new PersonalCombatSkill(-30));
        }
    }

    [Trait]
    public class Inbred
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Inbred))
                .Set<Modifier, Rationality>(new Rationality(-0.3))
                .Set<Modifier, Fertility>(new Fertility(-0.3))
                .Set<Modifier, Diplomacy>(new Diplomacy(-5))
                .Set<Modifier, Martial>(new Martial(-5))
                .Set<Modifier, Stewardship>(new Stewardship(-5))
                .Set<Modifier, Learning>(new Learning(-5))
                .Set<Modifier, Intrigue>(new Intrigue(-5))
                .Set<Modifier, PersonalCombatSkill>(new PersonalCombatSkill(-20));
        }
    }


    [Trait]
    public class Robust
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Robust))
                .Set<Modifier, Health>(new Health(1.0));
        }
    }

    /// <summary>
    /// These ruling families, reminiscent of the Great Houses, control vast interstellar territories. 
    /// They believe in their inherited right to rule and maintain a strong sense of tradition.
    /// Key Traits: Hierarchical, expansionist, traditionalist.
    /// </summary>
    [Ideology]
    public class DomusImperialis
    {
        public static void Init(World world)
        {
            world.Entity(nameof(DomusImperialis));
        }
    }

    /// <summary>
    ///  Ideology of freedom and rebellion
    ///  These are the outlaws and rebels of the galaxy, 
    ///  living outside the bounds of established society. 
    ///  They value freedom and independence, 
    ///  taking what they need through force and cunning.
    /// </summary>
    [Ideology]
    public class PraedonesSpatii
    {
        public static void Init(World world)
        {
            world.Entity(nameof(PraedonesSpatii));
        }
    }

    /// <summary>
    /// Ideology of masters of trade and commerce, 
    /// prioritizing profit and economic growth above all else. 
    /// They believe in the power of the free market to bring prosperity and stability to the galaxy.
    /// </summary>
    [Ideology]
    public class CommerciumInterstellare
    {
        public static void Init(World world)
        {
            world.Entity(nameof(CommerciumInterstellare));
        }
    }

    /// <summary>
    /// This ideology sees technology and artificial 
    /// intelligence as the ultimate path to progress 
    /// and enlightenment.They may seek to merge 
    /// with machines or elevate AI to a position of power and reverence
    /// </summary>
    [Ideology]
    public class CultusMachina
    {
        public static void Init(World world)
        {
            world.Entity(nameof(CultusMachina));
        }
    }


    /// <summary>
    /// This ideology places the interests of 
    /// humanity above all else, advocating for the 
    /// expansion and dominance of the human race throughout 
    /// the galaxy. They may view other species as 
    /// inferior or potential threats.
    /// </summary>
    [Ideology]
    public class ImperiumHumanitas
    {
        public static void Init(World world)
        {
            world.Entity(nameof(ImperiumHumanitas));
        }
    }

    /// <summary>
    ///  This ideology believes in achieving harmony and stability 
    ///  across the galaxy through diplomacy, cooperation, 
    ///  and mutual understanding. 
    ///  They prioritize conflict resolution and disarmament, 
    ///  seeking to create a lasting peace for all.
    /// </summary>
    [Ideology]
    public class PaxUniversalis
    {
        public static void Init(World world)
        {
            world.Entity(nameof(PaxUniversalis));
        }
    }

    /// <summary>
    ///  This faction believes in the right of individual star systems or 
    ///  planets to self-determination and independence. 
    ///  They reject centralized authority and advocate 
    ///  for a decentralized, fragmented galactic political landscape.
    /// </summary>
    [Ideology]
    public class SecessioStellarum
    {
        public static void Init(World world)
        {
            world.Entity(nameof(SecessioStellarum));
        }
    }

    [House]
    public class DomusAquila
    {
        public static void Init(World world)
        {
            world.Entity(nameof(DomusAquila));
        }
    }

    [House]
    public class DomusLibra
    {
        public static void Init(World world)
        {
            world.Entity(nameof(DomusLibra));
        }
    }

    [House]
    public class DomusSerpens
    {
        public static void Init(World world)
        {
            world.Entity(nameof(DomusSerpens));
        }
    }

    [House]
    public class DomusViridis
    {
        public static void Init(World world)
        {
            world.Entity(nameof(DomusViridis));
        }
    }

    [House]
    public class DomusCerebrum
    {
        public static void Init(World world)
        {
            world.Entity(nameof(DomusCerebrum));
        }
    }

    /// <summary>
    /// Calculating Mind Culture
    /// </summary>
    [Culture]
    public class MentisCalculantis
    {
        public static void Init(World world)
        {
            world.Entity(nameof(MentisCalculantis));
        }
    }

    [Culture]
    public class BellatorumImperatoris
    {
        public static void Init(World world)
        {
            world.Entity(nameof(BellatorumImperatoris));
        }
    }

    /// <summary>
    ///  Sisters of Wisdom Culture
    /// </summary>
    [Culture]
    public class SororumSapientium
    {
        public static void Init(World world)
        {
            world.Entity(nameof(SororumSapientium));
        }
    }

    /// <summary>
    /// Genetic Secrets Culture
    /// </summary>
    [Culture]
    public class GeneticaeArcanum
    {
        public static void Init(World world)
        {
            world.Entity(nameof(GeneticaeArcanum));
        }
    }

    /// <summary>
    /// Technology Culture
    /// </summary>
    [Culture]
    public class Technologiae
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Technologiae));
        }
    }

    /// <summary>
    /// Opulent Culture
    /// </summary>
    [Culture]
    public class Opulentae
    {
        public static void Init(World world)
        {
            world.Entity(nameof(Opulentae));
        }
    }

    /// <summary>
    /// Suggests a civilization obsessed with time travel 
    /// or manipulating temporal mechanics.
    /// </summary>
    [Culture]
    public class DynastiaChronos
    {
        public static void Init(World world)
        {
            world.Entity(nameof(DynastiaChronos));
        }
    }

    /// <summary>
    /// Hints at a once-great civilization reduced to a 
    /// powerful but scattered fleet, seeking to reclaim their former glory.
    /// </summary>
    [Culture]
    public class ClassisReliquiae
    {
        public static void Init(World world)
        {
            world.Entity(nameof(ClassisReliquiae));
        }
    }

    /// <summary>
    /// Hints at a civilization with powerful psychic abilities, 
    /// potentially influencing diplomacy and warfare in unique ways.
    /// </summary>
    [Culture]
    public class AscensusPsionicus
    {
        public static void Init(World world)
        {
            world.Entity(nameof(AscensusPsionicus));
        }
    }

    /// <summary>
    ///  Evokes a wandering, adaptable people who may 
    ///  have unique technologies or cultural 
    ///  practices related to space travel.
    /// </summary>
    [Culture]
    public class NomadiNebulae
    {
        public static void Init(World world)
        {
            world.Entity(nameof(NomadiNebulae));
        }
    }

    /// <summary>
    /// Suggests a society guided by prophecy or a mysterious entity, 
    /// potentially impacting their decision-making and long-term goals.
    /// </summary>
    [Culture]
    public class ConcordiaOraculi
    {
        public static void Init(World world)
        {
            world.Entity(nameof(ConcordiaOraculi));
        }
    }


    /// <summary>
    /// The Children of the Singularity: Proles Singularitatis
    /// A transhumanist society that has undergone radical 
    /// technological and biological transformations.
    /// They may possess advanced cybernetic enhancements, 
    /// genetic modifications, or even uploaded consciousnesses.
    /// Their diplomatic approach could range from cooperative 
    /// to supremacist, depending on their views on the future of organic life and their place in the universe.
    /// </summary>
    [Culture]
    public class ProlesSingularitatis
    {
        public static void Init(World world)
        {
            world.Entity(nameof(ProlesSingularitatis));
        }
    }

    /// <summary>
    /// A nomadic people who embrace the concept of entropy 
    /// and the inevitable decay of all things.
    /// They may possess unique technologies or 
    /// philosophies that allow them to thrive in 
    /// harsh and unpredictable environments.
    /// Their diplomatic approach could be unpredictable 
    /// and opportunistic, seeking to exploit chaos and 
    /// instability for their own benefit.
    /// </summary>
    [Culture]
    public class VagantesEntropiae
    {
        public static void Init(World world)
        {
            world.Entity(nameof(VagantesEntropiae));
        }
    }

    /// <summary>
    /// A society dedicated to the preservation and dissemination of 
    /// knowledge, viewing themselves as custodians of the galaxy's collective memory.
    /// They may possess vast archives and repositories of information, 
    /// accessible through advanced neural interfaces or psionic abilities.
    /// Their diplomatic approach could involve offering knowledge and 
    /// insights in exchange for resources or alliances, or acting as 
    /// mediators in conflicts between other factions.
    /// </summary>
    [Culture]
    public class BibliothecaVivens
    {
        public static void Init(World world)
        {
            world.Entity(nameof(BibliothecaVivens))
                .Set<Modifier, Diplomacy>(new Diplomacy(3));
        }
    }

    /// <summary>
    /// A civilization that values dreams and the subconscious mind, 
    /// believing that reality is shaped by collective dreams and aspirations.
    /// They may possess advanced technologies that allow them 
    /// to access and manipulate dreams, potentially influencing 
    /// the thoughts and actions of others.
    /// Their diplomatic approach could involve subtle psychological 
    /// manipulation or the sharing of dream-inspired visions and prophecies.
    /// </summary>
    [Culture]
    public class TextoresSomniorum
    {
        public static void Init(World world)
        {
            world.Entity(nameof(TextoresSomniorum));
        }
    }

    /// <summary>
    /// A society deeply connected to music and celestial rhythms, 
    /// believing that the universe operates on a grand symphony of vibrations and harmonies.
    /// They may possess unique technologies that harness sound or 
    /// music for various purposes, from communication to warfare.
    /// Their diplomatic approach could involve cultural exchange 
    /// and musical performances, seeking to establish harmony and 
    /// understanding through shared artistic expression.
    /// </summary>
    [Culture]
    public class HarmoniaSphaerarum
    {
        public static void Init(World world)
        {
            world.Entity(nameof(HarmoniaSphaerarum));
        }
    }

    /// <summary>
    /// Great Houses Culture
    /// </summary>
    [Culture]
    public class MagnaDomus
    {
        public static void Init(World world)
        {
            world.Entity(nameof(MagnaDomus));
        }
    }

    public struct PreDefinedEntities : IFlecsModule
    {

        public readonly void InitModule(World world)
        {
            world.Timer("GameSpeed")
                .Interval(1.0f);
            TraitInitializer.Run(world);
            EducationInitalizer.Run(world);
            ProfessionInitalizer.Run(world);
            HouseInitalizer.Run(world);
            CultureInitalizer.Run(world);
            IdeologyInitializer.Run(world);
            ReligionInitalizer.Run(world);
            IdeologyInitializer.Run(world);
            SpeciesInitalizer.Run(world);
        }
    }
}