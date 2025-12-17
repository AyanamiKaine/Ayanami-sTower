namespace InvictaDB.Tests;

/// <summary>
/// Tests demonstrating LINQ queries on InvictaDatabase
/// </summary>
public class LinqQueryUnitTest
{
    internal record Person(string Name, int Age, string Department);
    internal record Building(string Name, string City, Ref<Person> Owner);
    internal record Order(string ProductName, decimal Price, Ref<Person> Customer);

    private InvictaDatabase CreateTestDatabase()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>()
            .RegisterTable<Building>()
            .RegisterTable<Order>();

        // People
        db = db.Insert("alice", new Person("Alice", 30, "Engineering"));
        db = db.Insert("bob", new Person("Bob", 25, "Marketing"));
        db = db.Insert("charlie", new Person("Charlie", 35, "Engineering"));
        db = db.Insert("diana", new Person("Diana", 28, "Marketing"));

        // Buildings
        db = db.Insert("tower", new Building("Tower", "New York", "alice"));
        db = db.Insert("plaza", new Building("Plaza", "Boston", "bob"));
        db = db.Insert("center", new Building("Center", "New York", "charlie"));

        // Orders
        db = db.Insert("order1", new Order("Laptop", 1200m, "alice"));
        db = db.Insert("order2", new Order("Phone", 800m, "alice"));
        db = db.Insert("order3", new Order("Tablet", 500m, "bob"));
        db = db.Insert("order4", new Order("Monitor", 300m, "charlie"));

