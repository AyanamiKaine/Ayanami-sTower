using System;
using System.Collections.Generic;
using System.Linq; // We'll use LINQ extensively

namespace Avalonia.Flecs.StellaLearning.Data // Use the same namespace or a related one
{
    /// <summary>
    /// Provides extension methods for querying collections of Tag objects.
    /// </summary>
    public static class TagQueryExtensions
    {
        // --- Basic Existence Checks ---

        /// <summary>
        /// Determines whether the sequence contains a tag with the specified name (case-sensitive).
        /// </summary>
        /// <param name="tags">The collection of tags to check.</param>
        /// <param name="tagName">The name of the tag to search for.</param>
        /// <returns>true if a tag with the specified name is found; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tags or tagName is null.</exception>
        public static bool ContainsTagNamed(this IEnumerable<Tag> tags, string tagName)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (tagName == null) throw new ArgumentNullException(nameof(tagName));

            // Uses the Tag.Equals method implicitly via LINQ's Any and the Tag's equality implementation
            // return tags.Any(tag => tag.Name == tagName);
            // More robustly, using the implemented Equals:
            return tags.Contains(new Tag(tagName)); // Relies on GetHashCode and Equals being correctly implemented
        }

        /// <summary>
        /// Determines whether the sequence contains a tag with the specified name (case-insensitive).
        /// </summary>
        /// <param name="tags">The collection of tags to check.</param>
        /// <param name="tagName">The name of the tag to search for.</param>
        /// <param name="comparisonType">The string comparison type to use (defaults to OrdinalIgnoreCase).</param>
        /// <returns>true if a tag with the specified name is found using the specified comparison; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tags or tagName is null.</exception>
        public static bool ContainsTagNamed(this IEnumerable<Tag> tags, string tagName, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (tagName == null) throw new ArgumentNullException(nameof(tagName));

            return tags.Any(tag => tag.Name.Equals(tagName, comparisonType));
        }

        // --- Filtering and Searching ---

        /// <summary>
        /// Filters the sequence to find all tags with the specified name (case-sensitive).
        /// </summary>
        /// <param name="tags">The collection of tags to filter.</param>
        /// <param name="tagName">The exact name of the tags to find.</param>
        /// <returns>An IEnumerable Tag containing tags that match the specified name.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tags or tagName is null.</exception>
        public static IEnumerable<Tag> FindTagsNamed(this IEnumerable<Tag> tags, string tagName)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (tagName == null) throw new ArgumentNullException(nameof(tagName));

