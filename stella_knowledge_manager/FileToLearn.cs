using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json; // Add this using directive at the top

namespace stella_knowledge_manager
{
    public class FileToLearn(Guid id, string name, string pathToFile, string description, double easeFactor, double priority) : IPrettyPrint , ISRS
    {
        public Guid Id = id; 
        public string Name = name;
        public string PathToFile = pathToFile;
        public string Description = description;
        public double EaseFactor { get; set; } = easeFactor; // Default ease
        public DateTime NextReviewDate { get; set; } = DateTime.Now;
        public double Priority = priority;
        public int NumberOfTimeSeen = 0;

        public void PrettyPrint()
        {
            Console.WriteLine("File Details:");
            Console.WriteLine($"  ID:                   {Id}");
            Console.WriteLine($"  Name:                 {Name}");
            Console.WriteLine($"  Path:                 {PathToFile}");
            Console.WriteLine($"  Description:          {Description}");
            Console.WriteLine($"  Ease Factor:          {EaseFactor}");
            Console.WriteLine($"  Priority:             {Priority}");
            Console.WriteLine($"  Next Review:          {NextReviewDate.ToLocalTime()}"); // Localized time
            Console.WriteLine($"  Number of Times Seen: {NumberOfTimeSeen}");
        }
    }
}
