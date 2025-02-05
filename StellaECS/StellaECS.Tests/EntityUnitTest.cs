namespace StellaECS.Tests;

public class EntityUnitTest
{
    [Fact]
    public void CreationShouldReturnNewEntity()
    {
        World world = new();
        var entity = world.CreateEntity();

        var expectedEntityID = 1;
        var actualEntityID = entity.ID;

        Assert.Equal(expectedEntityID, actualEntityID);
    }

    [Fact]
    public void EntityDefineName()
    {
        World world = new();
        var entity = world.CreateEntity("Cool Name");

        var expectedEntityName = "Cool Name";
        var actualEntityName = entity.Name;

        Assert.Equal(expectedEntityName, actualEntityName);
    }

    [Fact]
    public void DefineMultipleEntites()
    {
        World world = new();

        for (int i = 0; i < 1000; i++)
        {
            world.CreateEntity();
        }

        var expectedNumberOfEntities = 1000;
        var actualEntityNumber = world.Size;

        Assert.Equal(expectedNumberOfEntities, actualEntityNumber);
    }
}
