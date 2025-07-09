using System;
using System.CommandLine;
using System.Threading.Tasks;
using AyanamisTower.Email;

namespace AyanamisTower.EmailSenderCLI;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <returns>A task that represents the asynchronous operation, returning an integer exit code.</returns>
    static async Task<int> Main(string[] args)
    {
        // Define the command-line arguments and options.
        var subjectArgument = new Argument<string>("subject", "The subject of the email.");
        var messageArgument = new Argument<string>("message", "The content of the email message.");
        var levelOption = new Option<StatusLevel>("--level", () => StatusLevel.Info, "The status level of the message (Info, Success, Warning, Error).");

        // Create the root command for the application.
        var rootCommand = new RootCommand("A command-line tool to send status emails.");
        rootCommand.AddArgument(subjectArgument);
        rootCommand.AddArgument(messageArgument);
        rootCommand.AddOption(levelOption);

        // Set the handler for the root command.
        rootCommand.SetHandler(async (string subject, string message, StatusLevel level) =>
        {
            try
            {
                // Validate environment variables before attempting to send
                if (!ValidateEnvironmentVariables())
                {
                    Environment.Exit(1);
                }

                Console.WriteLine($"Sending {level} email...");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Level: {level}");

                // Create the email service and send the message
                var emailService = new EmailStatusService();
                await emailService.SendStatusUpdateAsync(subject, message, level);

                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                Console.WriteLine("\nTroubleshooting:");
                Console.WriteLine("1. Ensure environment variables are set in ~/.bashrc");
                Console.WriteLine("2. Run 'source ~/.bashrc' to reload environment");
                Console.WriteLine("3. Verify Gmail App Password is correct");
                Console.WriteLine("4. Check internet connectivity");
                Environment.Exit(1);
            }
        }, subjectArgument, messageArgument, levelOption);

        // Add convenience subcommands
        AddConvenienceCommands(rootCommand);

        // Execute the command
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Validates that all required environment variables are set.
    /// </summary>
    /// <returns>True if all required environment variables are present, false otherwise.</returns>
    private static bool ValidateEnvironmentVariables()
    {
        var requiredVars = new[]
        {
            "EMAIL_FROM",
            "EMAIL_PASSWORD",
            "EMAIL_SMTP_SERVER",
            "EMAIL_SMTP_PORT"
        };

        bool allPresent = true;

        foreach (var varName in requiredVars)
        {
            var value = Environment.GetEnvironmentVariable(varName);
            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine($"Error: Environment variable '{varName}' is not set.");
                allPresent = false;
            }
        }

        if (!allPresent)
        {
            Console.WriteLine("\nPlease ensure the following environment variables are set in ~/.bashrc:");
            Console.WriteLine("export EMAIL_FROM=\"your-email@gmail.com\"");
            Console.WriteLine("export EMAIL_PASSWORD=\"your-app-password\"");
            Console.WriteLine("export EMAIL_SMTP_SERVER=\"smtp.gmail.com\"");
            Console.WriteLine("export EMAIL_SMTP_PORT=\"587\"");
            Console.WriteLine("export EMAIL_TO=\"your-email@gmail.com\"");
            Console.WriteLine("\nThen run: source ~/.bashrc");
        }

        return allPresent;
    }

    /// <summary>
    /// Adds convenience subcommands for different status levels.
    /// </summary>
    /// <param name="rootCommand">The root command to add subcommands to.</param>
    private static void AddConvenienceCommands(RootCommand rootCommand)
    {
        // Info command
        var infoCommand = new Command("info", "Send an info status email");
        var infoSubject = new Argument<string>("subject", "The subject of the email.");
        var infoMessage = new Argument<string>("message", "The content of the email message.");
        infoCommand.AddArgument(infoSubject);
        infoCommand.AddArgument(infoMessage);
        infoCommand.SetHandler(async (string subject, string message) =>
        {
            await SendWithLevel(subject, message, StatusLevel.Info);
        }, infoSubject, infoMessage);

        // Success command
        var successCommand = new Command("success", "Send a success status email");
        var successSubject = new Argument<string>("subject", "The subject of the email.");
        var successMessage = new Argument<string>("message", "The content of the email message.");
        successCommand.AddArgument(successSubject);
        successCommand.AddArgument(successMessage);
        successCommand.SetHandler(async (string subject, string message) =>
        {
            await SendWithLevel(subject, message, StatusLevel.Success);
        }, successSubject, successMessage);

        // Warning command
        var warningCommand = new Command("warning", "Send a warning status email");
        var warningSubject = new Argument<string>("subject", "The subject of the email.");
        var warningMessage = new Argument<string>("message", "The content of the email message.");
        warningCommand.AddArgument(warningSubject);
        warningCommand.AddArgument(warningMessage);
        warningCommand.SetHandler(async (string subject, string message) =>
        {
            await SendWithLevel(subject, message, StatusLevel.Warning);
        }, warningSubject, warningMessage);

        // Error command
        var errorCommand = new Command("error", "Send an error status email");
        var errorSubject = new Argument<string>("subject", "The subject of the email.");
        var errorMessage = new Argument<string>("message", "The content of the email message.");
        errorCommand.AddArgument(errorSubject);
        errorCommand.AddArgument(errorMessage);
        errorCommand.SetHandler(async (string subject, string message) =>
        {
            await SendWithLevel(subject, message, StatusLevel.Error);
        }, errorSubject, errorMessage);

        // Test command
        var testCommand = new Command("test", "Send a test email to verify configuration");
        testCommand.SetHandler(async () =>
        {
            await SendWithLevel("CLI Test", "This is a test email from the CLI application.", StatusLevel.Info);
        });

        // Add all subcommands to root
        rootCommand.AddCommand(infoCommand);
        rootCommand.AddCommand(successCommand);
        rootCommand.AddCommand(warningCommand);
        rootCommand.AddCommand(errorCommand);
        rootCommand.AddCommand(testCommand);
    }

    /// <summary>
    /// Helper method to send an email with a specific status level.
    /// </summary>
    /// <param name="subject">The email subject.</param>
    /// <param name="message">The email message.</param>
    /// <param name="level">The status level.</param>
    private static async Task SendWithLevel(string subject, string message, StatusLevel level)
    {
        try
        {
            if (!ValidateEnvironmentVariables())
            {
                Environment.Exit(1);
            }

            Console.WriteLine($"Sending {level} email...");
            var emailService = new EmailStatusService();
            await emailService.SendStatusUpdateAsync(subject, message, level);
            Console.WriteLine("Email sent successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
            Environment.Exit(1);
        }
    }
}