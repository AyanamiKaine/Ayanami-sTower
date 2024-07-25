## Stella Testing

## Why Another Testing-Library/Framework?

Simple, I wanted to write my tests right next to the implementation, now I can write the test methods besides the implemenation method or create a test_class.cs and write them there. With the ability to simply call `StellaTesting.RunTests()`. I personally use this (C# REPL)[https://github.com/waf/CSharpRepl] and having the ability to run all tests in the REPL is quite powerful.

- No Dependency on Visual-Studio
- No Stupid Test Runner
- Simple function call to run tests.
- Ability to run all tests found in all assemblies (of course only those whose uses this library)

And its trival to add the possiblity to run tests in the delivered executable

```csharp
static void Main(string[] args)
{
    if (args.Length > 0 && args[0] == "--tests")
    {
        RunTests();
    }
    else
    {
        // Normal application logic (not testing)
        Console.WriteLine("Running normal application mode.");
    }
}
```
