using InvictaDB;
using InvictaDB.Persistence;

namespace InvictaDB.Tests;

/// <summary>
/// Unit tests for the database persistence functionality.
/// </summary>
public class DatabasePersistenceUnitTest
{
    private record TestCharacter(string Id, string Name, int Age);
    private record GameConfig(int Turn, DateTime GameDate);

    /// <summary>
    /// Tests that a database can be serialized to JSON.
    /// </summary>
    [Fact]
    public void Serialize_DatabaseWithData_ReturnsValidJson()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestCharacter>()
            .Insert("char1", new TestCharacter("char1", "Alice", 25))
            .Insert("char2", new TestCharacter("char2", "Bob", 30))
            .InsertSingleton(new GameConfig(5, new DateTime(2025, 1, 15)));

        // Act
        var json = db.Serialize(turn: 5, description: "Test Save");

        // Assert
        Assert.NotEmpty(json);
        Assert.Contains("Alice", json);
        Assert.Contains("Bob", json);
        Assert.Contains("Test Save", json);
    }

    /// <summary>
    /// Tests that a snapshot captures metadata correctly.
    /// </summary>
    [Fact]
    public void CreateSnapshot_IncludesMetadata()
    {
        // Arrange
        var db = new InvictaDatabase()
            .RegisterTable<TestCharacter>()
            .Insert("char1", new TestCharacter("char1", "Alice", 25));

        // Act
        var snapshot = db.CreateSnapshot(turn: 42, description: "Autosave Turn 42");

        // Assert
        Assert.Equal(42, snapshot.Metadata.Turn);
        Assert.Equal("Autosave Turn 42", snapshot.Metadata.Description);
        Assert.True(snapshot.Metadata.CreatedAt <= DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Tests synchronous save and load to file.
    /// </summary>
    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesData()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            var db = new InvictaDatabase()
                .RegisterTable<TestCharacter>()
                .Insert("char1", new TestCharacter("char1", "Alice", 25))
                .Insert("char2", new TestCharacter("char2", "Bob", 30));

            // Act
            db.SaveToFile(tempFile, turn: 10);
            var loadedDb = DatabasePersistence.LoadFromFile(tempFile);

            // Assert
            var loadedTable = loadedDb.GetTable<TestCharacter>();
            Assert.Equal(2, loadedTable.Count);

            var alice = loadedDb.GetEntry<TestCharacter>("char1");
            Assert.Equal("Alice", alice.Name);
            Assert.Equal(25, alice.Age);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Tests async save completes without blocking.
    /// </summary>
    [Fact]
    public async Task SaveToFileAsync_CompletesSuccessfully()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            var db = new InvictaDatabase()
                .RegisterTable<TestCharacter>()
                .Insert("char1", new TestCharacter("char1", "Alice", 25));

            // Act
            await db.SaveToFileAsync(tempFile, turn: 1, description: "Async Save");

            // Assert
            Assert.True(File.Exists(tempFile));
            var content = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("Alice", content);
            Assert.Contains("Async Save", content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Tests fire-and-forget save with completion callback.
    /// </summary>
    [Fact]
    public async Task FireAndForgetSaveAsync_CallsOnComplete()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var completionSource = new TaskCompletionSource<bool>();

        try
        {
            var db = new InvictaDatabase()
                .RegisterTable<TestCharacter>()
                .Insert("char1", new TestCharacter("char1", "Alice", 25));

            // Act
            var task = db.FireAndForgetSaveAsync(
                tempFile,
                turn: 1,
                onComplete: () => completionSource.SetResult(true),
                onError: ex => completionSource.SetException(ex));

            // Wait for completion callback (with timeout)
            var completed = await Task.WhenAny(completionSource.Task, Task.Delay(5000));

            // Assert
            Assert.Same(completionSource.Task, completed);
            Assert.True(await completionSource.Task);
            Assert.True(File.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Tests that async save is truly non-blocking (game can continue).
    /// </summary>
    [Fact]
    public async Task SaveToFileAsync_DoesNotBlockGameLoop()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            var db = new InvictaDatabase()
                .RegisterTable<TestCharacter>()
                .Insert("char1", new TestCharacter("char1", "Alice", 25));

            // Act - Start async save
            var saveTask = db.SaveToFileAsync(tempFile, turn: 5);

            // Simulate game continuing - these operations should work immediately
            // while the save is happening in the background
            var db2 = db.Insert("char2", new TestCharacter("char2", "Bob", 30));
            var db3 = db2.Insert("char3", new TestCharacter("char3", "Charlie", 35));

            // Assert - Game state progressed independently
            Assert.Equal(3, db3.GetTable<TestCharacter>().Count);
            Assert.Single(db.GetTable<TestCharacter>()); // Original unchanged (immutable!)

            // Wait for save to complete
            await saveTask;

            // The saved file should only have the original data (Turn 5 snapshot)
            var loadedDb = DatabasePersistence.LoadFromFile(tempFile);
            Assert.Single(loadedDb.GetTable<TestCharacter>());
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Tests loading async.
    /// </summary>
    [Fact]
    public async Task LoadFromFileAsync_LoadsCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            var db = new InvictaDatabase()
                .RegisterTable<TestCharacter>()
                .Insert("char1", new TestCharacter("char1", "Alice", 25));

            db.SaveToFile(tempFile);

            // Act
            var loadedDb = await DatabasePersistence.LoadFromFileAsync(tempFile);

            // Assert
            var alice = loadedDb.GetEntry<TestCharacter>("char1");
            Assert.Equal("Alice", alice.Name);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
