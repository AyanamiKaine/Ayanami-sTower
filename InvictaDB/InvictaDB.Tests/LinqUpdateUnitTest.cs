namespace InvictaDB.Tests;

/// <summary>
/// Tests demonstrating LINQ queries combined with database updates
/// </summary>
public class LinqUpdateUnitTest
{
    internal record Person(string Name, int Age, string Department, decimal Salary);
    internal record Building(string Name, string City, Ref<Person> Owner, int Floors);
    internal record Order(string ProductName, decimal Price, Ref<Person> Customer, bool Shipped);

    private InvictaDatabase CreateTestDatabase()
    {
        var db = new InvictaDatabase()
            .RegisterTable<Person>()
            .RegisterTable<Building>()
            .RegisterTable<Order>();

        // People
        db = db.Insert("alice", new Person("Alice", 30, "Engineering", 80000m));
        db = db.Insert("bob", new Person("Bob", 25, "Marketing", 60000m));
        db = db.Insert("charlie", new Person("Charlie", 35, "Engineering", 90000m));
        db = db.Insert("diana", new Person("Diana", 28, "Marketing", 65000m));

        // Buildings
        db = db.Insert("tower", new Building("Tower", "New York", "alice", 50));
        db = db.Insert("plaza", new Building("Plaza", "Boston", "bob", 30));
        db = db.Insert("center", new Building("Center", "New York", "charlie", 40));

        // Orders
        db = db.Insert("order1", new Order("Laptop", 1200m, "alice", false));
        db = db.Insert("order2", new Order("Phone", 800m, "alice", false));
        db = db.Insert("order3", new Order("Tablet", 500m, "bob", true));
        db = db.Insert("order4", new Order("Monitor", 300m, "charlie", false));

        return db;
    }

    /// <summary>
    /// Query and update a single entity
    /// </summary>
    [Fact]
    public void QueryAndUpdate_SingleEntity()
    {
        var db = CreateTestDatabase();

        // Find Alice and give her a raise
        var alice = db.GetEntry<Person>("alice");
        var updatedAlice = alice with { Salary = alice.Salary * 1.10m }; // 10% raise

        db = db.Insert("alice", updatedAlice);

        var result = db.GetEntry<Person>("alice");
        Assert.Equal(88000m, result.Salary);
    }

    /// <summary>
    /// Query by condition and update all matching entities
    /// </summary>
    [Fact]
    public void QueryAndUpdate_AllMatchingEntities()
    {
        var db = CreateTestDatabase();

        // Give all engineers a 15% raise
        var engineers = db.GetTable<Person>()
            .Where(kvp => kvp.Value.Department == "Engineering")
            .ToList();

        foreach (var kvp in engineers)
        {
            var updated = kvp.Value with { Salary = kvp.Value.Salary * 1.15m };
            db = db.Insert(kvp.Key, updated);
        }

        Assert.Equal(92000m, db.GetEntry<Person>("alice").Salary);   // 80000 * 1.15
        Assert.Equal(103500m, db.GetEntry<Person>("charlie").Salary); // 90000 * 1.15
        Assert.Equal(60000m, db.GetEntry<Person>("bob").Salary);      // Unchanged (Marketing)
    }

    /// <summary>
    /// Bulk update using LINQ Aggregate/Fold pattern
    /// </summary>
    [Fact]
    public void QueryAndUpdate_BulkUpdateWithAggregate()
    {
        var db = CreateTestDatabase();

        // Age everyone by 1 year using Aggregate
        db = db.GetTable<Person>()
            .Aggregate(db, (currentDb, kvp) =>
                currentDb.Insert(kvp.Key, kvp.Value with { Age = kvp.Value.Age + 1 }));

        Assert.Equal(31, db.GetEntry<Person>("alice").Age);
        Assert.Equal(26, db.GetEntry<Person>("bob").Age);
        Assert.Equal(36, db.GetEntry<Person>("charlie").Age);
        Assert.Equal(29, db.GetEntry<Person>("diana").Age);
    }

