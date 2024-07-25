# Stella Testing

## Dependencies

- net8.0

## Installing

## How To Use

### 1. Import the namespace

```csharp
using Stella.Testing;
```

### 2. Create a class

- The name can be whatever you want.

```csharp
public class UnitTest
{
    ...
}
```

### 3. Create functions with the `[ST_TEST]` annotation

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
    private static TestingResult TestAssertStringEqual()
    {
        string a = "Hello";
        return AssertEqual("Hello", a, "String a and b are NOT equal");
    }
...

```

### 4. Run Tests

You can either run all tests that you defined in you main application or you can all tests found in your project as well as any dependencies that might defined tests too.

```csharp
StellaTesting.RunTests();
StellaTesting.RunAllTestsFoundInAllAssemblies();
```

# Why Another Testing-Library/Framework?

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
        StellaTesting.RunTests();
    }
    else
    {
        // Normal application logic (not testing)
        Console.WriteLine("Running normal application mode.");
    }
}
```
