using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{
    public static class StringSimilarityCalculator
    {
        public static int CalculateLevenshteinDistance(string a, string b)
        {
            int n = a.Length;
            int m = b.Length;
            int[,] dp = new int[n + 1, m + 1];

            // Initialize base cases
            for (int i = 0; i <= n; i++) dp[i, 0] = i;
            for (int j = 0; j <= m; j++) dp[0, j] = j;

            // Calculate Levenshtein distance recursively 
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;

                    dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1,  // Deletion
                                                 dp[i, j - 1] + 1), // Insertion
                                        dp[i - 1, j - 1] + cost); // Substitution
                }
            }

            return dp[n, m];
        }
        public static double CalculateJaroWinklerDistance(string a, string b)
        {
            double jaroDistance = CalculateJaroDistance(a, b);

            // Constants used in Jaro-Winkler 
            int prefixLength = Math.Min(a.Length, 4);
            double scalingFactor = 0.1;

            int commonPrefixes = 0;
            for (int i = 0; i < prefixLength; i++)
            {
                if (a[i] == b[i])
                    commonPrefixes++;
                else
                    break;
            }

            double jaroWinklerDistance = jaroDistance + commonPrefixes * scalingFactor * (1 - jaroDistance);
            return jaroWinklerDistance;
        }

        private static double CalculateJaroDistance(string a, string b)
        {
            if (a == b)
                return 1.0;

            int window = Math.Max(a.Length, b.Length) / 2 - 1;

            int aMatches = 0, bMatches = 0;
            bool[] aMatchFlags = new bool[a.Length];
            bool[] bMatchFlags = new bool[b.Length];

            // Find matches
            for (int i = 0; i < a.Length; i++)
            {
                int start = Math.Max(0, i - window);
                int end = Math.Min(b.Length - 1, i + window);
                for (int j = start; j <= end; j++)
                {
                    if (!bMatchFlags[j] && a[i] == b[j])
                    {
                        aMatchFlags[i] = bMatchFlags[j] = true;
                        aMatches++;
                        bMatches++;
                        break;
                    }
                }
            }

            if (aMatches == 0 || bMatches == 0)
                return 0.0;

            // Count transpositions
            int transpositions = 0;
            for (int i = 0; i < a.Length; i++)
                if (aMatchFlags[i])
                    for (int j = 0; j < b.Length; j++)
                        if (bMatchFlags[j] && a[i] == b[j] && i != j)
                            transpositions++;

            double jaroDistance = ((double)aMatches / a.Length +
                                   (double)bMatches / b.Length +
                                   ((double)aMatches - transpositions / 2.0) / aMatches) / 3.0;
            return jaroDistance;
        }
    }
}