    /// <summary>
    /// Conditional bulk update
    /// </summary>
    [Fact]
    public void QueryAndUpdate_ConditionalBulkUpdate()
    {
        var db = CreateTestDatabase();

        // Ship all orders over $500
        var expensiveOrders = db.GetTable<Order>()
            .Where(kvp => kvp.Value.Price > 500m && !kvp.Value.Shipped)
            .ToList();

        foreach (var kvp in expensiveOrders)
        {
            db = db.Insert(kvp.Key, kvp.Value with { Shipped = true });
        }

        Assert.True(db.GetEntry<Order>("order1").Shipped);  // Laptop $1200
        Assert.True(db.GetEntry<Order>("order2").Shipped);  // Phone $800
        Assert.True(db.GetEntry<Order>("order3").Shipped);  // Tablet $500 - was already shipped
        Assert.False(db.GetEntry<Order>("order4").Shipped); // Monitor $300 - below threshold
    }

    /// <summary>
    /// Update based on aggregated data from another table
    /// </summary>
    [Fact]
    public void QueryAndUpdate_BasedOnRelatedData()
    {
        var db = CreateTestDatabase();

        // Give bonus to people based on their total order value
        var orderTotalsByCustomer = db.GetTable<Order>()
            .GroupBy(kvp => kvp.Value.Customer.Id)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.Value.Price));

        foreach (var (personId, totalOrders) in orderTotalsByCustomer)
        {
            var person = db.GetEntry<Person>(personId);
            var bonus = totalOrders * 0.01m; // 1% of order total as bonus
            db = db.Insert(personId, person with { Salary = person.Salary + bonus });
        }

        Assert.Equal(80020m, db.GetEntry<Person>("alice").Salary);   // 80000 + (2000 * 0.01)
        Assert.Equal(60005m, db.GetEntry<Person>("bob").Salary);     // 60000 + (500 * 0.01)
        Assert.Equal(90003m, db.GetEntry<Person>("charlie").Salary); // 90000 + (300 * 0.01)
    }

    /// <summary>
    /// Update entity with reference to another entity
    /// </summary>
    [Fact]
    public void QueryAndUpdate_ChangeReference()
    {
        var db = CreateTestDatabase();

        // Transfer Tower ownership from Alice to Charlie
        var tower = db.GetEntry<Building>("tower");
        var updatedTower = tower with { Owner = "charlie" };
        db = db.Insert("tower", updatedTower);

        var result = db.GetEntry<Building>("tower");
        Assert.Equal("charlie", result.Owner.Id);
        Assert.Equal("Charlie", result.Owner.Resolve(db).Name);
    }

    /// <summary>
    /// Update multiple related entities in a transaction-like manner
    /// </summary>
    [Fact]
    public void QueryAndUpdate_MultipleRelatedEntities()
    {
        var db = CreateTestDatabase();

        // Promote Diana: change department and give raise
        var diana = db.GetEntry<Person>("diana");
        var promotedDiana = diana with
        {
            Department = "Engineering",
            Salary = diana.Salary * 1.25m
        };
        db = db.Insert("diana", promotedDiana);

        // Also transfer a building to her
        var plaza = db.GetEntry<Building>("plaza");
        db = db.Insert("plaza", plaza with { Owner = "diana" });

        var resultDiana = db.GetEntry<Person>("diana");
        var resultPlaza = db.GetEntry<Building>("plaza");

        Assert.Equal("Engineering", resultDiana.Department);
        Assert.Equal(81250m, resultDiana.Salary);
        Assert.Equal("diana", resultPlaza.Owner.Id);
    }

    /// <summary>
    /// Query, transform, and insert new entries based on existing data
    /// </summary>
    [Fact]
    public void QueryAndInsert_DeriveNewEntries()
    {
        var db = CreateTestDatabase();

        // Create follow-up orders for all shipped orders
        var shippedOrders = db.GetTable<Order>()
            .Where(kvp => kvp.Value.Shipped)
            .ToList();

        foreach (var kvp in shippedOrders)
        {
            var followUp = new Order(
                ProductName: $"{kvp.Value.ProductName} Warranty",
                Price: kvp.Value.Price * 0.1m,
                Customer: kvp.Value.Customer,
                Shipped: false
            );
            db = db.Insert($"{kvp.Key}_warranty", followUp);
        }

        var warrantyOrder = db.GetEntry<Order>("order3_warranty");
        Assert.Equal("Tablet Warranty", warrantyOrder.ProductName);
        Assert.Equal(50m, warrantyOrder.Price);
        Assert.Equal("bob", warrantyOrder.Customer.Id);
    }

    /// <summary>
    /// Update with computed values from LINQ aggregation
    /// </summary>
    [Fact]
    public void QueryAndUpdate_WithComputedAggregation()
    {
        var db = CreateTestDatabase();

        // Set each person's salary to department average if below it
        var deptAverages = db.GetTable<Person>()
            .GroupBy(kvp => kvp.Value.Department)
            .ToDictionary(g => g.Key, g => g.Average(p => p.Value.Salary));

        var belowAverage = db.GetTable<Person>()
            .Where(kvp => kvp.Value.Salary < deptAverages[kvp.Value.Department])
            .ToList();

        foreach (var kvp in belowAverage)
        {
            var avgSalary = deptAverages[kvp.Value.Department];
            db = db.Insert(kvp.Key, kvp.Value with { Salary = avgSalary });
        }

        // Engineering avg was 85000, Alice was 80000 -> now 85000
        Assert.Equal(85000m, db.GetEntry<Person>("alice").Salary);
        // Marketing avg was 62500, Bob was 60000 -> now 62500
        Assert.Equal(62500m, db.GetEntry<Person>("bob").Salary);
        // Charlie and Diana were above average, unchanged
        Assert.Equal(90000m, db.GetEntry<Person>("charlie").Salary);
        Assert.Equal(65000m, db.GetEntry<Person>("diana").Salary);
    }

    /// <summary>
    /// Chain multiple query-update operations
    /// </summary>
    [Fact]
    public void QueryAndUpdate_ChainedOperations()
    {
        var db = CreateTestDatabase();

        // Step 1: Ship all pending orders
        db = db.GetTable<Order>()
            .Where(kvp => !kvp.Value.Shipped)
            .Aggregate(db, (currentDb, kvp) =>
                currentDb.Insert(kvp.Key, kvp.Value with { Shipped = true }));

        // Step 2: Add floors to all NY buildings
        db = db.GetTable<Building>()
            .Where(kvp => kvp.Value.City == "New York")
            .Aggregate(db, (currentDb, kvp) =>
                currentDb.Insert(kvp.Key, kvp.Value with { Floors = kvp.Value.Floors + 10 }));

        // Step 3: Give raises to building owners
        var ownerIds = db.GetTable<Building>()
            .Select(kvp => kvp.Value.Owner.Id)
            .Distinct()
            .ToList();

        foreach (var ownerId in ownerIds)
        {
            var owner = db.GetEntry<Person>(ownerId);
            db = db.Insert(ownerId, owner with { Salary = owner.Salary + 5000m });
        }

        // Verify all operations
        Assert.True(db.GetTable<Order>().All(kvp => kvp.Value.Shipped));
        Assert.Equal(60, db.GetEntry<Building>("tower").Floors);  // 50 + 10
        Assert.Equal(50, db.GetEntry<Building>("center").Floors); // 40 + 10
        Assert.Equal(30, db.GetEntry<Building>("plaza").Floors);  // Boston, unchanged
        Assert.Equal(85000m, db.GetEntry<Person>("alice").Salary);
        Assert.Equal(65000m, db.GetEntry<Person>("bob").Salary);
        Assert.Equal(95000m, db.GetEntry<Person>("charlie").Salary);
    }

    /// <summary>
    /// Query with join-like operation and update
    /// </summary>
    [Fact]
    public void QueryAndUpdate_JoinAndUpdate()
    {
        var db = CreateTestDatabase();

        // Find buildings owned by engineers and add 5 floors
        var engineerIds = db.GetTable<Person>()
            .Where(kvp => kvp.Value.Department == "Engineering")
            .Select(kvp => kvp.Key)
            .ToHashSet();

        var engineerBuildings = db.GetTable<Building>()
            .Where(kvp => engineerIds.Contains(kvp.Value.Owner.Id))
            .ToList();

        foreach (var kvp in engineerBuildings)
        {
            db = db.Insert(kvp.Key, kvp.Value with { Floors = kvp.Value.Floors + 5 });
        }

        Assert.Equal(55, db.GetEntry<Building>("tower").Floors);  // Alice (Engineer)
        Assert.Equal(45, db.GetEntry<Building>("center").Floors); // Charlie (Engineer)
        Assert.Equal(30, db.GetEntry<Building>("plaza").Floors);  // Bob (Marketing) - unchanged
    }

    /// <summary>
    /// Immutability verification - original database unchanged
    /// </summary>
    [Fact]
    public void QueryAndUpdate_OriginalDatabaseUnchanged()
    {
        var originalDb = CreateTestDatabase();
        var originalAliceSalary = originalDb.GetEntry<Person>("alice").Salary;

        // Make updates on a new reference
        var updatedDb = originalDb;
        var alice = updatedDb.GetEntry<Person>("alice");
        updatedDb = updatedDb.Insert("alice", alice with { Salary = 999999m });

        // Original is unchanged
        Assert.Equal(originalAliceSalary, originalDb.GetEntry<Person>("alice").Salary);
        Assert.Equal(999999m, updatedDb.GetEntry<Person>("alice").Salary);
    }

    /// <summary>
    /// Time-travel: keep snapshots and compare
    /// </summary>
    [Fact]
    public void QueryAndUpdate_TimeTravelWithSnapshots()
    {
        var db = CreateTestDatabase();
        var snapshots = new List<InvictaDatabase> { db };

        // Round 1: Give everyone a raise
        db = db.GetTable<Person>()
            .Aggregate(db, (currentDb, kvp) =>
                currentDb.Insert(kvp.Key, kvp.Value with { Salary = kvp.Value.Salary * 1.1m }));
        snapshots.Add(db);

        // Round 2: Another raise
        db = db.GetTable<Person>()
            .Aggregate(db, (currentDb, kvp) =>
                currentDb.Insert(kvp.Key, kvp.Value with { Salary = kvp.Value.Salary * 1.1m }));
        snapshots.Add(db);

        // We can query any snapshot
        Assert.Equal(80000m, snapshots[0].GetEntry<Person>("alice").Salary);  // Original
        Assert.Equal(88000m, snapshots[1].GetEntry<Person>("alice").Salary);  // After 1st raise
        Assert.Equal(96800m, snapshots[2].GetEntry<Person>("alice").Salary);  // After 2nd raise
    }

    /// <summary>
    /// Rollback by using previous snapshot
    /// </summary>
    [Fact]
    public void QueryAndUpdate_RollbackToSnapshot()
    {
        var db = CreateTestDatabase();
        var checkpoint = db; // Save checkpoint

        // Make some bad updates
        db = db.Insert("alice", db.GetEntry<Person>("alice") with { Salary = 0m });
        db = db.Insert("bob", db.GetEntry<Person>("bob") with { Salary = 0m });

        Assert.Equal(0m, db.GetEntry<Person>("alice").Salary);

        // Rollback
        db = checkpoint;

        Assert.Equal(80000m, db.GetEntry<Person>("alice").Salary);
        Assert.Equal(60000m, db.GetEntry<Person>("bob").Salary);
    }
}
