using System.Text.RegularExpressions;

namespace StellaLisp.Tests;

public class ReaderUnitTests
{
    [Fact]
    public void ReadSimpleExpression()
    {
        var inputString = "(+ 2 2)";

        var reader = new Reader();

        List<string> actualOutput = reader.ReadString(inputString).Cast<Match>() // Cast to IEnumerable<Match>
                                         .Select(match => match.Value) // Extract the Value property
                                         .ToList()
                                         ; // Convert to a List<string>

        List<string> expectedOutput = ["(", "+", " 2", " 2", ")", ""];


        Assert.Equal(expectedOutput, actualOutput);
    }
}