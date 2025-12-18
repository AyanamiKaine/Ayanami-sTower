using InvictaDB;
using StellaInvicta;

namespace StellaInvicta.Tests;

/// <summary>
/// Unit tests for the Game class undo/redo functionality.
/// </summary>
public class UndoRedoUnitTest
{
    private record TestEntity(string Id, string Name, int Value);

    /// <summary>
    /// Tests that undo returns the previous state.
    /// </summary>
    [Fact]
    public void Undo_AfterSimulateDay_ReturnsPreviousState()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>()
            .Insert("e1", new TestEntity("e1", "Original", 100))
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);
        var originalTick = db.GetSingleton<long>("CurrentTick");

        // Act
        var afterDay = game.SimulateDay(db);
        var afterDayTick = afterDay.GetSingleton<long>("CurrentTick");

        var undone = game.Undo(afterDay);
        var undoneTick = undone.GetSingleton<long>("CurrentTick");

        // Assert
        Assert.True(afterDayTick > originalTick); // Time progressed
        Assert.Equal(originalTick, undoneTick);   // Undo restored original tick
        Assert.True(game.CanRedo);                // Can redo after undo
        Assert.False(game.CanUndo);               // No more undo available
    }

    /// <summary>
    /// Tests that redo restores the undone state.
    /// </summary>
    [Fact]
    public void Redo_AfterUndo_RestoresState()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);

        // Act
        var afterDay = game.SimulateDay(db);
        var afterDayTick = afterDay.GetSingleton<long>("CurrentTick");

        var undone = game.Undo(afterDay);
        var redone = game.Redo(undone);
        var redoneTick = redone.GetSingleton<long>("CurrentTick");

        // Assert
        Assert.Equal(afterDayTick, redoneTick); // Redo restored the post-simulate state
    }

    /// <summary>
    /// Tests that new action clears redo history.
    /// </summary>
    [Fact]
    public void SimulateDay_AfterUndo_ClearsRedoHistory()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);

        // Act
        var afterFirstDay = game.SimulateDay(db);
        var undone = game.Undo(afterFirstDay);

        Assert.True(game.CanRedo); // Can redo before new action

        var afterSecondDay = game.SimulateDay(undone); // New action

        // Assert
        Assert.False(game.CanRedo); // Redo cleared after new action
        Assert.True(game.CanUndo);  // Can still undo
    }

    /// <summary>
    /// Tests that history is limited by MaxHistorySize.
    /// </summary>
    [Fact]
    public void SimulateDay_ExceedsMaxHistory_TrimsOldestStates()
    {
        // Arrange
        var game = new Game { MaxHistorySize = 3 };
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);

        // Act - Simulate 5 days (should only keep last 3 in history)
        var current = db;
        for (int i = 0; i < 5; i++)
        {
            current = game.SimulateDay(current);
        }

        // Assert
        Assert.Equal(3, game.UndoCount); // Only 3 states kept
    }

    /// <summary>
    /// Tests that ClearHistory removes all history.
    /// </summary>
    [Fact]
    public void ClearHistory_RemovesAllHistory()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);

        var afterDay = game.SimulateDay(db);
        game.Undo(afterDay);

        Assert.True(game.CanUndo || game.CanRedo); // Has some history

        // Act
        game.ClearHistory();

        // Assert
        Assert.False(game.CanUndo);
        Assert.False(game.CanRedo);
        Assert.Equal(0, game.UndoCount);
        Assert.Equal(0, game.RedoCount);
    }

    /// <summary>
    /// Tests that PeekHistory returns state without modifying history.
    /// </summary>
    [Fact]
    public void PeekHistory_ReturnsStateWithoutModifyingHistory()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);

        var afterDay1 = game.SimulateDay(db);
        var afterDay2 = game.SimulateDay(afterDay1);

        var historyCountBefore = game.UndoCount;

        // Act
        var peeked = game.PeekHistory(1); // Most recent (after day 1)

        // Assert
        Assert.NotNull(peeked);
        Assert.Equal(historyCountBefore, game.UndoCount); // History unchanged
    }

    /// <summary>
    /// Tests that JumpToHistory moves multiple steps back.
    /// </summary>
    [Fact]
    public void JumpToHistory_MovesMultipleStepsBack()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);
        var initialTick = db.GetSingleton<long>("CurrentTick");

        var afterDay1 = game.SimulateDay(db);
        var afterDay2 = game.SimulateDay(afterDay1);
        var afterDay3 = game.SimulateDay(afterDay2);

        // Act - Jump back 3 steps to initial state
        var jumped = game.JumpToHistory(afterDay3, 3);

        // Assert
        Assert.Equal(initialTick, jumped.GetSingleton<long>("CurrentTick"));
        Assert.True(game.CanRedo); // Skipped states are in redo
    }

    /// <summary>
    /// Tests that BranchFromHistory allows divergent timelines.
    /// </summary>
    [Fact]
    public void BranchFromHistory_CreatesDivergentTimeline()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .RegisterTable<TestEntity>()
            .Insert("e1", new TestEntity("e1", "Original", 100))
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);

        var afterDay1 = game.SimulateDay(db);
        var day1Tick = afterDay1.GetSingleton<long>("CurrentTick");
        var afterDay2 = game.SimulateDay(afterDay1);

        // Act - Branch from day 1 state
        var (branched, success) = game.BranchFromHistory(afterDay2, 1);

        // Assert
        Assert.True(success);
        Assert.NotSame(afterDay2, branched);

        // Branched state should be the day 1 state (before day 2 simulation)
        var branchedTick = branched.GetSingleton<long>("CurrentTick");
        Assert.Equal(day1Tick, branchedTick); // We're back at day 1

        // The current state (afterDay2) is now saved in history
        Assert.True(game.CanUndo);
    }

    /// <summary>
    /// Tests that undo with no history returns current state.
    /// </summary>
    [Fact]
    public void Undo_WithNoHistory_ReturnsCurrentState()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);

        // Act
        var result = game.Undo(db);

        // Assert
        Assert.Same(db, result); // Returns same instance when nothing to undo
    }

    /// <summary>
    /// Tests nested simulations only save one checkpoint.
    /// </summary>
    [Fact]
    public void SimulateMonth_SavesOnlyOneCheckpoint()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1)); // January has 31 days

        db = game.Init(db);

        // Act
        var afterMonth = game.SimulateMonth(db);

        // Assert - Should only have 1 checkpoint, not 31 (one per day)
        Assert.Equal(1, game.UndoCount);
    }

    #region Monthly/Yearly Snapshot Tests

    /// <summary>
    /// Tests that SimulateMonth creates a monthly snapshot.
    /// </summary>
    [Fact]
    public void SimulateMonth_CreatesMonthlySnapshot()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 3, 1)); // March 1, 2025

        db = game.Init(db);

        // Act
        var afterMonth = game.SimulateMonth(db);

        // Assert
        Assert.Equal(1, game.MonthlySnapshotCount);
        var snapshot = game.GetMonthlySnapshot(2025, 3);
        Assert.NotNull(snapshot);
        Assert.Equal(new DateTime(2025, 3, 1), snapshot.GameDate);
    }

    /// <summary>
    /// Tests that SimulateYear creates yearly snapshot.
    /// Note: Without DateSystem, the game date doesn't auto-advance, so each month has the same key.
    /// This tests the yearly snapshot is created.
    /// </summary>
    [Fact]
    public void SimulateYear_CreatesYearlySnapshot()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1)); // Jan 1, 2025

        db = game.Init(db);

        // Act
        var afterYear = game.SimulateYear(db);

        // Assert
        Assert.Equal(1, game.YearlySnapshotCount);

        var yearlySnapshot = game.GetYearlySnapshot(2025);
        Assert.NotNull(yearlySnapshot);
        Assert.Equal(new DateTime(2025, 1, 1), yearlySnapshot.GameDate);
    }

    /// <summary>
    /// Tests that monthly snapshots with different dates are stored separately.
    /// </summary>
    [Fact]
    public void MonthlySnapshots_DifferentMonthsAreSavedSeparately()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);

        // Act - Manually create snapshots for different months
        game.CreateMonthlySnapshot(db);

        db = db.InsertSingleton(new DateTime(2025, 2, 1));
        game.CreateMonthlySnapshot(db);

        db = db.InsertSingleton(new DateTime(2025, 3, 1));
        game.CreateMonthlySnapshot(db);

        // Assert
        Assert.Equal(3, game.MonthlySnapshotCount);
        Assert.NotNull(game.GetMonthlySnapshot(2025, 1));
        Assert.NotNull(game.GetMonthlySnapshot(2025, 2));
        Assert.NotNull(game.GetMonthlySnapshot(2025, 3));
    }

    /// <summary>
    /// Tests that monthly snapshots are trimmed when limit exceeded.
    /// </summary>
    [Fact]
    public void MonthlySnapshots_AreTrimmedWhenLimitExceeded()
    {
        // Arrange
        var game = new Game { MaxMonthlySnapshots = 3 };
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);

        // Act - Create 6 monthly snapshots manually
        for (int month = 1; month <= 6; month++)
        {
            db = db.InsertSingleton(new DateTime(2025, month, 1));
            game.CreateMonthlySnapshot(db);
        }

        // Assert - Only 3 kept (newest ones)
        Assert.Equal(3, game.MonthlySnapshotCount);

        // Oldest should be removed
        Assert.Null(game.GetMonthlySnapshot(2025, 1));
        Assert.Null(game.GetMonthlySnapshot(2025, 2));
        Assert.Null(game.GetMonthlySnapshot(2025, 3));

        // Newest should remain
        Assert.NotNull(game.GetMonthlySnapshot(2025, 4));
        Assert.NotNull(game.GetMonthlySnapshot(2025, 5));
        Assert.NotNull(game.GetMonthlySnapshot(2025, 6));
    }

    /// <summary>
    /// Tests that JumpToMonth returns to the correct state.
    /// </summary>
    [Fact]
    public void JumpToMonth_ReturnsToSnapshotState()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);
        var initialTick = db.GetSingleton<long>("CurrentTick");

        // Create snapshots for 3 different months
        game.CreateMonthlySnapshot(db);

        db = db.InsertSingleton(new DateTime(2025, 2, 1));
        db = db.InsertSingleton("CurrentTick", 1000L);
        game.CreateMonthlySnapshot(db);

        db = db.InsertSingleton(new DateTime(2025, 3, 1));
        db = db.InsertSingleton("CurrentTick", 2000L);
        game.CreateMonthlySnapshot(db);

        // Act - Jump back to January
        var jumped = game.JumpToMonth(db, 2025, 1);
        var jumpedTick = jumped.GetSingleton<long>("CurrentTick");

        // Assert
        Assert.Equal(initialTick, jumpedTick);
        Assert.True(game.CanUndo); // Current state saved to undo
    }

    /// <summary>
    /// Tests that JumpToYear returns to the correct state.
    /// </summary>
    [Fact]
    public void JumpToYear_ReturnsToSnapshotState()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);
        var year2025Tick = db.GetSingleton<long>("CurrentTick");

        // Create snapshot for 2025
        game.CreateYearlySnapshot(db);

        // Advance to 2026
        db = db.InsertSingleton(new DateTime(2026, 1, 1));
        db = db.InsertSingleton("CurrentTick", 5000L);
        game.CreateYearlySnapshot(db);

        // Advance to 2027
        db = db.InsertSingleton(new DateTime(2027, 1, 1));
        db = db.InsertSingleton("CurrentTick", 10000L);

        // Act - Jump back to 2025
        var jumped = game.JumpToYear(db, 2025);
        var jumpedTick = jumped.GetSingleton<long>("CurrentTick");

        // Assert
        Assert.Equal(year2025Tick, jumpedTick);
    }

    /// <summary>
    /// Tests that GetMonthlySnapshots returns snapshots in order.
    /// </summary>
    [Fact]
    public void GetMonthlySnapshots_ReturnsInChronologicalOrder()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);

        // Create snapshots in non-chronological order
        db = db.InsertSingleton(new DateTime(2025, 3, 1));
        game.CreateMonthlySnapshot(db);

        db = db.InsertSingleton(new DateTime(2025, 1, 1));
        game.CreateMonthlySnapshot(db);

        db = db.InsertSingleton(new DateTime(2025, 4, 1));
        game.CreateMonthlySnapshot(db);

        db = db.InsertSingleton(new DateTime(2025, 2, 1));
        game.CreateMonthlySnapshot(db);

        // Act
        var snapshots = game.GetMonthlySnapshots();

        // Assert
        Assert.Equal(4, snapshots.Count);
        Assert.Equal("2025-01", snapshots[0].Key);
        Assert.Equal("2025-02", snapshots[1].Key);
        Assert.Equal("2025-03", snapshots[2].Key);
        Assert.Equal("2025-04", snapshots[3].Key);
    }

    /// <summary>
    /// Tests that ClearSnapshots removes all snapshots.
    /// </summary>
    [Fact]
    public void ClearSnapshots_RemovesAllSnapshots()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);
        db = game.SimulateYear(db, saveToHistory: false);

        Assert.True(game.MonthlySnapshotCount > 0);
        Assert.True(game.YearlySnapshotCount > 0);

        // Act
        game.ClearSnapshots();

        // Assert
        Assert.Equal(0, game.MonthlySnapshotCount);
        Assert.Equal(0, game.YearlySnapshotCount);
    }

    /// <summary>
    /// Tests that manual snapshot creation works.
    /// </summary>
    [Fact]
    public void CreateMonthlySnapshot_ManuallyCreatesSnapshot()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 7, 15)); // Mid-July

        db = game.Init(db);

        // Act
        game.CreateMonthlySnapshot(db);

        // Assert
        Assert.Equal(1, game.MonthlySnapshotCount);
        var snapshot = game.GetMonthlySnapshot(2025, 7);
        Assert.NotNull(snapshot);
        Assert.Equal(new DateTime(2025, 7, 15), snapshot.GameDate);
    }

    /// <summary>
    /// Tests that snapshots can be disabled.
    /// </summary>
    [Fact]
    public void SimulateMonth_WithSnapshotsDisabled_DoesNotCreateSnapshot()
    {
        // Arrange
        var game = new Game();
        var db = new InvictaDatabase()
            .InsertSingleton(new DateTime(2025, 1, 1));

        db = game.Init(db);

        // Act
        var afterMonth = game.SimulateMonth(db, saveSnapshots: false);

        // Assert
        Assert.Equal(0, game.MonthlySnapshotCount);
    }

    #endregion
}
