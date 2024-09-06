using Flecs.NET.Core;

namespace StellaInvicta.GameLogic.Functions
{
    public static class Religions
    {
        public static Entity Get(World world, string religionName)
        {
            string pathToReligionEntity = "StellaInvicta.Module.PreDefinedEntities." + religionName;
            return world.Lookup(pathToReligionEntity);
        }
    }
}