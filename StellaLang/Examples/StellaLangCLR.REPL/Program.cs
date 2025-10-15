// To run this code, create a new .NET Console App and paste this in.
// No external packages are needed.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

/// <summary>
/// A simple, stateful JIT compiler and REPL for a Forth-like language.
/// It maintains a persistent stack of objects, allowing it to handle any C# type.
/// Each line is compiled to CLR bytecode that calls a C# standard library (ForthOperations)
/// to manipulate the shared stack.
/// </summary>
public static class ForthJitInterpreter
{
    private static readonly Dictionary<string, Action<Stack<object>>> UserDefinedWords = new Dictionary<string, Action<Stack<object>>>(StringComparer.OrdinalIgnoreCase);


    public static void Main(string[] args)
    {
        Console.WriteLine("--- CLR Forth Stateful JIT Interpreter (Type-Agnostic) ---");
        Console.WriteLine("Handles int, double, and string types. Enter words or numbers.");
        Console.WriteLine("Example: type '\"hello world\" .ToUpper', then '123 .ToString'.");
        Console.WriteLine("Type 'exit' to quit.");

        var compiler = new ForthCompiler();
        // The data stack now holds 'object', allowing for any type.
        var dataStack = new Stack<object>();
        ForthOperations.UserWords = UserDefinedWords;

        // REPL (Read-Eval-Print Loop)
        while (true)
        {
            string stackState = string.Join(" ", dataStack.Reverse().Select(o => o is string ? $"\"{o}\"" : (o?.ToString() ?? "null")));
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"Stack: [ {stackState} ]");
            Console.ResetColor();
            Console.Write("\n> ");

            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

            try
            {
                // Special REPL commands
                if (input.Equals(".words", StringComparison.OrdinalIgnoreCase))
                {
                    var words = ForthOperations.UserWords.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray();
                    Console.WriteLine("User-defined words:");
                    foreach (var w in words) Console.WriteLine("  " + w);
                    continue;
                }

                // Compile the input into a delegate that operates on our object stack.
                Action<Stack<object>> compiledAction = compiler.Compile(input);

                // Execute the generated code against our persistent stack.
                compiledAction(dataStack);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}

/// <summary>
/// The "standard library" for our Forth language. Contains the actual implementation
/// for all the core words/operators. The JIT-compiler generates calls to these static methods.
/// </summary>
public static class ForthOperations
{
    // Case-insensitive dictionary for user-defined words
    public static Dictionary<string, Action<Stack<object>>> UserWords = new Dictionary<string, Action<Stack<object>>>(StringComparer.OrdinalIgnoreCase);

    public static void Add(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'+' requires two values on the stack.");
        var b = s.Pop();
        var a = s.Pop();

        // Polymorphic behavior for '+'
        if (a is string || b is string)
        {
            s.Push(a.ToString() + b.ToString());
        }
        else
        {
            // Using 'dynamic' makes numeric promotion (int to double, etc.) easy.
            dynamic dyn_a = a;
            dynamic dyn_b = b;
            s.Push(dyn_a + dyn_b);
        }
    }

    public static void Subtract(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'-' requires two values on the stack.");
        dynamic b = s.Pop();
        dynamic a = s.Pop();
        s.Push(a - b);
    }

    public static void Multiply(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'*' requires two values on the stack.");
        dynamic b = s.Pop();
        dynamic a = s.Pop();
        s.Push(a * b);
    }

    public static void Divide(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'/' requires two values on the stack.");
        dynamic b = s.Pop();
        dynamic a = s.Pop();
        s.Push(a / b);
    }

    public static void LessOrEqual(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'<=' requires two values on the stack.");
        dynamic b = s.Pop();
        dynamic a = s.Pop();
        // Push a boolean result
        s.Push(a <= b);
    }

    public static void Equal(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'=' requires two values on the stack.");
        var b = s.Pop();
        var a = s.Pop();
        s.Push(object.Equals(a, b));
    }

    public static void Less(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'<' requires two values on the stack.");
        dynamic b = s.Pop();
        dynamic a = s.Pop();
        s.Push(a < b);
    }

    public static void Greater(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'>' requires two values on the stack.");
        dynamic b = s.Pop();
        dynamic a = s.Pop();
        s.Push(a > b);
    }

    public static void And(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'and' requires two values on the stack.");
        var b = s.Pop();
        var a = s.Pop();
        bool ab = Convert.ToBoolean(a);
        bool bb = Convert.ToBoolean(b);
        s.Push(ab && bb);
    }

    public static void Or(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'or' requires two values on the stack.");
        var b = s.Pop();
        var a = s.Pop();
        bool ab = Convert.ToBoolean(a);
        bool bb = Convert.ToBoolean(b);
        s.Push(ab || bb);
    }

    public static void Rot(Stack<object> s)
    {
        if (s.Count < 3) throw new InvalidOperationException("'rot' requires three values on the stack.");
        var c = s.Pop();
        var b = s.Pop();
        var a = s.Pop();
        s.Push(b);
        s.Push(c);
        s.Push(a);
    }

    public static void Cons(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'cons' requires two values on the stack.");
        var a = s.Pop();
        var b = s.Pop();
        var list = new List<object> { b, a };
        s.Push(list);
    }

    public static void OneMinus(Stack<object> s)
    {
        if (s.Count < 1) throw new InvalidOperationException("'1-' requires one value on the stack.");
        dynamic a = s.Pop();
        s.Push(a - 1);
    }

    public static void Dup(Stack<object> s)
    {
        if (s.Count < 1) throw new InvalidOperationException("'dup' requires one value on the stack.");
        s.Push(s.Peek());
    }

    public static void Drop(Stack<object> s)
    {
        if (s.Count < 1) throw new InvalidOperationException("'drop' requires one value on the stack.");
        s.Pop();
    }

    public static void Swap(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'swap' requires two values on the stack.");
        var b = s.Pop();
        var a = s.Pop();
        s.Push(b);
        s.Push(a);
    }

    public static void Over(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'over' requires two values on the stack.");
        var b = s.Pop();
        var a = s.Peek();
        s.Push(b);
        s.Push(a);
    }

    /// <summary>
    /// Pops an object from the stack, finds a parameterless instance method on it,
    /// invokes it, and pushes the return value back onto the stack if it's not void.
    /// </summary>
    public static void InvokeInstanceMethod(Stack<object> s, string methodName)
    {
        if (s.Count < 1) throw new InvalidOperationException($"Method call '.{methodName}' requires an object instance on the stack.");

        var instance = s.Pop();
        if (instance == null) throw new NullReferenceException($"Cannot call method '.{methodName}' on a null instance.");

        var type = instance.GetType();
        // Find a public, parameterless, case-insensitive instance method.
        // NOTE: This version only supports parameterless methods for simplicity.
        var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase, null, Type.EmptyTypes, null) ?? throw new MissingMethodException(type.Name, methodName);
        object? result = methodInfo.Invoke(instance, null);

        // If the method was not void, push its result back.
        if (methodInfo.ReturnType != typeof(void) && result != null)
        {
            s.Push(result);
        }
    }

    /// <summary>
    /// Looks up a user-defined word by name and executes its compiled delegate.
    /// </summary>
    public static void InvokeUserWord(Stack<object> s, string wordName)
    {
        if (!UserWords.TryGetValue(wordName, out var action))
        {
            throw new InvalidOperationException($"The user-defined word '{wordName}' was not found.");
        }
        action(s);
    }

    /// <summary>
    /// Prints the available built-in and user-defined words to the console.
    /// This is exposed as the Forth word `words` (and `.words`).
    /// </summary>
    public static void Words(Stack<object> s)
    {
        // Built-in operations are the keys in ForthCompiler.Operations. We don't have
        // direct access to that private dictionary here, so we hardcode the common ones
        // for display, and then append user-defined words.
        var builtIns = new[] { "+", "-", "*", "/", "dup", "drop", "swap", "over" };

        Console.WriteLine("Built-in words:");
        foreach (var b in builtIns) Console.WriteLine("  " + b);

        Console.WriteLine("User-defined words:");
        foreach (var w in UserWords.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine("  " + w);
        }
    }

    /// <summary>
    /// Pops the top of the stack and converts it to a boolean for branching.
    /// Returns true for non-false, non-zero values. Returns false for null, false, or zero.
    /// This is intended to be called from emitted IL (it returns a bool on the eval stack).
    /// </summary>
    public static bool PopBool(Stack<object> s)
    {
        if (s.Count < 1) return false;
        var obj = s.Pop();
        if (obj == null) return false;
        if (obj is bool b) return b;
        try
        {
            dynamic d = obj;
            return d != 0;
        }
        catch
        {
            return true; // treat other objects as truthy
        }
    }
}


public class ForthCompiler
{
    private static readonly MethodInfo? PushMethod = typeof(Stack<object>).GetMethod("Push");
    private static readonly MethodInfo? InvokeInstanceMethodInfo = typeof(ForthOperations).GetMethod("InvokeInstanceMethod");
    private static readonly MethodInfo? InvokeUserWordInfo = typeof(ForthOperations).GetMethod("InvokeUserWord");
    private static readonly MethodInfo? PopBoolMethod = typeof(ForthOperations).GetMethod("PopBool");

