using System;
using System.Collections.Generic; // For List<string>
using System.Linq; // For Linq methods if needed later
using System.Text;
using CSScriptLib;
using Flecs.NET.Core;

// Note: CSScriptLib.Extensions might not be needed if not using specific extension methods like AddUsings
/// <summary>
/// Test
/// </summary>
public static class ReplExample
{
    // Store active using statements
    private static readonly List<string> activeUsings =
    [
        "System",
        "System.Linq",
        "System.Collections.Generic",
        "Flecs",
        // Add any other default usings here
    ];

    /// <summary>
    /// ECS WORLD
    /// </summary>
    public static World World { get; } = World.Create();

    /// <summary>
    /// Main
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
        Console.WriteLine("CS-Script REPL Example (Multi-line support)");
        Console.WriteLine("Enter C# code. Multi-line blocks require an empty line to finish.");
        Console.WriteLine("Single lines and 'using' directives are processed immediately.");
        Console.WriteLine("Type '#usings' to see current usings, '#clear' to clear buffer.");
        Console.WriteLine("Type 'exit' or 'quit' to exit.");
        Console.WriteLine("------------------------------------");

        // 1. Create a persistent evaluator instance
        // We don't add usings here anymore, we'll manage them separately
        IEvaluator evaluator = CSScript.Evaluator.ReferenceDomainAssemblies(); // Start with common assemblies

        // Print initial usings
        PrintCurrentUsings();

        // 2. StringBuilder to accumulate multi-line input
        StringBuilder codeBuffer = new StringBuilder();

        string? input;
        do
        {
            // 3. Adjust prompt
            Console.Write(codeBuffer.Length == 0 ? "> " : ".. ");
            input = Console.ReadLine();
            string trimmedInput = input?.Trim() ?? string.Empty;

            // --- Handle REPL Commands ---
            if (codeBuffer.Length == 0) // Commands only work when buffer is empty
            {
                if (
                    trimmedInput.Equals("exit", StringComparison.InvariantCultureIgnoreCase)
                    || trimmedInput.Equals("quit", StringComparison.InvariantCultureIgnoreCase)
                )
                    break;
                if (trimmedInput.Equals("#usings", StringComparison.InvariantCultureIgnoreCase))
                {
                    PrintCurrentUsings();
                    continue;
                }
                if (trimmedInput.Equals("#clear", StringComparison.InvariantCultureIgnoreCase))
                {
                    codeBuffer.Clear();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Input buffer cleared.");
                    Console.ResetColor();
                    continue;
                }
            }

            // 5. Check for explicit end of multi-line input (empty line)
            if (string.IsNullOrWhiteSpace(input))
            {
                if (codeBuffer.Length > 0)
                {
                    // Pass the current evaluator instance
                    EvaluateCodeBuffer(evaluator, codeBuffer);
                }
                continue;
            }

            // --- Input is NOT empty ---

            // 6. SPECIAL HANDLING for 'using' directives
            if (trimmedInput.StartsWith("using ") && codeBuffer.Length == 0) // Handle only when entered on its own line
            {
                // Extract namespace(s) - basic parsing
                string potentialNamespace = trimmedInput["using ".Length..].TrimEnd(';');
                if (
                    !string.IsNullOrWhiteSpace(potentialNamespace)
                    && !activeUsings.Contains(potentialNamespace)
                )
                {
                    activeUsings.Add(potentialNamespace); // Add to our list
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Added using: {potentialNamespace}");
                    Console.ResetColor();
                    PrintCurrentUsings(); // Show updated list
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
                // 'using' handled, don't add to buffer or try to eval further this iteration
                continue;
            }

            // 7. Append the non-empty line to the buffer (if not handled as 'using' or command)
            bool wasBufferEmpty = codeBuffer.Length == 0;
            codeBuffer.AppendLine(input);

            // 8. If the buffer was empty before adding this line, try immediate evaluation
            if (wasBufferEmpty)
            {
                try
                {
                    // Pass the current evaluator instance
                    string codeToEvaluate = PrepareCodeWithUsings(codeBuffer.ToString());

                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("Evaluating...");
                    Console.ResetColor();

                    object result = evaluator.Eval(codeToEvaluate);

                    // Success! Print result and clear buffer.
                    if (result != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(result);
                        Console.ResetColor();
                    }
                    codeBuffer.Clear(); // Clear buffer after successful single-line evaluation
                }
                catch (Exception ex)
                {
                    // Evaluation failed. Check if likely incomplete.
                    // More robust check: Look for specific compiler error codes if possible,
                    // otherwise stick to message heuristics.
                    bool likelyIncomplete = IsLikelyIncompleteError(ex.Message);

                    if (likelyIncomplete)
                    {
                        // Assume starting multi-line block. Keep buffer.
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        // Only show error details if verbose mode is desired
                        // Console.WriteLine($"Info: Input potentially incomplete, awaiting more lines... ({ex.Message})");
                        Console.WriteLine(
                            $"Info: Input potentially incomplete, awaiting more lines..."
                        );
                        Console.ResetColor();
                        // DO NOT clear codeBuffer
                    }
                    else
                    {
                        // Treat as a standard error for a single line. Print and clear buffer.
                        PrintEvaluationError(ex);
                        codeBuffer.Clear();
                    }
                }
            }
            // If buffer was not empty, continue loop, wait for empty line trigger.
        } while (true);

        Console.WriteLine("Exiting REPL.");
        (evaluator as IDisposable)?.Dispose();
    }

