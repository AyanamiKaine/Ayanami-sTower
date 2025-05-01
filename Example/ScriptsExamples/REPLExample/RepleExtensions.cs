using System.Collections.Concurrent;
using System.Text;
using CSScriptLib;

/// <summary>
/// Contains the extension method to start a Flecs-aware REPL session,
/// assuming the Flecs World is accessible via a known static property (e.g., ReplExample.World).
/// </summary>
public static class ReplExtensions
{
    // Note: No context class or property name needed anymore.

    /// <summary>
    /// Starts an interactive REPL session that runs World.Progress() on the same thread.
    /// Assumes the Flecs World is accessible via a known static property (defined in the main application).
    /// Handles asynchronous console input and ensures evaluation happens on the calling thread.
    /// This method BLOCKS the calling thread until the REPL exits.
    /// </summary>
    /// <param name="evaluator">The CS-Script evaluator instance to use.</param>
    /// <param name="staticWorldAccessor">The full static path to access the World instance (e.g., "YourApp.StaticClass.World").</param>
    /// <param name="worldProgressAction">An action that calls World.Progress() on the correct instance.</param>
    /// <param name="defaultUsings">Optional list of default namespaces to include.</param>
    /// <param name="progressFrequency">Approximate time between World.Progress calls. Default is 16ms (~60 FPS).</param>
    public static void StartStaticWorldRepl(
        this IEvaluator evaluator,
        string staticWorldAccessor, // e.g., "ReplExample.World"
        Action worldProgressAction, // Action to call World.Progress()
        IEnumerable<string>? defaultUsings = null,
        TimeSpan? progressFrequency = null
    )
    {
        // --- REPL State Initialization ---
        var activeUsings = new List<string>(
            defaultUsings
                ??
                [
                    "System",
                    "System.Linq",
                    "System.Collections.Generic",
                    "Flecs.NET.Core",
                    "Flecs.NET.Bindings",
                ]
        );
        var inputQueue = new BlockingCollection<string>();
        using var cancellationTokenSource = new CancellationTokenSource();
        TimeSpan freq = progressFrequency ?? TimeSpan.FromMilliseconds(16);

        // --- REPL Setup Output ---
        Console.WriteLine(
            "CS-Script REPL Session Started (Async Input, Integrated World.Progress)"
        );
        Console.WriteLine(
            $"World.Progress() called by REPL loop approximately every {freq.TotalMilliseconds}ms."
        );
        Console.WriteLine("Enter C# code. Multi-line blocks require an empty line to finish.");
        Console.WriteLine("Single lines and 'using' directives are processed immediately.");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(
            $"Access the Flecs world instance using its static path: {staticWorldAccessor}"
        ); // Use provided path
        Console.ResetColor();
        Console.WriteLine("Type '#usings' to see current usings, '#clear' to clear buffer/input.");
        Console.WriteLine("Type 'exit' or 'quit' to exit.");
        Console.WriteLine("------------------------------------");

        PrintCurrentUsings(activeUsings);

        // --- Start Input Thread ---
        Thread inputThread = new Thread(
            () => ReadInputLoop(inputQueue, cancellationTokenSource.Token)
        )
        {
            IsBackground = true,
            Name = "ConsoleInputThread",
        };
        inputThread.Start();

        // --- Main REPL Loop (Calling Thread) ---
        StringBuilder codeBuffer = new StringBuilder();
        bool exitRequested = false;
        DateTime lastProgressTime = DateTime.UtcNow;

        while (!exitRequested && !cancellationTokenSource.IsCancellationRequested)
        {
            DateTime loopStartTime = DateTime.UtcNow;

            // 1. Process Flecs World using the provided action
            if (loopStartTime - lastProgressTime >= freq)
            {
                try
                {
                    worldProgressAction(); // Use the delegate to call Progress
                    lastProgressTime = loopStartTime;
                }
                catch (Exception ex)
                {
                    PrintEvaluationError(
                        new InvalidOperationException(
                            $"Error during World.Progress via action: {ex.Message}",
                            ex
                        )
                    );
                }
            }

            // 2. Check for and process input
            if (inputQueue.TryTake(out string? input, 0, cancellationTokenSource.Token))
            {
                string trimmedInput = input?.Trim() ?? string.Empty;

                // --- Handle REPL Commands ---
                if (codeBuffer.Length == 0)
                {
                    if (
                        trimmedInput.Equals("exit", StringComparison.InvariantCultureIgnoreCase)
                        || trimmedInput.Equals("quit", StringComparison.InvariantCultureIgnoreCase)
                    )
                    {
                        exitRequested = true;
                        cancellationTokenSource.Cancel();
                        continue;
                    }
                    if (trimmedInput.Equals("#usings", StringComparison.InvariantCultureIgnoreCase))
                    {
                        PrintCurrentUsings(activeUsings);
                        Console.Write("> ");
                        continue;
                    }
                    if (trimmedInput.Equals("#clear", StringComparison.InvariantCultureIgnoreCase))
                    {
                        codeBuffer.Clear();
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("Input buffer cleared.");
                        Console.ResetColor();
                        Console.Write("> ");
                        continue;
                    }
                }

                // --- Handle End of Multi-line Input (Empty Line) ---
                if (string.IsNullOrWhiteSpace(input))
                {
                    if (codeBuffer.Length > 0)
                    {
                        // No context setting needed anymore
                        EvaluateCompleteBuffer(evaluator, codeBuffer, activeUsings);
                        Console.Write("> ");
                    }
                    else
                    {
                        Console.Write("> ");
                    }
                    continue;
                }

                // --- Handle 'using' Directives ---
                if (trimmedInput.StartsWith("using ") && codeBuffer.Length == 0)
                {
                    HandleUsingDirective(trimmedInput, activeUsings);
                    Console.Write("> ");
                    continue;
                }

                // --- Append to Code Buffer ---
                bool wasBufferEmpty = codeBuffer.Length == 0;
                codeBuffer.AppendLine(input);

                // --- Try Immediate Evaluation (Single Line) ---
                if (wasBufferEmpty)
                {
                    // No context setting needed anymore
                    TryEvaluateSingleLine(evaluator, codeBuffer, activeUsings);
                    Console.Write(codeBuffer.Length == 0 ? "> " : ".. ");
                }
                else
                {
                    Console.Write(".. ");
                }
            }
            else // No input available
            {
                // --- Idle Sleep ---
                TimeSpan timeUntilNextProgress = (lastProgressTime + freq) - DateTime.UtcNow;
                TimeSpan sleepDuration = TimeSpan.FromMilliseconds(1);
                if (timeUntilNextProgress > TimeSpan.Zero && timeUntilNextProgress < sleepDuration)
                    sleepDuration = timeUntilNextProgress;
                if (sleepDuration > TimeSpan.Zero)
                    Thread.Sleep(sleepDuration);
            }
        } // End of main REPL loop

        // --- Cleanup ---
        Console.WriteLine("\nExiting REPL session...");
        inputQueue.Dispose();
        Console.WriteLine("REPL session cleanup complete.");
    }

