using System;
using AyanamisTower.SFPM;
using Flecs.NET.Core;

namespace AyanamisTower.NihilEx.Test;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class MemoryKeys
{
    public const string ObservedSuspiciousActivity = "ObservedSuspiciousActivity";
    public const string TimesVisitedTavern = "TimesVisitedTavern";
    public const string PlayerSentiment = "PlayerSentiment";
    public const string QuestFindLostAmulet_State = "QuestFindLostAmuletState";
    public const string RegionAltarActivated = "RegionAltarActivated";
    public const string DialogueLearnedSecretWeakness = "DialogueLearnedSecretWeakness";
}

public class MemoryTest
{
    [Fact]
    public void BasicIdea()
    {
        /*
        A memory is nothing more than an abitrary storage of information.
        In combination with the fuzzy pattern matcher its true power can
        be seen.
        */

        var sceneMemory = new Memory();
        sceneMemory
            .SetValue("concept", "OnHit")
            .SetValue("attacker", "Hunter")
            .SetValue("damage", 12.4);

        var ruleExecutedFlag = "None"; // Use string to identify which rule ran

        List<Rule> rules =
        [
            // Rule 0: Doesn't match facts
            new Rule(
                criterias:
                [
                    new Criteria<string>("who", who => who == "Nick"), // Fact "who" doesn't exist
                    new Criteria<string>(
                        "concept",
                        concept => concept == "onHit"
                    ) // Case mismatch
                    ,
                ],
                payload: () =>
                {
                    ruleExecutedFlag = "Rule 0";
                }
            ),
            // Rule 1: Matches exactly (3 criteria) - MOST SPECIFIC
            new Rule(
                criterias:
                [
                    new Criteria<string>("attacker", attacker => attacker == "Hunter"),
                    new Criteria<string>("concept", concept => concept == "OnHit"), // Correct case
                    new Criteria<double>("damage", damage => damage == 12.4),
                ],
                payload: () =>
                {
                    ruleExecutedFlag = "Rule 1";
                } // This payload should run
            ),
            // Rule 2: Matches (2 criteria) - Less specific than Rule 1
            new Rule(
                criterias:
                [
                    new Criteria<string>("concept", concept => concept == "OnHit"),
                    new Criteria<double>("damage", damage => damage > 10.0),
                ],
                payload: () =>
                {
                    ruleExecutedFlag = "Rule 2";
                }
            ),
            // Rule 3: Matches (2 criteria) - Less specific than Rule 1
            new Rule(
                criterias:
                [
                    new Criteria<string>("attacker", attacker => attacker.StartsWith('H')),
                    new Criteria<double>("damage", damage => damage < 20.0),
                ],
                payload: () =>
                {
                    ruleExecutedFlag = "Rule 3";
                }
            ),
        ];

        rules.Match(sceneMemory);
        Assert.NotEqual("None", ruleExecutedFlag);
    }

    [Fact]
    public void FuzzyPatternMatcherIntegration() { }
}
