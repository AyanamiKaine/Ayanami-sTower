using Flecs.NET.Core;

namespace StellaInvicta.GameLogic.Functions
{


    /// <summary>
    /// A list of functions that are related to character interactions
    /// This can be interactions between charcters or how characters 
    /// interact with the world
    /// </summary>
    public static class CharacterInteractions
    {
        /// <summary>
        /// Proposes a marriage to an entity, it checks the opinion and
        /// compatbility of <c>a</c> to <c>b</c> entities. If <c>b</c> agress the marriage
        /// will be created.
        /// </summary>
        /// <param name="a"><c>Entity a</c> proposes Marriage to <c>Entity b</c></param>
        /// <param name="b"><c>Entity b</c> receives Marriage proposal from <c>Entity a</c></param>
        public static void ProposeMarriage(Entity a, Entity b)
        {

        }

        /// <summary>
        /// Here <c>characterA</c> wants a divorce with <c>characterB</c>
        /// </summary>
        /// <param name="characterA">wants a divorce with <c>characterB</c></param>
        /// <param name="characterB">gets divorced from <c>characterA</c></param>
        public static void BreakMarriage(Entity characterA, Entity characterB)
        {
            // Here we simply remove the relationship of marriage between the characters.
        }

        public static int CalcualteOpinionBetweenTwoCharacters(Entity characterA, Entity characterB)
        {
            // Check if they have the same traits, culture, ideology,
            // (TODO: THIS SYSTEM DOES NOT YET EXIST) Check for other opinion modifiers, like TriedToKill me negative opinion modifiers.
            return 0;
        }
    }

}