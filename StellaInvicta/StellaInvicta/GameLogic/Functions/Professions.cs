using Flecs.NET.Core;

namespace StellaInvicta.GameLogic.Functions
{
    public static class Professions
    {
        public static Entity Get(World world, string professionName)
        {
            string pathToProfessionEntity = "StellaInvicta.Module.PreDefinedEntities." + professionName;
            return world.Lookup(pathToProfessionEntity);
        }
    }
}