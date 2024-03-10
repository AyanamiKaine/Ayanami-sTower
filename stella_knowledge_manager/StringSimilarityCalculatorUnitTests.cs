using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace stella_knowledge_manager
{
    public class StringSimilarityCalculatorUnitTests
    {
        [Fact]
        public void CalculateJaroDistanceTestSame()
        {
            string wordA = "Hello, World!";
            string wordB = "Hello, World!";

            var similarityScore = StringSimilarityCalculator.CalculateJaroWinklerDistance(wordA, wordB);

            Assert.Equal(1, similarityScore);
        }

        [Fact]
        public void CalculateJaroDistanceTestNotSimilar()
        {
            string wordA = "Hello, World!";
            string wordB = "world";

            var similarityScore = StringSimilarityCalculator.CalculateJaroWinklerDistance(wordA, wordB);

            Assert.True( 0.6 > similarityScore, $"Similarty score should be below 0.6 but is instead {similarityScore}");
        }

        [Fact]
        public void CalculateLevenshteinDistanceTestSame()
        {
            string wordA = "Hello, World!";
            string wordB = "Hello, World!";

            var similarityScore = StringSimilarityCalculator.CalculateLevenshteinDistance(wordA, wordB);

            Assert.Equal(0, similarityScore);
        }

        [Fact]
        public void CalculateLevenshteinDistanceTestNotSimilar()
        {
            string wordA = "Hello, World!";
            string wordB = "world";

            var similarityScore = StringSimilarityCalculator.CalculateLevenshteinDistance(wordA, wordB);

            Assert.True(5 < similarityScore, $"Similarty score should be bigger then 5 but is instead {similarityScore}");
        }
    }
}
