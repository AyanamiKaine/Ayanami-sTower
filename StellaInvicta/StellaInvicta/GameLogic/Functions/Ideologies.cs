using Flecs.NET.Core;

namespace StellaInvicta.GameLogic.Functions
{
    public static class Ideologies
    {
        public static Entity Get(World world, string ideologyName)
        {
            string pathToIdeologyEntity = "StellaInvicta.Module.PreDefinedEntities." + ideologyName;
            return world.Lookup(pathToIdeologyEntity);
        }
    }
}