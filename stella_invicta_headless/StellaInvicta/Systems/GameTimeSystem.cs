using Flecs.NET.Core;
using NLog;
using NLog.LayoutRenderers;
using StellaInvicta.Components;

namespace StellaInvicta.Systems;


/// <summary>
/// System responsible for managing and advancing game time within the game world.
/// </summary>
/// <remarks>
/// This system operates on the GameDate component and handles the progression of time
/// in the game. It provides functionality to enable and disable time advancement.
/// </remarks>
public class GameTimeSystem() : ISystem
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    Entity systemEntity;

    /// <summary>
    /// Enables the game time system and initializes time tracking functionality.
    /// </summary>
    /// <param name="world">The game world instance to enable the system in.</param>
    /// <returns>The system entity that represents the enabled game time system.</returns>
    public Entity Enable(World world)
    {
        return systemEntity = world.System<GameDate>("GameTimeSystem")
            .TermAt(0).Singleton()
            .Each((ref GameDate gameDate) =>
            {
                Logger.ConditionalDebug($"Advancing GameTime by one day | Current Date: {gameDate.GetFormattedDate()}");
                gameDate.AdvanceDay();
            }).Enable();
    }

    /// <summary>
    /// Disables the game time system by deactivating its associated system entity.
    /// </summary>
    public void Disable()
    {
        systemEntity.Disable();
    }
}