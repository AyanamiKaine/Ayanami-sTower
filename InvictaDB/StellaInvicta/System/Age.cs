// The age system should increment the age of characters based on the current game date and their birthdate.
using InvictaDB;
using InvictaDB.Messaging;
using StellaInvicta.Data;
using static InvictaDB.Messaging.GameMessages;

namespace StellaInvicta.System.Age;

/// <summary>
/// Manages character aging based on the game date.
/// Characters age when the current game date passes their birthday.
/// </summary>
public class AgeSystem : ISystem
{
    /// <summary>
    /// The sender name for the age system.
    /// </summary>
    public const string AgeSystemSender = "Age System";

    /// <inheritdoc/>
    public string Name => AgeSystemSender;

    /// <inheritdoc/>
    public string Description => "Manages character aging based on the game date.";

    /// <inheritdoc/>
    public string Author => "InvictaDB Team";

    /// <inheritdoc/>
    public bool Enabled { get; set; } = true;

    /// <inheritdoc/>
    public bool IsInitialized { get; set; }

    /// <inheritdoc/>
    public InvictaDatabase Initialize(InvictaDatabase db)
    {
        IsInitialized = true;
        // Register the Character table if not already registered
        if (!db.TableExists<Character>())
        {
            db = db.RegisterTable<Character>();
        }
        return db;
    }

    /// <inheritdoc/>
    public InvictaDatabase Shutdown(InvictaDatabase db)
    {
        // No specific shutdown logic needed for the age system.
        return db;
    }

    /// <inheritdoc/>
    public InvictaDatabase Run(InvictaDatabase db)
    {
        // Get NewDay messages to check for birthdays (don't consume - other systems may need them)
        var newDayMessages = db.Messages.GetMessages<NewDay>().ToList();

        if (newDayMessages.Count == 0)
        {
            return db; // No new day, nothing to do
        }

        // Process each NewDay message (usually just one per tick)
        foreach (var message in newDayMessages)
        {
            if (message.Payload is NewDay newDay)
            {
                db = ProcessBirthdays(db, newDay.NewDate);
            }
        }

        return db;
    }

    /// <summary>
    /// Processes birthdays for all characters on the given date.
    /// </summary>
    /// <param name="db">The database.</param>
    /// <param name="currentDate">The current game date.</param>
    /// <returns>The updated database.</returns>
    private InvictaDatabase ProcessBirthdays(InvictaDatabase db, DateTime currentDate)
    {
        var characters = db.GetTable<Character>();

        // Find characters whose birthday is today (same month and day)
        var birthdayCharacters = characters
            .Where(kvp => IsBirthday(kvp.Value.BirthDate, currentDate))
            .ToList();

        if (birthdayCharacters.Count == 0)
        {
            return db; // No birthdays today
        }

        // Use batch operations to update all birthday characters in a single state transition
        return db.Batch(batch =>
        {
            foreach (var kvp in birthdayCharacters)
            {
                var character = kvp.Value;
                var newAge = CalculateAge(character.BirthDate, currentDate);

                // Only update if age actually changed (handles edge cases)
                if (newAge != character.Age)
                {
                    var updatedCharacter = character with { Age = newAge };
                    batch.Insert(kvp.Key, updatedCharacter);

                    // Send a birthday message
                    batch.SendMessage(Name, new CharacterBirthday(kvp.Key, character.Name, newAge, currentDate));
                }
            }
        });
    }

    /// <summary>
    /// Checks if today is the character's birthday.
    /// </summary>
    /// <param name="birthDate">The character's birth date.</param>
    /// <param name="currentDate">The current date.</param>
    /// <returns>True if today is the character's birthday.</returns>
    private static bool IsBirthday(DateTime birthDate, DateTime currentDate)
    {
        return birthDate.Month == currentDate.Month && birthDate.Day == currentDate.Day;
    }

    /// <summary>
    /// Calculates the age of a character based on birth date and current date.
    /// </summary>
    /// <param name="birthDate">The character's birth date.</param>
    /// <param name="currentDate">The current date.</param>
    /// <returns>The character's age in years.</returns>
    private static int CalculateAge(DateTime birthDate, DateTime currentDate)
    {
        var age = currentDate.Year - birthDate.Year;

        // Adjust if birthday hasn't occurred yet this year
        if (currentDate.Month < birthDate.Month ||
            (currentDate.Month == birthDate.Month && currentDate.Day < birthDate.Day))
        {
            age--;
        }

        return age;
    }

    /// <summary>
    /// Message sent when a character celebrates a birthday.
    /// </summary>
    /// <param name="CharacterId">The character's ID.</param>
    /// <param name="CharacterName">The character's name.</param>
    /// <param name="NewAge">The character's new age.</param>
    /// <param name="Date">The date of the birthday.</param>
    public record CharacterBirthday(string CharacterId, string CharacterName, int NewAge, DateTime Date);
}
