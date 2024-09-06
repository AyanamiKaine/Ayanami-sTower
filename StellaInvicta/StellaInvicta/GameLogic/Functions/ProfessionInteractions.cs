using Flecs.NET.Core;

namespace StellaInvicta.GameLogic.Functions
{


    /// <summary>
    /// A list of functions that are related to profession interactions.
    /// Here we create a list of various interactions only a specific 
    /// profression has. // Fleet Commanders cant build factories.
    /// </summary>
    public static class ProfessionInteractions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder">Who builds the factory</param>
        /// <param name="location">Where the factory will be build</param>
        public static void BuildFactory(Entity builder, Entity location)
        {

        }

        public static void RemoveFactory(Entity factory, Entity location)
        {

        }
    }
}