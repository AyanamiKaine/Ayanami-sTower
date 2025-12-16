namespace InvictaDB.Tests;
/// <summary>
/// PLACEHOLDER TEST
/// </summary>
public class InvictaDatabaseUnitTest
{
    internal class Person
    {
        public required string Name { get; set; }
        public int Age { get; set; }
    }


    /// <summary>
    /// Table creation for the database by string
    /// </summary>
    [Fact]
    public void RegisteringTableByString()
    {
        InvictaDatabase db = [];
        db = db.RegisterTable<Person>("People");

        Assert.True(db.TableExists("People"));
    }

    /// <summary>
    /// Table creation for the database by string
    /// </summary>
    [Fact]
    public void RegisteringTableByType()
    {
        InvictaDatabase db = [];
        db = db.RegisterTable<Person>();

        Assert.True(db.TableExists<Person>());
    }

    /// <summary>
    /// Table creation for the database by string
    /// </summary>
    [Fact]
    public void DoesTableExistString()
    {
        InvictaDatabase db = [];
        db = db.RegisterTable<Person>("People");

        Assert.True(db.TableExists("People"));
    }

    /// <summary>
    /// Table creation for the database by type
    /// </summary>
    [Fact]
    public void DoesTableExistType()
    {
        InvictaDatabase db = [];
        db = db.RegisterTable<Person>("People");

        Assert.True(db.TableExists<Person>());
    }

    /// <summary>
    /// Get entry by ID
    /// </summary>
    [Fact]
    public void GetEntryById()
    {
        InvictaDatabase db = [];
        db = db.RegisterTable<Person>("People");

        var person = new Person { Name = "Alice", Age = 30 };
        db = db.Insert("person1", person);

        var retrievedPerson = db.GetEntry<Person>("person1");
        Assert.Equal("Alice", retrievedPerson.Name);
        Assert.Equal(30, retrievedPerson.Age);
    }

    /// <summary>
    /// Getting a table by name
    /// </summary>
    [Fact]
    public void GetTable()
    {
        InvictaDatabase db = [];
        db = db.RegisterTable<Person>("People");

        var person1 = new Person { Name = "Alice", Age = 30 };
        var person2 = new Person { Name = "Bob", Age = 25 };
        db = db.Insert("person1", person1);
        db = db.Insert("person2", person2);

        var table = db.GetTable<Person>("People");
        Assert.Equal(2, table.Count);
        Assert.Equal("Alice", table["person1"].Name);
        Assert.Equal("Bob", table["person2"].Name);
    }
}
