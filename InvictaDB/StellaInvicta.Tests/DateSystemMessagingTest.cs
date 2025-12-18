using InvictaDB;
using InvictaDB.Messaging;
using StellaInvicta;
using StellaInvicta.System.Date;
using static InvictaDB.Messaging.GameMessages;

namespace StellaInvicta.Tests;

/// <summary>
/// Tests for DateSystem messaging functionality.
/// </summary>
public class DateSystemMessagingTest
{
    /// <summary>
    /// DateSystem should send NewHour message on every tick.
    /// </summary>
    [Fact]
    public void DateSystem_SendsNewHourMessage()
    {
        var db = new InvictaDatabase();
        var dateSystem = new DateSystem();
        var game = new Game();
        game.AddSystem(dateSystem.Name, dateSystem);
        db = game.Init(db);
        db = db.ClearMessages(); // Clear any initialization messages

        db = game.SimulateHour(db);

        var hourMessages = db.Messages.GetMessages<NewHour>().ToList();
        Assert.Single(hourMessages);

        var payload = hourMessages[0].GetPayload<NewHour>();
        Assert.NotNull(payload);
        Assert.Equal(new DateTime(1, 1, 1, 1, 0, 0), payload.NewDate);
        Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0), payload.PreviousDate);
    }

    /// <summary>
    /// DateSystem should send NewDay message when day changes.
    /// </summary>
    [Fact]
    public void DateSystem_SendsNewDayMessage()
    {
        var db = new InvictaDatabase();
        var dateSystem = new DateSystem();
        var game = new Game();
        game.AddSystem(dateSystem.Name, dateSystem);
        db = game.Init(db);
        db = db.ClearMessages();

        // Simulate 24 hours to cross into new day
        db = game.SimulateDay(db);

        var dayMessages = db.Messages.GetMessages<NewDay>().ToList();
        Assert.Single(dayMessages);

        var payload = dayMessages[0].GetPayload<NewDay>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.NewDate.Day);
    }

    /// <summary>
    /// DateSystem should send NewMonth message when month changes.
    /// </summary>
    [Fact]
    public void DateSystem_SendsNewMonthMessage()
    {
        var db = new InvictaDatabase();
        var dateSystem = new DateSystem();
        var game = new Game();
        game.AddSystem(dateSystem.Name, dateSystem);
        db = game.Init(db);
        db = db.ClearMessages();

        // Simulate a full month
        db = game.SimulateMonth(db);

        var monthMessages = db.Messages.GetMessages<NewMonth>().ToList();
        Assert.Single(monthMessages);

        var payload = monthMessages[0].GetPayload<NewMonth>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.NewDate.Month);
        Assert.Equal(1, payload.PreviousMonth);
    }

    /// <summary>
    /// DateSystem should send NewYear message when year changes.
    /// </summary>
    [Fact]
    public void DateSystem_SendsNewYearMessage()
    {
        var db = new InvictaDatabase();
        var dateSystem = new DateSystem();
        var game = new Game();
        game.AddSystem(dateSystem.Name, dateSystem);
        db = game.Init(db);
        db = db.ClearMessages();

        // Simulate a full year
        db = game.SimulateYear(db);

        var yearMessages = db.Messages.GetMessages<NewYear>().ToList();
        Assert.Single(yearMessages);

        var payload = yearMessages[0].GetPayload<NewYear>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.NewDate.Year);
        Assert.Equal(1, payload.PreviousYear);
    }

    /// <summary>
    /// DateSystem should send NewSeason message when season changes.
    /// </summary>
    [Fact]
    public void DateSystem_SendsNewSeasonMessage()
    {
        var db = new InvictaDatabase();
        var dateSystem = new DateSystem();
        var game = new Game();
        game.AddSystem(dateSystem.Name, dateSystem);
        db = game.Init(db);
        db = db.ClearMessages();

        // Starting date is Jan 1 (Winter). Simulate until March (Spring)
        // Jan has 31 days, Feb has 28 days = 59 days to get to March
        for (int i = 0; i < 59; i++)
        {
            db = game.SimulateDay(db);
        }

        var seasonMessages = db.Messages.GetMessages<NewSeason>().ToList();
        Assert.Single(seasonMessages);

        var payload = seasonMessages[0].GetPayload<NewSeason>();
        Assert.NotNull(payload);
        Assert.Equal(Season.Spring, payload.Season);
    }

    /// <summary>
    /// DateSystem should send NewWeek message on Mondays.
    /// </summary>
    [Fact]
    public void DateSystem_SendsNewWeekMessageOnMonday()
    {
        var db = new InvictaDatabase();
        var dateSystem = new DateSystem();
        var game = new Game();
        game.AddSystem(dateSystem.Name, dateSystem);
        db = game.Init(db);

        // Jan 1, year 1 is a Monday in .NET's DateTime
        // Simulate to Jan 8 (next Monday) - 7 days
        for (int i = 0; i < 7; i++)
        {
            db = db.ClearMessages();
            db = game.SimulateDay(db);
        }

        var weekMessages = db.Messages.GetMessages<NewWeek>().ToList();
        Assert.Single(weekMessages);

        var payload = weekMessages[0].GetPayload<NewWeek>();
        Assert.NotNull(payload);
        Assert.Equal(DayOfWeek.Monday, payload.NewDate.DayOfWeek);
    }

    /// <summary>
    /// Messages should have correct sender.
    /// </summary>
    [Fact]
    public void DateSystem_MessagesSender_IsDateSystem()
    {
        var db = new InvictaDatabase();
        var dateSystem = new DateSystem();
        var game = new Game();
        game.AddSystem(dateSystem.Name, dateSystem);
        db = game.Init(db);
        db = db.ClearMessages();

        db = game.SimulateHour(db);

        var messages = db.Messages.GetMessagesFrom(DateSystemSender).ToList();
        Assert.Single(messages);
    }

    /// <summary>
    /// Multiple hours should generate multiple NewHour messages.
    /// </summary>
    [Fact]
    public void DateSystem_MultipleHours_MultipleMessages()
    {
        var db = new InvictaDatabase();
        var dateSystem = new DateSystem();
        var game = new Game();
        game.AddSystem(dateSystem.Name, dateSystem);
        db = game.Init(db);
        db = db.ClearMessages();

        // Simulate 5 hours
        for (int i = 0; i < 5; i++)
        {
            db = game.SimulateHour(db);
        }

        var hourMessages = db.Messages.GetMessages<NewHour>().ToList();
        Assert.Equal(5, hourMessages.Count);
    }

    /// <summary>
    /// ConsumeMessages should allow systems to process and clear messages.
    /// </summary>
    [Fact]
    public void DateSystem_ConsumeMessages_AllowsProcessing()
    {
        var db = new InvictaDatabase();
        var dateSystem = new DateSystem();
        var game = new Game();
        game.AddSystem(dateSystem.Name, dateSystem);
        db = game.Init(db);
        db = db.ClearMessages();

        db = game.SimulateDay(db);

        // Consume NewHour messages
        var (hourMessages, dbAfterConsume) = db.ConsumeMessages<NewHour>();

        Assert.Equal(24, hourMessages.Count);
        Assert.Empty(dbAfterConsume.Messages.GetMessages<NewHour>());

        // NewDay message should still be there
        Assert.Single(dbAfterConsume.Messages.GetMessages<NewDay>());
    }
}
