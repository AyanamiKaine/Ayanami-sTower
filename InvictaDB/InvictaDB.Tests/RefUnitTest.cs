namespace InvictaDB.Tests;

/// <summary>
/// Tests for the Ref&lt;T&gt; reference type
/// </summary>
public class RefUnitTest
{
    internal record Person(string Name, int Age);
    internal record Building(string Name, Ref<Person> Owner);
    internal record Company(string Name, Ref<Person> CEO, Ref<Building> Headquarters);

    /// <summary>
    /// Basic reference creation and resolution
    /// </summary>
    [Fact]
    public void RefResolve_ReturnsReferencedEntity()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>()
            .RegisterTable<Building>();

        var alice = new Person("Alice", 30);
        db = db.Insert("alice", alice);

        var tower = new Building("Tower", "alice"); // Implicit conversion to Ref<Person>
        db = db.Insert("tower", tower);

        var building = db.Get<Building>("tower");
        var owner = building.Owner.Resolve(db);

        Assert.Equal("Alice", owner.Name);
        Assert.Equal(30, owner.Age);
    }

    /// <summary>
    /// TryResolve returns null for non-existent reference
    /// </summary>
    [Fact]
    public void RefTryResolve_ReturnsNullForMissingEntity()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>()
            .RegisterTable<Building>();

        var building = new Building("Abandoned", "ghost"); // Reference to non-existent person
        db = db.Insert("abandoned", building);

        var retrieved = db.Get<Building>("abandoned");
        var owner = retrieved.Owner.TryResolve(db);

        Assert.Null(owner);
    }

    /// <summary>
    /// Resolve throws for empty reference
    /// </summary>
    [Fact]
    public void RefResolve_ThrowsForEmptyReference()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        var emptyRef = Ref<Person>.Empty;

        Assert.Throws<InvalidOperationException>(() => emptyRef.Resolve(db));
    }

    /// <summary>
    /// TryResolve returns null for empty reference
    /// </summary>
    [Fact]
    public void RefTryResolve_ReturnsNullForEmptyReference()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        var emptyRef = Ref<Person>.Empty;
        var result = emptyRef.TryResolve(db);

        Assert.Null(result);
    }

    /// <summary>
    /// Exists returns true for existing entity
    /// </summary>
    [Fact]
    public void RefExists_ReturnsTrueForExistingEntity()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        db = db.Insert("alice", new Person("Alice", 30));

        Ref<Person> aliceRef = "alice";

        Assert.True(aliceRef.Exists(db));
    }

    /// <summary>
    /// Exists returns false for non-existent entity
    /// </summary>
    [Fact]
    public void RefExists_ReturnsFalseForMissingEntity()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        Ref<Person> ghostRef = "ghost";

        Assert.False(ghostRef.Exists(db));
    }

    /// <summary>
    /// Exists returns false for empty reference
    /// </summary>
    [Fact]
    public void RefExists_ReturnsFalseForEmptyReference()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>();

        var emptyRef = Ref<Person>.Empty;

        Assert.False(emptyRef.Exists(db));
    }

    /// <summary>
    /// Chained references can be resolved
    /// </summary>
    [Fact]
    public void RefChain_CanResolveMultipleReferences()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>()
            .RegisterTable<Building>()
            .RegisterTable<Company>();

        db = db.Insert("ceo", new Person("Bob", 45));
        db = db.Insert("hq", new Building("Headquarters", "ceo"));
        db = db.Insert("acme", new Company("ACME Corp", "ceo", "hq"));

        var company = db.Get<Company>("acme");
        var ceo = company.CEO.Resolve(db);
        var headquarters = company.Headquarters.Resolve(db);
        var buildingOwner = headquarters.Owner.Resolve(db);

        Assert.Equal("Bob", ceo.Name);
        Assert.Equal("Headquarters", headquarters.Name);
        Assert.Equal("Bob", buildingOwner.Name); // CEO owns the HQ building
    }

    /// <summary>
    /// Reference equality works correctly
    /// </summary>
    [Fact]
    public void RefEquality_WorksCorrectly()
    {
        Ref<Person> ref1 = "alice";
        Ref<Person> ref2 = "alice";
        Ref<Person> ref3 = "bob";

        Assert.Equal(ref1, ref2);
        Assert.NotEqual(ref1, ref3);
        Assert.True(ref1 == ref2);
        Assert.True(ref1 != ref3);
    }

    /// <summary>
    /// Empty reference properties work correctly
    /// </summary>
    [Fact]
    public void RefEmpty_PropertiesAreCorrect()
    {
        var emptyRef = Ref<Person>.Empty;
        Ref<Person> validRef = "alice";

        Assert.True(emptyRef.IsEmpty);
        Assert.False(validRef.IsEmpty);
        Assert.Equal(string.Empty, emptyRef.ToString()); // Empty string, not null
        Assert.Equal("alice", validRef.ToString());
    }

    /// <summary>
    /// Reference updates when entity is updated
    /// </summary>
    [Fact]
    public void RefResolve_GetsUpdatedEntity()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>()
            .RegisterTable<Building>();

        db = db.Insert("alice", new Person("Alice", 30));
        db = db.Insert("tower", new Building("Tower", "alice"));

        // Update Alice's age
        db = db.Insert("alice", new Person("Alice", 31));

        var building = db.Get<Building>("tower");
        var owner = building.Owner.Resolve(db);

        Assert.Equal(31, owner.Age); // Gets the updated version
    }
}
