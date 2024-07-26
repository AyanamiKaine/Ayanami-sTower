# Stella Testing: A Lightweight, Reflection-Based C# Testing Library

## Features

- **Simplicity:** Write tests as regular methods with the `[ST_TEST]` attribute.
- **Flexibility:** Execute tests from your main application or discover and run tests across all loaded assemblies.
- **Extensibility:** Easily integrate with C# REPLs or add testing capabilities to your executables.
- **Clear Output:** Color-coded results for easy identification of passed and failed tests.

## Installing

```bash
dotnet add package StellaTesting
```

## How To Use

### 1. Import the namespace

```csharp
using Stella.Testing;
```

### 2. Create functions with the `[ST_TEST]` annotation

You can turn any function into a test with `[ST_Test]` but it must return an `TestingResult` object.

```csharp
public class TestingResult(string errorMessage, bool passed)
{
    public string ErrorMessage = errorMessage;
    public bool Passed = passed;

    //Automatically formats a pretty string of the test result ready to be printed to the console!
    public string PrettyToString()
    {
        string status = Passed ? "Passed [✓]" : "Failed [X]";
        return $"{status} : {ErrorMessage}";
    }
}
```

Stella Testing provides some assert helper function that each return a `TestingResult` object

```csharp
...
[ST_TEST]
private static TestingResult TestMyMethod()
{
    // Your test logic here
    return AssertEqual(expectedValue, actualValue, "MyMethod failed");
}
...

```

Stella Testing provides convenient assertion functions:

- `AssertEqual<T>(expected, actual, message)`
- `AssertTrue(condition, message)`
- `AssertFalse(condition, message)`

### 3. Run Tests

You can either run all tests that you defined in you main application or you can all tests found in your project as well as any dependencies that might defined tests too.

```csharp
StellaTesting.RunTests();
StellaTesting.RunAllTestsFoundInAllAssemblies();
```

# Why Another Testing-Library/Framework?

Simple, I wanted to write my tests right next to the implementation, now I can write the test methods besides the implemenation method or create a test_class.cs and write them there. With the ability to simply call `StellaTesting.RunTests()`. I personally use this (C# REPL)[https://github.com/waf/CSharpRepl] and having the ability to run all tests in the REPL is quite powerful.

- No Dependency on Visual-Studio
- No Test Runner
- Simple function call to run tests.
- Ability to run all tests found in all assemblies (of course only those whose uses this library)

And its trival to add the possiblity to run tests in your executable

```csharp
static void Main(string[] args)
{
    if (args.Length > 0 && args[0] == "--run-tests")
    {
        StellaTesting.RunTests();
        return;  // Exit after running tests
    }

    // ... your normal application logic
}
```

# Contributing

Contributions are welcome! Please feel free to open issues or submit pull requests.

# License

MIT
