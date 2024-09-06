using StellaInvicta.Module;
using Flecs.NET.Core;

namespace StellaInvicta.GameLogic.Functions
{



    public static class Characters
    {
        public static Entity CreateCharacter(World world)
        {
            Entity character = world.Entity()
                                    .Add<Alive>()
                                    .Set<Health>(new())
                                    .Set<PersonalCombatSkill>(new())
                                    .Set<Diplomacy>(new())
                                    .Set<Martial>(new())
                                    .Set<Stewardship>(new())
                                    .Set<Intrigue>(new())
                                    .Set<Learning>(new())
                                    .Set<Fertility>(new())
                                    .Set<Piety>(new(0))
                                    .Set<Wealth>(new(0))
                                    .Set<Prestige>(new(0));
            return character;
        }

        public static Entity Get(World world, string characterName)
        {
            string pathTocharacternEntity = "StellaInvicta.Module.PreDefinedEntities." + characterName;
            return world.Lookup(pathTocharacternEntity);
        }

        /// <summary>
        /// Attaches an trait to a entity character and then returns the character entity
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static Entity AddTrait(Entity character)
        {
            throw new NotImplementedException();
        }

        public static Entity RemoveTrait(Entity character)
        {
            throw new NotImplementedException();
        }

        public static Entity Rename(Entity character)
        {
            throw new NotImplementedException();
        }

        public static void AddPiety(Entity character)
        {
            throw new NotImplementedException();
        }

        public static void SubtractPiety(Entity character)
        {
            throw new NotImplementedException();
        }

        public static void AddPrestige(Entity character)
        {
            throw new NotImplementedException();
        }

        public static void SubtractPrestige(Entity character)
        {
            throw new NotImplementedException();
        }

        public static void AddWealth(Entity character)
        {
            throw new NotImplementedException();
        }

        public static void SubtractWealth(Entity character)
        {
            throw new NotImplementedException();
        }

        public static void ChangeReligion(Entity character)
        {
            throw new NotImplementedException();
        }

        public static void ChangeCulture(Entity character)
        {
            throw new NotImplementedException();
        }

        public static void AddChildren(Entity character)
        {
            throw new NotImplementedException();
        }

        public static void RemoveChildren(Entity character)
        {
            throw new NotImplementedException();
        }

        public static void AddWife(Entity character)
        {
            throw new NotImplementedException();
        }

        public static void RemoveWife(Entity character)
        {
            throw new NotImplementedException();
        }
    }

}