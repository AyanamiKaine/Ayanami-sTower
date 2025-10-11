using System;

namespace StellaLang.Tests;

public class DebugDoesRequireCreate
{
    [Fact]
    public void TestDoesWithoutCreate()
    {
        var vm = new VM();
        var forth = new ForthInterpreter(vm);

        // This should compile fine
        forth.Interpret(": TEST DOES> @ ;");
        
        // But calling it should fail
        var ex = Assert.Throws<InvalidOperationException>(() =>
            forth.Interpret("TEST"));
            
        Assert.Contains("CREATE", ex.Message);
        Console.WriteLine($"Exception message: {ex.Message}");
    }
}
