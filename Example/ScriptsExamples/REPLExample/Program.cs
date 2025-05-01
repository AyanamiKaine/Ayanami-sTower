using System;
using System.Collections.Concurrent; // For BlockingCollection
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading; // For Thread and CancellationTokenSource
using CSScriptLib;
using Flecs.NET.Bindings; // Assuming these are correct for your setup
using Flecs.NET.Core;

/// <summary>
/// Repl example
/// </summary>
public static class ReplExample
{
    // Store active using statements (thread-safe access not strictly needed as only main thread modifies)
    private static readonly List<string> activeUsings =
    [
        "System",
        "System.Linq",
        "System.Collections.Generic",
        "Flecs.NET.Core", // Make sure correct Flecs namespaces are included
        "Flecs",
        // Add any other default usings here
    ];

    // Thread-safe queue for communication between input thread and main thread
    private static readonly BlockingCollection<string> inputQueue = [];

    // Cancellation token source to signal exit
    private static readonly CancellationTokenSource cancellationTokenSource = new();

    // Name for the injected context property to access the World
    private const string WorldContextPropertyName = "WorldInstance";

    /// <summary>
    /// ECS WORLD - Created and managed by the main application
    /// </summary>
    public static World World { get; private set; } = World.Create();

    /// <summary>
    /// Main
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
        // --- Flecs Initialization (Main Thread) ---
        try
        {
            // Add any initial Flecs setup for your application here
            World.Import<Ecs.Stats>(); // Example
            World.Set(default(flecs.EcsRest));
            Console.WriteLine("Flecs world initialized.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error initializing Flecs: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
            return;
        }

        // --- Create Evaluator (Main Thread) ---
        // Use ReferenceDomainAssemblies to make types from the main app available.
        IEvaluator evaluator = CSScript.Evaluator.ReferenceDomainAssemblies();

        // --- Start the REPL Session (Blocking Call) ---
        // The StartFlecsRepl method now contains the main REPL loop.
        // It will run until 'exit' or 'quit' is entered.
        evaluator.StartStaticWorldRepl("ReplExample.World", () => World.Progress()); // Pass the created world instance

        // --- Application Cleanup (After REPL Exits) ---
        Console.WriteLine("\nMain application cleaning up...");
        World.Dispose(); // Dispose the world if it was created
        (evaluator as IDisposable)?.Dispose(); // Dispose evaluator if possible
        Console.WriteLine("Main application cleanup complete. Press Enter to exit.");
        Console.ReadLine();
    }