    // A dictionary mapping Forth words to their C# implementation methods.
    private static readonly Dictionary<string, MethodInfo?> Operations = new Dictionary<string, MethodInfo?>(StringComparer.OrdinalIgnoreCase)
    {
        { "+", typeof(ForthOperations).GetMethod("Add") },
        { "-", typeof(ForthOperations).GetMethod("Subtract") },
        { "*", typeof(ForthOperations).GetMethod("Multiply") },
        { "/", typeof(ForthOperations).GetMethod("Divide") },
        { "dup", typeof(ForthOperations).GetMethod("Dup") },
        { "drop", typeof(ForthOperations).GetMethod("Drop") },
        { "swap", typeof(ForthOperations).GetMethod("Swap") },
        { "over", typeof(ForthOperations).GetMethod("Over") },
        { "<=", typeof(ForthOperations).GetMethod("LessOrEqual") },
        { "1-", typeof(ForthOperations).GetMethod("OneMinus") },
        { "=", typeof(ForthOperations).GetMethod("Equal") },
        { "<", typeof(ForthOperations).GetMethod("Less") },
        { ">", typeof(ForthOperations).GetMethod("Greater") },
        { "and", typeof(ForthOperations).GetMethod("And") },
        { "or", typeof(ForthOperations).GetMethod("Or") },
        { "rot", typeof(ForthOperations).GetMethod("Rot") },
        { "cons", typeof(ForthOperations).GetMethod("Cons") },
        { "words", typeof(ForthOperations).GetMethod("Words") },
        { ".words", typeof(ForthOperations).GetMethod("Words") }
    };


