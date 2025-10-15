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
                if (input.Equals(".types", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Registered types:");
                    foreach (var k in ForthOperations.RegisteredTypes.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)) Console.WriteLine("  " + k);
                    continue;
                }
                if (input.Equals(".namespaces", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Registered namespaces:");
                    foreach (var ns in ForthOperations.RegisteredNamespaces) Console.WriteLine("  " + ns);
                    continue;
                }
                if (input.StartsWith(".debug", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && parts[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                    {
                        ForthCompiler.DebugEmitTokens = true;
                        Console.WriteLine("Debug emission ON");
                        continue;
                    }
                    else if (parts.Length > 1 && parts[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                    {
                        ForthCompiler.DebugEmitTokens = false;
                        Console.WriteLine("Debug emission OFF");
                        continue;
                    }
                    Console.WriteLine("Usage: .debug on|off");
                    continue;
                }
                if (input.StartsWith(".tco", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && parts[1].Equals("on", StringComparison.OrdinalIgnoreCase))
                    {
                        ForthCompiler.EnableTailCallOptimization = true;
                        Console.WriteLine("Tail-call optimization ON");
                        continue;
                    }
                    else if (parts.Length > 1 && parts[1].Equals("off", StringComparison.OrdinalIgnoreCase))
                    {
                        ForthCompiler.EnableTailCallOptimization = false;
                        Console.WriteLine("Tail-call optimization OFF");
                        continue;
                    }
                    Console.WriteLine($"TCO is currently {(ForthCompiler.EnableTailCallOptimization ? "ON" : "OFF")}");
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

    // Cache for method invocation resolution: key is (receiverType or null for static search, methodName, providedArgCount)
    // Value is the resolved MethodInfo (or null) to avoid repeated reflection scanning.
    private static readonly Dictionary<(Type?, string, int), MethodInfo?> MethodInvocationCache = new Dictionary<(Type?, string, int), MethodInfo?>();
    private static readonly object MethodInvocationCacheLock = new object();

    public static void Add(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'+' requires two values on the stack.");
        var b = s.Pop();
        var a = s.Pop();

        // Polymorphic behavior for '+'
        if (a is string || b is string)
        {
            s.Push(a.ToString() + b);
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

    public static void ZeroEqual(Stack<object> s)
    {
        if (s.Count < 1) throw new InvalidOperationException("'0=' requires one value on the stack.");
        var a = s.Pop();
        if (a == null) { s.Push(true); return; }
        try
        {
            dynamic d = a;
            s.Push(d == 0);
        }
        catch
        {
            s.Push(false);
        }
    }

    public static void Equal(Stack<object> s)
    {
        if (s.Count < 2) throw new InvalidOperationException("'=' requires two values on the stack.");
        var b = s.Pop();
        var a = s.Pop();
        s.Push(Equals(a, b));
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

    /// <summary>
    /// Pop the top of the stack and print it to the console.
    /// </summary>
    public static void PrintTop(Stack<object> s)
    {
        if (s.Count < 1) return;
        var v = s.Pop();
        Console.WriteLine(v?.ToString() ?? "null");
        try { Console.Out.Flush(); } catch { }
    }

    /// <summary>
    /// Print the entire data stack (top at right) without modifying it.
    /// </summary>
    public static void PrintStack(Stack<object> s)
    {
        var arr = s.Reverse().Select(o => o is string ? $"\"{o}\"" : (o?.ToString() ?? "null")).ToArray();
        Console.WriteLine("Stack: [ " + string.Join(" ", arr) + " ]");
        try { Console.Out.Flush(); } catch { }
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
        if (s.Count < 1) throw new InvalidOperationException($"Method call '.{methodName}' requires at least one value on the stack.");

        // Snapshot the stack (top-first)
        var stackArr = s.ToArray();

        // We will try two strategies, preferring instance-method invocation semantics when
        // the top item can act as a receiver, otherwise falling back to static invocation
        // using the top N args. For each strategy we attempt to find a method that can be
        // satisfied by the available stack values.

        // 1) Instance method: treat stackArr[0] as receiver and use up to stackArr.Length-1 args
        var receiver = stackArr[0];
        // If the receiver is a System.Type, treat this as a static method invocation on that type.
        if (receiver is Type asType)
        {
            var rType = asType;
            int providedArgs = Math.Max(0, stackArr.Length - 1);
            var key = ((Type?)rType, methodName, providedArgs);
            MethodInfo? resolved = null;
            lock (MethodInvocationCacheLock)
            {
                MethodInvocationCache.TryGetValue(key, out resolved);
            }

            if (resolved == null)
            {
                var candidates = rType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)
                    .Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase) && m.GetParameters().Length <= providedArgs)
                    .ToArray();

                foreach (var m in candidates)
                {
                    var pars = m.GetParameters();
                    int n = pars.Length;
                    var args = new object?[n];
                    bool ok = true;
                    for (int i = 0; i < n; i++)
                    {
                        var src = stackArr[i + 1]; // top-first, skip receiver
                        int targetIndex = n - 1 - i;
                        var pType = pars[targetIndex].ParameterType;
                        if (!TryConvert(src, pType, out var converted)) { ok = false; break; }
                        args[targetIndex] = converted;
                    }
                    if (ok) { resolved = m; break; }
                }

                lock (MethodInvocationCacheLock)
                {
                    MethodInvocationCache[key] = resolved; // maybe null
                }
            }

            if (resolved != null)
            {
                var pars = resolved.GetParameters();
                int n = pars.Length;
                var args = new object?[n];
                for (int i = 0; i < n; i++)
                {
                    var src = stackArr[i + 1];
                    int targetIndex = n - 1 - i;
                    var pType = pars[targetIndex].ParameterType;
                    if (!TryConvert(src, pType, out var converted)) throw new InvalidOperationException($"Cannot convert argument to parameter type {pType.FullName} for method {resolved.Name}");
                    args[targetIndex] = converted;
                }

                // Pop receiver and consumed args
                s.Pop(); // receiver
                for (int p = 0; p < n; p++) s.Pop();

                var result = resolved.Invoke(null, args);
                if (resolved.ReturnType != typeof(void) && result != null) s.Push(result);
                return;
            }
            // otherwise fallthrough to try instance/static lookup without receiver
        }
        if (receiver != null)
        {
            var rType = receiver.GetType();
            int providedArgs = Math.Max(0, stackArr.Length - 1);
            var key = ((Type?)rType, methodName, providedArgs);
            MethodInfo? resolved = null;
            lock (MethodInvocationCacheLock)
            {
                MethodInvocationCache.TryGetValue(key, out resolved);
            }

            if (resolved == null)
            {
                // Find candidate instance methods with name and parameter count <= providedArgs
                var methods = rType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                    .Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                // Scoring: prefer methods where required params <= providedArgs and fewer missing params
                var candidates = new List<(MethodInfo m, int missing)>();
                foreach (var m in methods)
                {
                    var pars = m.GetParameters();
                    int n = pars.Length;
                    if (n > providedArgs) continue; // not enough args
                    int required = pars.Count(p => !p.HasDefaultValue && !p.IsOptional);
                    if (providedArgs < required) continue;
                    int missing = n - providedArgs; // usually <=0
                    candidates.Add((m, Math.Max(0, -missing)));
                }

                // pick simplest candidate (fewest parameters)
                var best = candidates.OrderBy(c => c.m.GetParameters().Length).FirstOrDefault();
                resolved = best.m;

                lock (MethodInvocationCacheLock)
                {
                    MethodInvocationCache[key] = resolved; // may be null
                }
            }

            if (resolved != null)
            {
                var pars = resolved.GetParameters();
                int n = pars.Length;
                // Convert and build args from stackArr[1..]
                var args = new object?[n];
                bool ok = true;
                for (int i = 0; i < n; i++)
                {
                    // stackArr index for provided args: i -> stackArr[1 + i_provided?]
                    // We map top-first provided args to last parameters similarly to ctor earlier.
                    var provided = stackArr.Length - 1; // number of provided args
                    var val = stackArr[i + 1]; // THIS maps differently; instead reuse ctor mapping logic
                    int targetIndex = n - 1 - i;
                    var pType = pars[targetIndex].ParameterType;
                    try
                    {
                        var src = stackArr[i + 1];
                        if (src == null)
                        {
                            if (pType.IsValueType && Nullable.GetUnderlyingType(pType) == null) { ok = false; break; }
                            args[targetIndex] = null;
                        }
                        else if (pType.IsInstanceOfType(src)) args[targetIndex] = src;
                        else args[targetIndex] = Convert.ChangeType(src, pType);
                    }
                    catch { ok = false; break; }
                }

                if (!ok) throw new InvalidOperationException($"Provided arguments cannot be converted for instance method '{methodName}'.");

                // Pop receiver and the consumed args
                s.Pop(); // receiver
                for (int p = 0; p < Math.Min(n, stackArr.Length - 1); p++) s.Pop();

                var res = resolved.Invoke(receiver, args);
                if (resolved.ReturnType != typeof(void) && res != null) s.Push(res);
                return;
            }
        }

        // 2) Static method: attempt to find a static method named methodName where parameter count <= stack length
        int providedStaticArgs = stackArr.Length;
        var staticKey = ((Type?)null, methodName, providedStaticArgs);
        MethodInfo? staticResolved = null;
        lock (MethodInvocationCacheLock)
        {
            MethodInvocationCache.TryGetValue(staticKey, out staticResolved);
        }
        if (staticResolved == null)
        {
            // Search loaded assemblies for static methods matching name and parameter count <= providedStaticArgs
            var candidates = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase))
                .Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase) && m.GetParameters().Length <= providedStaticArgs)
                .ToArray();

            foreach (var m in candidates)
            {
                var pars = m.GetParameters();
                int n = pars.Length;
                // attempt to convert top-n stack items to parameters
                var args = new object?[n];
                bool ok = true;
                for (int i = 0; i < n; i++)
                {
                    var src = stackArr[i]; // top-first
                    int targetIndex = n - 1 - i;
                    var pType = pars[targetIndex].ParameterType;
                    if (!TryConvert(src, pType, out var converted)) { ok = false; break; }
                    args[targetIndex] = converted;
                }
                if (ok)
                {
                    staticResolved = m;
                    lock (MethodInvocationCacheLock)
                    {
                        MethodInvocationCache[staticKey] = staticResolved;
                    }
                    break;
                }
            }
            lock (MethodInvocationCacheLock)
            {
                if (!MethodInvocationCache.ContainsKey(staticKey)) MethodInvocationCache[staticKey] = staticResolved; // maybe null
            }
        }

        if (staticResolved != null)
        {
            var pars = staticResolved.GetParameters();
            int n = pars.Length;
            var args = new object?[n];
            for (int i = 0; i < n; i++)
            {
                var src = stackArr[i];
                int targetIndex = n - 1 - i;
                var pType = pars[targetIndex].ParameterType;
                if (src == null) args[targetIndex] = null;
                else if (pType.IsInstanceOfType(src) || pType.IsAssignableFrom(src.GetType())) args[targetIndex] = src;
                else
                {
                    try
                    {
                        args[targetIndex] = Convert.ChangeType(src, pType);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Cannot convert argument {i} of type {src.GetType().FullName} to parameter type {pType.FullName} for method {staticResolved.DeclaringType?.FullName}.{staticResolved.Name}: {ex.Message}", ex);
                    }
                }
            }

            // Pop consumed args
            for (int p = 0; p < n; p++) s.Pop();
            var result = staticResolved.Invoke(null, args);
            if (staticResolved.ReturnType != typeof(void) && result != null) s.Push(result);
            return;
        }

        // Nothing matched
        throw new MissingMethodException("<stack>", methodName);
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
    /// Invoke a static method on a resolved type by name using values on the stack as arguments.
    /// Token form: TypeName.MethodName (e.g. Vector2.Min). This resolves the type (using ResolveRegisteredType
    /// which considers registered namespaces), finds a matching public static method and invokes it.
    /// </summary>
    public static void InvokeStaticMethod(Stack<object> s, string typeName, string methodName)
    {
        var t = ResolveTypeByName(typeName);
        if (t == null) throw new InvalidOperationException($"Type '{typeName}' not found.");

        var stackArr = s.ToArray(); // top-first
        int provided = stackArr.Length;
        var key = ((Type?)t, methodName, provided);
        MethodInfo? resolved = null;
        lock (MethodInvocationCacheLock)
        {
            MethodInvocationCache.TryGetValue(key, out resolved);
        }

        if (resolved == null)
        {
            var candidates = t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase)
                .Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase) && m.GetParameters().Length <= provided)
                .ToArray();

            foreach (var m in candidates)
            {
                var pars = m.GetParameters();
                int n = pars.Length;
                var args = new object?[n];
                bool ok = true;
                for (int i = 0; i < n; i++)
                {
                    var src = stackArr[i]; // top-first
                    int targetIndex = n - 1 - i;
                    var pType = pars[targetIndex].ParameterType;
                    try
                    {
                        if (src == null)
                        {
                            if (pType.IsValueType && Nullable.GetUnderlyingType(pType) == null) { ok = false; break; }
                            args[targetIndex] = null;
                        }
                        else if (pType.IsInstanceOfType(src) || pType.IsAssignableFrom(src.GetType())) args[targetIndex] = src;
                        else args[targetIndex] = Convert.ChangeType(src, pType);
                    }
                    catch { ok = false; break; }
                }
                if (ok) { resolved = m; break; }
            }

            lock (MethodInvocationCacheLock)
            {
                MethodInvocationCache[key] = resolved; // maybe null
            }
        }

        if (resolved == null) throw new MissingMethodException(typeName, methodName);

        var parsFinal = resolved.GetParameters();
        int pn = parsFinal.Length;
        var finalArgs = new object?[pn];
        for (int i = 0; i < pn; i++)
        {
            var src = stackArr[i];
            int targetIndex = pn - 1 - i;
            var pType = parsFinal[targetIndex].ParameterType;
            if (!TryConvert(src, pType, out var converted)) throw new InvalidOperationException($"Cannot convert argument to parameter type {pType.FullName} for method {resolved.Name}");
            finalArgs[targetIndex] = converted;
        }

        // Pop consumed args
        for (int p = 0; p < pn; p++) s.Pop();
        var res = resolved.Invoke(null, finalArgs);
        if (resolved.ReturnType != typeof(void) && res != null) s.Push(res);
    }

    // Resolve by attempting Type.GetType first, then the registered/types search we already have
    private static Type? ResolveTypeByName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;
        // Try direct resolution first
        var t = Type.GetType(typeName, false, true);
        if (t != null) return t;

        // If the name contains dots, try to find by full name in loaded assemblies
        if (typeName.Contains('.'))
        {
            var found = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(typeName, false, true))
                .FirstOrDefault(x => x != null);
            if (found != null) return found;
        }

        // Fallback to previous ResolveRegisteredType (searches RegisteredTypes and RegisteredNamespaces)
        return ResolveRegisteredType(typeName);
    }

    // A registry of host-exposed types that can be constructed from the REPL.
    // Keys are case-insensitive aliases used by the REPL (e.g. 'MyType').
    public static Dictionary<string, Type> RegisteredTypes { get; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

    // A list of namespaces (and optional assembly-qualified hints) to search when
    // resolving unregistered type names. This lets users do `using System.Numerics`
    // and then refer to `Vector2` without registering every single type.
    public static List<string> RegisteredNamespaces { get; } = new List<string>();

    public static void RegisterNamespace(string ns)
    {
        if (string.IsNullOrWhiteSpace(ns)) return;
        // Case-insensitive check to avoid duplicate namespace entries
        if (!RegisteredNamespaces.Exists(s => string.Equals(s, ns, StringComparison.OrdinalIgnoreCase))) RegisteredNamespaces.Add(ns);
    }

    // Automatically register a set of common namespaces so the REPL can resolve
    // commonly used CLR types without explicit registration.
    static ForthOperations()
    {
        RegisterNamespace("System");
        RegisterNamespace("System.Numerics");
        RegisterNamespace("System.Collections");
        RegisterNamespace("System.Collections.Generic");
        RegisterNamespace("System.Text");
        RegisterNamespace("System.IO");
        RegisterNamespace("System.Linq");
        RegisterNamespace("System.Threading");
        RegisterNamespace("System.Threading.Tasks");
        RegisterNamespace("System.Drawing");
    }

    public static void Namespaces(Stack<object> s)
    {
        Console.WriteLine("Registered namespaces:");
        foreach (var ns in RegisteredNamespaces) Console.WriteLine("  " + ns);
    }

    // Try to convert/assign src to targetType. Returns true and sets outVal on success.
    // This avoids calling Convert.ChangeType on non-IConvertible types like Vector2.
    private static bool TryConvert(object? src, Type targetType, out object? outVal)
    {
        outVal = null;
        if (src == null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null) return false;
            outVal = null; return true;
        }
        var srcType = src.GetType();
        if (targetType.IsInstanceOfType(src) || targetType.IsAssignableFrom(srcType))
        {
            outVal = src; return true;
        }
        // Handle enums from string/int
        if (targetType.IsEnum)
        {
            try
            {
                if (src is string ss) { outVal = Enum.Parse(targetType, ss, true); return true; }
                outVal = Enum.ToObject(targetType, src); return true;
            }
            catch { return false; }
        }
        // Last resort: attempt Convert.ChangeType when source is IConvertible
        if (src is IConvertible)
        {
            try
            {
                outVal = Convert.ChangeType(src, targetType);
                return true;
            }
            catch { return false; }
        }
        return false;
    }

    // Try to resolve a type alias: first check RegisteredTypes, then try RegisteredNamespaces
    // by combining namespace + alias and searching loaded assemblies.
    private static Type? ResolveRegisteredType(string alias)
    {
        if (RegisteredTypes.TryGetValue(alias, out var t)) return t;

        foreach (var ns in RegisteredNamespaces)
        {
            var full = ns + "." + alias;
            var resolved = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(full, false, true))
                .FirstOrDefault(x => x != null);
            if (resolved != null) return resolved;
        }

        // Last resort: search all loaded assemblies for a matching type name
        var fallback = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetTypes().FirstOrDefault(ti => string.Equals(ti.Name, alias, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault(x => x != null);
        return fallback;
    }

    /// <summary>
    /// Register a host type under an alias so the REPL can construct it via 'new:alias'.
    /// </summary>
    public static void RegisterType(string alias, Type t)
    {
        if (string.IsNullOrEmpty(alias)) throw new ArgumentException("alias");
        RegisteredTypes[alias] = t ?? throw new ArgumentNullException(nameof(t));
    }

    // Generic helper: RegisterType("Foo", typeof(Foo)) -> RegisterType<Foo>("Foo")
    public static void RegisterType<T>(string alias) where T : new()
    {
        RegisterType(alias, typeof(T));
    }

    // Called from emitted IL: create a new instance of the registered alias and push it.
    // Uses the parameterless constructor via Activator.CreateInstance. Throws if alias is unknown.
    public static void CreateAndPush(Stack<object> s, string alias)
    {
        var t = ResolveRegisteredType(alias);
        if (t == null) throw new InvalidOperationException($"Unknown registered type or type not found '{alias}'");
        object? inst = Activator.CreateInstance(t);
        if (inst == null) throw new InvalidOperationException($"Failed to create instance of '{alias}'");
        s.Push(inst);
    }

    /// <summary>
    /// Push the Type object for the registered alias onto the stack (no instance).
    /// Token form supported by the compiler: type:Alias
    /// </summary>
    public static void CreateTypeAndPush(Stack<object> s, string alias)
    {
        var t = ResolveRegisteredType(alias);
        if (t == null) throw new InvalidOperationException($"Unknown registered type or type not found '{alias}'");
        s.Push(t);
    }

    // Pop an alias string from the stack and create an instance of that registered type.
    // Exposed as the 'new' word.
    public static void CreateFromStack(Stack<object> s)
    {
        if (s.Count < 1) throw new InvalidOperationException("'new' requires a type alias string on the stack.");
        var aliasObj = s.Pop();
        if (aliasObj == null) throw new InvalidOperationException("Alias cannot be null");
        CreateAndPush(s, aliasObj.ToString()!);
    }

    /// <summary>
    /// Pop an alias string from the stack and push the resolved Type object.
    /// Exposed as the word 'type'.
    /// </summary>
    public static void CreateTypeFromStack(Stack<object> s)
    {
        if (s.Count < 1) throw new InvalidOperationException("'type' requires a type alias string on the stack.");
        var aliasObj = s.Pop();
        if (aliasObj == null) throw new InvalidOperationException("Alias cannot be null");
        var alias = aliasObj.ToString()!;
        var t = ResolveRegisteredType(alias);
        if (t == null) throw new InvalidOperationException($"Unknown registered type '{alias}'");
        s.Push(t);
    }

    // Constructor helper: expects arguments followed by an alias string on top of the stack.
    // Example usage: 1 2 "Vec2" ctor  -> will attempt to find a constructor matching (int, int)
    public static void Ctor(Stack<object> s)
    {
        if (s.Count < 1) throw new InvalidOperationException("ctor requires a type alias on the stack");
        // Peek alias (it's on top)
        var aliasObj = s.Pop();
        if (aliasObj == null) throw new InvalidOperationException("Alias cannot be null");
        var alias = aliasObj.ToString()!;
        var t = ResolveRegisteredType(alias);
        if (t == null) throw new InvalidOperationException($"Unknown registered type '{alias}'");

        // Collect constructors and determine the maximum arity we'll consider.
        var allCtors = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance).ToArray();
        if (allCtors.Length == 0) throw new InvalidOperationException($"Type '{alias}' has no public constructors.");
        int maxParams = allCtors.Max(c => c.GetParameters().Length);

        var stackArr = s.ToArray(); // top-first
        int providedAll = stackArr.Length;
        // We only consider up to maxParams items from the top of the stack as ctor args.
        int provided = Math.Min(providedAll, maxParams);

        // Candidate selection: prefer ctors where provided <= n and where required params are satisfied.
        var candidates = new List<(ConstructorInfo ctor, ParameterInfo[] pars, int missing)>();
        foreach (var ctor in allCtors)
        {
            var pars = ctor.GetParameters();
            int n = pars.Length;
            // If caller provided more args than the ctor accepts, skip.
            if (provided > n) continue;

            // Count required parameters (no default value and not optional)
            int required = pars.Count(p => !p.HasDefaultValue && !p.IsOptional);
            if (provided < required) continue; // not enough provided args to satisfy required params

            int missing = n - provided; // number of leading params we must fill with defaults

            // Verify that missing leading params can be filled (have default or are value types)
            bool ok = true;
            for (int j = 0; j < missing; j++)
            {
                var pj = pars[j];
                if (!pj.HasDefaultValue && !pj.IsOptional && !pj.ParameterType.IsValueType)
                {
                    // If it's a reference type without a default, we'll accept null as a last resort but mark as ok.
                    // To be conservative, we still allow it; caller can detect runtime errors.
                }
            }
            if (!ok) continue;

            candidates.Add((ctor, pars, missing));
        }

        if (candidates.Count == 0) throw new InvalidOperationException($"No matching constructor found for '{alias}' with the provided arguments.");

        // Pick the candidate with the fewest missing params (closest fit), then fewest params overall.
        var best = candidates.OrderBy(c => c.missing).ThenBy(c => c.pars.Length).First();
        var bestPars = best.pars;
        int nParams = bestPars.Length;

        // Build argument list of length nParams. Missing leading params get default values when available.
        var args = new object?[nParams];
        int missingCount = nParams - provided;
        for (int j = 0; j < missingCount; j++)
        {
            var pj = bestPars[j];
            if (pj.HasDefaultValue) args[j] = pj.DefaultValue;
            else if (pj.ParameterType.IsValueType) args[j] = Activator.CreateInstance(pj.ParameterType);
            else args[j] = null;
        }

        // Fill provided args from the stack (top-first -> last parameter)
        bool convertOk = true;
        for (int i = 0; i < provided; i++)
        {
            var val = stackArr[i]; // top-first
            int targetIndex = nParams - 1 - i; // map to parameter position
            var pType = bestPars[targetIndex].ParameterType;
            if (val == null)
            {
                if (pType.IsValueType && Nullable.GetUnderlyingType(pType) == null)
                {
                    // can't assign null to non-nullable value type
                    convertOk = false; break;
                }
                args[targetIndex] = null;
            }
            else if (pType.IsInstanceOfType(val))
            {
                args[targetIndex] = val;
            }
            else if (!TryConvert(val, pType, out var conv))
            {
                convertOk = false; break;
            }
            else
            {
                args[targetIndex] = conv;
            }
        }

        if (!convertOk) throw new InvalidOperationException($"Provided arguments cannot be converted to constructor parameter types for '{alias}'.");

        // Pop the provided arguments from the stack (only those we consumed)
        for (int p = 0; p < provided; p++) s.Pop();

        // Invoke constructor
        var inst = best.ctor.Invoke(args);
        s.Push(inst);
    }

    // Print registered type aliases
    public static void Types(Stack<object> s)
    {
        Console.WriteLine("Registered types:");
        foreach (var k in RegisteredTypes.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)) Console.WriteLine("  " + k);
    }

    /// <summary>
    /// Prints the available built-in and user-defined words to the console.
    /// This is exposed as the Forth word `words` (and `.words`).
    /// </summary>
    public static void Words(Stack<object> s)
    {
        // Dynamically query the compiler for known built-in words so this listing
        // scales as new built-ins are added. Then append user-defined words.
        Console.WriteLine("Built-in words:");
        foreach (var b in ForthCompiler.GetBuiltInWords().OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine("  " + b);
        }

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

    public static int PopInt(Stack<object> s)
    {
        if (s.Count < 1) return 0;
        var obj = s.Pop();
        if (obj == null) return 0;
        try
        {
            return Convert.ToInt32(obj);
        }
        catch
        {
            return 0;
        }
    }

    // Predicate helpers: leave the stack unchanged and push a boolean indicating
    // whether the top value is (or can be interpreted as) the requested type.
    // These are exposed as words like '?int', '?bool', '?string', '?double'.
    public static void IsInt(Stack<object> s)
    {
        if (s.Count < 1) { s.Push(false); return; }
        var obj = s.Peek();
        if (obj == null) { s.Push(false); return; }
        try
        {
            Convert.ToInt32(obj);
            s.Push(true);
        }
        catch
        {
            s.Push(false);
        }
    }

    public static void IsBool(Stack<object> s)
    {
        if (s.Count < 1) { s.Push(false); return; }
        var obj = s.Peek();
        if (obj == null) { s.Push(false); return; }
        try
        {
            Convert.ToBoolean(obj);
            s.Push(true);
        }
        catch
        {
            s.Push(false);
        }
    }

    public static void IsString(Stack<object> s)
    {
        if (s.Count < 1) { s.Push(false); return; }
        var obj = s.Peek();
        s.Push(obj is string);
    }

    public static void IsDouble(Stack<object> s)
    {
        if (s.Count < 1) { s.Push(false); return; }
        var obj = s.Peek();
        if (obj == null) { s.Push(false); return; }
        try
        {
            Convert.ToDouble(obj);
            s.Push(true);
        }
        catch
        {
            s.Push(false);
        }
    }

    public static void DebugEnter(Stack<object> s, string name)
    {
        // Only print when debug emission is enabled on the compiler side.
        if (!ForthCompiler.DebugEmitTokens) return;
        Console.WriteLine($"ENTER {name} StackCount: {s.Count}");
        try { Console.Out.Flush(); } catch { }
    }
}


