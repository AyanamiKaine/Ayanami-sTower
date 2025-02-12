using Flecs.NET.Core;

namespace SFPM.Flecs.Tests;


public struct NPC { };
public record struct Player { };
public record struct Health(int Value);
public record struct Position(int X, int Y);
public record struct Name(string Value) : IComparable<Name>
{
    public readonly int CompareTo(Name other) => Value.CompareTo(other.Value);
}
public record struct Map(string Name) : IComparable<Map>
{
    public readonly int CompareTo(Map other) => Name.CompareTo(other.Name);
}
public class UnitTest1
{

    /// <summary>
    /// Implementing the basic usage. 
    /// </summary>
    [Fact]
    public void BasicIdea()
    {
        World world = World.Create();
        world.Set(new Map("circus"));

        var ruleExecuted = false;
        var player = world.Entity();
        var rule1 = world.Entity();
        var rule2 = world.Entity();
        var rule3 = world.Entity();
        var rule4 = world.Entity();

        player
            .Set<Name>(new("Nick"))
            .Set<Health>(new(100))
            .Set<Position>(new(10, 20));

        /*
        We can implement this because components are stored in arrays, so when we have to entites with one rule component they should be stored in one array.

        We dont need to create a array of rules ourselves, not only that but we get tagged rules for free.

        The cool thing is we can easily modify, add or remove existing rules. 

        We could also go further write rules in C# scripts so they can be modified, added or removed at runtime 
        */

        rule1.Set<NPC, Rule>(new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                ], () =>
                {
                    ruleExecuted = false;
                }));

        rule2.Set<NPC, Rule>(new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<int>("nearAllies", nearAllies => { return nearAllies > 1; }),
                ], () =>
                {
                    ruleExecuted = false;
                }));

        rule3.Set<NPC, Rule>(new Rule([
                    new Criteria<Name>("who", who => { return who.Value == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<Map>("curMap", curMap => { return curMap.Name == "circus"; }),
                ], () =>
                {
                    // In the payload of rules we can interact with the ecs world,
                    // here we simulate a map change.
                    ref Map curMap = ref world.GetMut<Map>();
                    curMap.Name = "AirPort";
                    ruleExecuted = true;
                }));

        rule4.Set<NPC, Rule>(new Rule([
                    new Criteria<string>("who", who => { return who == "Nick"; }),
                    new Criteria<string>("concept", concept => { return concept == "onHit"; }),
                    new Criteria<string>("hitBy", hitBy => { return hitBy == "zombieClown"; }),
                ], () =>
                {
                    ruleExecuted = false;
                }));

        var queryData = new Dictionary<string, object>
            {
                { "concept",    "onHit" },
                { "who",        player.Get<Name>()}, // Here we query the data from an entity and its component
                { "curMap",     world.Get<Map>()}    // If the component data changes it gets automaticall reflected here
            };

        world.Match<NPC>(queryData);

        Assert.True(ruleExecuted);
        Assert.Equal("AirPort", world.Get<Map>().Name);
    }
}
