using System;
using System.Text;
using Wasmtime;

static class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("--- Running hello.wat example ---");
        RunHello();
        Console.WriteLine("\n-----------------------------------\n");

        Console.WriteLine("--- Running hello_return.wat example ---");
        RunHelloReturn();
        Console.WriteLine("\n-----------------------------------\n");

        Console.WriteLine("--- Running hello_two_way.wat example ---");
        RunHelloTwoWay();
    }

    static void RunHello()
    {
        Console.WriteLine("This example demonstrates calling a C# function from WASM.");
        using var engine = new Engine();
        using var linker = new Linker(engine);
        using var store = new Store(engine);

        // Define the imported function in C# that our WASM module will call.
        linker.Define(
            "",
            "hello",
            Function.FromCallback(store, () =>
            {
                Console.WriteLine("Host (C#) says: Hello!");
            })
        );

        using var module = Module.FromTextFile(engine, "hello.wat");
        var instance = linker.Instantiate(store, module);

        var run = instance.GetAction("run");
        if (run is null)
        {
            Console.WriteLine("Error: 'run' export not found.");
            return;
        }

        Console.WriteLine("C# is calling the 'run' export in WASM...");
        run(); // This will trigger the WASM module to call back into our C# 'hello' function.
        Console.WriteLine("WASM's 'run' function has completed.");
        Console.WriteLine("Finished hello.wat example.");
    }

    static void RunHelloReturn()
    {
        Console.WriteLine("This example demonstrates reading a string from WASM memory.");
        using var engine = new Engine();
        using var module = Module.FromTextFile(engine, "hello_return.wat");
        using var store = new Store(engine);
        var instance = new Instance(store, module);

        var memory = instance.GetMemory("memory");
        var getGreeting = instance.GetFunction<(int, int)>("get_greeting");

        if (memory is null || getGreeting is null)
        {
            Console.WriteLine("Error: A required export was not found.");
            return;
        }

        // Call the WASM function to get the pointer and length of the string.
        (int resultPointer, int resultLength) = getGreeting();
        Console.WriteLine($"WASM's 'get_greeting' returned pointer={resultPointer}, length={resultLength}");

        // Read the string directly from the module's memory.
        string result = memory.ReadString(resultPointer, resultLength);
        Console.WriteLine("\n--- Result from WASM ---");
        Console.WriteLine(result);
        Console.WriteLine("------------------------\n");
        Console.WriteLine("Finished hello_return.wat example.");
    }

    static void RunHelloTwoWay()
    {
        Console.WriteLine("This example demonstrates passing a string to WASM, having WASM process it, and reading the result back.");
        // --- 1. Setup ---
        using var engine = new Engine();
        // Make sure the .wat file is named correctly.
        using var module = Module.FromTextFile(engine, "hello_two_way.wat");
        using var store = new Store(engine);
        var instance = new Instance(store, module);

        // --- 2. Get all the exported functions and memory ---
        var memory = instance.GetMemory("memory");
        var allocate = instance.GetFunction<int, int>("allocate");
        var deallocate = instance.GetAction<int, int>("deallocate");
        var formatGreeting = instance.GetFunction<int, int, (int, int)>("format_greeting");

        if (memory is null || allocate is null || deallocate is null || formatGreeting is null)
        {
            Console.WriteLine("Error: A required export was not found.");
            return;
        }

        // --- 3. Pass a string FROM C# TO WASM ---
        const string name = "Wasmtime User";
        Console.WriteLine($"C# is preparing to send the string: '{name}'");

        // a) Allocate memory inside WASM for our input string.
        int inputPointer = allocate(name.Length);
        Console.WriteLine($"WASM allocated {name.Length} bytes for the input string at address {inputPointer}");

        // b) Write the C# string into the allocated WASM memory.
        memory.WriteString(inputPointer, name);
        Console.WriteLine("C# wrote the string into WASM memory.");

        // --- 4. Call the WASM function to process the string ---
        Console.WriteLine("\nC# is calling 'format_greeting'...");
        (int resultPointer, int resultLength) = formatGreeting(inputPointer, name.Length);
        Console.WriteLine($"WASM returned a result string at address {resultPointer} with length {resultLength}");

        // --- 5. Read the result string FROM WASM TO C# ---
        string result = memory.ReadString(resultPointer, resultLength);
        Console.WriteLine("\n--- Result from WASM ---");
        Console.WriteLine(result);
        Console.WriteLine("------------------------\n");

        // --- 6. Clean up memory inside WASM ---
        // This is good practice to avoid memory leaks within the WASM module.
        Console.WriteLine("C# is 'deallocating' memory in WASM (calling no-op deallocate).");
        deallocate(inputPointer, name.Length);
        deallocate(resultPointer, resultLength);

        Console.WriteLine("Finished hello_two_way.wat example.");
    }
}