    public Action<Stack<object>> Compile(string source)
    {
        // Support top-level colon definitions of the form: : name <tokens> ;
        // If the source starts with ':' we parse a definition and register it instead of
        // emitting code that executes immediately.
        var trimmed = source.Trim();
        if (trimmed.StartsWith(":"))
        {
            // Tokenize and expect pattern: : name <body tokens...> ;
            var tokens = Tokenize(trimmed);
            if (tokens.Count < 3) throw new InvalidOperationException("Invalid definition. Usage: : name ... ;");
            if (!tokens[^1].Equals(";")) throw new InvalidOperationException("Definition must end with ';'");

            // tokens[0] == ":"; tokens[1] == name; tokens[2..^1] == body
            var name = tokens[1];
            var bodyTokens = tokens.Skip(2).Take(tokens.Count - 3).ToArray();

            // Compile body to a delegate that will operate on the stack when invoked.
            var userDynamicMethod = new DynamicMethod(
                "ForthUserWord_" + name + "_" + Guid.NewGuid().ToString("N"),
                typeof(void),
                new[] { typeof(Stack<object>) },
                typeof(ForthCompiler).Module
            );
            var userIl = userDynamicMethod.GetILGenerator();

            // Emit IL for the body with support for IF/ELSE/THEN, RECURSE and EXIT
            EmitSequence(userIl, bodyTokens, 0, bodyTokens.Length, name);

            userIl.Emit(OpCodes.Ret);

            var action = (Action<Stack<object>>)userDynamicMethod.CreateDelegate(typeof(Action<Stack<object>>));

            // Register/replace existing word
            ForthOperations.UserWords[name] = action;

            // Return a no-op action for the REPL (definition itself does nothing at runtime)
            return _ => { };
        }

        var dynamicMethod = new DynamicMethod(
            "ForthJitAction_" + Guid.NewGuid().ToString("N"),
            typeof(void),
            new[] { typeof(Stack<object>) },
            typeof(ForthCompiler).Module
        );

        ILGenerator il = dynamicMethod.GetILGenerator();

        var words = Tokenize(source);

        var wordArray = words.ToArray();
        EmitSequence(il, wordArray, 0, wordArray.Length, null);

        il.Emit(OpCodes.Ret);
        return (Action<Stack<object>>)dynamicMethod.CreateDelegate(typeof(Action<Stack<object>>));
    }