    // Helper to prepend active using statements to the code
    static string PrepareCodeWithUsings(string code)
    {
        StringBuilder finalCode = new();
        foreach (string usingNamespace in activeUsings)
        {
            finalCode.AppendLine($"using {usingNamespace};");
        }
        finalCode.Append(code); // Append the actual user code
        return finalCode.ToString();
    }

    // Updated EvaluateCodeBuffer to use the helper
    static void EvaluateCodeBuffer(IEvaluator evaluator, StringBuilder buffer)
    {
        if (buffer.Length == 0)
            return;
        string userCode = buffer.ToString();
        buffer.Clear(); // Clear buffer immediately

        string codeToEvaluate = PrepareCodeWithUsings(userCode);

        try
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Evaluating block...");
            // Console.WriteLine($"--- Code ---\n{codeToEvaluate}\n------------"); // Uncomment for debugging
            Console.ResetColor();
            object result = evaluator.Eval(codeToEvaluate);
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

    // Heuristic check for incomplete code errors
    static bool IsLikelyIncompleteError(string errorMessage)
    {
        errorMessage = errorMessage.ToLowerInvariant();
        // Add more specific error codes or messages as needed
        return errorMessage.Contains("expected ;")
            || errorMessage.Contains("expected }")
            || errorMessage.Contains("expected {")
            || errorMessage.Contains("unexpected end of file")
            || errorMessage.Contains("unterminated string literal")
            || errorMessage.Contains("body expected") // Often follows method/class declaration
            || errorMessage.Contains("cs1513") // } expected
            || errorMessage.Contains("cs1002") // ; expected
            || errorMessage.Contains("cs1525"); // Invalid expression term
    }

    static void PrintCurrentUsings()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Active usings:");
        if (activeUsings.Count == 0)
        {
            Console.WriteLine("  (none)");
        }
        else
        {
            foreach (string u in activeUsings)
            {
                Console.WriteLine($"  using {u};");
            }
        }
        Console.ResetColor();
    }

    static void PrintEvaluationError(Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        // Improve error message display - remove the script path noise if present
        string message = ex.Message;
        if (ex is CSScriptLib.CompilerException compEx && compEx.Message.Contains("error CS"))
        {
            // Try to extract just the core error message(s)
            var errorLines = message
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.Contains("error CS") || line.Contains("warning CS"))
                .Select(line =>
                    line.Substring(
                        line.IndexOf("error CS", StringComparison.OrdinalIgnoreCase) > 0
                            ? line.IndexOf("error CS", StringComparison.OrdinalIgnoreCase)
                            : line.IndexOf("warning CS", StringComparison.OrdinalIgnoreCase)
                    )
                );
            if (errorLines.Any())
            {
                message = string.Join("\n", errorLines);
            }
            // else fallback to full message
        }

        Console.WriteLine($"Error: {message}");
        // Console.WriteLine($"Debug Info: {ex.ToString()}"); // Uncomment for full stack trace
        Console.ResetColor();
    }
}
