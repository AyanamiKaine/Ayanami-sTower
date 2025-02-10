namespace SFPM.Tests;

public class SFPMUnitTest
{
    [Fact]
    public void Creation()
    {
        var SFPM = new SFPM();
        Assert.NotNull(SFPM);
    }




    /// <summary>
    /// When we use the pattern match we want to be able to see how each rule is scored.
    /// 
    /// This is important because we want to be able to say select the rule that matches the most
    /// does not need to match every condition in the rule. Also this makes it so rules with more
    /// conditions (i.e. that are more specific) are valued more than general once.
    /// </summary>
    [Fact]
    public void Scoring()
    {

    }
}
