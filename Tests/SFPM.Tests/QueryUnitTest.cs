using System; // For Action
using System.Collections.Generic; // For Dictionary, List
using AyanamisTower.SFPM; // Ensure this using is present
using NLog;
using Xunit; // Assuming you are using xUnit

namespace AyanamisTower.SFPM.Tests
{
    /// <summary>
    /// Contains unit tests for the updated Query class and matching logic.
    /// </summary>
    public class QueryUnitTest
    {
        /// <summary>
        /// Initializes logging for tests (e.g., deactivate).
        /// </summary>
        public QueryUnitTest()
        {
            // Deactivate Logging for unit tests if desired
            LogManager.Configuration?.LoggingRules.Clear(); // Added null check
            LogManager.ReconfigExistingLoggers();
        }

        /// <summary>
        /// Tests the creation of a Query object using the static helper.
        /// </summary>
        [Fact]
        public void Creation_FromEmptyDictionary()
        {
            // Arrange
            var emptyFacts = new Dictionary<string, object>();

            // Act
            var query = Query.FromDictionary(emptyFacts); // Use the static helper

            // Assert
            Assert.NotNull(query); // Basic assertion
        }

        /// <summary>
        /// Tests creating a Query object with initial data.
        /// (Replaces the old AddingKeyValue test)
        /// </summary>
        [Fact]
        public void Creation_WithInitialData()
        {
            // Arrange
            var initialFacts = new Dictionary<string, object>
            {
                { "concept", "OnHit" },
                { "attacker", "Hunter" },
                { "damage", 12.4 },
            };

            // Act
            var query = Query.FromDictionary(initialFacts);

            // Assert
            Assert.NotNull(query);
            // We can't directly inspect the internal IFactSource easily without more changes,
            // so we'll rely on the matching tests to verify data is used correctly.
        }

        /// <summary>
        /// Tests matching a query against rules, selecting the most specific rule.
        /// </summary>
        [Fact]
        public void QueryMatching_SelectsMostSpecificRule()
        {
            // Arrange
            var facts = new Dictionary<string, object>
            {
                { "concept", "OnHit" },
                { "attacker", "Hunter" },
                { "damage", 12.4 },
            };
            var query = Query.FromDictionary(facts); // Create query with facts

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

            // Act
            query.Match(rules: rules);

            // Assert
            Assert.Equal("Rule 1", ruleExecutedFlag); // Verify the most specific rule's payload ran
        }

        /// <summary>
        /// Tests that a rule's payload can modify the underlying facts,
        /// affecting subsequent matches.
        /// </summary>
        [Fact]
        public void QueryMatching_PayloadModifiesFactsForNextMatch()
        {
            // Arrange

            // Use a dictionary that the payload can modify
            var facts = new Dictionary<string, object>
            {
                { "concept", "OnHit" },
                { "attacker", "Hunter" },
                { "damage", 12.4 },
                // "EventAHappened" does not exist initially
            };

            var secondRuleExecuted = false;

            // Define the payload for the first rule that will modify the facts
            void modifyFactsPayload()
            {
                Console.WriteLine("Payload of Rule 1 executing: Adding EventAHappened=true");
                facts["EventAHappened"] = true; // Modify the dictionary
            }

            List<Rule> rules =
            [
                // Rule 0: Doesn't match facts
                new Rule(
                    criterias:
                    [
                        new Criteria<string>("who", who => who == "Nick"),
                        new Criteria<string>("concept", concept => concept == "onHit"),
                    ],
                    payload: () => { }
                ),
                // Rule 1: Matches initial facts (3 criteria) - Its payload modifies facts
                new Rule(
                    criterias:
                    [
                        new Criteria<string>("attacker", attacker => attacker == "Hunter"),
                        new Criteria<string>("concept", concept => concept == "OnHit"),
                        new Criteria<double>("damage", damage => damage == 12.4),
                    ],
                    payload: modifyFactsPayload // Assign the modifying payload
                ),
                // Rule 2: Matches *after* Rule 1's payload runs (4 criteria)
                new Rule(
                    criterias:
                    [
                        new Criteria<string>("attacker", attacker => attacker == "Hunter"),
                        new Criteria<string>("concept", concept => concept == "OnHit"),
                        new Criteria<double>("damage", damage => damage == 12.4),
                        new Criteria<bool>(
                            "EventAHappened",
                            eventAHappened => eventAHappened == true
                        ) // Requires modification
                        ,
                    ],
                    payload: () =>
                    {
                        Console.WriteLine("Payload of Rule 2 executing.");
                        secondRuleExecuted = true; // Set flag for assertion
                    }
                ),
                // Rule 3: Less specific, shouldn't be chosen in second pass
                new Rule(
                    criterias:
                    [
                        new Criteria<string>("attacker", attacker => attacker.StartsWith('H')),
                        new Criteria<double>("damage", damage => damage < 20.0),
                    ],
                    payload: () => { }
                ),
            ];

            // Act

            // --- First Match ---
            // Create query with original facts. Rule 1 should match and execute its payload.
            var query1 = Query.FromDictionary(facts);
            Console.WriteLine("--- Performing first match ---");
            query1.Match(rules: rules);
            Assert.False(secondRuleExecuted, "Second rule should not have executed yet.");
            Assert.True(
                facts.ContainsKey("EventAHappened"),
                "Fact dictionary should now contain EventAHappened."
            );
            Assert.True(
                (bool)facts["EventAHappened"],
                "EventAHappened should be true in dictionary."
            );

            // --- Second Match ---
            // Create a *new* query instance with the *same underlying dictionary* (which is now modified).
            // Rule 2 should now be the most specific matching rule.
            var query2 = Query.FromDictionary(facts); // Uses the modified dictionary
            Console.WriteLine("\n--- Performing second match ---");
            query2.Match(rules: rules);

            // Assert
            Assert.True(
                secondRuleExecuted,
                "The second rule (requiring EventAHappened) should have executed."
            );
        }
    }
}
