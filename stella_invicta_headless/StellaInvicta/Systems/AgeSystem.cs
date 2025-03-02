using Flecs.NET.Core;
using NLog;
using NLog.LayoutRenderers;
using StellaInvicta.Components;
using StellaInvicta.Tags.Relationships;

namespace StellaInvicta.Systems;

/// <summary>
/// Ensures that with passing time when the birthday of an entity is reached its age increases.
/// </summary>
public class AgeSystem() : ISystem
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    Entity systemEntity;

    /// <summary>
    /// Enables the Age system that calculates and updates entity ages based on their birthday and current game date.
    /// </summary>
    /// <param name="world">The world containing the entities to process.</param>
    /// <param name="simulationSpeed">The timer entity that controls the tick rate of this system.</param>
    /// <returns>The system entity that was enabled.</returns>
    public Entity Enable(World world, TimerEntity simulationSpeed)
    {
        return systemEntity = world.System<Age>("AgeSystem")
            .With<Birthday, GameDate>() // Look for Birthday relationship to GameDate
            .TickSource(simulationSpeed)
            .Each((Entity e, ref Age age) =>
            {
                var gameDate = world.Get<GameDate>();
                var birthday = e.GetSecond<Birthday, GameDate>();

                // Calculate age based on years difference
                int calculatedAge = gameDate.Year - birthday.Year;

                // Adjust age if birthday hasn't occurred yet this year
                if (gameDate.Month < birthday.Month ||
                    (gameDate.Month == birthday.Month && gameDate.Day < birthday.Day))
                {
                    calculatedAge--;
                }

                // Update age if it's different from the calculated age
                if (age.Value != calculatedAge)
                {
                    Logger.ConditionalDebug($"Updating age for {e.Name()} from {age.Value} to {calculatedAge}");
                    age.Value = calculatedAge;
                }
            }).Enable();
    }

    /// <summary>
    /// Disables the age system.
    /// </summary>
    public void Disable()
    {
        systemEntity.Disable();
    }
}