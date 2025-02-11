namespace SFPM.Tests;

public class QueryUnitTest
{
    [Fact]
    public void Creation()
    {
        var query = new Query();
    }

    [Fact]
    public void AddingKeyValue()
    {
        var query = new Query();

        query
            .Add("concept", "OnHit")
            .Add("attacker", "Hunter")
            .Add("damage", 12.4);
    }
}