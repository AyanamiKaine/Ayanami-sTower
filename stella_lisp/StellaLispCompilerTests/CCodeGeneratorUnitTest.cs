namespace StellaLispCompilerTests;

public class CCodeGeneratorUnitTest
{
    [Fact]
    public void Print()
    {
        var stellaLispCode = """(print "Hello, World")""";

        var expectedCCode =
        """
        #include <stdio.h>

        int main() {
            printf("Hello, World!");
            return 0;
        }
        """;

        var actualGeneratedCCode = "";

        Assert.Equal(expectedCCode, actualGeneratedCCode);
    }
}
