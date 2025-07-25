/*
Stella Learning is a modern learning app.
Copyright (C) <2025>  <Patrick, Grohs>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Returns all unique tags
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    public static List<string> GetAllUniqueTags(this Collection<LiteratureSourceItem> collection)
    {
        if (collection == null)
        {
            return []; // Return an empty list if the input list is null
        }

        // Step 1: Select all tag lists (handling null tag lists)
        // Step 2: Flatten them into a single sequence of tags
        // Step 3: Filter out any null or empty tags (optional, but good practice)
        // Step 4: Get distinct tags
        // Step 5: Convert to a list

        List<string> uniqueTags = [.. collection
            .Where(item => item?.Tags != null) // Ensure object and its Tags list are not null
            .SelectMany(item => item.Tags)                 // Flatten all tag lists into one sequence
            .Where(tag => !string.IsNullOrEmpty(tag))    // Optional: Filter out null or empty tags
            .Distinct(StringComparer.OrdinalIgnoreCase)];                                   // Convert the result to a List<string>

        return uniqueTags;
    }
}
