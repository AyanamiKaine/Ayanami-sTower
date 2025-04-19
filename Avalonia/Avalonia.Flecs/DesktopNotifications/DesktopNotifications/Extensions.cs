using System.Collections.Generic;

namespace DesktopNotifications
{
    /// <summary>
    /// Provides extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Tries to get the key associated with the specified value in the dictionary.
        /// </summary>
        /// <typeparam name="K">The type of the keys in the dictionary.</typeparam>
        /// <typeparam name="V">The type of the values in the dictionary.</typeparam>
        /// <param name="dict">The dictionary to search.</param>
        /// <param name="value">The value to locate in the dictionary.</param>
        /// <param name="key">When this method returns, contains the key associated with the specified value, if the value is found; otherwise, the default value for the type of the key parameter.</param>
        /// <returns><c>true</c> if the dictionary contains an element with the specified value; otherwise, <c>false</c>.</returns>
        public static bool TryGetKey<K, V>(this IDictionary<K, V> dict, V value, out K key)
        {
            foreach (var entry in dict)
            {
                if (entry.Value?.Equals(value) == true)
                {
                    key = entry.Key;
                    return true;
                }
            }

            key = default!;

            return false;
        }
    }
}