        return db;
    }

    /// <summary>
    /// Filter entities using Where
    /// </summary>
    [Fact]
    public void LinqWhere_FiltersEntities()
    {
        var db = CreateTestDatabase();

        var engineers = db.GetTable<Person>()
            .Where(kvp => kvp.Value.Department == "Engineering")
            .Select(kvp => kvp.Value)
            .ToList();

        Assert.Equal(2, engineers.Count);
        Assert.Contains(engineers, p => p.Name == "Alice");
        Assert.Contains(engineers, p => p.Name == "Charlie");
    }

    /// <summary>
    /// Filter by age range
    /// </summary>
    [Fact]
    public void LinqWhere_FiltersByAgeRange()
    {
        var db = CreateTestDatabase();

        var youngPeople = db.GetTable<Person>()
            .Where(kvp => kvp.Value.Age < 30)
            .Select(kvp => kvp.Value.Name)
            .ToList();

        Assert.Equal(2, youngPeople.Count);
        Assert.Contains("Bob", youngPeople);
        Assert.Contains("Diana", youngPeople);
    }

    /// <summary>
    /// Project entities using Select
    /// </summary>
    [Fact]
    public void LinqSelect_ProjectsToNewType()
    {
        var db = CreateTestDatabase();

        var namesToAges = db.GetTable<Person>()
            .Select(kvp => new { kvp.Value.Name, kvp.Value.Age })
            .OrderBy(x => x.Age)
            .ToList();

        Assert.Equal(4, namesToAges.Count);
        Assert.Equal("Bob", namesToAges[0].Name);
        Assert.Equal(25, namesToAges[0].Age);
    }

    /// <summary>
    /// Order entities using OrderBy
    /// </summary>
    [Fact]
    public void LinqOrderBy_SortsEntities()
    {
        var db = CreateTestDatabase();

        var sortedByAge = db.GetTable<Person>()
            .OrderBy(kvp => kvp.Value.Age)
            .Select(kvp => kvp.Value.Name)
            .ToList();

        Assert.Equal(["Bob", "Diana", "Alice", "Charlie"], sortedByAge);
    }

    /// <summary>
    /// Order descending using OrderByDescending
    /// </summary>
    [Fact]
    public void LinqOrderByDescending_SortsEntitiesDescending()
    {
        var db = CreateTestDatabase();

        var sortedByAgeDesc = db.GetTable<Person>()
            .OrderByDescending(kvp => kvp.Value.Age)
            .Select(kvp => kvp.Value.Name)
            .ToList();

        Assert.Equal(["Charlie", "Alice", "Diana", "Bob"], sortedByAgeDesc);
    }

    /// <summary>
    /// Group entities using GroupBy
    /// </summary>
    [Fact]
    public void LinqGroupBy_GroupsEntities()
    {
        var db = CreateTestDatabase();

        var byDepartment = db.GetTable<Person>()
            .GroupBy(kvp => kvp.Value.Department)
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(2, byDepartment["Engineering"]);
        Assert.Equal(2, byDepartment["Marketing"]);
    }

    /// <summary>
    /// Aggregate using Sum
    /// </summary>
    [Fact]
    public void LinqSum_AggregatesValues()
    {
        var db = CreateTestDatabase();

        var totalOrderValue = db.GetTable<Order>()
            .Sum(kvp => kvp.Value.Price);

        Assert.Equal(2800m, totalOrderValue);
    }

    /// <summary>
    /// Aggregate using Average
    /// </summary>
    [Fact]
    public void LinqAverage_CalculatesAverage()
    {
        var db = CreateTestDatabase();

        var averageAge = db.GetTable<Person>()
            .Average(kvp => kvp.Value.Age);

        Assert.Equal(29.5, averageAge);
    }

    /// <summary>
    /// Find first matching entity using First/FirstOrDefault
    /// </summary>
    [Fact]
    public void LinqFirstOrDefault_FindsFirstMatch()
    {
        var db = CreateTestDatabase();

        var firstEngineer = db.GetTable<Person>()
            .Where(kvp => kvp.Value.Department == "Engineering")
            .Select(kvp => kvp.Value)
            .FirstOrDefault();

        Assert.NotNull(firstEngineer);
        Assert.Equal("Engineering", firstEngineer.Department);
    }

    /// <summary>
    /// Check existence using Any
    /// </summary>
    [Fact]
    public void LinqAny_ChecksExistence()
    {
        var db = CreateTestDatabase();

        var hasEngineers = db.GetTable<Person>()
            .Any(kvp => kvp.Value.Department == "Engineering");

        var hasHR = db.GetTable<Person>()
            .Any(kvp => kvp.Value.Department == "HR");

        Assert.True(hasEngineers);
        Assert.False(hasHR);
    }

    /// <summary>
    /// Count with predicate
    /// </summary>
    [Fact]
    public void LinqCount_CountsMatchingEntities()
    {
        var db = CreateTestDatabase();

        var engineerCount = db.GetTable<Person>()
            .Count(kvp => kvp.Value.Department == "Engineering");

        Assert.Equal(2, engineerCount);
    }

    /// <summary>
    /// Join tables using references
    /// </summary>
    [Fact]
    public void LinqJoin_JoinsTablesViaReference()
    {
        var db = CreateTestDatabase();

        var buildingsWithOwners = db.GetTable<Building>()
            .Select(kvp => new
            {
                BuildingName = kvp.Value.Name,
                City = kvp.Value.City,
                Owner = kvp.Value.Owner.Resolve(db)
            })
            .ToList();

        Assert.Equal(3, buildingsWithOwners.Count);

        var tower = buildingsWithOwners.First(b => b.BuildingName == "Tower");
        Assert.Equal("Alice", tower.Owner.Name);
        Assert.Equal("New York", tower.City);
    }

    /// <summary>
    /// Filter and resolve references
    /// </summary>
    [Fact]
    public void LinqWhereWithRef_FiltersAndResolvesReferences()
    {
        var db = CreateTestDatabase();

        // Find all buildings in New York with their owners
        var nyBuildings = db.GetTable<Building>()
            .Where(kvp => kvp.Value.City == "New York")
            .Select(kvp => new
            {
                Building = kvp.Value.Name,
                OwnerName = kvp.Value.Owner.Resolve(db).Name
            })
            .ToList();

        Assert.Equal(2, nyBuildings.Count);
        Assert.Contains(nyBuildings, b => b.Building == "Tower" && b.OwnerName == "Alice");
        Assert.Contains(nyBuildings, b => b.Building == "Center" && b.OwnerName == "Charlie");
    }

    /// <summary>
    /// Aggregate orders by customer using GroupBy with reference resolution
    /// </summary>
    [Fact]
    public void LinqGroupByRef_GroupsByReferencedEntity()
    {
        var db = CreateTestDatabase();

        var ordersByCustomer = db.GetTable<Order>()
            .GroupBy(kvp => kvp.Value.Customer.Id)
            .Select(g => new
            {
                CustomerName = db.GetEntry<Person>(g.Key).Name,
                TotalSpent = g.Sum(kvp => kvp.Value.Price),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToList();

        Assert.Equal(3, ordersByCustomer.Count);
        Assert.Equal("Alice", ordersByCustomer[0].CustomerName);
        Assert.Equal(2000m, ordersByCustomer[0].TotalSpent);
        Assert.Equal(2, ordersByCustomer[0].OrderCount);
    }

    /// <summary>
    /// Complex query combining multiple LINQ operations
    /// </summary>
    [Fact]
    public void LinqComplex_CombinesMultipleOperations()
    {
        var db = CreateTestDatabase();

        // Find engineers who own buildings, ordered by age
        var engineersWithBuildings = db.GetTable<Person>()
            .Where(p => p.Value.Department == "Engineering" && db.GetTable<Building>().Any(b => b.Value.Owner.Id == p.Key))
            .OrderBy(p => p.Value.Age)
            .Select(p => new
            {
                p.Value.Name,
                p.Value.Age,
                Buildings = db.GetTable<Building>()
                    .Where(b => b.Value.Owner.Id == p.Key)
                    .Select(b => b.Value.Name)
                    .ToList()
            })
            .ToList();

        Assert.Equal(2, engineersWithBuildings.Count);
        Assert.Equal("Alice", engineersWithBuildings[0].Name);
        Assert.Contains("Tower", engineersWithBuildings[0].Buildings);
        Assert.Equal("Charlie", engineersWithBuildings[1].Name);
        Assert.Contains("Center", engineersWithBuildings[1].Buildings);
    }

    /// <summary>
    /// Query using IDs (keys)
    /// </summary>
    [Fact]
    public void LinqWithKeys_QueriesUsingIds()
    {
        var db = CreateTestDatabase();

        // Get specific entries by ID pattern
        var aNames = db.GetTable<Person>()
            .Where(kvp => kvp.Key.StartsWith('a') || kvp.Key.StartsWith('b'))
            .Select(kvp => kvp.Value.Name)
            .ToList();

        Assert.Equal(2, aNames.Count);
        Assert.Contains("Alice", aNames);
        Assert.Contains("Bob", aNames);
    }

    /// <summary>
    /// Use ToDictionary to create lookup
    /// </summary>
    [Fact]
    public void LinqToDictionary_CreatesLookup()
    {
        var db = CreateTestDatabase();

        var personLookupByName = db.GetTable<Person>()
            .ToDictionary(kvp => kvp.Value.Name, kvp => kvp.Value);

        Assert.Equal(30, personLookupByName["Alice"].Age);
        Assert.Equal(25, personLookupByName["Bob"].Age);
    }

    /// <summary>
    /// Use Max/Min to find extremes
    /// </summary>
    [Fact]
    public void LinqMaxMin_FindsExtremes()
    {
        var db = CreateTestDatabase();

        var oldestAge = db.GetTable<Person>().Max(kvp => kvp.Value.Age);
        var youngestAge = db.GetTable<Person>().Min(kvp => kvp.Value.Age);
        var mostExpensiveOrder = db.GetTable<Order>().MaxBy(kvp => kvp.Value.Price);

        Assert.Equal(35, oldestAge);
        Assert.Equal(25, youngestAge);
        Assert.Equal("Laptop", mostExpensiveOrder!.Value.ProductName);
    }

    /// <summary>
    /// Distinct values
    /// </summary>
    [Fact]
    public void LinqDistinct_GetsUniqueValues()
    {
        var db = CreateTestDatabase();

        var departments = db.GetTable<Person>()
            .Select(kvp => kvp.Value.Department)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        Assert.Equal(["Engineering", "Marketing"], departments);
    }

    /// <summary>
    /// Take and Skip for pagination
    /// </summary>
    [Fact]
    public void LinqTakeSkip_Paginates()
    {
        var db = CreateTestDatabase();

        var page1 = db.GetTable<Person>()
            .OrderBy(kvp => kvp.Value.Name)
            .Take(2)
            .Select(kvp => kvp.Value.Name)
            .ToList();

        var page2 = db.GetTable<Person>()
            .OrderBy(kvp => kvp.Value.Name)
            .Skip(2)
            .Take(2)
            .Select(kvp => kvp.Value.Name)
            .ToList();

        Assert.Equal(["Alice", "Bob"], page1);
        Assert.Equal(["Charlie", "Diana"], page2);
    }
}
