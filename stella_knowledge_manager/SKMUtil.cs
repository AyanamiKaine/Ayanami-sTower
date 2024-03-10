using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{
    public static class SKMUtil
    {
        /// <summary>
        /// This should not be a main function of the class as its only a utility function
        /// </summary>
        public static void ShowAllItems(SKM skm)
        {
            // Use LINQ for conciseness
            var sortedItems = skm.PriorityList
                                .OrderByDescending(item => item.Priority) // Sort by priority
                                .ToList();

            foreach (var item in sortedItems)
            {
                item.PrettyPrint();
                //Console.WriteLine($"ID: {item.Id}. File Name: {item.PathToFile}, Priority: {item.Priority}, Due Data: {item.LastReviewDate}");  // Improved formatting
            }
            PrintStats(skm);
        }

        public static List<FileToLearn> SearchAndSort(SKM skm, string searchString)
        {
            return skm.PriorityList
                .Select(item => new {
                    Data = item,
                    Similarity = StringSimilarityCalculator.CalculateJaroWinklerDistance(item.Name, searchString)
                }) // Calculate similarity (use Jaro-Winkler as desired)
                .OrderBy(item => item.Similarity) // Sort by ascending similarity
                .Select(item => item.Data)
                .ToList();
        }

        /// <summary>
        /// This should not be a main function of the class as its a application function
        /// </summary>
        public static void PrintStats(SKM skm)
        {
            Console.WriteLine($"Total Number of items: {skm.PriorityList.Count}");
        }

        /// <summary>
        /// This should not be a main function of the class as its a application function
        /// Here we want to implement the ability to take a quiz
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public static void TakeQuiz()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This should not be a main function of the class as its a application function
        /// Here we want to implement the ability to take a quiz
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public static void TakeFlashCards()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This should not be a main function of the class as its a application function
        /// Here we want to implement the ability to take a quiz
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public static void TakeCloze()
        {
            throw new NotImplementedException();
        }
    }
}
