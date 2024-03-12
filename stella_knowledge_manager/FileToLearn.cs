using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json; // Add this using directive at the top

namespace stella_knowledge_manager
{
    public class FileToLearn(Guid id, string name, string pathToFile, string description, double easeFactor, double priority) : IPrettyPrint , ISRSItem
    {
        public Guid Id { get; set; } = id; 
        public string Name { get; set; } = name;
        public string PathToFile { get; set; } = pathToFile;
        public string Description { get; set; } = description;
        public double EaseFactor { get; set; } = easeFactor; // Default ease
        public DateTime NextReviewDate { get; set; } = DateTime.Now;
        public double Priority { get; set; } = priority;
        public int NumberOfTimeSeen { get; set; } = 0;

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
