using Flecs.NET.Core;

namespace StellaInvicta.GameLogic.Functions
{
    public static class Cultures
    {
        public static Entity Get(World world, string cultureName)
        {
            string pathToCultureEntity = "StellaInvicta.Module.PreDefinedEntities." + cultureName;
            return world.Lookup(pathToCultureEntity);
        }
    }
}