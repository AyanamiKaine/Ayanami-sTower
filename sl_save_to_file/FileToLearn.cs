using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SaveFileToLearn
{
    public class FileToLearn(Guid id, string name, string pathToFile, string description, double easeFactor, double priority)
    {
        public Guid Id { get; set; } = id; 
        public string Name { get; set; } = name;
        public string PathToFile { get; set; } = pathToFile;
        public string Description { get; set; } = description;
        public double EaseFactor { get; set; } = easeFactor; // Default ease
        public DateTime NextReviewDate { get; set; } = DateTime.Now;
        public double Priority { get; set; } = priority;
        public int NumberOfTimeSeen { get; set; } = 0;

        public string PrettyPrint()
        {
            return $"""
                File Details:
                    ID:                     {Id}
                    Name:                   {Name}
                    Path:                   {PathToFile}
                    Description             {Description}
                    Ease Factor:            {EaseFactor}
                    Prority:                {Priority}
                    Next Review:            {NextReviewDate.ToLocalTime()}
                    Number of Times Seen:   {NumberOfTimeSeen}
                """;
        }
    }
}