    // A simple tokenizer that respects quoted strings.
    private List<string> Tokenize(string source)
    {
        // Remove parenthesis comments like ( this is a comment )
        int idx = 0;
        var sb = new System.Text.StringBuilder();
        bool inComment = false;
        while (idx < source.Length)
        {
            var c = source[idx];
            if (!inComment && c == '(')
            {
                inComment = true;
                idx++;
                continue;
            }
            if (inComment && c == ')')
            {
                inComment = false;
                idx++;
                continue;
            }
            if (!inComment) sb.Append(c);
            idx++;
        }
        var cleaned = sb.ToString();
        var tokens = new List<string>();
        var currentToken = "";
        bool inString = false;
        foreach (char c in cleaned)
        {
            if (char.IsWhiteSpace(c) && !inString)
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(currentToken);
                    currentToken = "";
                }
            }
            else if (c == '"')
            {
                currentToken += c;
                if (inString)
                {
                    tokens.Add(currentToken);
                    currentToken = "";
                }
                inString = !inString;
            }
            else
            {
                currentToken += c;
            }
        }
        if (currentToken.Length > 0) tokens.Add(currentToken);
        return tokens;
    }

    // Emit a sequence of tokens with support for basic control flow and recursion.
    // If 'defName' is non-null, RECURSE will compile a call to that word by name.
    private void EmitSequence(ILGenerator il, string[] tokens, int start, int count, string? defName)
    {
        int i = start;
        while (i < start + count)
        {
            var w = tokens[i];

            if (w.Equals("IF", StringComparison.OrdinalIgnoreCase))
            {
                // Find matching ELSE or THEN
                int elseIdx = FindMatching(tokens, i + 1, start + count, "ELSE");
                int thenIdx = FindMatching(tokens, i + 1, start + count, "THEN");
                if (thenIdx < 0) throw new InvalidOperationException("IF without THEN");

                // Emit condition: push the stack and call PopBool which pops stack and returns bool
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, PopBoolMethod!); // pops and leaves bool
                var afterIf = il.DefineLabel();
                var elseLabel = il.DefineLabel();

                // if false jump to elseLabel/afterIf
                il.Emit(OpCodes.Brfalse, elseLabel);

                // then emit the true-branch
                int trueEnd = (elseIdx >= 0) ? elseIdx : thenIdx;
                EmitSequence(il, tokens, i + 1, trueEnd - (i + 1), defName);
                // jump to afterIf
                il.Emit(OpCodes.Br, afterIf);

                // else label
                il.MarkLabel(elseLabel);
                if (elseIdx >= 0)
                {
                    EmitSequence(il, tokens, elseIdx + 1, thenIdx - (elseIdx + 1), defName);
                }

                il.MarkLabel(afterIf);

                i = thenIdx + 1;
                continue;
            }
            else if (w.Equals("RECURSE", StringComparison.OrdinalIgnoreCase))
            {
                if (defName == null) throw new InvalidOperationException("RECURSE used outside a definition");
                // Emit a call to the user word by name
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, defName);
                il.Emit(OpCodes.Call, InvokeUserWordInfo!);
            }
            else if (w.Equals("literal", StringComparison.OrdinalIgnoreCase))
            {
                // Compile-time immediate: next token is emitted as a literal push into the definition
                if (defName == null) throw new InvalidOperationException("'literal' used outside a definition");
                i++;
                if (i >= start + count) throw new InvalidOperationException("literal with no following token");
                var lit = tokens[i];
                // Emit it directly
                EmitToken(il, lit);
            }
            else if (w.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
            {
                il.Emit(OpCodes.Ret);
            }
            else
            {
                EmitToken(il, w);
            }

            i++;
        }
    }

    // Find the matching token for IF/ELSE/THEN, handling nested IFs correctly.
    // If target is "ELSE" it will return the first ELSE at the same nesting level or -1.
    // If target is "THEN" it will return the THEN that closes the current IF.
    private int FindMatching(string[] tokens, int start, int end, string target)
    {
        int depth = 0;
        for (int i = start; i < end; i++)
        {
            var t = tokens[i];
            if (t.Equals("IF", StringComparison.OrdinalIgnoreCase))
            {
                depth++;
            }
            else if (t.Equals("THEN", StringComparison.OrdinalIgnoreCase))
            {
                if (depth == 0 && target.Equals("THEN", StringComparison.OrdinalIgnoreCase)) return i;
                depth--;
                if (depth < 0) depth = 0;
            }
            else if (t.Equals("ELSE", StringComparison.OrdinalIgnoreCase))
            {
                if (depth == 0 && target.Equals("ELSE", StringComparison.OrdinalIgnoreCase)) return i;
            }
        }
        return -1;
    }

    // Emit IL for a single token into the provided ILGenerator.
    private void EmitToken(ILGenerator il, string word)
    {
        if (int.TryParse(word, out int i))
        {
            il.Emit(OpCodes.Ldarg_0);        // Load the stack
            il.Emit(OpCodes.Ldc_I4, i);      // Load integer constant
            il.Emit(OpCodes.Box, typeof(int)); // Box the int to an object
            il.Emit(OpCodes.Call, PushMethod!); // Call stack.Push(object)
        }
        else if (double.TryParse(word, out double d))
        {
            il.Emit(OpCodes.Ldarg_0);           // Load the stack
            il.Emit(OpCodes.Ldc_R8, d);         // Load double constant
            il.Emit(OpCodes.Box, typeof(double));// Box the double to an object
            il.Emit(OpCodes.Call, PushMethod!);  // Call stack.Push(object)
        }
        else if (word.StartsWith("\"") && word.EndsWith("\""))
        {
            string str = word[1..^1];
            il.Emit(OpCodes.Ldarg_0);        // Load the stack
            il.Emit(OpCodes.Ldstr, str);     // Load string constant
            il.Emit(OpCodes.Call, PushMethod!); // Call stack.Push(object)
        }
        else if (Operations.TryGetValue(word, out var methodInfo))
        {
            // Call the static helper method (Add/Subtract/etc.)
            il.Emit(OpCodes.Ldarg_0);      // Pass the stack as the argument
            il.Emit(OpCodes.Call, methodInfo!); // Call the static helper method
        }
        else if (word.StartsWith("."))
        {
            string methodName = word[1..];
            il.Emit(OpCodes.Ldarg_0);               // Load the stack argument
            il.Emit(OpCodes.Ldstr, methodName);     // Load the method name string
            il.Emit(OpCodes.Call, InvokeInstanceMethodInfo!); // Call the helper
        }
        else if (ForthOperations.UserWords.ContainsKey(word))
        {
            il.Emit(OpCodes.Ldarg_0);          // Load the stack argument
            il.Emit(OpCodes.Ldstr, word);      // Load the word name to look up
            il.Emit(OpCodes.Call, InvokeUserWordInfo!); // Call the helper to execute it
        }
        else
        {
            throw new InvalidOperationException($"Unknown word: '{word}'");
        }
    }
}