            // Uses the Tag.Equals method implicitly via LINQ's Where and the Tag's equality implementation
            // return tags.Where(tag => tag.Name == tagName);
            // More robustly, using the implemented Equals:
            return tags.Where(tag => tag.Equals(new Tag(tagName))); // More explicit use of Equals
        }

        /// <summary>
        /// Filters the sequence to find all tags with the specified name using a specific string comparison.
        /// </summary>
        /// <param name="tags">The collection of tags to filter.</param>
        /// <param name="tagName">The name of the tags to find.</param>
        /// <param name="comparisonType">The string comparison type to use (e.g., OrdinalIgnoreCase).</param>
        /// <returns>An IEnumerable Tag containing tags that match the specified name under the given comparison.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tags or tagName is null.</exception>
        public static IEnumerable<Tag> FindTagsNamed(this IEnumerable<Tag> tags, string tagName, StringComparison comparisonType)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (tagName == null) throw new ArgumentNullException(nameof(tagName));

            return tags.Where(tag => tag.Name.Equals(tagName, comparisonType));
        }

        /// <summary>
        /// Finds tags whose names contain the specified substring.
        /// </summary>
        /// <param name="tags">The collection of tags to search.</param>
        /// <param name="substring">The substring to search for within tag names.</param>
        /// <param name="comparisonType">The string comparison type to use (defaults to OrdinalIgnoreCase).</param>
        /// <returns>An IEnumerable Tag containing tags whose names contain the substring.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tags or substring is null.</exception>
        public static IEnumerable<Tag> FindTagsContaining(this IEnumerable<Tag> tags, string substring, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            ArgumentNullException.ThrowIfNull(tags);
            ArgumentNullException.ThrowIfNull(substring);
            // Handle empty substring if needed, currently returns all tags
            if (substring.Length == 0) return tags;
            return tags.Where(tag => tag.Name.Contains(substring, comparisonType));
        }

        /// <summary>
        /// Finds tags whose names start with the specified prefix.
        /// </summary>
        /// <param name="tags">The collection of tags to search.</param>
        /// <param name="prefix">The prefix to match at the beginning of tag names.</param>
        /// <param name="comparisonType">The string comparison type to use (defaults to OrdinalIgnoreCase).</param>
        /// <returns>An IEnumerable Tag containing tags whose names start with the prefix.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tags or prefix is null.</exception>
        public static IEnumerable<Tag> FindTagsStartingWith(this IEnumerable<Tag> tags, string prefix, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (prefix == null) throw new ArgumentNullException(nameof(prefix));
            if (prefix == string.Empty) return tags; // Or return Enumerable.Empty<Tag>() if that's preferred

            return tags.Where(tag => tag.Name.StartsWith(prefix, comparisonType));
        }

        /// <summary>
        /// Finds tags whose names end with the specified suffix.
        /// </summary>
        /// <param name="tags">The collection of tags to search.</param>
        /// <param name="suffix">The suffix to match at the end of tag names.</param>
        /// <param name="comparisonType">The string comparison type to use (defaults to OrdinalIgnoreCase).</param>
        /// <returns>An IEnumerable Tag  containing tags whose names end with the suffix.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tags or suffix is null.</exception>
        public static IEnumerable<Tag> FindTagsEndingWith(this IEnumerable<Tag> tags, string suffix, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (suffix == null) throw new ArgumentNullException(nameof(suffix));
            if (suffix == string.Empty) return tags; // Or return Enumerable.Empty<Tag>() if that's preferred

            return tags.Where(tag => tag.Name.EndsWith(suffix, comparisonType));
        }

        /// <summary>
        /// Finds tags whose names match any of the provided names (case-sensitive).
        /// </summary>
        /// <param name="tags">The collection of tags to search.</param>
        /// <param name="namesToMatch">A collection of tag names to search for.</param>
        /// <returns>An IEnumerable Tag  containing tags whose names match any in the provided list.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tags or namesToMatch is null.</exception>
        public static IEnumerable<Tag> FindTagsMatchingAny(this IEnumerable<Tag> tags, IEnumerable<string> namesToMatch)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (namesToMatch == null) throw new ArgumentNullException(nameof(namesToMatch));

            // Use a HashSet for efficient lookups if namesToMatch is potentially large
            var nameSet = new HashSet<string>(namesToMatch);
            return tags.Where(tag => nameSet.Contains(tag.Name));
        }

        /// <summary>
        /// Finds tags whose names match any of the provided names using a specific string comparison.
        /// </summary>
        /// <param name="tags">The collection of tags to search.</param>
        /// <param name="namesToMatch">A collection of tag names to search for.</param>
        /// <param name="comparisonType">The string comparison type to use.</param>
        /// <returns>An IEnumerable Tag  containing tags whose names match any in the provided list.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tags or namesToMatch is null.</exception>
        public static IEnumerable<Tag> FindTagsMatchingAny(this IEnumerable<Tag> tags, IEnumerable<string> namesToMatch, StringComparison comparisonType)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (namesToMatch == null) throw new ArgumentNullException(nameof(namesToMatch));

            // Use a HashSet with the appropriate comparer for efficiency
            var nameSet = new HashSet<string>(namesToMatch, GetStringComparer(comparisonType));
            return tags.Where(tag => nameSet.Contains(tag.Name));
        }


        // --- Counting ---

        /// <summary>
        /// Counts the number of tags with the specified name (case-sensitive).
        /// </summary>
        /// <param name="tags">The collection of tags.</param>
        /// <param name="tagName">The name of the tag to count.</param>
        /// <returns>The number of tags matching the name.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tags or tagName is null.</exception>
        public static int CountTagsNamed(this IEnumerable<Tag> tags, string tagName)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (tagName == null) throw new ArgumentNullException(nameof(tagName));

            // Efficiently counts using the Tag's equality implementation
            return tags.Count(tag => tag.Equals(new Tag(tagName)));
        }

        /// <summary>
        /// Counts the number of tags with the specified name using a specific string comparison.
        /// </summary>
        /// <param name="tags">The collection of tags.</param>
        /// <param name="tagName">The name of the tag to count.</param>
        /// <param name="comparisonType">The string comparison type to use.</param>
        /// <returns>The number of tags matching the name under the given comparison.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tags or tagName is null.</exception>
        public static int CountTagsNamed(this IEnumerable<Tag> tags, string tagName, StringComparison comparisonType)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (tagName == null) throw new ArgumentNullException(nameof(tagName));

            return tags.Count(tag => tag.Name.Equals(tagName, comparisonType));
        }

        /// <summary>
        /// Counts the number of tags whose names contain the specified substring.
        /// </summary>
        /// <param name="tags">The collection of tags.</param>
        /// <param name="substring">The substring to search for within tag names.</param>
        /// <param name="comparisonType">The string comparison type to use (defaults to OrdinalIgnoreCase).</param>
        /// <returns>The number of tags whose names contain the substring.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tags or substring is null.</exception>
        public static int CountTagsContaining(this IEnumerable<Tag> tags, string substring, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            if (substring == null) throw new ArgumentNullException(nameof(substring));
            if (substring == string.Empty) return tags.Count(); // Count all if substring is empty

#if NETCOREAPP || NET5_0_OR_GREATER
            return tags.Count(tag => tag.Name.Contains(substring, comparisonType));
#else
                return tags.Count(tag => tag.Name.IndexOf(substring, comparisonType) >= 0);
#endif
        }


        // --- Utility Methods ---

        /// <summary>
        /// Gets a collection of all unique tag names from the sequence.
        /// </summary>
        /// <param name="tags">The collection of tags.</param>
        /// <param name="comparisonType">The string comparison to use for determining uniqueness (defaults to OrdinalIgnoreCase).</param>
        /// <returns>An IEnumerable string containing the distinct tag names.</returns>
        /// <exception cref="ArgumentNullException">Thrown if tags is null.</exception>
        public static IEnumerable<string> GetDistinctTagNames(this IEnumerable<Tag> tags, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            if (tags == null) throw new ArgumentNullException(nameof(tags));

            return tags.Select(tag => tag.Name)
                       .Distinct(GetStringComparer(comparisonType));
        }

        // --- Helper for String Comparison ---
        private static IEqualityComparer<string> GetStringComparer(StringComparison comparisonType)
        {
            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return StringComparer.CurrentCulture;
                case StringComparison.CurrentCultureIgnoreCase:
                    return StringComparer.CurrentCultureIgnoreCase;
                case StringComparison.InvariantCulture:
                    return StringComparer.InvariantCulture;
                case StringComparison.InvariantCultureIgnoreCase:
                    return StringComparer.InvariantCultureIgnoreCase;
                case StringComparison.Ordinal:
                    return StringComparer.Ordinal;
                case StringComparison.OrdinalIgnoreCase:
                    return StringComparer.OrdinalIgnoreCase;
                default:
                    throw new ArgumentOutOfRangeException(nameof(comparisonType), "Unsupported string comparison type.");
            }
        }
    }
}