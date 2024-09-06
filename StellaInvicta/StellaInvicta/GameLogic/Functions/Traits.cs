using Flecs.NET.Core;

namespace StellaInvicta.GameLogic.Functions
{
    public static class Traits
    {
        public static Entity Get(World world, string traitName)
        {
            string pathToTraitEntity = "StellaInvicta.Module.PreDefinedEntities." + traitName;
            return world.Lookup(pathToTraitEntity);
        }
    }
}