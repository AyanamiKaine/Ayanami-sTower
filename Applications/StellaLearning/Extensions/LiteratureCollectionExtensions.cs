using System;
using System.Collections.ObjectModel;
using System.Linq;
using AyanamisTower.StellaLearning.Data;

namespace AyanamisTower.StellaLearning.Extensions;

/// <summary>
/// Provides extension methods for collections of literature source items.
/// </summary>
public static class LiteratureCollectionExtensions
{
    /// <summary>
    /// Removes duplicate file paths from a collection of literature source items.
    /// </summary>
    /// <param name="collection">The collection to remove duplicates from.</param>
    public static void RemoveDuplicateFilePaths(this Collection<LiteratureSourceItem> collection)
    {
        Console.WriteLine("\n--- Running RemoveDuplicateFilePaths_UsingGroupBy ---");
        // 1. Use LINQ to identify items to remove
        //    - Group by FilePath (case-insensitive, handle nulls)
        //    - Filter groups having more than one item (duplicates)
        //    - From each duplicate group, select all items EXCEPT the first one
        //    - Collect these items into a list
        var itemsToRemove = collection
            .OfType<LocalFileSourceItem>()
            .GroupBy(item => item?.FilePath, StringComparer.OrdinalIgnoreCase) // Group by FilePath, case-insensitive
            .Where(group => group.Count() > 1) // Find groups with more than one item
            .SelectMany(group => group.Skip(1)) // Select all but the first item from each duplicate group
            .ToList(); // Materialize the list of items to remove BEFORE modifying the collection

        // 2. Remove the identified items from the ObservableCollection
        if (itemsToRemove.Count != 0)
        {
            Console.WriteLine($"Found {itemsToRemove.Count} duplicate item(s) to remove:");
            // It's crucial to remove items using the collection's Remove method
            // to trigger CollectionChanged notifications correctly.
            foreach (var itemToRemove in itemsToRemove)
            {
                Console.WriteLine($"- Removing: {itemToRemove}");
                collection.Remove(itemToRemove); // This triggers notification
            }
            Console.WriteLine("Duplicates removed.");
        }
        else
        {
            Console.WriteLine("No duplicate file paths found to remove.");
        }
    }
}
