using System; // For Action
using System.Collections.Generic; // For Dictionary, List
using AyanamisTower.SFPM; // Ensure this using is present
using NLog;
using Xunit; // Assuming using xUnit
using Xunit.Abstractions;

namespace AyanamisTower.SFPM.Tests
{
    /// <summary>
    /// Contains unit tests for the <see cref="Rule"/> class evaluation logic,
    /// tested via the Query.Match mechanism.
    /// </summary>
    public class RuleUnitTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        /// <summary>
        /// Initializes logging and test output helper.
        /// </summary>
        public RuleUnitTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            // Deactivate Logging for unit tests if desired
            LogManager.Configuration?.LoggingRules.Clear(); // Added null check
            LogManager.ReconfigExistingLoggers();
        }

        /// <summary>
        /// Tests evaluation of a simple rule with two criteria that strictly match all facts.
        /// </summary>
        [Fact]
        public void Evaluate_RuleWithTwoMatchingCriteria_ExecutesPayload() // Renamed for clarity
        {
            // Arrange
            var payloadExecuted = false;
            var rule1 = new Rule(
                criterias:
                [
                    new Criteria<string>("who", "Nick", Operator.Equal),
                    new Criteria<string>("concept", "onHit", Operator.Equal),
                ],
                payload: () =>
                {
                    payloadExecuted = true;
                } // Set flag in payload
            );

            var factsDict = new Dictionary<string, object>
            {
                { "concept", "onHit" },
                { "who", "Nick" },
            };
            var query = Query.FromDictionary(factsDict);

            // Act
            query.Match([rule1]);

            // Assert
            Assert.True(payloadExecuted, "Payload should execute when all criteria match.");
        }

        /// <summary>
        /// Tests evaluation of a simple rule with two predicate criteria that strictly match all facts.
        /// </summary>
        [Fact]
        public void Evaluate_RuleWithTwoMatchingPredicates_ExecutesPayload() // Renamed
        {
            // Arrange
            var payloadExecuted = false;
            var rule1 = new Rule(
                criterias:
                [
                    new Criteria<string>("who", who => who == "Nick"),
                    new Criteria<string>("concept", concept => concept == "onHit"),
                ],
                payload: () =>
                {
                    payloadExecuted = true;
                } // Set flag
            );

            var factsDict = new Dictionary<string, object>
            {
                { "concept", "onHit" },
                { "who", "Nick" },
            };
            var query = Query.FromDictionary(factsDict);

            // Act
            query.Match([rule1]);

            // Assert
            Assert.True(
                payloadExecuted,
                "Payload should execute when all predicate criteria match."
            );
        }

        /// <summary>
        /// Tests evaluation of a simple rule with two criteria where only one fact matches (or is present),
        /// expecting the rule *not* to match.
        /// </summary>
        [Fact]
        public void Evaluate_RuleWithOneMissingFact_DoesNotExecutePayload() // Renamed
        {
            // Arrange
            var payloadExecuted = false;
            var rule1 = new Rule(
                criterias:
                [
                    new Criteria<string>("who", "Nick", Operator.Equal), // Requires "who"
                    new Criteria<string>("concept", "onHit", Operator.Equal),
                ],
                payload: () => payloadExecuted = true // Set flag
            );

            // Fact "who" is missing
            var factsDict = new Dictionary<string, object> { { "concept", "onHit" } };
            var query = Query.FromDictionary(factsDict);

            // Act
            query.Match([rule1]);

            // Assert
            Assert.False(
                payloadExecuted,
                "Payload should NOT execute when a required fact is missing."
            );
        }

        /// <summary>
        /// Tests evaluation of a simple rule with two criteria where one fact has the wrong value,
        /// expecting the rule *not* to match.
        /// </summary>
        [Fact]
        public void Evaluate_RuleWithOneWrongValue_DoesNotExecutePayload() // New Test
        {
            // Arrange
            var payloadExecuted = false;
            var rule1 = new Rule(
                criterias:
                [
                    new Criteria<string>("who", "Nick", Operator.Equal),
                    new Criteria<string>(
                        "concept",
                        "onHit",
                        Operator.Equal
                    ) // Requires "onHit"
                    ,
                ],
                payload: () =>
                {
                    payloadExecuted = true;
                } // Set flag
            );

            // "concept" has wrong value
            var factsDict = new Dictionary<string, object>
            {
                { "concept", "onMiss" },
                { "who", "Nick" },
            };
            var query = Query.FromDictionary(factsDict);

            // Act
            query.Match([rule1]);

            // Assert
            Assert.False(
                payloadExecuted,
                "Payload should NOT execute when a criteria value does not match."
            );
        }

        /// <summary>
        /// Tests that when multiple rules match with the same highest criteria count and priority,
        /// one of them is selected (pseudo-randomly).
        /// Runs multiple times to increase chance of hitting all tied rules.
        /// </summary>
        [Fact]
        public void Match_SelectsRandomlyOnCriteriaAndPriorityTie() // Renamed
        {
            // Arrange
            // Flags to check if each potential winner's payload ran at least once
            var rule2Executed = false;
            var rule3Executed = false;
            var rule4Executed = false;

            // Facts that will match rules 2, 3, and 4 (all have 3 criteria)
            var factsDict = new Dictionary<string, object>
            {
                { "who", "Nick" },
                { "concept", "onHit" },
                { "curMap", "circus" },
                { "health", 0.66 }, // Not used by winning rules, but present
                { "nearAllies", 2 }, // Used by Rule 2
                {
                    "hitBy",
                    "zombieClown"
                } // Used by Rule 4
                ,
            };

            // Create the Query object once
            var query = Query.FromDictionary(factsDict);

            List<Rule> rules =
            [
                // Rule 1: Less specific (2 criteria), should not be chosen
                new Rule(
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                    ],
                    payload: () => { /* Should not run */
                    }
                ),
                // Rule 2: Matches (3 criteria)
                new Rule(
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                        new Criteria<int>(
                            "nearAllies",
                            nearAllies => nearAllies > 1
                        ) // Matches value 2
                        ,
                    ],
                    payload: () =>
                    {
                        _testOutputHelper.WriteLine("Rule 2 Executed");
                        rule2Executed = true;
                    }
                ),
                // Rule 3: Matches (3 criteria)
                new Rule(
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                        new Criteria<string>(
                            "curMap",
                            curMap => curMap == "circus"
                        ) // Matches value "circus"
                        ,
                    ],
                    payload: () =>
                    {
                        _testOutputHelper.WriteLine("Rule 3 Executed");
                        rule3Executed = true;
                    }
                ),
                // Rule 4: Matches (3 criteria)
                new Rule(
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                        new Criteria<string>(
                            "hitBy",
                            hitBy => hitBy == "zombieClown"
                        ) // Matches value "zombieClown"
                        ,
                    ],
                    payload: () =>
                    {
                        _testOutputHelper.WriteLine("Rule 4 Executed");
                        rule4Executed = true;
                    }
                ),
            ];

            // Act
            // Run the match many times to increase probability that random selection hits all tied rules.
            // Note: This doesn't *guarantee* hitting all 3, but makes it highly likely.
            // A more robust test might involve mocking Random or checking distribution.
            int executionCount = 0;
            for (int i = 0; i < 1000; i++)
            {
                // Reset flags only if needed for specific logic, but here we want to see if they *ever* get set true.
                query.Match(rules: rules);
                if (rule2Executed || rule3Executed || rule4Executed)
                    executionCount++; // Track if *any* winning rule ran on this iteration
            }
            _testOutputHelper.WriteLine(
                $"Total executions of a winning rule: {executionCount} / 1000"
            );

            // Assert
            // Check that *at least one* of the target rules was executed across all runs.
            Assert.True(
                rule2Executed || rule3Executed || rule4Executed,
                "At least one of the tied rules (2, 3, or 4) should have executed."
            );
            // Ideally, we'd assert that *all* were hit, but randomness makes that hard to guarantee in a single test run.
            // For stricter testing, consider dependency injection for Random or multiple test runs.
            Assert.True(
                rule2Executed,
                "Rule 2 should have been executed at least once over 1000 runs."
            ); // High probability
            Assert.True(
                rule3Executed,
                "Rule 3 should have been executed at least once over 1000 runs."
            ); // High probability
            Assert.True(
                rule4Executed,
                "Rule 4 should have been executed at least once over 1000 runs."
            ); // High probability
        }

        /// <summary>
        /// Tests the Left For Dead example scenario matching the most specific rule.
        /// </summary>
        [Fact]
        public void Match_LeftForDeadExample_SelectsMostSpecific() // Renamed
        {
            // Arrange
            var rule1Executed = false; // 2 criteria
            var rule2Executed = false; // 3 criteria
            var rule3Executed = false; // 3 criteria
            var rule4Executed = false; // 3 criteria
            var rule5Executed = false; // 4 criteria - MOST SPECIFIC

            var factsDict = new Dictionary<string, object>
            {
                { "who", "Nick" },
                { "concept", "onHit" },
                { "curMap", "circus" },
                { "health", 0.66 },
                { "nearAllies", 2 },
                { "hitBy", "zombieClown" },
            };
            var query = Query.FromDictionary(factsDict);

            List<Rule> rules =
            [
                new Rule( // Rule 1 (2 criteria)
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                    ],
                    payload: () =>
                    {
                        _testOutputHelper.WriteLine("Rule 1: Ouch");
                        rule1Executed = true;
                    }
                ),
                new Rule( // Rule 2 (3 criteria)
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                        new Criteria<int>("nearAllies", nearAllies => nearAllies > 1),
                    ],
                    payload: () =>
                    {
                        _testOutputHelper.WriteLine("Rule 2: ow help!");
                        rule2Executed = true;
                    }
                ),
                new Rule( // Rule 3 (3 criteria)
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                        new Criteria<string>("curMap", curMap => curMap == "circus"),
                    ],
                    payload: () =>
                    {
                        _testOutputHelper.WriteLine("Rule 3: This Circus Sucks!");
                        rule3Executed = true;
                    }
                ),
                new Rule( // Rule 4 (3 criteria)
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                        new Criteria<string>("hitBy", hitBy => hitBy == "zombieClown"),
                    ],
                    payload: () =>
                    {
                        _testOutputHelper.WriteLine("Rule 4: Stupid Clown!");
                        rule4Executed = true;
                    }
                ),
                new Rule( // Rule 5 (4 criteria) - MOST SPECIFIC MATCH
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                        new Criteria<string>("hitBy", hitBy => hitBy == "zombieClown"),
                        new Criteria<string>("curMap", curMap => curMap == "circus"),
                    ],
                    payload: () =>
                    {
                        _testOutputHelper.WriteLine("Rule 5: I hate circus clowns!");
                        rule5Executed = true;
                    } // Should execute
                ),
            ];

            // Act
            query.Match(rules: rules);

            // Assert
            Assert.False(rule1Executed, "Rule 1 (less specific) should not execute.");
            Assert.False(rule2Executed, "Rule 2 (less specific) should not execute.");
            Assert.False(rule3Executed, "Rule 3 (less specific) should not execute.");
            Assert.False(rule4Executed, "Rule 4 (less specific) should not execute.");
            Assert.True(rule5Executed, "Rule 5 (most specific) should execute.");
        }
    }
}
