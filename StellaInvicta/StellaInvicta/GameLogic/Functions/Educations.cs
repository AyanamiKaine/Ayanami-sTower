using Flecs.NET.Core;

namespace StellaInvicta.GameLogic.Functions
{
    public static class Educations
    {
        public static Entity Get(World world, string educationName)
        {
            string pathToEducationEntity = "StellaInvicta.Module.PreDefinedEntities." + educationName;
            return world.Lookup(pathToEducationEntity);
        }
    }
}