    // Method to run on the dedicated input thread
    private static void ReadInputLoop()
    {
        // Display initial prompt from this thread as main thread might be busy
        Console.Write("> ");

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                // Blocking call - waits for user input
                string? input = Console.ReadLine();

                if (input != null)
                {
                    // Add the received input to the queue for the main thread
                    inputQueue.Add(input, cancellationTokenSource.Token);
                }
                else
                {
                    // ReadLine returned null (e.g., end of stream/Ctrl+Z), treat as exit
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        cancellationTokenSource.Cancel(); // Signal main thread
                        inputQueue.CompleteAdding(); // Stop adding more items
                    }
                    break; // Exit input loop
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested while Add is waiting
                break; // Exit loop gracefully
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Console.ReadLine"))
            {
                // Handle cases where console input might become unavailable
                Console.WriteLine("\nConsole input stream closed or unavailable.");
                if (!cancellationTokenSource.IsCancellationRequested)
                    cancellationTokenSource.Cancel();
                inputQueue.CompleteAdding();
                break;
            }
            catch (Exception ex) // Catch unexpected errors on input thread
            {
                Console.WriteLine($"\nError in input thread: {ex.Message}");
                if (!cancellationTokenSource.IsCancellationRequested)
                    cancellationTokenSource.Cancel(); // Signal main thread about the problem
                inputQueue.CompleteAdding();
                break; // Exit loop
            }
        }
        Console.WriteLine("Input thread finished.");
    }

    // --- Refactored Helper Methods (Executed on Main Thread) ---

    private static void HandleUsingDirective(string trimmedInput)
    {
        string potentialNamespace = trimmedInput["using ".Length..].TrimEnd(';');
        if (
            !string.IsNullOrWhiteSpace(potentialNamespace)
            && !activeUsings.Contains(potentialNamespace)
        )
        {
            activeUsings.Add(potentialNamespace);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Added using: {potentialNamespace}");
            Console.ResetColor();
            PrintCurrentUsings();
        }
        else if (activeUsings.Contains(potentialNamespace))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Using '{potentialNamespace}' is already active.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Invalid using statement.");
            Console.ResetColor();
        }
    }

    // Returns true if evaluation was attempted (success or definite error), false if likely incomplete
    private static bool TryEvaluateSingleLine(IEvaluator evaluator, StringBuilder buffer)
    {
        string codeToEvaluate = PrepareCodeWithUsings(buffer.ToString());
        bool evaluationAttempted = true;

        try
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("\nEvaluating..."); // Newline for cleaner output
            Console.ResetColor();

            object? result = evaluator.Eval(codeToEvaluate);

            if (result != null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(result);
                Console.ResetColor();
            }
            buffer.Clear(); // Clear buffer on success
        }
        catch (Exception ex)
        {
            bool likelyIncomplete = IsLikelyIncompleteError(ex.Message);
            if (likelyIncomplete)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Info: Input potentially incomplete, awaiting more lines...");
                Console.ResetColor();
                evaluationAttempted = false; // Signal that buffer should not be cleared
                // DO NOT clear codeBuffer
            }
            else
            {
                PrintEvaluationError(ex);
                buffer.Clear(); // Clear buffer on definite error
            }
        }
        return evaluationAttempted;
    }

    // (PrepareCodeWithUsings, EvaluateCodeBuffer, IsLikelyIncompleteError, PrintCurrentUsings, PrintEvaluationError remain largely the same)
    // Minor change: Added newline before "Evaluating..." messages for better console layout

    static string PrepareCodeWithUsings(string code)
    {
        StringBuilder finalCode = new();
        // 1. Standard Usings first
        foreach (string usingNamespace in activeUsings)
        {
            finalCode.AppendLine($"using {usingNamespace};");
        }

        // 2. Context Class Definition (AFTER usings, BEFORE user code)
        //    Using WorldContextPropertyName for easier reference/change.
        finalCode.AppendLine(
            $"public static class __REPL_CONTEXT__ {{ public static Flecs.NET.Core.World {WorldContextPropertyName} => ReplExample.World; }}"
        );

        // 3. Append user code directly AFTER the class definition
        finalCode.Append(code);

        // Note: No "using static __REPL_CONTEXT__;" is added here.
        // User must explicitly use __REPL_CONTEXT__.WorldInstance

        // Uncomment for debugging the generated code:
        // Console.WriteLine($"--- Code Sent to Eval ---\n{finalCode}\n-------------------------");

        return finalCode.ToString();
    }

    static void EvaluateCodeBuffer(IEvaluator evaluator, StringBuilder buffer)
    {
        if (buffer.Length == 0)
            return;
        string userCode = buffer.ToString();
        buffer.Clear();

        string codeToEvaluate = PrepareCodeWithUsings(userCode);

        try
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("\nEvaluating block..."); // Newline for cleaner output
            Console.ResetColor();
            object? result = evaluator.Eval(codeToEvaluate);
            if (result != null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(result);
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            PrintEvaluationError(ex);
        }
    }

    static bool IsLikelyIncompleteError(string errorMessage)
    {
        errorMessage = errorMessage.ToLowerInvariant();
        return errorMessage.Contains("expected ;")
            || errorMessage.Contains("expected }")
            || errorMessage.Contains("expected {")
            || errorMessage.Contains("unexpected end of file")
            || errorMessage.Contains("unterminated string literal")
            || errorMessage.Contains("body expected")
            || errorMessage.Contains("cs1513") // } expected
            || errorMessage.Contains("cs1002") // ; expected
            || errorMessage.Contains("cs1525"); // Invalid expression term
    }

    static void PrintCurrentUsings()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Active usings:");
        if (activeUsings.Count == 0)
            Console.WriteLine("  (none)");
        else
            foreach (string u in activeUsings)
                Console.WriteLine($"  using {u};");
        Console.ResetColor();
    }

    static void PrintEvaluationError(Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        string message = ex.Message;
        // Simplified error extraction
        if (ex is CSScriptLib.CompilerException compEx && compEx.Message.Contains("error CS"))
        {
            var errorLine = compEx
                .Message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(line => line.Contains("error CS"));
            if (errorLine != null)
                message = errorLine;
        }
        else if (
            ex is System.Reflection.TargetInvocationException tie
            && tie.InnerException != null
        )
        {
            // Show the actual exception thrown by the evaluated code
            message =
                $"Runtime Error: {tie.InnerException.GetType().Name}: {tie.InnerException.Message}";
            // Optionally add stack trace from InnerException here if needed
        }

        Console.WriteLine($"\nError: {message}"); // Newline for cleaner output
        // Console.WriteLine($"Debug Info: {ex.ToString()}");
        Console.ResetColor();
    }
}
