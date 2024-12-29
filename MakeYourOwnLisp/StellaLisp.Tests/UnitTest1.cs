namespace StellaLisp.Tests;

/// <summary>
/// Represents Step 0 of the Make-A-Lisp guide.
/// This step is basically just creating a skeleton of your interpreter.
/// </summary>
public class BarebonesTests
{
    [Fact]
    public void EvalShouldReturnInputString()
    {
        var actualOutput = StellaLisp.Eval("""(println "Hello World")""");
        var expectedOutput = """(println "Hello World")""";

        Assert.Equal(expectedOutput, actualOutput);
    }

    [Fact]
    public void PrintShouldReturnInputString()
    {
        var actualOutput = StellaLisp.Print("""(println "Hello World")""");
        var expectedOutput = """(println "Hello World")""";

        Assert.Equal(expectedOutput, actualOutput);
    }

    [Fact]
    public void ReadShouldReturnInputString()
    {
        var stringReader = new StringReader("""(println "Hello World")""");


        var actualOutput = StellaLisp.Read(stringReader);
        var expectedOutput = """(println "Hello World")""";

        Assert.Equal(expectedOutput, actualOutput);
    }
}