public class ForthCompiler
{
    // Allow runtime toggling of tail-call optimization for debugging.
    public static bool EnableTailCallOptimization = true;
    // When true, compiled methods will include a Console.WriteLine for each token as it's executed.
    public static bool DebugEmitTokens = false;

    private static readonly MethodInfo? ConsoleWriteLineString = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
    private static readonly MethodInfo? ConsoleOutGetter = typeof(Console).GetProperty("Out")!.GetGetMethod();
    private static readonly MethodInfo? TextWriterFlush = typeof(System.IO.TextWriter).GetMethod("Flush");

    private static readonly MethodInfo? PushMethod = typeof(Stack<object>).GetMethod("Push");
    private static readonly MethodInfo? InvokeInstanceMethodInfo = typeof(ForthOperations).GetMethod("InvokeInstanceMethod");
    private static readonly MethodInfo? InvokeUserWordInfo = typeof(ForthOperations).GetMethod("InvokeUserWord");
    private static readonly MethodInfo? PopBoolMethod = typeof(ForthOperations).GetMethod("PopBool");
    private static readonly MethodInfo? PopIntMethod = typeof(ForthOperations).GetMethod("PopInt");
    private static readonly MethodInfo? CreateAndPushInfo = typeof(ForthOperations).GetMethod("CreateAndPush");
    private static readonly MethodInfo? CreateTypeAndPushInfo = typeof(ForthOperations).GetMethod("CreateTypeAndPush");
    private static readonly MethodInfo? CreateTypeFromStackInfo = typeof(ForthOperations).GetMethod("CreateTypeFromStack");

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
    { "0=", typeof(ForthOperations).GetMethod("ZeroEqual") },
        { "=", typeof(ForthOperations).GetMethod("Equal") },
        { "<", typeof(ForthOperations).GetMethod("Less") },
        { ">", typeof(ForthOperations).GetMethod("Greater") },
        { "and", typeof(ForthOperations).GetMethod("And") },
        { "or", typeof(ForthOperations).GetMethod("Or") },
        { "rot", typeof(ForthOperations).GetMethod("Rot") },
        { "cons", typeof(ForthOperations).GetMethod("Cons") },
    { ".", typeof(ForthOperations).GetMethod("PrintTop") },
    { ".s", typeof(ForthOperations).GetMethod("PrintStack") },
        { "words", typeof(ForthOperations).GetMethod("Words") },
        { ".words", typeof(ForthOperations).GetMethod("Words") }
    };

    // Add predicate words to the operations map
    static ForthCompiler()
    {
        // Register ?int, ?bool, ?string, ?double
        Operations["?int"] = typeof(ForthOperations).GetMethod("IsInt");
        Operations["?bool"] = typeof(ForthOperations).GetMethod("IsBool");
        Operations["?string"] = typeof(ForthOperations).GetMethod("IsString");
        Operations["?double"] = typeof(ForthOperations).GetMethod("IsDouble");
        // Register object/type helpers
        Operations["new"] = typeof(ForthOperations).GetMethod("CreateFromStack");
        Operations["ctor"] = typeof(ForthOperations).GetMethod("Ctor");
        Operations["types"] = typeof(ForthOperations).GetMethod("Types");
        Operations[".types"] = typeof(ForthOperations).GetMethod("Types");
        // type helpers: push a Type object
        Operations["type"] = typeof(ForthOperations).GetMethod("CreateTypeFromStack");
        Operations[".type"] = typeof(ForthOperations).GetMethod("CreateTypeFromStack");
        // namespace helpers
        Operations["using"] = typeof(ForthOperations).GetMethod("RegisterNamespace");
        Operations["namespaces"] = typeof(ForthOperations).GetMethod("Namespaces");
        Operations[".namespaces"] = typeof(ForthOperations).GetMethod("Namespaces");
    }

    /// <summary>
    /// Return the set of built-in word names. This allows other parts of the
    /// program (like the Words printer) to list built-ins without depending on
    /// the internal MethodInfo values.
    /// </summary>
    public static IEnumerable<string> GetBuiltInWords()
    {
        return Operations.Keys;
    }


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

            // Define a start label so we can implement tail-call optimization (RECURSE -> branch)
            var methodStart = userIl.DefineLabel();
            userIl.MarkLabel(methodStart);

            // Emit a call to the debug hook so we can observe method entries/calls
            userIl.Emit(OpCodes.Ldarg_0);
            userIl.Emit(OpCodes.Ldstr, name);
            var debugEnter = typeof(ForthOperations).GetMethod("DebugEnter");
            userIl.Emit(OpCodes.Call, debugEnter!);

            // Emit IL for the body with support for IF/ELSE/THEN, RECURSE and EXIT
            EmitSequence(userIl, bodyTokens, 0, bodyTokens.Length, name, null, EnableTailCallOptimization, methodStart);

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
    private static List<string> Tokenize(string source)
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

    // Emit a sequence of tokens with support for control flow, recursion and loops.
    // If 'defName' is non-null, RECURSE will compile a call to that word by name.
    // loopLocals is a stack of LocalBuilder representing the current loop indices (for 'i').
    private void EmitSequence(ILGenerator il, string[] tokens, int start, int count, string? defName, List<LocalBuilder>? loopLocals = null, bool enableTailOptimization = false, Label? methodStartLabel = null)
    {
        int i = start;
        loopLocals ??= new List<LocalBuilder>();
        while (i < start + count)
        {
            var w = tokens[i];

            // If debug emission is enabled, print the token (and flush) at runtime.
            if (DebugEmitTokens)
            {
                il.Emit(OpCodes.Ldstr, $"TOK: {w}");
                il.Emit(OpCodes.Call, ConsoleWriteLineString!);
                // flush
                il.Emit(OpCodes.Call, ConsoleOutGetter!);
                il.Emit(OpCodes.Callvirt, TextWriterFlush!);
            }

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
                EmitSequence(il, tokens, i + 1, trueEnd - (i + 1), defName, loopLocals);
                // jump to afterIf
                il.Emit(OpCodes.Br, afterIf);

                // else label
                il.MarkLabel(elseLabel);
                if (elseIdx >= 0)
                {
                    EmitSequence(il, tokens, elseIdx + 1, thenIdx - (elseIdx + 1), defName, loopLocals);
                }

                il.MarkLabel(afterIf);

                i = thenIdx + 1;
                continue;
            }
            else if (w.Equals("BEGIN", StringComparison.OrdinalIgnoreCase))
            {
                // Find matching REPEAT
                int repeatIdx = FindMatching(tokens, i + 1, start + count, "REPEAT");
                if (repeatIdx < 0) throw new InvalidOperationException("BEGIN without REPEAT");

                // Find optional WHILE between BEGIN and REPEAT
                int whileIdx = FindMatching(tokens, i + 1, repeatIdx, "WHILE");

                var loopStart = il.DefineLabel();
                var loopEnd = il.DefineLabel();

                il.MarkLabel(loopStart);

                if (whileIdx >= 0)
                {
                    // Emit body up to WHILE
                    EmitSequence(il, tokens, i + 1, whileIdx - (i + 1), defName, loopLocals);

                    // Emit condition and branch to end if false
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, PopBoolMethod!);
                    il.Emit(OpCodes.Brfalse, loopEnd);

                    // Emit remainder between WHILE and REPEAT
                    EmitSequence(il, tokens, whileIdx + 1, repeatIdx - (whileIdx + 1), defName, loopLocals);
                }
                else
                {
                    // No WHILE: emit whole body and then loop
                    EmitSequence(il, tokens, i + 1, repeatIdx - (i + 1), defName, loopLocals);
                }

                il.Emit(OpCodes.Br, loopStart);
                il.MarkLabel(loopEnd);

                i = repeatIdx + 1;
                continue;
            }

            else if (w.Equals("RECURSE", StringComparison.OrdinalIgnoreCase))
            {
                if (defName == null) throw new InvalidOperationException("RECURSE used outside a definition");
                // Tail-call optimization: if this RECURSE is the last thing in the method
                // and tail optimization is enabled, emit a branch back to methodStart instead of a call.
                bool isTail = enableTailOptimization && (i == start + count - 1);
                if (isTail && methodStartLabel.HasValue)
                {
                    // Branch to start (simulate tail-call)
                    il.Emit(OpCodes.Br, methodStartLabel.Value);
                }
                else
                {
                    // Regular call to the user word
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldstr, defName);
                    il.Emit(OpCodes.Call, InvokeUserWordInfo!);
                }
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
            else if (w.Equals("DO", StringComparison.OrdinalIgnoreCase) || w.Equals("?DO", StringComparison.OrdinalIgnoreCase))
            {
                // Find matching LOOP
                int loopIdx = FindMatching(tokens, i + 1, start + count, "LOOP");
                if (loopIdx < 0) throw new InvalidOperationException("DO without LOOP");

                bool isQ = w.Equals("?DO", StringComparison.OrdinalIgnoreCase);

                // Declare locals: index and limit
                var localIndex = il.DeclareLocal(typeof(int));
                var localLimit = il.DeclareLocal(typeof(int));

                // Pop start then limit from stack: stack has ( limit start ) so Pop start then limit
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, PopIntMethod!);
                il.Emit(OpCodes.Stloc, localIndex);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, PopIntMethod!);
                il.Emit(OpCodes.Stloc, localLimit);

                var loopStart = il.DefineLabel();
                var loopEnd = il.DefineLabel();

                // ?DO: if index >= limit skip entirely
                if (isQ)
                {
                    il.Emit(OpCodes.Ldloc, localIndex);
                    il.Emit(OpCodes.Ldloc, localLimit);
                    il.Emit(OpCodes.Bge, loopEnd);
                }

                // Mark loop start
                il.MarkLabel(loopStart);

                // Push this loop index into context
                loopLocals.Add(localIndex);

                // Emit body
                EmitSequence(il, tokens, i + 1, loopIdx - (i + 1), defName, loopLocals);

                // Pop loop index context
                loopLocals.RemoveAt(loopLocals.Count - 1);

                // increment index
                il.Emit(OpCodes.Ldloc, localIndex);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, localIndex);

                // if index < limit goto loopStart
                il.Emit(OpCodes.Ldloc, localIndex);
                il.Emit(OpCodes.Ldloc, localLimit);
                il.Emit(OpCodes.Blt, loopStart);

                il.MarkLabel(loopEnd);

                i = loopIdx + 1;
                continue;
            }
            else if (w.Equals("i", StringComparison.OrdinalIgnoreCase))
            {
                if (loopLocals == null || loopLocals.Count == 0) throw new InvalidOperationException("i used outside of loop");
                var current = loopLocals[loopLocals.Count - 1];
                // Push current index onto the Forth stack
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Box, typeof(int));
                il.Emit(OpCodes.Call, PushMethod!);
            }
            else
            {
                EmitToken(il, w);
            }

            i++;
        }
    }

    // Find the matching token for control flow handling nested IFs and DOs correctly.
    // Supports matching targets: ELSE, THEN, LOOP.
    private static int FindMatching(string[] tokens, int start, int end, string target)
    {
        int ifDepth = 0;
        int doDepth = 0;
        int beginDepth = 0;

        for (int i = start; i < end; i++)
        {
            var t = tokens[i];
            if (t.Equals("IF", StringComparison.OrdinalIgnoreCase))
            {
                ifDepth++;
            }
            else if (t.Equals("THEN", StringComparison.OrdinalIgnoreCase))
            {
                if (ifDepth == 0 && target.Equals("THEN", StringComparison.OrdinalIgnoreCase)) return i;
                ifDepth = Math.Max(0, ifDepth - 1);
            }
            else if (t.Equals("ELSE", StringComparison.OrdinalIgnoreCase))
            {
                if (ifDepth == 0 && target.Equals("ELSE", StringComparison.OrdinalIgnoreCase)) return i;
            }
            else if (t.Equals("DO", StringComparison.OrdinalIgnoreCase) || t.Equals("?DO", StringComparison.OrdinalIgnoreCase))
            {
                doDepth++;
            }
            else if (t.Equals("LOOP", StringComparison.OrdinalIgnoreCase))
            {
                if (doDepth == 0 && target.Equals("LOOP", StringComparison.OrdinalIgnoreCase)) return i;
                doDepth = Math.Max(0, doDepth - 1);
            }
            else if (t.Equals("BEGIN", StringComparison.OrdinalIgnoreCase))
            {
                beginDepth++;
            }
            else if (t.Equals("WHILE", StringComparison.OrdinalIgnoreCase))
            {
                if (beginDepth == 0 && target.Equals("WHILE", StringComparison.OrdinalIgnoreCase)) return i;
            }
            else if (t.Equals("REPEAT", StringComparison.OrdinalIgnoreCase))
            {
                if (beginDepth == 0 && target.Equals("REPEAT", StringComparison.OrdinalIgnoreCase)) return i;
                beginDepth = Math.Max(0, beginDepth - 1);
            }
        }
        return -1;
    }

    // Emit IL for a single token into the provided ILGenerator.
    private static void EmitToken(ILGenerator il, string word)
    {
        // Numbers (int)
        if (int.TryParse(word, out int i))
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Box, typeof(int));
            il.Emit(OpCodes.Call, PushMethod!);
            return;
        }

        // Numbers (double)
        if (double.TryParse(word, out double d))
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_R8, d);
            il.Emit(OpCodes.Box, typeof(double));
            il.Emit(OpCodes.Call, PushMethod!);
            return;
        }

        // Quoted string
        if (word.StartsWith("\"") && word.EndsWith("\""))
        {
            var s = word[1..^1];
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, s);
            il.Emit(OpCodes.Call, PushMethod!);
            return;
        }

        // Built-in operations map
        if (Operations.TryGetValue(word, out var opMi))
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, opMi!);
            return;
        }

        // Instance method form: .MethodName
        if (word.StartsWith("."))
        {
            var nm = word[1..];
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, nm);
            il.Emit(OpCodes.Call, InvokeInstanceMethodInfo!);
            return;
        }

        // Type.StaticMethod form: TypeName.MethodName (don't treat leading '.' here)
        if (word.Contains('.') && !word.StartsWith("."))
        {
            var pieces = word.Split(new[] { '.' }, 2);
            var tname = pieces[0];
            var mname = pieces[1];
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, tname);
            il.Emit(OpCodes.Ldstr, mname);
            var invokeStaticMi = typeof(ForthOperations).GetMethod("InvokeStaticMethod");
            il.Emit(OpCodes.Call, invokeStaticMi!);
            return;
        }

        // type:Alias -> push the Type object for alias
        if (word.StartsWith("type:", StringComparison.OrdinalIgnoreCase))
        {
            var alias = word.Substring(5);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, alias);
            il.Emit(OpCodes.Call, CreateTypeAndPushInfo!);
            return;
        }

        // User-defined word
        if (ForthOperations.UserWords.ContainsKey(word))
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, word);
            il.Emit(OpCodes.Call, InvokeUserWordInfo!);
            return;
        }

        // new:Alias
        if (word.StartsWith("new:", StringComparison.OrdinalIgnoreCase))
        {
            var alias = word.Substring(4);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, alias);
            il.Emit(OpCodes.Call, CreateAndPushInfo!);
            return;
        }

        // Unknown token: no-op (could throw instead)
        return;
    }
}

