using System.Reflection;

namespace Stella.Testing
{
    // Custom attribute to mark test methods
    [AttributeUsage(AttributeTargets.Method)]
    public class ST_TESTAttribute : Attribute { }

    public class StellaTesting
    {
        public class TestingResult(string errorMessage, bool passed)
        {
            public string ErrorMessage = errorMessage;
            public bool Passed = passed;

            //Automatically formats a pretty string of the test result ready to be printed to the console!
            internal string PrettyToString()
            {
                string status = Passed ? "Passed [✓]" : "Failed [X]";
                return $"{status} : {ErrorMessage}";
            }
        }


        // This runs alls tests found not only that you defined but that every dependencie defined too
        // This can be helpful when debugging but beware if many tests are found it could take a while.
        public static void RunAllTestsFoundInAllAssemblies()
        {
            Console.WriteLine("Running all tests found in all assemblies...\n");

            // Get all loaded assemblies in the current AppDomain
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {

                // Get all methods in the assembly with the ST_TEST attribute
                MethodInfo[] methods = assembly.GetTypes()
                    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    .Where(m => m.GetCustomAttribute(typeof(ST_TESTAttribute)) != null)
                    .ToArray();

                if (methods.Length != 0)
                {
                    string assemblyName = assembly.GetName().Name;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\nRunning Tests found in the assembly: {assemblyName}");
                    Console.ResetColor(); // Reset color after each test
                }

                foreach (MethodInfo method in methods)
                {
                    // Check if method has the ST_TEST attribute
                    if (method.GetCustomAttribute(typeof(ST_TESTAttribute)) != null)
                    {
                        TestingResult testingResult = (TestingResult)method.Invoke(null, null);
                        Console.ForegroundColor = testingResult.Passed ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.WriteLine($"{method.Name} Result: {testingResult.PrettyToString()}");

                        Console.ResetColor(); // Reset color after each test
                    }
                }
            }
        }

        public static void RunTests()
        {
            Console.WriteLine("Running tests...");

            // Get the main assembly where the RunTests method is called from (the main project's assembly)
            Assembly mainAssembly = Assembly.GetEntryAssembly();

            if (mainAssembly == null)
            {
                Console.WriteLine("Error: Could not determine the main assembly.");
                return;
            }
            // Get all methods in the assembly with the ST_TEST attribute
            MethodInfo[] methods = mainAssembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                .Where(m => m.GetCustomAttribute(typeof(ST_TESTAttribute)) != null)
                .ToArray();

            Parallel.ForEach(methods, method =>
            {
                // Check if method has the ST_TEST attribute
                if (method.GetCustomAttribute(typeof(ST_TESTAttribute)) != null)
                {
                    TestingResult testingResult = (TestingResult)method.Invoke(null, null);
                    Console.ForegroundColor = testingResult.Passed ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.WriteLine($"{method.Name} Result: {testingResult.PrettyToString()}");

                    Console.ResetColor(); // Reset color after each test
                }
            });
        }

        public static TestingResult AssertEqual<T>(T expected, T actual, string message = "")
        {
            if (!expected.Equals(actual))
            {
                return new TestingResult($"Assertion Failed: Expected: {expected}, Actual: {actual}. {message}", false);
            }

            return new TestingResult($"Assertion was as Expected: {expected}, Actual: {actual}", true);
        }

        [ST_TEST]
        private static TestingResult TestAssertStringEqual()
        {
            string a = "Hello";
            return AssertEqual("Hello", a, "String a and b are NOT equal");
        }

        public static TestingResult AssertTrue(bool condition, string message = "")
        {
            if (!condition)
            {
                return new TestingResult($"Assertion Failed: Expected true, got false. {message}", false);
            }

            return new TestingResult($"Assertion was as Expected true, got true.", true);
        }

        public static TestingResult AssertFalse(bool condition, string message = "")
        {
            if (condition)
            {
                return new TestingResult($"Assertion Failed: Expected false, got true. {message}", false);
            }

            return new TestingResult($"Assertion was as Expected false, got false", true);
        }
    }
}