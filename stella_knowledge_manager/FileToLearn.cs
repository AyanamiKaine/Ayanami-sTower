using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json; // Add this using directive at the top

namespace stella_knowledge_manager
{
    public class FileToLearn(string id, string name, string pathToFile, string description, double easeFactor, double priority) : IPrettyPrint
    {
        public string Id = id; 
        public string Name = name;
        public string PathToFile = pathToFile;
        public string Description = description;
        public double EaseFactor { get; set; } = easeFactor; // Default ease
        public DateTime NextReviewDate { get; set; } = DateTime.Now;
        public double Priority = priority;

        public void PrettyPrint()
        {
            Console.WriteLine("File Details:");
            Console.WriteLine($"  ID: {Id}");
            Console.WriteLine($"  Name: {Name}");
            Console.WriteLine($"  Path: {PathToFile}");
            Console.WriteLine($"  Description: {Description}");
            Console.WriteLine($"  Ease Factor: {EaseFactor}");
            Console.WriteLine($"  Priority: {Priority}");
            Console.WriteLine($"  Next Review: {NextReviewDate.ToLocalTime()}"); // Localized time
        }
    }

    public class IdGenerator
    {
        public static string GenerateId(string name)
        {
            // 1. Hash the name
            var hashAlgorithm = SHA256.Create();
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            byte[] hash = hashAlgorithm.ComputeHash(nameBytes);

            // 2. Get time component
            long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            // 3. Format the ID
            string timePart = milliseconds.ToString().Substring(milliseconds.ToString().Length - 4);
            string hashPart = GetTwoCharsFromHash(hash);

            return timePart + hashPart;
        }

        private static string GetTwoCharsFromHash(byte[] hash)
        {
            // Implement a strategy to select two characters from the hash 
            // Example: Convert the first two bytes to a hexadecimal string
            string hex = BitConverter.ToString(hash, 0, 2).Replace("-", "");
            return hex.Substring(0, 2);
        }
    }

    public class SpacedRepetitionScheduler
    {
        public static DateTime CalculateNextReviewDate(FileToLearn item, string recallEvaluation)
        {
            int interval = 1; // Initial interval

            if (recallEvaluation == "g")
            {
                // Good recall
                interval = (int)Math.Round(interval * item.EaseFactor, 0);
                item.EaseFactor += 0.1; // Adjust ease
            }
            else if (recallEvaluation == "b")
            {
                // Bad recall
                interval = 1; // Reset to initial interval
                item.EaseFactor -= 0.2; // Decrease ease (make harder)

                // Ensure ease factor doesn't go below a certain value (e.g., 1.3)
                item.EaseFactor = Math.Max(1.3, item.EaseFactor);
            }
            else
            {
                // Repeat - don't change the interval in this case
                interval = 0;
            }

            return DateTime.Now.AddDays(interval);
        }
    }
}
