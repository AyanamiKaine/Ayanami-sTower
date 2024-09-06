using Flecs.NET.Core;

namespace StellaInvicta.GameLogic.Functions
{
    public static class Houses
    {
        public static Entity Get(World world, string houseName)
        {
            string pathToHousesEntity = "StellaInvicta.Module.PreDefinedEntities." + houseName;
            return world.Lookup(pathToHousesEntity);
        }
    }
}