    // --- Private Helper Methods for the Extension ---

    // Input loop remains the same
    private static void ReadInputLoop(BlockingCollection<string> queue, CancellationToken token)
    {
        Console.Write("> ");
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (token.IsCancellationRequested)
                    break;
                string? input = Console.ReadLine();
                if (input != null)
                    queue.Add(input, token);
                else
                {
                    if (!token.IsCancellationRequested)
                        queue.CompleteAdding();
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Console.ReadLine"))
            {
                Console.WriteLine("\nConsole input stream closed.");
                if (!token.IsCancellationRequested)
                    queue.CompleteAdding();
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nInput thread error: {ex.Message}");
                if (!token.IsCancellationRequested)
                    queue.CompleteAdding();
                break;
            }
        }
        Console.WriteLine("Input thread finished.");
    }

    // Using directive handling remains the same
    private static void HandleUsingDirective(string trimmedInput, List<string> activeUsings)
    {
        string potentialNamespace = trimmedInput["using ".Length..].TrimEnd(';');
        if (
            !string.IsNullOrWhiteSpace(potentialNamespace)
            && !activeUsings.Contains(potentialNamespace)
        )
        {
            activeUsings.Add(potentialNamespace);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nAdded using: {potentialNamespace}");
            Console.ResetColor();
            PrintCurrentUsings(activeUsings);
        }
        else if (activeUsings.Contains(potentialNamespace))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\nUsing '{potentialNamespace}' is already active.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nError: Invalid using statement.");
            Console.ResetColor();
        }
    }

    // TryEvaluateSingleLine - Simplified (no world/context needed)
    private static bool TryEvaluateSingleLine(
        IEvaluator evaluator,
        StringBuilder buffer,
        List<string> activeUsings
    )
    {
        string codeToEvaluate = PrepareCodeWithUsings(buffer.ToString(), activeUsings);
        bool evaluationAttempted = true;
        try
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("\nEvaluating...");
            Console.ResetColor();
            object? result = evaluator.Eval(codeToEvaluate); // No host object passed
            if (result != null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(result);
                Console.ResetColor();
            }
            buffer.Clear();
        }
        catch (Exception ex)
        {
            bool likelyIncomplete = IsLikelyIncompleteError(ex.Message);
            if (likelyIncomplete)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Info: Input potentially incomplete...");
                Console.ResetColor();
                evaluationAttempted = false;
            }
            else
            {
                PrintEvaluationError(ex);
                buffer.Clear();
            }
        }
        return evaluationAttempted;
    }

    // PrepareCodeWithUsings - Simplified (no world/context needed)
    private static string PrepareCodeWithUsings(string userCode, List<string> activeUsings)
    {
        StringBuilder finalCode = new();
        foreach (string usingNamespace in activeUsings)
        {
            finalCode.AppendLine($"using {usingNamespace};");
        }
        // No context class added here
        finalCode.Append(userCode);
        return finalCode.ToString();
    }

    // EvaluateCompleteBuffer - Simplified (no world/context needed)
    private static void EvaluateCompleteBuffer(
        IEvaluator evaluator,
        StringBuilder buffer,
        List<string> activeUsings
    )
    {
        if (buffer.Length == 0)
            return;
        string userCode = buffer.ToString();
        buffer.Clear();
        string codeToEvaluate = PrepareCodeWithUsings(userCode, activeUsings);
        try
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("\nEvaluating block...");
            Console.ResetColor();
            object? result = evaluator.Eval(codeToEvaluate); // No host object passed
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

    // TrySetReplContext - REMOVED (no longer needed)

    // IsLikelyIncompleteError remains the same
    private static bool IsLikelyIncompleteError(string errorMessage)
    { /* ... */
        errorMessage = errorMessage.ToLowerInvariant();
        return errorMessage.Contains("expected ;")
            || errorMessage.Contains("expected }")
            || errorMessage.Contains("expected {")
            || errorMessage.Contains("unexpected end of file")
            || errorMessage.Contains("unterminated string literal")
            || errorMessage.Contains("body expected")
            || errorMessage.Contains("cs1513")
            || errorMessage.Contains("cs1002")
            || errorMessage.Contains("cs1525");
    }

    // PrintCurrentUsings remains the same
    private static void PrintCurrentUsings(List<string> activeUsings)
    { /* ... */
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Active usings:");
        if (activeUsings.Count == 0)
            Console.WriteLine("  (none)");
        else
            foreach (string u in activeUsings)
                Console.WriteLine($"  using {u};");
        Console.ResetColor();
    }

    // PrintEvaluationError remains the same
    private static void PrintEvaluationError(Exception ex)
    { /* ... */
        Console.ForegroundColor = ConsoleColor.Red;
        string message = ex.Message;
        if (ex is CSScriptLib.CompilerException compEx && compEx.Message.Contains("error CS"))
        {
            var errorLine = compEx
                .Message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(line => line.Contains("error CS"));
            if (errorLine != null)
                message = errorLine;
        }
        else if (
            ex is System.Reflection.TargetInvocationException tie
            && tie.InnerException != null
        )
        {
            message =
                $"Runtime Error: {tie.InnerException.GetType().Name}: {tie.InnerException.Message}";
        }
        else if (ex.InnerException != null)
        {
            message = $"Error: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
        }
        Console.WriteLine($"\nError: {message}");
        Console.ResetColor();
    }
}
