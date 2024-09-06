using Flecs.NET.Core;

namespace StellaInvicta.GameLogic.Functions
{
    public static class Specien
    {
        public static Entity Get(World world, string speciesName)
        {
            string pathToSpeciesEntity = "StellaInvicta.Module.PreDefinedEntities." + speciesName;
            return world.Lookup(pathToSpeciesEntity);
        }
    }
}