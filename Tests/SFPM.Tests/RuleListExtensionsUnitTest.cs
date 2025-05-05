using System;
using System.Collections.Generic;
using AyanamisTower.SFPM; // Ensure this using is present
using NLog;
using Xunit; // Assuming you are using xUnit

namespace AyanamisTower.SFPM.Tests
{
    /// <summary>
    /// Contains unit tests for the RuleListExtensions class.
    /// </summary>
    public class RuleListExtensionsUnitTest
    {
        /// <summary>
        /// Initializes logging for tests (e.g., deactivate).
        /// </summary>
        public RuleListExtensionsUnitTest()
        {
            // Deactivate Logging for unit tests if desired
            LogManager.Configuration?.LoggingRules.Clear(); // Added null check
            LogManager.ReconfigExistingLoggers();
        }

        /// <summary>
        /// Tests the MostSpecificRule extension method.
        /// NO CHANGES NEEDED HERE.
        /// </summary>
        [Fact]
        public void MostSpecificRuleTest()
        {
            // Arrange
            List<Rule> rules =
            [
                new Rule( /* Rule with 2 criteria */
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                    ],
                    payload: () => { }
                ),
                new Rule( /* Rule with 2 criteria */
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<int>("nearAllies", nearAllies => nearAllies > 1),
                    ],
                    payload: () => { }
                ),
                new Rule( /* Rule with 3 criteria - MOST SPECIFIC */
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                        new Criteria<string>("curMap", curMap => curMap == "circus"),
                    ],
                    payload: () => { }
                ),
                new Rule( /* Rule with 1 criterion */
                    criterias: [new Criteria<string>("who", who => who == "Nick")],
                    payload: () => { }
                ),
            ];

            // Act
            var mostSpecificRule = rules.MostSpecificRule();

            // Assert
            Assert.NotNull(mostSpecificRule);
            Assert.Equal(expected: 3, actual: mostSpecificRule.CriteriaCount);
        }

        /// <summary>
        /// Tests the LeastSpecificRule extension method.
        /// NO CHANGES NEEDED HERE.
        /// </summary>
        [Fact]
        public void LeastSpecificRuleTest()
        {
            // Arrange
            List<Rule> rules =
            [
                new Rule( /* Rule with 2 criteria */
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                    ],
                    payload: () => { }
                ),
                new Rule( /* Rule with 2 criteria */
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<int>("nearAllies", nearAllies => nearAllies > 1),
                    ],
                    payload: () => { }
                ),
                new Rule( /* Rule with 3 criteria */
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                        new Criteria<string>("curMap", curMap => curMap == "circus"),
                    ],
                    payload: () => { }
                ),
                new Rule( /* Rule with 1 criterion - LEAST SPECIFIC */
                    criterias: [new Criteria<string>("who", who => who == "Nick")],
                    payload: () => { }
                ),
            ];

            // Act
            var leastSpecificRule = rules.LeastSpecificRule(); // Corrected variable name

            // Assert
            Assert.NotNull(leastSpecificRule);
            Assert.Equal(expected: 1, actual: leastSpecificRule.CriteriaCount);
        }

        /// <summary>
        /// Tests the Match extension method indirectly by calling Query.Match,
        /// ensuring the correct rule payload is executed based on IFactSource data.
        /// </summary>
        [Fact]
        public void MatchOnRulesList_ExecutesCorrectPayload() // Renamed slightly for clarity
        {
            // Arrange
            var ruleExecuted = false;

            List<Rule> rules =
            [
                new Rule( // Rule 0: Doesn't match facts
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>(
                            "concept",
                            concept => concept == "WRONG"
                        ) // Mismatch
                        ,
                    ],
                    payload: () =>
                    {
                        ruleExecuted = false;
                    }
                ),
                new Rule( // Rule 1: Matches facts, but less specific
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<int>(
                            "nearAllies",
                            nearAllies => nearAllies > 1
                        ) // Fact missing
                        ,
                    ],
                    payload: () =>
                    {
                        ruleExecuted = false;
                    }
                ),
                new Rule( // Rule 2: Matches facts, most specific
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                        new Criteria<string>("curMap", curMap => curMap == "circus"),
                    ],
                    payload: () => // This payload should execute
                    {
                        ruleExecuted = true;
                    }
                ),
                new Rule( // Rule 3: Matches facts, least specific
                    criterias: [new Criteria<string>("who", predicate: who => who == "Nick")],
                    payload: () =>
                    {
                        ruleExecuted = false;
                    } // Should not be chosen over Rule 2
                ),
            ];

            // Create the facts dictionary
            var factsDict = new Dictionary<string, object>
            {
                { "concept", "onHit" },
                { "who", "Nick" },
                { "curMap", "circus" },
                // "nearAllies" is intentionally missing
            };

            // Create the Query object using the dictionary via the helper method
            // Query now encapsulates the IFactSource (DictionaryFactSource)
            var query = Query.FromDictionary(factsDict);

            // Act
            // Call Match on the Query object. This internally calls rules.Match(IFactSource).
            query.Match(rules);

            // Assert
            Assert.True(
                ruleExecuted,
                "The payload of the most specific matching rule (Rule 2) should have been executed."
            );
        }

        /// <summary>
        /// Add more tests for priority tie-breaking, random selection on tie, etc. if needed
        /// </summary>
        [Fact]
        public void MatchOnRulesList_HandlesPriority()
        {
            // Arrange
            var executedRuleName = "None";
            List<Rule> rules =
            [
                new Rule( // Rule A: Matches, Priority 1
                    name: "RuleA",
                    criterias: [new Criteria<int>("value", v => v > 10)],
                    payload: () => executedRuleName = "RuleA"
                )
                {
                    Priority = 1,
                },
                new Rule( // Rule B: Matches, Priority 5 (Higher)
                    name: "RuleB",
                    criterias: [new Criteria<int>("value", v => v < 20)],
                    payload: () => executedRuleName = "RuleB"
                )
                {
                    Priority = 5,
                },
                new Rule( // Rule C: Matches, Priority 5 (Higher)
                    name: "RuleC",
                    criterias: [new Criteria<int>("value", v => v == 15)],
                    payload: () => executedRuleName = "RuleC"
                )
                {
                    Priority = 5,
                },
            ];

            var factsDict = new Dictionary<string, object> { { "value", 15 } };
            var query = Query.FromDictionary(factsDict);

            // Act
            query.Match(rules);

            // Assert
            // Both B and C match and have the highest priority. One of them should be chosen.
            Assert.True(
                executedRuleName == "RuleB" || executedRuleName == "RuleC",
                $"Expected RuleB or RuleC, but got {executedRuleName}"
            );
            Assert.NotEqual("RuleA", executedRuleName); // Rule A has lower priority
            Assert.NotEqual("None", executedRuleName); // Ensure one rule actually ran
        }
    }
}
