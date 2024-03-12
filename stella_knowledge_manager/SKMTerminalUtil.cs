using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager
{
    /// <summary>
    /// Here we provide various utility functions for SKM that only work in the terminal
    /// </summary>
    public class SKMTerminalUtil
    {
        /// <summary>
        /// Pretty Prints all items in SKM to the terminal sorted by priority
        /// </summary>
        /// <param name="skm"></param>
        public static void ShowAllItems(SKM skm)
        {
            // Use LINQ for conciseness
            var sortedItems = skm.PriorityList
                                .OrderByDescending(item => item.Priority) // Sort by priority
                                .ToList();

            foreach (var item in sortedItems)
            {
                Console.WriteLine(item.PrettyPrint() + "\n");
            }
            PrintStats(skm);
        }

        public static void PrintStats(SKM skm)
        {
            Console.WriteLine($"Total Number of items: {skm.PriorityList.Count}");
        }
    }
}
